using System.Runtime.InteropServices;

namespace VmDetection.NativeStructs
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct HV_DETAILS
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U8)]
        internal ulong[] DataUInt64;
        [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
        internal uint[] DataUInt32;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_HYPERVISOR_DETAIL_INFORMATION
    {
        internal HV_DETAILS HvVendorAndMaxFunction;
        internal HV_DETAILS HypervisorInterface;
        internal HV_DETAILS HypervisorVersion;
        internal HV_DETAILS HvFeatures;
        internal HV_DETAILS HwFeatures;
        internal HV_DETAILS EnlightenmentInfo;
        internal HV_DETAILS ImplementationLimits;
    }
}
