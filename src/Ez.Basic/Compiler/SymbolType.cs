using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.Compiler
{
    internal enum SymbolType
    {
        Function = 1 << 0,
        Method = 1 << 1,
        Variable = 1 << 3,
        Nil = 1 << 2,
        Numeric = 1 << 3,
        String = 1 << 4,
        Object = 1 << 5,
    }
}
