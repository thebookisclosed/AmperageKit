using System;
using System.Runtime.InteropServices;
using VmDetection.NativeStructs;

namespace VmDetection
{
    public static class VmHelpers
    {
        const int SystemHypervisorDetailInformation = 159;

        public static bool IsVM()
        {
            var info = new SYSTEM_HYPERVISOR_DETAIL_INFORMATION();
            var retCode = NativeMethods.NtQuerySystemInformation(SystemHypervisorDetailInformation, ref info, Marshal.SizeOf(info), IntPtr.Zero);
            if (retCode != 0)
                return false;
            var hvId = info.HvVendorAndMaxFunction.DataUInt32[1] | ((ulong)info.HvVendorAndMaxFunction.DataUInt32[2] << 32);
            // QCOM
            if (hvId != 0 && hvId != 0x4D4F4351 && (info.HvFeatures.DataUInt64[0] & 0x100000000000) == 0)
                return true;
            return false;
        }
    }
}
