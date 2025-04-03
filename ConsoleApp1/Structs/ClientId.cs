using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ClientId
    {
        public nint UniqueProcess; // PVOID
        public nint UniqueThread;  // PVOID
    }
}
