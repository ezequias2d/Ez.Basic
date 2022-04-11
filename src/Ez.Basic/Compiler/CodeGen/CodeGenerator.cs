using Ez.Basic.Compiler.Lexer;
using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static Ez.Basic.Compiler.Parser.Ast;

namespace Ez.Basic.Compiler.CodeGen
{
    internal ref struct CodeGenerator
    {
        private readonly Chunk m_chunk;
        private readonly ILogger m_logger;
        private readonly Dictionary<string, int> m_locals;
        private SymbolTable m_scope;
        private bool m_hadError;
        private int m_sp;

        public CodeGenerator(ILogger logger, Chunk targetChunk)
        {
            m_logger = logger;
            m_chunk = targetChunk;
            m_hadError = false;
            m_locals = new Dictionary<string, int>();
            m_scope = null;
            m_sp = 0;
            BeginScope();
        }


        public bool CodeGen(Node block)
        {
            m_hadError = false;
            
            foreach(var stmt in block.Data)
            {
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
                case NodeKind.If:
                    IfStatement(node);
                    break;
                case NodeKind.Block:
                    DeclareBlock(node);
                    break;
                default:
                    throw new CodeGenException();
            }
        }

        private void PrintStatement(Node node)
        {
            Expression(node.ChildLeft);
            Pop();
            Emit(node, Opcode.Print);
        }

        private void ExpressionStatement(Node node)
        {
            Expression(node.ChildLeft);
            Pop();
            Emit(node, Opcode.Pop);
        }

        private void LetStatement(Node node)
        {
            DeclareVariable(node);
        }

        private void IfStatement(Node node)
        {
            bool hasElse = node.ChildRight is not null;
            Expression(node.Condition);

            var branch = Emit(node, Opcode.BranchFalse);
            Pop();
            Statement(node.ChildLeft);

            int elseJump = m_chunk.Count;
            if(hasElse)
                elseJump = Emit(node, Opcode.BranchAlways);

            if(hasElse)
            {
                var elseBranch = m_chunk.Count;
                Statement(node.ChildRight);

                var delta = m_chunk.Count - elseJump - 1;
                m_chunk.InsertVarint(elseJump + 1, delta);
                elseJump = elseBranch;
            }

            {
                var delta = elseJump - branch;
                m_chunk.InsertVarint(branch + 1, delta);
            }
        }

        private void Expression(Node node)
        {
            Debug.Assert(node.Type.Class == NodeClass.Expr);
            
            switch(node.Type.Kind)
            {
                case NodeKind.Literal: Literal(node); break;
                case NodeKind.Unary: Unary(node); break;
                case NodeKind.Binary: Binary(node); break;
                case NodeKind.Variable: Variable(node); break;
                case NodeKind.Assign: Assign(node); break;
                default:
                    throw new NotImplementedException();
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
                case LiteralType.Null: EmitNull(node); break;
                case LiteralType.String: String(node); break;
                case LiteralType.Number: Number(node); break;
                default: throw new CodeGenException();
            }
            Push();
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
            Pop();
            Push();
            switch (op)
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

            Pop(2);
            Push();
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

        private void Variable(Node node)
        {
            EnsureVariable(node);

            if (!m_scope.Lookup(node.Token.Lexeme.ToString(), out var depth))
                ErrorAt(node, "The variable must be declared.");

            var delta = m_sp - depth;
            Emit(node, Opcode.GetVariable);
            m_chunk.WriteVarint(delta);

            Push();
        }

        private void Assign(Node node)
        {
            EnsureAssign(node);
            EnsureVariable(node.ChildLeft);

            Expression(node.ChildRight);
            Pop();
            var lexeme = node.ChildLeft.Token.Lexeme.ToString();

            if (!m_scope.Lookup(lexeme, out var depth))
                ErrorAt(node, "The variable must be declared.");

            var delta = m_sp - depth;
            Emit(node, Opcode.SetVariable);
            m_chunk.WriteVarint(delta);

            Push();
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
        private void EnsureVariable(Node node)
        {
            EnsureExpr(node, NodeKind.Variable);
        }

        [Conditional("DEBUG")]
        private void EnsureAssign(Node node)
        {
            EnsureExpr(node, NodeKind.Assign);
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

        private int Emit<T>(Node node, in T value) where T : unmanaged
        {
            return m_chunk.Write(value, node.Token.Line);
        }

        private void EmitConstant(Node node, double value)
        {
            Emit(node, Opcode.Constant);
            var index = m_chunk.AddConstant(value);
            m_chunk.WriteVarint(index);
        }

        private void EmitNull(Node node)
        {
            Emit(node, Opcode.Null);
        }

        private T LastEmitted<T>() where T : unmanaged
        {
            return m_chunk.Peek<T>();
        }

        private void DeclareVariable(Node node)
        {
            var name = node.Token.Lexeme.ToString();

            SymbolType type = SymbolType.Variable;
            if (node.ChildLeft != null)
            {
                var init = node.ChildLeft;
                Expression(init);

                var opcode = LastEmitted<Opcode>();
                switch (opcode)
                {
                    case Opcode.Concatenate:
                        type |= SymbolType.String;
                        break;
                    case Opcode.Constant:
                        type |= init.Type.LiteralType == LiteralType.String
                            ? SymbolType.String
                            : SymbolType.Numeric;
                        break;
                    case Opcode.Add:
                    case Opcode.Subtract:
                    case Opcode.Multiply:
                    case Opcode.Divide:
                    case Opcode.Negate:
                        type |= SymbolType.Numeric;
                        break;
                }
            }
            else
                EmitNull(node);

            AddLocal(name, type);
        }

        private void DeclareBlock(Node node)
        {
            BeginScope();
            foreach(var stmt in node.Data)
                Statement(stmt);
            EndScope(node);
        }

        private void AddLocal(string name, SymbolType type)
        {
            m_scope.Insert(name, type, m_sp);
        }

        private void BeginScope()
        {
            m_scope = new SymbolTable(m_scope, m_sp);
        }

        private void EndScope(Node node)
        {
            var delta = m_sp - m_scope.Depth;
            
            if(delta > 0)
            {
                Emit(node, Opcode.PopN);
                m_chunk.WriteVarint(delta);
            }

            m_sp = m_scope.Depth;
            m_scope = m_scope.Parent;
        }

        private void EmitConstant(Node node, string value)
        {
            Emit(node, Opcode.Constant);
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

        private int Push(int count = 1)
        {
            var sp = m_sp;
            m_sp += count;
            return sp;
        }

        private int Pop(int count = 1)
        {
            m_sp -= count;
            return m_sp;
        }
    }
}
