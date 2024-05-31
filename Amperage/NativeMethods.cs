using System;
using System.Runtime.InteropServices;

namespace Amperage
{
    internal class NativeMethods
    {
        const string pkgExt = "ext-ms-win-kernel32-package-l1-1-1.dll";
        [DllImport(pkgExt, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int OpenPackageInfoByFullName([MarshalAs(UnmanagedType.LPWStr)] string packageFullName, uint reserved, out IntPtr packageInfoReference);

        [DllImport(pkgExt, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static unsafe extern int GetPackageApplicationIds(
            IntPtr packageInfoReference,
            ref uint bufferLength,
            byte* buffer,
            out uint count);

        [DllImport(pkgExt, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int ClosePackageInfo(IntPtr packageInfoReference);

        [DllImport(AppModeConstants.NpuDetectPath, ExactSpelling = true)]
        internal static extern int npudetect_detect_npugeneration();

        const string kernel32 = "kernel32.dll";
        [DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

        [DllImport(kernel32, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hLibModule);

        [DllImport(kernel32, SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        const string imagehlp = "imagehlp.dll";
        [DllImport(imagehlp, CharSet = CharSet.Ansi, ExactSpelling = true)]
        internal static extern uint MapFileAndCheckSumA([MarshalAs(UnmanagedType.LPStr)] string Filename, out uint HeaderSum, out uint CheckSum);
    }
}
