using System;
using System.Runtime.InteropServices;
using VmDetection.NativeStructs;

namespace VmDetection
{
    internal class NativeMethods
    {
        [DllImport("ntdll.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int NtQuerySystemInformation(
            int SystemInformationClass,
            ref SYSTEM_HYPERVISOR_DETAIL_INFORMATION SystemInformation,
            int SystemInformationLength,
            IntPtr ReturnLength);
    }
}
