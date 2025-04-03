using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Structs
{

    // Can be found with dt combase!_OBJECT_TYPE_INFORMATION
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectTypeInformation
    {
        public UnicodeString TypeName;              // _UNICODE_STRING
        public uint TotalNumberOfObjects;           // Uint4B
        public uint TotalNumberOfHandles;           // Uint4B
        public uint TotalPagedPoolUsage;            // Uint4B
        public uint TotalNonPagedPoolUsage;         // Uint4B
        public uint TotalNamePoolUsage;             // Uint4B
        public uint TotalHandleTableUsage;          // Uint4B
        public uint HighWaterNumberOfObjects;       // Uint4B
        public uint HighWaterNumberOfHandles;       // Uint4B
        public uint HighWaterPagedPoolUsage;        // Uint4B
        public uint HighWaterNonPagedPoolUsagege;   // Uint4B
        public uint HighWaterNamePoolUsage;         // Uint4B
        public uint HighWaterHandleTableUsagee;     // Uint4B
        public uint InvalidAttributes;              // Uint4B
        public GenericMapping GenericMapping;       // _GENERIC_MAPPING
        public uint ValidAccessMask;                // Uint4B
        public byte SecurityRequired;               // UChar
        public uint MaintainHandleCount;            // UChar
        public uint TypeIndex;                      // UChar
        public uint ReservedByte;                   // Char
        public uint PoolType;                       // Uint4B
        public uint DefaultPagedPoolCharge;         // Uint4B
        public uint DefaultNonPagedPoolCharge;      // Uint4B
    }
}
