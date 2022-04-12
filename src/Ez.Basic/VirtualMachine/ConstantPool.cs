using System.Collections.Generic;

namespace Ez.Basic.VirtualMachine
{
    internal class ConstantPool
    {
        private readonly GC m_gc;
        private List<Value> m_values;

        public ConstantPool(GC gc)
        {
            m_gc = gc;
            m_values = new List<Value>();
            
        }

        public int AddConstant(in Value value)
        {
            var index = m_values.IndexOf(value);
            if(index == -1)
            {
                m_values.Add(value);
                index = m_values.Count - 1;
            }
            return index;
        }

        public int AddStringConstant(string value)
        {
            var index = m_values.FindIndex(c => c.IsObject && m_gc.GetObject(c.ObjectReference).Equals(value));
            if(index == -1)
            {
                var reference = m_gc.AddObject(value, true);
                m_values.Add(reference);
                index = m_values.Count - 1;
            }
            return index;
        }

        public Value GetConstant(in int index)
        {
            return m_values[index];
        }
    }
}
