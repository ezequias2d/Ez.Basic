using Ez.Basic.Compiler.Lexer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.Compiler.Parser
{
    internal ref struct Ast
    {
        public enum NodeClass
        {
            None,
            Expr,
            Stmt,
        }

        public enum NodeKind
        {
            // expr
            Assign,
            Binary,
            Call,
            IndexGet,
            IndexSet,
            Grouping,
            Array,
            Literal,
            Logical,
            Unary,
            Variable,
            Constant,

            // stmt
            Expr,
            Print,
            Let,
            Block,
            If,
            While,
            For,
            Continue,
            Exit,
            Def,
            Sub,
            Class,
            Getter,
            Setter,
            Return,
        }

        public enum LiteralType
        {
            None,
            Number,
            String,
            Null,
            Bool,
        }

        public readonly struct NodeType
        {
            public readonly NodeClass Class;
            public readonly NodeKind Kind;
            public readonly LiteralType LiteralType;
        }

        public ref struct Node
        {
            public NodeType Type;
            public Token Token;
            public Span<int> Children;
            public int Parent;
        }
    }
}
