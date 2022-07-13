using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.Compiler.Lexer
{
    /*
     Code example:

        def pow(x, y)
            let tmp = 1
            let p = 0
            while p < y
                tmp = tmp * x
                p = p + 1
            next
            return tmp
        end
    
        def pow(x, y)
            let tmp = 1
            for p = 0 to y - 1 step 1 do
                tmp = tmp * x
            next
            return tmp
        end

        def pow(x, y)
            if y == 0 do
                return 1
            next
        
            let tmp = 1
            let p = 0
            do
                tmp = tmp * x
                p = p + 1
            while p < y
        end

        def pow(x, y)
            let tmp = 1
            let p = 0
            until p >= y do
                tmp = tmp * x
                p = p + 1
            next
        end
    
        def pow(x, y)
            if y == 0 do
                return 1
            else do
                return pow(x, y - 1) * x
        end
     
     */
    public enum TokenType
    {
        None,
        Semicolon,      // ; used for comments
        LeftParen,      // (
        RightParen,     // )
        LeftBracket,    // [
        RightBracket,   // ]
        Comma,          // ,
        Dot,            // .
        Minus,          // -
        Plus,           // +
        Slash,          // /
        Star,           // *
        Mod,            // %

        Bang,           // !
        BangEqual,      // !=
        Equal,          // =
        EqualEqual,     // ==
        Greater,        // >
        GreaterEqual,   // >=
        Less,           // <
        LessEqual,      // <=

        Identifier,     // <string>
        Label,          // <string>:
        String,         // "<string>"
        Number,         // <number>

        True,
        False,
        Null,
        Is,
        And,
        Or,
        Xor,
        Not,

        // declaration
        Def,        // function
        Sub,        // method
        Mould,      // basically class
        Module,     // basically static class
        Global,     // exposes globaly
        This,       // this(this class)
        Base,       // base(base class)
        Let,        // declare a variable
        Const,      // declare a constant

        // define a function
        // define a multi dimensional array
        For,
        Do,
        While,
        Until,
        Goto,
        If,
        Next,
        Else,
        To,
        Step,
        Return,
        End,

        // console commands
        Print,
        Input,
        Clear,

        Error,
        EoF,
    }
}
