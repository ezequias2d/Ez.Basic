using Ez.Basic.VirtualMachine.Objects;
using System.Collections.Generic;

namespace Ez.Basic.VirtualMachine
{
    internal class ConstantPool
    {
        private List<double> m_numeric_constants;
        private List<BasicString> m_string_constants;

        public ConstantPool()
        {
            m_numeric_constants = new List<double>();
            m_string_constants = new List<BasicString>();
        }

        public int AddNumericConstant(in double value)
        {
            var index = m_numeric_constants.IndexOf(value);
            if(index == -1)
            {
                m_numeric_constants.Add(value);
                index = m_numeric_constants.Count - 1;
            }
            return index;
        }

        public int AddStringConstant(string value)
        {
            var index = m_string_constants.FindIndex(s => s.Value.Equals(value));
            if(index == -1)
            {
                m_string_constants.Add(new BasicString(value));
                index = m_string_constants.Count - 1;
            }
            return index;
        }

        public double GetNumericConstant(in int index)
        {
            return m_numeric_constants[index];
        }

        public BasicString GetStringConstant(in int index)
        {
            return m_string_constants[index];
        }
    }
}
