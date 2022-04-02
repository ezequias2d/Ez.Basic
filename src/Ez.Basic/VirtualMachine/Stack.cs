using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    internal class Stack : IEnumerable<Value>
    {
        private Value[] m_array;
        private int m_sp;

        public Stack(int stackSize = 4096)
        {
            m_array = new Value[stackSize];
            m_sp = 0;
        }

        public void Push(in Value value)
        {
            if (m_sp >= m_array.Length)
                throw new Exception("Stack overflow!");

            m_array[m_sp++] = value;
        }

        public Value Pop()
        {
            if (m_sp == 0)
                throw new Exception("Stack underflow(is empty)!");

            return m_array[--m_sp];
        }

        public void Reset()
        {
            m_sp = 0;
        }

        public IEnumerator<Value> GetEnumerator()
        {
           for(var i = 0; i < m_sp; i++)
            {
                yield return m_array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
