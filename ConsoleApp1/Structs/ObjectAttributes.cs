using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectAttributes
    {
        public uint Length;                     // ULONG
        public nint RootDirectory;              // HANDLE
        public nint ObjectName;                 // PUNICODE_STRING
        public uint Attributes;                 // ULONG
        public nint SecurityDescriptor;         // PVOID
        public nint SecurityQualityOfService;   // PVOID
    }
}
