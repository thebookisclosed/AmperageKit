using ProcessCreationExt.NativeStructs;
using System;
using System.Runtime.InteropServices;

namespace ProcessCreationExt
{
    internal class NativeMethods
    {
        const string kernel32 = "kernel32.dll";
        [DllImport(kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport(kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int processId);

        const string advapi32 = "advapi32.dll";
        [DllImport(advapi32, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport(advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            bool DisableAllPrivileges,
            [MarshalAs(UnmanagedType.Struct)] ref TOKEN_PRIVILEGES NewState,
            uint BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport(advapi32, SetLastError = true)]
        internal static extern bool DuplicateTokenEx(
            IntPtr ExistingTokenHandle,
            uint dwDesiredAccess,
            IntPtr lpThreadAttributes,
            int TokenType,
            int ImpersonationLevel,
            out IntPtr DuplicateTokenHandle);

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CreateProcessWithTokenW(
            IntPtr hToken,
            int dwLogonFlags,
            string lpApplicationName,
            string lpCommandLine,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        const string userenv = "userenv.dll";
        [DllImport(userenv, SetLastError = true)]
        internal static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport(userenv, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
    }
}
