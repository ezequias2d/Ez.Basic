using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    internal class ConstantPool
    {
        private List<Value> m_array;

        public ConstantPool()
        {
            m_array = new List<Value>();
        }

        public int AddConstant(in Value value)
        {
            m_array.Add(value);
            return m_array.Count - 1;
        }

        public Value GetConstant(in int index)
        {
            return m_array[index];
        }
    }
}
