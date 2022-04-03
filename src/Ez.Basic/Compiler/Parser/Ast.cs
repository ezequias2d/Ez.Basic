using Ez.Basic.Compiler.Lexer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ez.Basic.Compiler.Parser
{
    internal class Ast
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

            public NodeType(NodeClass nodeClass, NodeKind kind, LiteralType type = LiteralType.None)
            {
                Debug.Assert(kind != NodeKind.Literal || type != LiteralType.None);
                Class = nodeClass;
                Kind = kind;
                LiteralType = type; 
            }

            public override string ToString()
            {
                return $"{{{Class}, {Kind}, {LiteralType}}}";
            }
        }

        public class Node
        {
            public NodeType Type;
            public Token Token;
            public Node ChildLeft;
            public Node ChildRight;
            public Node Condition;

            public Node(NodeType type, Token token, Node childLeft, Node childRight, Node condition)
            {
                Type = type;
                Token = token;
                ChildLeft = childLeft;
                ChildRight = childRight;
                Condition = condition;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.Append(" [");
                if(Type.LiteralType != LiteralType.None)
                    sb.Append($"{Token.Lexeme}");
                else
                {
                    sb.Append($"{Type.Kind} ");


                    if (Token.Type == TokenType.Identifier ||
                        Type.Kind == NodeKind.Binary ||
                        Type.Kind == NodeKind.Logical ||
                        Type.Kind == NodeKind.Unary) 
                        sb.Append($"{Token.Lexeme} ");

                    if (Condition != null)
                        sb.Append($"{Condition}");

                    if (ChildLeft != null)
                        sb.Append($"{ChildLeft}");


                    if (ChildRight != null)
                        sb.Append($"{ChildRight}");
                }

                if (sb[sb.Length - 1] == '\n')
                    sb.Remove(sb.Length - 1, 1);

                sb.Append("]");

                return sb.ToString();
            }
        }

        public class Block : Node
        {
            public ReadOnlyMemory<Node> Statements;
            public Token EndToken;

            public Block(Token token, Token endToken, ReadOnlyMemory<Node> statements) 
                : base(new NodeType(NodeClass.Stmt, NodeKind.Block), token, null, null, null)
            {
                Statements = statements;
                EndToken = endToken;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"{Token.Lexeme} \n");

                foreach(var stmt in Statements.Span)
                {
                    sb.Append("\t");
                    if (stmt != null)
                        sb.AppendLine(stmt.ToString().Replace("\t", "\t\t"));
                    else
                        sb.AppendLine("NULL");
                }

                sb.Append($"\n{EndToken}");

                return sb.ToString();
            }
        }
    }
}
