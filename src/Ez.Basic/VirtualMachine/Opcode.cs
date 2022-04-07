using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    public enum Opcode : byte
    {
        Constant,
        Null,
        True,
        False,
        Pop,
        Equal,
        NotEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
        Add,
        Subtract,
        Multiply,
        Divide,
        Concatenate,
        Not,
        Negate,
        Print,
        Return,
    }
}
