using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Peb
    {
        [FieldOffset(0x02)] public byte BeingDebugged;  // BYTE
        [FieldOffset(0x20)] private nint ProcessParameters;
        [FieldOffset(0xbc)] public uint NtGlobalFlag;   // UINT32
        [FieldOffset(0x7c4)] public uint NtGlobalFlag2; // UINT32

        public RtlUserProcessParameters GetProcessParameters()
        {
            return Marshal.PtrToStructure<RtlUserProcessParameters>(ProcessParameters);
        }

    }
}
