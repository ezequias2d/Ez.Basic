using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    public enum Opcode : byte
    {
        Constant,
        Add,
        Subtract,
        Multiply,
        Divide,
        Negate,
        Return,
    }
}
