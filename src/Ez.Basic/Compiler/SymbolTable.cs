using System;
using System.Linq;
using System.Collections.Generic;

namespace Ez.Basic.Compiler
{
    internal class SymbolTable
    {
        private Entry[] m_table;
        private int m_count;

        public bool Insert(string symbolName, SymbolType type)
        {
            if (Lookup(symbolName))
                return false;

            var index = m_count;
            m_count = index + 1;
            EnsureSize(m_count);

            m_table[index] = new Entry
            {
                Type = type,
            };
            return true;
        }

        public bool Lookup(string symbolName)
        {
            if (m_table == null)
                return false;
            return m_table.Any((e) => e.SymbolName == symbolName);
        }

        private void EnsureSize(int size)
        {
            if (m_table.Length >= size)
                return;

            var newSize = Math.Max(m_table.Length * 2, 16);
            Array.Resize(ref m_table, newSize);
        }

        private struct Entry
        {
            public string SymbolName;
            public SymbolType Type;
        }
    }
}
