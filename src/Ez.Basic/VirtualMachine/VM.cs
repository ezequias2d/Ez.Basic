using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    public class VM
    {
        private readonly ILogger m_logger;
        private readonly Stack<Value> m_stack;
        private readonly Stack<Context> m_contexts;
        private readonly GC m_gc;
        private Module m_module;

        public VM(GC gc, ILogger logger)
        {
            m_logger = logger;
            m_stack = new Stack<Value>();
            m_contexts = new Stack<Context>();
            m_gc = gc;
        }

        private ref Context Context => ref m_contexts.Peek();

        public InterpretResult Interpret(Module module, string endpoint)
        {
            m_module = module;
            return Run(endpoint);
        }

        private InterpretResult Run(string endpoint)
        {
            if(!m_module.SymbolTable.Lookup(endpoint, out SymbolEntry entry))
                throw new InvalidOperationException("The endpoint dont exist.");
            
            var main = entry.Data;

            m_contexts.Push(new Context(main, m_module.Chunk));

            var sb = new StringBuilder();
            m_stack.Reset();
            while(m_contexts.Count > 0)
            {
                ref Context context = ref Context;
                if(m_module.Debug)
                {
                    sb.Clear();

                    sb.Append("          ");
                    foreach(var slot in m_stack)
                    {
                        sb.Append($"[{slot}]");
                    }
                    sb.AppendLine();

                    m_module.DisassembleInstruction(sb, context.PC);
                    Console.WriteLine(sb.ToString());
                }
                var opcode = ReadOpcode();
                Value a, b;
                int offset;
                int length;
                switch(opcode)
                {
                    #region constants
                    case Opcode.Constant:
                        var constant = ReadConstant();
                        Push(constant);
                        break;
                    case Opcode.Null: Push(Value.MakeNull()); break;
                    case Opcode.True: Push(true); break;
                    case Opcode.False: Push(false); break;
                    case Opcode.Pop: Pop(); break;
                    case Opcode.PopN: PopN(); break;
                    #endregion
                    #region logical
                    case Opcode.Equal:
                        b = Pop();
                        a = Pop();
                        Push(a == b);
                        break;
                    case Opcode.NotEqual:
                        b = Pop();
                        a = Pop();
                        Push(a != b);
                        break;
                    case Opcode.Greater:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a > b);
                        break;
                    case Opcode.GreaterEqual:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a >= b);
                        break;
                    case Opcode.Less:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a < b);
                        break;
                    case Opcode.LessEqual:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a <= b);
                        break;
                    case Opcode.Not:
                        Push(!Pop());
                        break;
                    #endregion logical
                    #region arithmetic
                    case Opcode.Add:
                        b = Pop();
                        a = Pop();
                        if (a.IsNumber && b.IsNumber)
                            Push(a + b);
                        //else if (a.IsString && b.IsString)
                        //    Push(a.String + b.String);
                        else
                        {
                            //RuntimeError($"Operands must be two numbers or two strings.");
                            RuntimeError($"Operands must be two numbers.");
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    case Opcode.Subtract:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a - b);
                        break;
                    case Opcode.Multiply:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a * b);
                        break;
                    case Opcode.Divide:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a / b);
                        break;
                    case Opcode.Mod:
                        if (!PopOperators(ValueType.Number, out a, out b))
                            return InterpretResult.RuntimeError;
                        Push(a % b);
                        break;
                    case Opcode.Negate:
                        if(Peek().Type != ValueType.Number)
                        {
                            RuntimeError("Operand must be a number.");
                            return InterpretResult.RuntimeError;
                        }
                        Push(-Pop());
                        break;
                    #endregion
                    case Opcode.Concatenate:
                        {
                            b = Pop();
                            a = Pop();

                            string str1, str2;
                            if (a.IsObject)
                                str1 = UnrefObject(ref a).ToString();
                            else
                                str1 = a.ToString();

                            if (b.IsObject)
                                str2 = UnrefObject(ref b).ToString();
                            else
                                str2 = b.ToString();

                            Push(str1 + str2);
                        }
                        break;
                    case Opcode.Print:
                        {
                            a = Pop();

                            string str;
                            if (a.IsObject)
                                str = UnrefObject(ref a).ToString();
                            else
                                str = a.ToString();

                            
                            Console.WriteLine(str);
                        }
                        break;
                    case Opcode.GetVariable:
                        Push(GetVariable());
                        break;
                    case Opcode.SetVariable:
                        a = Pop();
                        SetVariable(a);
                        break;
                    case Opcode.BranchTrue:
                        a = Peek();
                        length = context.Chunk.ReadVariant(context.PC, out offset);
                        
                        if(offset > 0 || !a.Boolean)
                            context.PC += length;

                        if (a.Boolean)
                            context.PC += offset - 1;

                        break;
                    case Opcode.BranchFalse:
                        a = Peek();
                        length = context.Chunk.ReadVariant(context.PC, out offset);
                        
                        if(offset > 0 || a.Boolean)
                            context.PC += length;

                        if (!a.Boolean)
                            context.PC += offset - 1;

                        break;
                    case Opcode.BranchAlways:
                        length = context.Chunk.ReadVariant(context.PC, out offset);
                        if(offset > 0)
                            context.PC += length;
                        context.PC += offset - 1;
                        break;
                    case Opcode.Call:
                    {
                        a = Pop();

                        string name = null; 
                        if(a.IsObject)
                            name = UnrefObject(ref a) as string;

                        if(name is null)
                            throw new NotImplementedException("The call name must be a string object.");
                        
                        if(!m_module.SymbolTable.Lookup(name, out entry))
                            throw new NotImplementedException("The call name not exist.");

                        m_contexts.Push(new Context(entry.Data, context.Chunk));
                    }
                        break;
                    case Opcode.Return:
                    {
                        if(m_contexts.Count == 1 && m_module.SymbolTable.Lookup(endpoint, out entry))
                            if(entry.Type.HasFlag(SymbolType.Function))
                                Console.WriteLine($"Exit: {Pop()}");
                            
                        m_contexts.Pop();
                    }
                        break;
                    default:
                        Console.WriteLine($"Invalid opcode {opcode}");
                        return InterpretResult.RuntimeError;
                }
            }

            return InterpretResult.Ok;
        }

        private bool PopOperators(ValueType type, out Value a, out Value b)
        {
            b = Pop();
            a = Pop();

            if(a.Type != type || b.Type != type)
            {
                RuntimeError($"Operands must be '{type}'.");
                return false;
            }
            return true;
        }

        private void Push(in Value value)
        {
            m_stack.Push(value);
        }

        private void Push(object obj)
        {
            var reference = m_gc.AddObject(obj);
            Push(reference);
        }

        private Value Peek()
        {
            return m_stack.Peek();
        }

        private Value Pop()
        {
            return m_stack.Pop();
        }

        private void PopN()
        {
            m_stack.Pop(Context.ReadVariant());
        }

        private object UnrefObject(ref Value reference)
        {
            var obj = m_gc.GetObject(reference);
            m_gc.RemoveObject(ref reference.m_as.Reference);
            return obj;
        }

        private Opcode ReadOpcode()
        {
            return Context.ReadOpcode();
        }

        private Value ReadConstant()
        {
            return m_module.GetConstant(Context.ReadVariant());
        }

        private Value GetVariable()
        {
            return m_stack.Peek(Context.ReadVariant());
        }

        private void SetVariable(Value value)
        {
            m_stack.Peek(Context.ReadVariant()) = value;
            Push(value);
        }

        private void RuntimeError(string message)
        {
            var line = Context.GetLine();
            m_logger.LogError($"[line {line}] in script");
            m_stack.Reset();
        }
    }
}
