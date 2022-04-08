using System;
using System.Linq;

namespace Ez.Basic.Compiler
{
    internal class SymbolTable
    {
        private Entry[] m_table;
        private int m_count;

        public SymbolTable(SymbolTable parent, int depth)
        {
            Parent = parent;
            Depth = depth;
        }

        public SymbolTable Parent { get; }
        public int Depth { get; }

        public bool Insert(string symbolName, SymbolType type, int variableDepth = 0)
        {
            if (Lookup(symbolName))
                return false;

            var index = m_count;
            m_count = index + 1;
            EnsureSize(m_count);

            m_table[index] = new Entry
            {
                SymbolName = symbolName,
                Type = type,
                VariableDepth = variableDepth,
            };
            return true;
        }

        public bool Lookup(string symbolName)
        {
            if (m_table == null)
                return false;
            return m_table.Any((e) => e.SymbolName == symbolName);
        }

        public bool Lookup(string symbolName, out int variableDepth)
        {
            variableDepth = 0;
            if (m_table == null)
                return false;

            try
            {
                var entry = m_table.First((e) => e.SymbolName == symbolName);
                variableDepth = entry.VariableDepth;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void EnsureSize(int size)
        {
            int tableSize = 0;
            if (m_table != null && (tableSize = m_table.Length) >= size)
                return;

            var newSize = Math.Max(tableSize * 2, 16);
            Array.Resize(ref m_table, newSize);
        }

        private struct Entry
        {
            public string SymbolName;
            public SymbolType Type;
            public int VariableDepth;
        }
    }
}
