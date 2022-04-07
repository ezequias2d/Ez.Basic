using Ez.Basic.VirtualMachine.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    public class Chunk
    {
        private DynamicArray m_code;
        private ConstantPool m_constants;
        private List<IAttribute> m_attributes;

        public Chunk(GC gc, bool debug = true)
        {
            GC = gc;
            m_code = new DynamicArray();
            m_constants = new ConstantPool(gc);
            m_attributes = new List<IAttribute>();
            
            LineNumberTable = new LineNumberTableAttribute();
            m_attributes.Add(LineNumberTable);
            Debug = debug;
        }

        public GC GC { get; }

        public bool Debug { get; }

        public LineNumberTableAttribute LineNumberTable { get; }
        
        public int Count => m_code.Count;
        public T Read<T>(int location) where T : unmanaged
        {
            //if(location >= Count)
            //    return m_code.Get<T>(Count - 1);
            return m_code.Get<T>(location);
        }

        public T Peek<T>() where T : unmanaged
        {
            return m_code.Peek<T>();
        }

        public int Write<T>(in T value, int line = -1) where T : unmanaged
        {
            var pc = m_code.Append(value);
            LineNumberTable.AddLine(pc, line);
            return pc;
        }

        public int WriteVarint(int value)
        {
            return m_code.WriteVarint(value);
        }

        public int ReadVariant(int location, out int value)
        {
            return m_code.ReadVarint(location, out value);
        }

        public int AddConstant(in Value value)
        {
            return m_constants.AddConstant(value);
        }

        public int AddStringConstant(in string value)
        {
            return m_constants.AddStringConstant(value);
        }

        public Value GetConstant(in int index)
        {
            return m_constants.GetConstant(index);
        }
    }
}
