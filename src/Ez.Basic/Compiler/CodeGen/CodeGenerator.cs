﻿using Ez.Basic.Compiler.Lexer;
using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using static Ez.Basic.Compiler.Parser.Ast;

namespace Ez.Basic.Compiler.CodeGen
{
    internal ref struct CodeGenerator
    {
        private readonly Module m_module;
        private readonly ILogger m_logger;
        private readonly Dictionary<string, int> m_locals;
        private SymbolTable m_scope;
        private bool m_hadError;
        private int m_sp;
        private Chunk m_chunk;

        public CodeGenerator(ILogger logger, Module module)
        {
            m_logger = logger;
            m_module = module;
            m_hadError = false;
            m_locals = new Dictionary<string, int>();
            m_scope = null;
            m_sp = 0;
            m_chunk = module.Chunk;
            m_scope = module.SymbolTable;
        }

        public bool CodeGen(Node block)
        {
            m_hadError = false;
            
            foreach(var stmt in block.Data)
            {
                Declaration(stmt);
            }

            return !m_hadError;
        }

        private void DefStatement(Node node)
        {
            EnsureStmt(node, NodeKind.Def);
            DeclareFunction(node, true);
        }

        private void SubStatement(Node node)
        {
            EnsureStmt(node, NodeKind.Sub);
            DeclareFunction(node, false);
        }

        private void Statement(Node node, IList<Node> returnNodes)
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
                case NodeKind.If:
                    IfStatement(node, returnNodes);
                    break;
                case NodeKind.Block:
                    BlockStatment(node, returnNodes);
                    break;
                case NodeKind.Until:
                    UntilStatement(node, returnNodes);
                    break;
                case NodeKind.While:
                    WhileStatement(node, returnNodes);
                    break;
                case NodeKind.Def:
                case NodeKind.Let:
                    Declaration(node);
                    break;
                case NodeKind.Return:
                    Return(node); 
                    returnNodes.Add(node);
                    break;
                default:
                    throw new CodeGenException();
            }
        }

        private void Declaration(Node node)
        {
            switch(node.Type.Kind)
            {
                case NodeKind.Def:
                    DefStatement(node);
                    break;
                case NodeKind.Sub:
                    SubStatement(node);
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
            Pop();
            Emit(node, Opcode.Print);
        }

        private void ExpressionStatement(Node node)
        {
            Expression(node.ChildLeft, false);
            Pop();
            Emit(node, Opcode.Pop);
        }

        private void LetStatement(Node node)
        {
            DeclareVariable(node);
        }

        private void IfStatement(Node node, IList<Node> returnStatements)
        {
            bool hasElse = node.ChildRight is not null;
            Expression(node.Condition);

            var elseBranch = Emit(node, Opcode.BranchFalse);
            Emit(node, Opcode.Pop);
            Pop();
            Statement(node.ChildLeft, returnStatements);

            var startElse = m_chunk.Count;
            if(hasElse)
            {
                var skipElse = Emit(node, Opcode.BranchAlways);

                Emit(node, Opcode.Pop);
                Statement(node.ChildRight, returnStatements);
                m_chunk.InsertVarint(skipElse + 1, m_chunk.Count - skipElse);

                startElse += 1 + m_chunk.ReadVariant(skipElse + 1, out _);
            }

            {
                var delta = startElse - elseBranch;
                m_chunk.InsertVarint(elseBranch + 1, delta);
            }

        }

        private void Expression(Node node, bool needValue = true)
        {
            Debug.Assert(node.Type.Class == NodeClass.Expr);
            
            switch(node.Type.Kind)
            {
                case NodeKind.Literal: Literal(node); break;
                case NodeKind.Unary: Unary(node); break;
                case NodeKind.Binary: Binary(node); break;
                case NodeKind.Variable: Variable(node); break;
                case NodeKind.Assign: Assign(node); break;
                case NodeKind.Logical: Logical(node); break;
                case NodeKind.Call: Call(node, needValue); break;
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
        }

        private void Number(Node node)
        {
            EnsureNumber(node);
            var value = double.Parse(node.Token.Lexeme.ToString(), CultureInfo.InvariantCulture);
            EmitConstant(node, value);
        }

        private void String(Node node)
        {
            EnsureString(node);
            var value = node.Token.Lexeme.Slice(1, node.Token.Lexeme.Length - 2).ToString();
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
                case TokenType.Mod:
                    Emit(node, Opcode.Mod);
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

            if (!m_scope.Lookup(node.Token.Lexeme.ToString(), out SymbolEntry entry))
                ErrorAt(node, "The variable must be declared.");

            var delta = m_sp - entry.Data;  // sp - depth
            Emit(node, Opcode.GetVariable);
            m_chunk.WriteVarint(delta);
            Push();

        }

        private void Logical(Node node)
        {
            EnsureLogical(node);

            Expression(node.ChildLeft);

            var op = node.Token.Type;

            Pop(1);
            Push();
            switch(op)
            {
                case TokenType.And:
                    LogicalAnd(node);
                    break;
                case TokenType.Or:
                    LogicalOr(node);
                    break;
                default:
                    throw new CodeGenException();
            }
        }

        private void LogicalAnd(Node node)
        {
            var endJump = Emit(node, Opcode.BranchFalse);

            Pop(1);
            Emit(node, Opcode.Pop);
            Expression(node.ChildRight);

            m_chunk.InsertVarint(endJump + 1, m_chunk.Count - endJump);
        }

        private void LogicalOr(Node node)
        {
            var elseJump = Emit(node, Opcode.BranchFalse);
            var endJump = Emit(node, Opcode.BranchAlways);

            Pop();
            Emit(node, Opcode.Pop);

            var elseLocation = m_chunk.Count;
            Expression(node.ChildRight);

            m_chunk.InsertVarint(endJump + 1, m_chunk.Count - endJump);
            m_chunk.InsertVarint(elseJump + 1, elseLocation - elseJump);
        }

        private void Call(Node node, bool needValue)
        {
            EnsureExpr(node, NodeKind.Call);
            var name = node.ChildLeft.Token.Lexeme.ToString();

            if (!m_scope.Lookup(name, out SymbolEntry entry))
                ErrorAt(node, $"The function or method '{name}' must be declared.");
            
            if(needValue && entry.Type.HasFlag(SymbolType.Method))
                ErrorAt(node, $"The expression '{name}' does not produce a value.");
            
            var sp = m_sp;
            foreach(var arg in node.Data)
                Expression(arg);

            EmitConstant(node, name);
            Emit(node, Opcode.Call);
            
            var d = m_sp - sp;

            if (d > 0 && entry.Type.HasFlag(SymbolType.Function))
                // ensure that dont pops the arg0, because is the return value.
                d--;

            Pop(d);
            EmitPop(node, d);
        }

        private void Return(Node node)
        {
            EnsureStmt(node, NodeKind.Return);
            EndFunctionScope(node);
            Emit(node, Opcode.Return);
        }

        private void Assign(Node node)
        {
            EnsureAssign(node);
            EnsureVariable(node.ChildLeft);

            Expression(node.ChildRight);
            Pop();
            var lexeme = node.ChildLeft.Token.Lexeme.ToString();


            EmitAssign(node, lexeme,"The variable must be declared.");
        }

        private void UntilStatement(Node node, IList<Node> returnStatements)
        {
            ConditionalLoopStatement(node, Opcode.BranchTrue, returnStatements);
        }

        private void WhileStatement(Node node, IList<Node> returnStatements)
        {
            ConditionalLoopStatement(node, Opcode.BranchFalse, returnStatements);
        }

        private void ConditionalLoopStatement(Node node, Opcode exitCondition,  IList<Node> returnStatements)
        {
            if(exitCondition != Opcode.BranchFalse && exitCondition != Opcode.BranchTrue)
                throw new CodeGenException();
            
            var loopStart = m_chunk.Count;
            Expression(node.Condition);

            var exit = Emit(node, exitCondition);
            Pop();
            EmitPop(node, 1);
            Statement(node.ChildLeft, returnStatements);

            var loopBranch = Emit(node, Opcode.BranchAlways);
            var loopOffset = loopStart - loopBranch;

            var exitOffset = m_chunk.Count - exit;

            int correctedExitOffset = exitOffset;
            int correctedLoopOffset = loopOffset;

            do
            {
                correctedExitOffset = exitOffset + Varint.EncodedSizeOf(correctedLoopOffset);
                correctedLoopOffset = loopOffset - Varint.EncodedSizeOf(correctedExitOffset);
            } while(correctedExitOffset != exitOffset + Varint.EncodedSizeOf(correctedLoopOffset) ||
                    correctedLoopOffset != loopOffset - Varint.EncodedSizeOf(correctedExitOffset));

            m_chunk.InsertVarint(loopBranch + 1, correctedLoopOffset);

            m_chunk.InsertVarint(exit + 1, correctedExitOffset);
            Emit(node, Opcode.Pop);
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
        private void EnsureLogical(Node node)
        {
            EnsureExpr(node, NodeKind.Logical);
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
            var index = m_module.AddConstant(value);
            m_chunk.WriteVarint(index);
            Push();
        }

        private void EmitNull(Node node)
        {
            Emit(node, Opcode.Null);
            Push();
        }

        private T LastEmitted<T>() where T : unmanaged
        {
            return m_chunk.Peek<T>();
        }

        private void DeclareFunction(Node node, bool returnValue)
        {
            const string returnVariable = "@return";
            var startPc = m_chunk.Count;
            var name = node.Token.Lexeme.ToString();

            SymbolType type = returnValue ? SymbolType.Function : SymbolType.Method;
            var returnStatements = new List<Node>();

            BeginScope(returnValue ? SymbolTableType.Function : SymbolTableType.Method);

            var @params = node.Parameters;
            var paramCount = node.Parameters.Length;
            for (var i = 0; i < paramCount; i++)
                AddParam(@params[i].Lexeme.ToString(), m_sp - paramCount + i + 1);

            if (returnValue)
            {
                if (paramCount < 1)
                {
                    EmitNull(node);
                    Push();
                    paramCount = 1;
                }

                AddParam(returnVariable, m_sp - paramCount + 2);
            }
           

            foreach(var stmt in node.ChildRight.Data)
                Statement(stmt, returnStatements);

            //EmitConstant(node, 0);
            //EmitAssign(node, returnVariable);

            if(returnValue) 
            {
                if (returnStatements.Count == 0)
                    throw new CodeGenException("Functions must be contains return statement");
            }
            else
            {
                EndFunctionScope(node);
                Emit(node, Opcode.Return);
            }
        
            AddSymbol(name, type, startPc);
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

        private void BlockStatment(Node node, IList<Node> returnNodes)
        {
            BeginScope(SymbolTableType.None);
            foreach(var stmt in node.Data)
                Statement(stmt, returnNodes);
            EndScope(node);
        }

        private void AddLocal(string name, SymbolType type)
        {
            m_scope.Insert(name, type, m_sp);
        }

        private void AddParam(string name, int index)
        {
            m_scope.Insert(name, SymbolType.Variable, m_sp + index);
        }

        private void AddSymbol(string name, SymbolType type, int data)
        {
            m_scope.Insert(name, type, data);
        }

        private void BeginScope(SymbolTableType type)
        {
            m_scope = new SymbolTable(m_scope, type, m_sp);
        }

        private void EndScope(Node node, int remaining = 0)
        {
            var delta = m_sp - (m_scope.Depth + remaining);

            //Pop(delta);
            EmitPop(node, delta);

            m_sp = m_scope.Depth;
            m_scope = m_scope.Parent;
        }

        private void EndFunctionScope(Node node)
        {
            while (m_scope.Type != SymbolTableType.Function && m_scope.Type != SymbolTableType.Method)
            {
                EndScope(node);
            }

            if (node.ChildLeft is not null)
            {
                Expression(node.ChildLeft);
                EmitAssign(node, "@return", "The 'return' keyword only work inside function(def) statements.");
            }

            if (m_scope.Type == SymbolTableType.Function)
                EndScope(node, 1);
            else
                EndScope(node);
        }

        private void EmitConstant(Node node, string value)
        {
            Emit(node, Opcode.Constant);
            var index = m_module.AddStringConstant(value);
            m_chunk.WriteVarint(index);
        }

        private void EmitPop(Node node, int n)
        {
            if(n == 1)
                Emit(node, Opcode.Pop);
            else if(n > 1)
            {
                Emit(node, Opcode.PopN);
                m_chunk.WriteVarint(n);
            }
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

        private void EmitAssign(Node node, string variable, string message = "The variable must be declared.")
        {
            if (!m_scope.Lookup(variable, out SymbolEntry entry))
                ErrorAt(node, message);

            var delta = m_sp - entry.Data;  // sp - depth
            Emit(node, Opcode.SetVariable);
            m_chunk.WriteVarint(delta);

            Push();
        }
    }
}
