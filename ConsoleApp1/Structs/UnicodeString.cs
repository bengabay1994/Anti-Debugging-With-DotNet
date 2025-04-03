using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UnicodeString : IDisposable
    {
        public ushort Length;         // USHORT
        public ushort MaximumLength;  // USHORT
        public nint Buffer;           // LPCWSTR

        public UnicodeString(string str)
        {
            Length = (ushort)(str.Length * 2);
            MaximumLength = (ushort)(str.Length * 2);
            Buffer = Marshal.StringToHGlobalUni(str);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Buffer);
            Buffer = nint.Zero;
        }

        public override string ToString()
        {
            return Marshal.PtrToStringUni(Buffer) ?? string.Empty;
        }
    }
}
