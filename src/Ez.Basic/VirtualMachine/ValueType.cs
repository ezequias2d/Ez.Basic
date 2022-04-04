using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    public enum ValueType : byte
    {
        Nil,
        Object,
        Bool,
        Number,
    }
}
