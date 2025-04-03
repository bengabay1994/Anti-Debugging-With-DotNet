using AntiDebugDotNet.Structs;
using System.Runtime.InteropServices;

namespace AntiDebugDotNet.Wrappers
{
    public class ObjectAllInformationWrapper
    {
        private nint _baseAddr;

        private uint _numberOfObjectsTypes;

        public readonly IReadOnlyCollection<ObjectTypeInformation> ObjectTypeInformation;

        // Getter for the base address
        public nint BaseAddress => _baseAddr;

        // Getter for the number of object types
        public uint NumberOfObjectTypes => _numberOfObjectsTypes;


        public ObjectAllInformationWrapper(nint baseAddr)
        {
            _baseAddr = baseAddr;
            _numberOfObjectsTypes = (uint)Marshal.ReadInt32(baseAddr);
            ObjectTypeInformation = _initCollection().AsReadOnly();
        }

        private IList<ObjectTypeInformation> _initCollection()
        {
            IList<ObjectTypeInformation> objectsList = new List<ObjectTypeInformation>();
            int alignment = nint.Size - 1;
            nint objectAddress = _baseAddr + nint.Size;
            for (int i = 0; i < _numberOfObjectsTypes; ++i)
            {
                // Adding nint.size instead of sizeof(uint) due to alignment requirements in x64 systems.
                ObjectTypeInformation objType = Marshal.PtrToStructure<ObjectTypeInformation>(objectAddress);
                objectsList.Add(objType);
                objectAddress = objType.TypeName.Buffer + ((objType.TypeName.MaximumLength + alignment) & ~alignment);
            }
            return objectsList;
        }

    }
}
