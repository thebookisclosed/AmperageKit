using Amperage.Extensions;
using Microsoft.Win32;
using ProcessCreationExt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Xml.Linq;
using VmDetection;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Amperage
{
    internal class Program
    {
        const string m_workloadsPath = "WorkloadComponents";
        const string m_detoursPath = "Detours";
        static string[] m_requiredWorkloads = new[] { "ContentExtraction", "ImageSearch", "Analysis" };
        static HashSet<string> m_workloadMsixList;
        static Dictionary<string, string> m_workloadProductDictionary;
        static bool m_debugPrivilegeEnabled = false;
        static bool m_npuDetectPatchRequired = false;
        static PackageManager m_pkgMgr;
        static PackageManager PkgMgrInstance {
            get
            {
                if (m_pkgMgr == null)
                {
                    m_pkgMgr = new PackageManager();
                }
                return m_pkgMgr;
            }
        }

        const string _notSupportedErr = "Your build doesn't support Recall, reason: {0}.";
        static void Main(string[] args)
        {
            Console.WriteLine("Amperage - Recall setup tool for unsupported hardware\n(c) 2024 @thebookisclosed\n");
            switch (args.Length)
            {
                case 0:
                    {
                        DisplayHelp();
                        break;
                    }
                case 1:
                    {
                        switch (args[0].ToLowerInvariant())
                        {
                            case "/?":
                            case "/help":
                                DisplayHelp();
                                break;
                            case "/install":
                                DoAmperage();
                                break;
                            case "/uninstall":
                                ConfigureHwReqDetour(false);
                                break;
                            case "/support":
                                DisplaySupportMeans();
                                break;
                        }
                        break;
                    }
                case 2:
                    switch (args[0])
                    {
                        case AppModeConstants.AIXSysRemove:
                            HighPrivilegeHandlers.DoAIXSystemRemove(args[1]);
                            break;
                        case AppModeConstants.HwReqDetour:
                            HighPrivilegeHandlers.DoHwReqDetourTITrampoline(args[1]);
                            break;
                        case AppModeConstants.HwReqDetourTI:
                            HighPrivilegeHandlers.DoHwReqDetour((HwReqDetourSetupFlags)int.Parse(args[1]));
                            break;
                    }
                    break;
            }

            if (Debugger.IsAttached)
                Console.ReadKey();
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Supported commands:\n  /install\tSets up Recall and hardware requirements detour\n  /uninstall\tRemoves hardware requirements detour\n  /support\tShows ways you can support the developer");
        }

        static void DoAmperage()
        {
            if (!CheckPrerequisites())
                return;

            var aixPkg = GetAIXPackage();
            if (aixPkg == null)
            {
                Console.WriteLine(_notSupportedErr, "Couldn't find AIX package");
                return;
            }

            if (!IsAIXAppIdSupported(aixPkg))
            {
                Console.WriteLine(_notSupportedErr, "AIX package doesn't contain Recall");
                return;
            }

            if (!IsAIXAppIdPresentInSR(aixPkg))
            {
                Console.WriteLine("AIX package needs to be repaired, please wait...");
                FixAIXAppInSR(aixPkg);
            }
            
            if (!IsHwReqDetourPresent())
            {
                if (!ConfigureHwReqDetour(true))
                    return;
            }

            if (!InstallWorkloadProducts())
                return;

            Console.WriteLine(@"Recall has been successfully enabled. Please reboot and wait for AI components to finish 1st time initialization.
Initialization is usually done once no significant CPU activity is produced by WorkloadsSessionHost.exe instances.

If you wish to uninstall the Hardware Requirements Detour, run this program with the parameter '/uninstall'
");
            DisplaySupportMeans();
        }

        static bool CheckPrerequisites()
        {
            Console.WriteLine("Verifying detours...");
            foreach (var flav in AppModeConstants.DetourFlavors)
            {
                foreach (var payload in AppModeConstants.DetourFileNames)
                {
                    var dllPath = $"{m_detoursPath}\\{flav}\\{payload}";
                    if (!File.Exists(dllPath))
                    {
                        Console.WriteLine("Couldn't find required file {0}", dllPath);
                        return false;
                    }
                }
            }

            var arch = GetProcessorArchitecture();
            var npuGen = GetNpuGeneration();
            if (npuGen != 0)
                Console.WriteLine("NPU generation: {0}", npuGen);
            else
                Console.WriteLine("No NPU found");
            if (arch == "arm64" && npuGen != 0)
            {
                if (VmHelpers.IsVM())
                {
                    Console.WriteLine("Virtual machine patch will be applied");
                    m_npuDetectPatchRequired = true;
                }
                else
                    m_requiredWorkloads[0] += ".K95";
            }
            Console.WriteLine("Verifying workloads...");
            m_workloadMsixList = new HashSet<string>();
            m_workloadProductDictionary = new Dictionary<string, string>();
            for (int i = 0; i < m_requiredWorkloads.Length; i++)
            {
                var wklMetaPath = $@"{m_workloadsPath}\Windows.Workload.{m_requiredWorkloads[i]}.{arch}.xml";
                if (!File.Exists(wklMetaPath))
                {
                    Console.WriteLine("Couldn't find required file {0}", wklMetaPath);
                    return false;
                }

                var xd = XDocument.Load(wklMetaPath);
                var product = xd.Element("UUP")?.Element("Product");
                var pkgs = product?.Element("Packages")?.Elements("Package");
                if (product == null || pkgs == null)
                {
                    Console.WriteLine("{0} contains invalid metadata", Path.GetFileName(wklMetaPath));
                    return false;
                }
                m_workloadProductDictionary[product.Attribute("Id").Value] = product.Attribute("Version").Value;
                foreach (var pkg in pkgs)
                {
                    var fullName = pkg.Attribute("PackageFullName").Value + ".msix";
                    var expectedPath = $"{m_workloadsPath}\\{fullName}";
                    if (!File.Exists(expectedPath))
                    {
                        Console.WriteLine("Couldn't find {0} which is required to install {1}, make sure it exists in the {2} folder",
                            fullName, Path.GetFileNameWithoutExtension(wklMetaPath), m_workloadsPath);
                        return false;
                    }
                    m_workloadMsixList.Add(expectedPath);
                }
            }
            return true;
        }

        static Package GetAIXPackage()
        {
            try { return PkgMgrInstance.FindPackages("MicrosoftWindows.Client.AIX_cw5n1h2txyewy").Single(); } catch { return null; }
        }

        static bool IsAIXAppIdSupported(Package aixPackage)
        {
            var manifestAppEntries = XDocument.Load(Path.Combine(aixPackage.InstalledLocation.Path, "AppxManifest.xml"))
                .Element("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Package")
                .Element("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Applications")
                .Elements("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Application");
            foreach (var entry in manifestAppEntries)
            {
                if (entry.Attribute("Id").Value == "AIXApp")
                    return true;
            }
            return false;
        }

        static unsafe bool IsAIXAppIdPresentInSR(Package aixPackage)
        {
            var fullName = aixPackage.Id.FullName;
            NativeMethods.OpenPackageInfoByFullName(fullName, 0, out IntPtr pkgRef);
            var bufLen = 0u;
            NativeMethods.GetPackageApplicationIds(pkgRef, ref bufLen, null, out uint appIdCount);
            var buf = new byte[bufLen];
            var isPresentInSR = false;
            fixed (byte* ptr = buf)
            {
                NativeMethods.GetPackageApplicationIds(pkgRef, ref bufLen, ptr, out appIdCount);
                for (var i = 0; i < appIdCount; i++)
                {
                    var appId = Marshal.PtrToStringUni(*(IntPtr*)(ptr + (i * sizeof(IntPtr))));
                    if (appId == "MicrosoftWindows.Client.AIX_cw5n1h2txyewy!AIXApp")
                    {
                        isPresentInSR = true;
                        break;
                    }
                }
            }
            NativeMethods.ClosePackageInfo(pkgRef);
            return isPresentInSR;
        }

        static bool FixAIXAppInSR(Package aixPackage)
        {
            Console.WriteLine("Flagging AIX package for removal");
            var fullName = aixPackage.Id.FullName;
            var aixUsers = PkgMgrInstance.FindUsers(fullName);
            foreach (var userInfo in aixUsers)
            {
                using (var sidKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\EndOfLife\{userInfo.UserSecurityId}\{fullName}"))
                { }
            }

            Console.WriteLine("Removing AIX package from all standard users");
            try
            {
                PkgMgrInstance.RemovePackageAsync(fullName, RemovalOptions.RemoveForAllUsers).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Removal failed\n" + ex.ToString());
                return false;
            }

            EnableDebugPrivilegeHelper();
            var winlogonPid = Process.GetProcessesByName("winlogon")[0].Id;
            var sysProcStarted = Utilities.StartAsPID(winlogonPid, $"\"{System.Reflection.Assembly.GetEntryAssembly().Location}\" {AppModeConstants.AIXSysRemove} {fullName}");
            if (!sysProcStarted)
            {
                Console.WriteLine("Failed to start AIX System Removal server");
                return false;
            }
            int srvRetCode;
            using (var pipeClient = new NamedPipeClientStream("Amperage" + AppModeConstants.AIXSysRemove))
            {
                try { pipeClient.Connect(2000); } catch { return false; }
                srvRetCode = pipeClient.ProcessServerData();
                if (srvRetCode != 0)
                    Console.WriteLine("AIX System Removal failed with return code 0x{0:X8}", srvRetCode);
            }

            //Console.ReadKey();

            Console.WriteLine("Cleaning up removal flags");
            foreach (var userInfo in aixUsers)
            {
                Registry.LocalMachine.DeleteSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\EndOfLife\{userInfo.UserSecurityId}\{fullName}", false);
                Registry.LocalMachine.DeleteSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\EndOfLife\{userInfo.UserSecurityId}\{fullName}", false);
            }

            if (srvRetCode != 0)
                return false;

            Console.WriteLine("Re-registering AIX package");
            try
            {
                PkgMgrInstance.RegisterPackageAsync(new Uri(@"C:\Windows\SystemApps\MicrosoftWindows.Client.AIX_cw5n1h2txyewy\appxmanifest.xml"), null, DeploymentOptions.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Registration failed\n" + ex.ToString());
                return false;
            }
            return true;
        }

        static bool ConfigureHwReqDetour(bool install)
        {
            Console.WriteLine("Starting TrustedInstaller service...");
            try
            {
                var scm = new ServiceController("TrustedInstaller");
                if (scm.Status != ServiceControllerStatus.Running)
                {
                    scm.Start();
                    scm.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start service");
                Console.WriteLine(ex.ToString());
                return false;
            }

            if (install)
            {
                Console.WriteLine("Setting up Hardware Requirements Detour directory");
                var dirSec = new DirectorySecurity(Path.GetDirectoryName(AppModeConstants.HwReqDetourStorage), AccessControlSections.All);
                dirSec.SetOwner(WindowsIdentity.GetCurrent().User);
                // Everyone, ALL APPLICATION PACKAGES
                var sids = new[] { "S-1-1-0", "S-1-15-2-1" };
                foreach (var sid in sids)
                {
                    dirSec.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                }

                try
                {
                    Directory.CreateDirectory(AppModeConstants.HwReqDetourStorage, dirSec);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to create persistent detours directory");
                    Console.WriteLine(ex.ToString());
                    return false;
                }

                try
                {
                    foreach (var flav in AppModeConstants.DetourFlavors)
                    {
                        var targetDir = Path.Combine(AppModeConstants.HwReqDetourStorage, flav);
                        Directory.CreateDirectory(targetDir);
                        foreach (var payload in AppModeConstants.DetourFileNames)
                        {
                            var dllPath = $"{m_detoursPath}\\{flav}\\{payload}";
                            var targetPath = Path.Combine(targetDir, payload);
                            if (!File.Exists(targetPath))
                                File.Copy(dllPath, targetPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to copy detours to persistent directory");
                    Console.WriteLine(ex.ToString());
                    Directory.Delete(AppModeConstants.HwReqDetourStorage, true);
                    return false;
                }
            }

            Console.WriteLine($"{(install ? "I" : "Uni")}nstalling Hardware Requirements Detour");
            EnableDebugPrivilegeHelper();
            var winlogonPid = Process.GetProcessesByName("winlogon")[0].Id;
            var detSetupFlag = install ? HwReqDetourSetupFlags.Install : HwReqDetourSetupFlags.Uninstall;
            if (m_npuDetectPatchRequired)
                detSetupFlag |= HwReqDetourSetupFlags.PatchNpuDetect;
            var sysProcStarted = Utilities.StartAsPID(winlogonPid, $"\"{System.Reflection.Assembly.GetEntryAssembly().Location}\" {AppModeConstants.HwReqDetour} {(int)detSetupFlag}");
            if (!sysProcStarted)
            {
                Console.WriteLine("Failed to start Hardware Requirements Detour Setup server");
                return false;
            }
            int srvRetCode;
            using (var pipeClient = new NamedPipeClientStream("Amperage" + AppModeConstants.HwReqDetour))
            {
                try { pipeClient.Connect(4000); } catch { return false; }
                srvRetCode = pipeClient.ProcessServerData();
                if (srvRetCode != 0)
                {
                    Console.WriteLine("Hardware Requirements Detour Setup failed with return code 0x{0:X8}", srvRetCode);
                    return false;
                }
            }

            if (!install)
            {
                Console.WriteLine("Hardware Requirements Detour has been uninstalled.");
                try
                {
                    var cleanupList = new List<string>();
                    foreach (var flav in AppModeConstants.DetourFlavors)
                    {
                        var targetDir = Path.Combine(AppModeConstants.HwReqDetourStorage, flav);
                        foreach (var payload in AppModeConstants.DetourFileNames)
                        {
                            var dllPath = $"{m_detoursPath}\\{flav}\\{payload}";
                            var targetPath = Path.Combine(targetDir, payload);
                            cleanupList.Add(targetPath);
                        }
                        cleanupList.Add(targetDir);
                    }
                    cleanupList.Add(AppModeConstants.HwReqDetourStorage);
                    PendingFileRenameOperations.FlagFilesForDeletion(cleanupList.ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to mark detours for deletion");
                    Console.WriteLine(ex.ToString());
                    Directory.Delete(AppModeConstants.HwReqDetourStorage, true);
                    return false;
                }
            }

            return true;
        }

        static bool InstallWorkloadProducts()
        {
            var currentDir = Directory.GetCurrentDirectory();
            foreach (var msix in m_workloadMsixList)
            {
                Console.WriteLine("Installing {0}", Path.GetFileNameWithoutExtension(msix));
                try
                {
                    PkgMgrInstance.AddPackageAsync(new Uri(Path.Combine(currentDir, msix)), null, DeploymentOptions.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Workload installation failed\n" + ex.ToString());
                    return false;
                }
            }
            try
            {
                using (var dynamicInstalled = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Update\TargetingInfo\DynamicInstalled", true))
                {
                    foreach (var prodKvp in m_workloadProductDictionary)
                    {
                        using (var prodRKey = dynamicInstalled.CreateSubKey(prodKvp.Key, true))
                        {
                            prodRKey.SetValue("Version", prodKvp.Value, RegistryValueKind.String);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to register workloads with Windows Update\n" + ex.ToString());
                return false;
            }
            return true;
        }

        static bool IsHwReqDetourPresent()
        {
            using (var rk = Registry.LocalMachine.OpenSubKey("SOFTWARE" + AppModeConstants.HwReqKeyPath, false))
            using (var rk32 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node" + AppModeConstants.HwReqKeyPath, false))
            {
                if (rk.GetValue("DllPathOriginal", null, RegistryValueOptions.DoNotExpandEnvironmentNames) != null ||
                    rk32.GetValue("DllPathOriginal", null, RegistryValueOptions.DoNotExpandEnvironmentNames) != null)
                    return true;
            }
            return false;
        }

        static int GetNpuGeneration()
        {
            if (!File.Exists(AppModeConstants.NpuDetectPath))
                return 0;
            return NativeMethods.npudetect_detect_npugeneration();
        }

        static void EnableDebugPrivilegeHelper()
        {
            if (!m_debugPrivilegeEnabled)
            {
                m_debugPrivilegeEnabled = true;
                Utilities.EnableDebug();
            }
        }

        static string GetProcessorArchitecture()
        {
            return Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").ToLowerInvariant();
        }

        static void DisplaySupportMeans()
        {
            Console.WriteLine("If you'd like to support me, you can do so by:\n - becoming a sponsor at github.com/thebookisclosed\n - visiting paypal.me/tfwboredom\n\nThank you!");
        }
    }
}
