using Amperage.Extensions;
using Microsoft.Win32;
using ProcessCreationExt;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Windows.Management.Deployment;

namespace Amperage
{
    static class HighPrivilegeHandlers
    {
        internal static void DoAIXSystemRemove(string aixFullName)
        {
            using (var pipeServer = new NamedPipeServerStream("Amperage" + AppModeConstants.AIXSysRemove, PipeDirection.InOut))
            {
                if (!pipeServer.WaitForClientFinite())
                    return;
                var pkgMgr = new PackageManager();
                pipeServer.WriteXPCString("Removing AIX package from SYSTEM");
                try
                {
                    pkgMgr.RemovePackageAsync(aixFullName).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    pipeServer.WriteXPCRetCode(ex.HResult);
                    pipeServer.WriteXPCString("Removal failed\n" + ex.ToString());
                    return;
                }
            }
        }

        internal static void DoHwReqDetourTITrampoline(string install)
        {
            var tiPid = Process.GetProcessesByName("TrustedInstaller")[0].Id;
            Utilities.StartAsPID(tiPid, $"\"{System.Reflection.Assembly.GetEntryAssembly().Location}\" {AppModeConstants.HwReqDetourTI} {install}");
        }

        internal static void DoHwReqDetour(HwReqDetourSetupFlags flags)
        {
            using (var pipeServer = new NamedPipeServerStream("Amperage" + AppModeConstants.HwReqDetour, PipeDirection.InOut))
            {
                if (!pipeServer.WaitForClientFinite())
                    return;
                if (flags.HasFlag(HwReqDetourSetupFlags.Install))
                {
                    try
                    {
                        if (flags.HasFlag(HwReqDetourSetupFlags.PatchNpuDetect))
                        {
                            var npuDir = Path.GetDirectoryName(AppModeConstants.NpuDetectPath);
                            if (!string.IsNullOrEmpty(npuDir))
                            {
                                Directory.SetCurrentDirectory(npuDir);
                            }
                            var npuLib = NativeMethods.LoadLibraryW(AppModeConstants.NpuDetectPath);
                            if (npuLib == IntPtr.Zero)
                            {
                                pipeServer.WriteXPCString("Failed to load NPUDetect library");
                                pipeServer.WriteXPCRetCode(Marshal.GetHRForLastWin32Error());
                                return;
                            }
                            var genProc = NativeMethods.GetProcAddress(npuLib, "npudetect_detect_npugeneration");
                            NativeMethods.FreeLibrary(npuLib);
                            var virtAddr = (long)((ulong)genProc.ToInt64() - (ulong)npuLib.ToInt64());

                            var backupPath = AppModeConstants.NpuDetectPath + ".bak";
                            if (!File.Exists(backupPath))
                            {
                                File.Move(AppModeConstants.NpuDetectPath, backupPath);
                                File.Copy(backupPath, AppModeConstants.NpuDetectPath);

                                uint retCode;
                                using (var fs = new FileStream(AppModeConstants.NpuDetectPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                                using (var br = new BinaryReader(fs, System.Text.Encoding.Default, true))
                                using (var bw = new BinaryWriter(fs, System.Text.Encoding.Default, true))
                                {
                                    var bpi = BasicPEInfo.Parse(br);
                                    var physAddr = bpi.VirtualToPhysicalAddress(virtAddr);
                                    fs.Position = physAddr;
                                    var originalInsn = br.ReadUInt64();
                                    fs.Position -= sizeof(ulong);
                                    bw.Write(0xD65F03C052800000);
                                    fs.Flush();
                                    retCode = NativeMethods.MapFileAndCheckSumA(AppModeConstants.NpuDetectPath, out uint _, out uint newSum);
                                    if (retCode != 0)
                                    {
                                        fs.Position = physAddr;
                                        bw.Write(originalInsn);
                                        fs.Flush();
                                    }
                                    else
                                    {
                                        fs.Position = bpi.ChecksumPosition;
                                        bw.Write(newSum);
                                    }
                                }

                                if (retCode != 0)
                                {
                                    pipeServer.WriteXPCString("Failed to recompute checksum");
                                    pipeServer.WriteXPCRetCode((int)retCode);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        pipeServer.WriteXPCString("Failed to patch NPUDetect");
                        pipeServer.WriteXPCString(ex.ToString());
                        pipeServer.WriteXPCRetCode(ex.HResult);
                        return;
                    }

                    try
                    {
                        using (var rk = Registry.LocalMachine.OpenSubKey("SOFTWARE" + AppModeConstants.HwReqKeyPath, true))
                        using (var rk32 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node" + AppModeConstants.HwReqKeyPath, true))
                        {
                            UpdateAndBackupDllPath(rk, Path.Combine(AppModeConstants.HwReqDetourStorage, AppModeConstants.DetourFlavors[0], AppModeConstants.DetourFileNames[0]));
                            UpdateAndBackupDllPath(rk32, Path.Combine(AppModeConstants.HwReqDetourStorage, AppModeConstants.DetourFlavors[1], AppModeConstants.DetourFileNames[0]));
                        }
                    }
                    catch (Exception ex)
                    {
                        pipeServer.WriteXPCString("Failed to configure detour in registry");
                        pipeServer.WriteXPCRetCode(ex.HResult);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        using (var rk = Registry.LocalMachine.OpenSubKey("SOFTWARE" + AppModeConstants.HwReqKeyPath, true))
                        using (var rk32 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node" + AppModeConstants.HwReqKeyPath, true))
                        {
                            RestoreOriginalDllPath(rk);
                            RestoreOriginalDllPath(rk32);
                        }
                    }
                    catch (Exception ex)
                    {
                        pipeServer.WriteXPCString("Failed to uninstall detour from registry");
                        pipeServer.WriteXPCRetCode(ex.HResult);
                        return;
                    }

                    try
                    {
                        var backupPath = AppModeConstants.NpuDetectPath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            var randName = AppModeConstants.NpuDetectPath + Path.GetRandomFileName();
                            File.Move(AppModeConstants.NpuDetectPath, randName);
                            File.Move(backupPath, AppModeConstants.NpuDetectPath);
                            PendingFileRenameOperations.FlagFilesForDeletion(new[] { randName });
                        }
                    }
                    catch (Exception ex)
                    {
                        pipeServer.WriteXPCString("Failed to revert NPUDetect patch");
                        pipeServer.WriteXPCRetCode(ex.HResult);
                        return;
                    }
                }
            }
        }

        static void UpdateAndBackupDllPath(RegistryKey rk, string newDllPath)
        {
            var ogPath = rk.GetValue("DllPath", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            rk.SetValue("DllPathOriginal", ogPath, RegistryValueKind.ExpandString);
            rk.SetValue("DllPath", newDllPath, RegistryValueKind.ExpandString);
        }

        static void RestoreOriginalDllPath(RegistryKey rk)
        {
            var ogPath = rk.GetValue("DllPathOriginal", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            rk.SetValue("DllPath", ogPath, RegistryValueKind.ExpandString);
            rk.DeleteValue("DllPathOriginal");
        }
    }

    [Flags]
    internal enum HwReqDetourSetupFlags
    {
        Uninstall = 0,
        Install = 1,
        PatchNpuDetect = 2
    }
}
