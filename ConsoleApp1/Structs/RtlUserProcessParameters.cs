using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RtlUserProcessParameters
    {
        public uint MaximumLength;
        public uint Length;
        public uint Flags;
        public uint DebugFlags;
        public nint ConsoleHandle;
        public uint ConsoleFlags;
        public nint StandardInput;
        public nint StandardOutput;
        public nint StandardError;
        public CurDir CurrentDirectory;
        public UnicodeString DllPath;
        public UnicodeString ImagePathName;
        public UnicodeString CommandLine;
        public nint Environment;
        public uint StartingX;
        public uint StartingY;
        public uint CountX;
        public uint CountY;
        public uint CountCharsX;
        public uint CountCharsY;
        public uint FillAttribute;
        public uint WindowFlags;
        public uint ShowWindowFlags;
        public UnicodeString WindowTitle;
        public UnicodeString DesktopInfo;
        public UnicodeString ShellInfo;
        public UnicodeString RuntimeData;
    }
}
