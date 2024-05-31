namespace Amperage
{
    static class AppModeConstants
    {
        internal const string AIXSysRemove = "AIXSysRemove";
        internal const string HwReqDetour = "HwReqDetour";
        internal const string HwReqDetourTI = "HwReqDetourTI";
        internal const string HwReqDetourStorage = @"C:\ProgramData\Amperage";
        internal const string HwReqKeyPath = @"\Microsoft\WindowsRuntime\ActivatableClassId\WindowsUdk.System.Profile.HardwareRequirements";
        internal const string NpuDetectPath = @"C:\Windows\SystemApps\Microsoft.WindowsAppRuntime.CBS_8wekyb3d8bbwe\NpuDetect\NPUDetect.dll";

        internal static string[] DetourFlavors = new[] { "arm64", "x86" };
        internal static readonly string[] DetourFileNames = new[] { "WindowsUdk.System.Profile.dll", "vcruntime140_app.dll" };
    }
}
