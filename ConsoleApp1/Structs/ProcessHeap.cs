using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ProcessHeap
    {
        [FieldOffset(0x70)] public uint Flags;
        [FieldOffset(0x74)] public uint ForceFlags;
    }
}
