using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic
{
    [Flags]
    public enum SymbolTableType
    {
        None        =      0,
        Breakable   = 1 << 0,
        Function    = 1 << 1,
        Method      = 1 << 2,
    }
}
