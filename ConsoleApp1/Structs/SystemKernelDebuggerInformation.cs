using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SystemKernelDebuggerInformation
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool KernelDebuggerEnabled;      // UChar
        [MarshalAs(UnmanagedType.U1)]
        public bool KernelDebuggerNotPresent;   // UChar
    }
}
