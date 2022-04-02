using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.Compiler.Parser
{
    internal class Expr
    {
        private readonly List<Node> m_nodes;

        public Expr()
        {
            m_nodes = new List<Node>();
        }
        
        public enum ExprType
        {
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
        }

        public struct Node
        {
            public readonly ExprType Type;
        }
    }
}
