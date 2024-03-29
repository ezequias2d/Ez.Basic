﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    public enum Opcode : byte
    {
        GetVariable,
        SetVariable,
        Constant,
        Null,
        True,
        False,
        Pop,
        PopN,
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
        Mod,
        Concatenate,
        Not,
        Negate,
        Print,
        BranchTrue,
        BranchFalse,
        BranchAlways,
        Call,
        Return,
    }
}
