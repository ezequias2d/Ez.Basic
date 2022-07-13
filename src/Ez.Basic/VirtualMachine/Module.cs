using System;
using System.Collections.Generic;

namespace Ez.Basic.VirtualMachine
{
    public class Module
    {
        private readonly ConstantPool m_constants;
        public Module(GC gc, bool debug)
        {
            GC = gc;
            m_constants = new(gc);
            SymbolTable = new(null, SymbolTableType.None, 0);
            Debug = debug;
            Chunk = new();
        }

        public Chunk Chunk { get; }
        public SymbolTable SymbolTable { get; }
        public GC GC { get; }

        public bool Debug { get; }

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