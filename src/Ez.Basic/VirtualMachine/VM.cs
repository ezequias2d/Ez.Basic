using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    public class VM
    {
        private readonly ILogger m_logger;
        private readonly Stack<Value> m_stack;
        private readonly GC m_gc;
        private Chunk m_chunk;
        private int m_PC;

        public VM(GC gc, ILogger logger)
        {
            m_logger = logger;
            m_stack = new Stack<Value>();
            m_gc = gc;
        }

        public InterpretResult Interpret(Chunk chunk)
        {
            m_chunk = chunk;
            m_PC = 0;
            return Run();
        }

        private InterpretResult Run()
        {
            var sb = new StringBuilder();
            m_stack.Reset();
            for (;;)
            {
                if(m_chunk.Debug)
                {
                    sb.Clear();

                    sb.Append("          ");
                    foreach(var slot in m_stack)
                    {
                        sb.Append($"[{slot}]");
                    }
                    sb.AppendLine();

                    m_chunk.DisassembleInstruction(sb, m_PC);
                    Console.WriteLine(sb.ToString());
                }
                var opcode = ReadOpcode();
                Value a, b;
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
                    case Opcode.BranchFalse:
                        a = Pop();
                        m_PC += m_chunk.ReadVariant(m_PC, out var offset);

                        if (!a.Boolean)
                            m_PC += offset;
                        break;
                    case Opcode.Return:
                        //var value = Pop();
                        //Console.WriteLine($"Return value is {value}");
                        return InterpretResult.Ok;
                }
            }
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
            m_PC += m_chunk.ReadVariant(m_PC, out var n);
            m_stack.Pop(n);
        }

        private object UnrefObject(ref Value reference)
        {
            var obj = m_gc.GetObject(reference);
            m_gc.RemoveObject(ref reference.m_as.Reference);
            return obj;
        }

        private Opcode ReadOpcode()
        {
            return m_chunk.Read<Opcode>(m_PC++);
        }

        private Value ReadConstant()
        {
            m_PC += m_chunk.ReadVariant(m_PC, out var constantIndex);
            return m_chunk.GetConstant(constantIndex);
        }

        private Value GetVariable()
        {
            m_PC += m_chunk.ReadVariant(m_PC, out var offset);
            return m_stack.Peek(offset);
        }

        private void SetVariable(Value value)
        {
            m_PC += m_chunk.ReadVariant(m_PC, out var offset);
            m_stack.Peek(offset) = value;
            Push(value);
        }

        private void RuntimeError(string message)
        {
            var line = m_chunk.LineNumberTable.GetLine(m_PC);
            m_logger.LogError($"[line {line}] in script");
            m_stack.Reset();
        }
    }
}
