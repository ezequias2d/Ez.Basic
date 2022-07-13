using System;
using System.Collections.Generic;
using System.Linq;

namespace Ez.Basic
{
    public class SymbolTable
    {
        private SymbolEntry[] m_table;
        private int m_count;

        public SymbolTable(SymbolTable parent, SymbolTableType type, int depth)
        {
            Parent = parent;
            Depth = depth;
            Type = type;
        }

        public SymbolTable Parent { get; }
        public int Depth { get; }
        public int Count => m_count;
        public SymbolTableType Type { get; }

        public bool Insert(string symbolName, SymbolType type, int data = 0)
        {
            if (Lookup(symbolName))
                return false;

            var index = m_count;
            m_count = index + 1;
            EnsureSize(m_count);

            m_table[index] = new SymbolEntry
            {
                SymbolName = symbolName,
                Type = type,
                Data = data,
            };
            return true;
        }

        public bool Lookup(string symbolName, out SymbolEntry entry)
        {
            entry = default;
            if (m_table == null)
                if (Parent != null)
                    return Parent.Lookup(symbolName, out entry);
                else 
                    return false;

            try
            {
                entry = m_table.First((e) => e.SymbolName == symbolName);
                return true;
            }
            catch (InvalidOperationException)
            {
                if (Parent != null)
                    return Parent.Lookup(symbolName, out entry);
                return false;
            }
        }

        public bool Lookup(string symbolName)
        {
            if (m_table == null)
                return false;
            return m_table.Any((e) => e.SymbolName == symbolName);
        }

        public bool Lookup(string symbolName, out int data)
        {
            data = 0;
            if (m_table == null)
                if (Parent != null)
                    return Parent.Lookup(symbolName, out data);
                else 
                    return false;

            try
            {
                var entry = m_table.First((e) => e.SymbolName == symbolName);
                data = entry.Data;
                return true;
            }
            catch (InvalidOperationException)
            {
                if (Parent != null)
                    return Parent.Lookup(symbolName, out data);
                return false;
            }
        }

        public IEnumerable<SymbolEntry> AllEntries() 
        {
            for(var i = 0; i < m_count; i++)
                yield return m_table[i];
        }

        private void EnsureSize(int size)
        {
            int tableSize = 0;
            if (m_table != null && (tableSize = m_table.Length) >= size)
                return;

            var newSize = Math.Max(tableSize * 2, 16);
            Array.Resize(ref m_table, newSize);
        }
    }
}
