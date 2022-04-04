using Ez.Basic.Compiler.Lexer;
using Ez.Basic.Compiler.Parser;
using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using static Ez.Basic.Compiler.Parser.Ast;

namespace Ez.Basic.Compiler.CodeGen
{
    internal ref struct CodeGenerator
    {
        private readonly Chunk m_chunk;
        private readonly ILogger m_logger;
        private readonly Stack<Node> m_stack;
        private bool m_hadError;
        private readonly Stack<SymbolTable> m_symbolTables;

        public CodeGenerator(ILogger logger, Chunk targetChunk, Node block)
        {
            m_logger = logger;
            m_chunk = targetChunk;
            m_hadError = false;
            m_stack = new Stack<Node>();
            m_symbolTables = new Stack<SymbolTable>();

            foreach(var stmt in block.Data)
                m_stack.Push(stmt);
        }

        public bool CodeGen()
        {
            m_hadError = false;

            while(m_stack.Has)
            {
                var stmt = m_stack.Pop();

                Statement(stmt);
            }


            return !m_hadError;
        }

        private void Statement(Node node)
        {
            if (node.Type.Class != NodeClass.Stmt)
                throw new CodeGenException();

            switch(node.Type.Kind)
            {
                case NodeKind.Print:
                    PrintStatement(node);
                    break;
                case NodeKind.Expr:
                    ExpressionStatement(node);
                    break;
                case NodeKind.Let:
                    LetStatement(node);
                    break;
                default:
                    throw new CodeGenException();
            }
        }

        private void PrintStatement(Node node)
        {
            Expression(node.ChildLeft);
            Emit(node, Opcode.Print);
        }

        private void ExpressionStatement(Node node)
        {
            Expression(node.ChildLeft);
            Emit(node, Opcode.Pop);
        }

        private void LetStatement(Node node)
        {
            DeclareVariable(node);
        }

        private void Expression(Node node)
        {
            Debug.Assert(node.Type.Class == NodeClass.Expr);

            switch(node.Type.Kind)
            {
                case NodeKind.Literal: Literal(node); break;
                case NodeKind.Unary: Unary(node); break;
                case NodeKind.Binary: Binary(node); break;
                    
            }
        }

        private void Literal(Node node)
        {
            switch(node.Type.LiteralType)
            {
                case LiteralType.Bool:
                    switch (node.Token.Type)
                    {
                        case TokenType.False: Emit(node, Opcode.False); break;
                        case TokenType.True: Emit(node, Opcode.True); break;
                        default: throw new CodeGenException();
                    }
                    break;
                case LiteralType.Null: Emit(node, Opcode.Null); break;
                case LiteralType.String: String(node); break;
                case LiteralType.Number: Number(node); break;
                default: throw new CodeGenException();
            }
        }

        private void Number(Node node)
        {
            EnsureNumber(node);
            var value = double.Parse(node.Token.Lexeme.ToString());
            EmitConstant(node, value);
        }

        private void String(Node node)
        {
            EnsureString(node);
            var value = node.Token.Lexeme.ToString();
            EmitConstant(node, value);
        }

        private void Unary(Node node)
        {
            EnsureUnary(node);
            Expression(node.ChildRight);
            
            var op = node.Token.Type;
            switch(op)
            {
                case TokenType.Minus: Emit(node, Opcode.Negate); break;
                case TokenType.Not: Emit(node, Opcode.Not); break;
                default:
                    throw new CodeGenException();
            }
        }

        private void Binary(Node node)
        {
            EnsureBinary(node);

            Expression(node.ChildLeft);
            Expression(node.ChildRight);

            var op = node.Token.Type;

            switch(op)
            {
                case TokenType.BangEqual: 
                    Emit(node, Opcode.NotEqual);
                    break;
                case TokenType.EqualEqual:
                    Emit(node, Opcode.Equal);
                    break;
                case TokenType.Greater:
                    Emit(node, Opcode.Greater);
                    break;
                case TokenType.GreaterEqual:
                    Emit(node, Opcode.GreaterEqual);
                    break;
                case TokenType.Less:
                    Emit(node, Opcode.Less);
                    break;
                case TokenType.LessEqual:
                    Emit(node, Opcode.LessEqual);
                    break;
                case TokenType.Plus:
                    Emit(node, Opcode.Add);
                    break;
                case TokenType.Minus:
                    Emit(node, Opcode.Subtract);
                    break;
                case TokenType.Star:
                    Emit(node, Opcode.Multiply);
                    break;
                case TokenType.Slash:
                    Emit(node, Opcode.Divide);
                    break;
                case TokenType.Comma:
                    Emit(node, Opcode.Concatenate);
                    break;
                default:
                    throw new CodeGenException();
            }
        }

        [Conditional("DEBUG")]
        private void EnsureStmt(Node node, NodeKind kind)
        {
            Debug.Assert(node.Type.Class == NodeClass.Stmt &&
                node.Type.Kind == kind &&
                node.Type.LiteralType == LiteralType.None);
        }

        [Conditional("DEBUG")]
        private void EnsureNumber(Node node)
        {
            EnsureLiteral(node, LiteralType.Number);
        }

        [Conditional("DEBUG")]
        private void EnsureString(Node node)
        {
            EnsureLiteral(node, LiteralType.String);
        }

        [Conditional("DEBUG")]
        private void EnsureUnary(Node node)
        {
            EnsureExpr(node, NodeKind.Unary);
        }

        [Conditional("DEBUG")]
        private void EnsureBinary(Node node)
        {
            EnsureExpr(node, NodeKind.Binary);
        }


        [Conditional("DEBUG")]
        private void EnsureLiteral(Node node, LiteralType type)
        {
            EnsureExpr(node, NodeKind.Literal, type);
        }

        [Conditional("DEBUG")]
        private void EnsureExpr(Node node, NodeKind kind, LiteralType literal = LiteralType.None)
        {
            Debug.Assert(node.Type.Class == NodeClass.Expr &&
                node.Type.Kind == kind &&
                node.Type.LiteralType == literal);
        }

        private void Emit<T>(Node node, T value) where T : unmanaged
        {
            m_chunk.Write(value, node.Token.Line);
        }

        private void EmitConstant(Node node, double value)
        {
            Emit(node, Opcode.NumericConstant);
            var index = m_chunk.AddNumericConstant(value);
            m_chunk.WriteVarint(index);
        }

        private T LastEmitted<T>() where T : unmanaged
        {
            return m_chunk.Peek<T>();
        }

        private void DeclareVariable(Node node)
        {
            var name = node.Token.Lexeme.ToString();
            var current = m_symbolTables.Peek();

            SymbolType type = SymbolType.Variable;
            if(node.ChildLeft != null)
            {
                var init = node.ChildLeft;
                Expression(init);
                var opcode = LastEmitted<Opcode>();
                switch(opcode)
                {
                    case Opcode.Concatenate:
                    case Opcode.StringConstant:
                        type |= SymbolType.String;
                        break;
                    case Opcode.NumericConstant:
                    case Opcode.Add:
                    case Opcode.Subtract:
                    case Opcode.Multiply:
                    case Opcode.Divide:
                    case Opcode.Negate:
                        type |= SymbolType.Numeric;
                        break;
                }
            }
            current.Insert(name, SymbolType.Variable);
        }

        private void EmitConstant(Node node, string value)
        {
            Emit(node, Opcode.StringConstant);
            var index = m_chunk.AddStringConstant(value);
            m_chunk.WriteVarint(index);
        }

        private CodeGenException ErrorAtCurrent(Node node, string message)
        {
            return ErrorAt(node, message);
        }

        private CodeGenException Error(Node node, string message)
        {
            return ErrorAt(node, message);
        }

        private CodeGenException ErrorAt(Node node, string message)
        {
            var sb = new StringBuilder();
            var token = node.Token;

            sb.Append($"[line {token.Line}] Error");

            if (token.Type == TokenType.EoF)
                sb.Append(" at end");
            else if (token.Type == TokenType.Error)
            {

            }
            else
                sb.Append($" at '{token.Lexeme.ToString()}'");

            sb.Append(": ");
            sb.AppendLine(message);

            m_logger?.LogError(sb.ToString());
            m_hadError = true;

            return new CodeGenException();
        }
    }
}
