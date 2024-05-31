using ProcessCreationExt.NativeStructs;
using System;
using System.Runtime.InteropServices;

namespace ProcessCreationExt
{
    public static class Utilities
    {
        public static void EnableDebug()
        {
            // 0xF01FF = TOKEN_ALL_ACCESS
            if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), 0xF01FF, out IntPtr hToken))
                return;
            if (!NativeMethods.LookupPrivilegeValue("", "SeDebugPrivilege", out LUID luid))
                return;
            TOKEN_PRIVILEGES debugPriv = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attrs = 2 // SE_PRIVILEGE_ENABLED
            };
            if (!NativeMethods.AdjustTokenPrivileges(hToken, false, ref debugPriv, 0, IntPtr.Zero, IntPtr.Zero))
                return;
            NativeMethods.CloseHandle(hToken);
        }

        public static bool StartAsPID(int pid, string commandLine)
        {
            IntPtr hToken, hDupToken, pEnv;
            hToken = hDupToken = pEnv = IntPtr.Zero;
            var procInfo = new PROCESS_INFORMATION();

            try
            {
                // 0x001FFFFF = PROCESS_ALL_ACCESS
                var hProc = NativeMethods.OpenProcess(0x001FFFFF, false, pid);
                if (hProc == IntPtr.Zero)
                    return false;

                // 0x10 = TOKEN_DUPLICATE | TOKEN_QUERY
                if (!NativeMethods.OpenProcessToken(hProc, 0xA, out hToken))
                    return false;

                // 0xF01FF = TOKEN_ALL_ACCESS
                // 1 = TokenPrimary
                // 1 = SecurityIdentification
                if (!NativeMethods.DuplicateTokenEx(hToken, 0xF01FF, IntPtr.Zero, 1, 1, out hDupToken))
                    return false;

                if (!NativeMethods.CreateEnvironmentBlock(out pEnv, hToken, false))
                    return false;

                uint dwCreationFlags = 0x8000400; // CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW
                var startInfo = new STARTUPINFO();
                startInfo.cb = Marshal.SizeOf(startInfo);

                if (!NativeMethods.CreateProcessWithTokenW(hDupToken,
                    1, // WithProfile
                    null,
                    commandLine,
                    dwCreationFlags,
                    pEnv,
                    System.IO.Directory.GetCurrentDirectory(),
                    ref startInfo,
                    out procInfo))
                    return false;

                return true;
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                    NativeMethods.CloseHandle(hToken);
                if (hDupToken != IntPtr.Zero)
                    NativeMethods.CloseHandle(hDupToken);
                if (pEnv != IntPtr.Zero)
                    NativeMethods.DestroyEnvironmentBlock(pEnv);
                NativeMethods.CloseHandle(procInfo.hThread);
                NativeMethods.CloseHandle(procInfo.hProcess);
            }
        }
    }
}
