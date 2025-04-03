using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CurDir
    {
        public UnicodeString DosPath;
        public nint Handle;
    }
}
