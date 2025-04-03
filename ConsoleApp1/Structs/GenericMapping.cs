using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GenericMapping
    {
        public uint GenericRead;
        public uint GenericWrite;
        public uint GenericExecute;
        public uint GenericAll;
    }
}
