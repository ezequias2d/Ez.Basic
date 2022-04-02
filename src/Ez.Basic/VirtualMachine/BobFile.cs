using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    internal struct BobFile
    {
        public uint Magic;
        public ushort MinorVersion;
        public ushort MajorVersion;
        public ConstantPoolEntry[] ConstantPool;
        public ushort ThisClass;
        public ushort BaseClass;
        

        public struct ConstantPoolEntry
        {
            public ConstantType Tag;
            public uint Data;
        }

        public struct FieldInfo
        {
            public int NameIndex;
            public int DescriptorIndex;
            public AttributeInfo[] Attributes;
        }

        public struct AttributeInfo
        {

        }
    }
}
