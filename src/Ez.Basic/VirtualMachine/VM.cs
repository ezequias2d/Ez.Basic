using Ez.Basic.VirtualMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    public class VM
    {
        private Chunk m_chunk;
        private readonly Stack m_stack;
        private int m_PC;
        public VM()
        {
            m_stack = new Stack();
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
                Value a;
                Value b;
                switch(opcode)
                {
                    case Opcode.Constant:
                        var constant = ReadConstant();
                        Push(constant);
                        break;
                    case Opcode.Add:
                        b = Pop();
                        a = Pop();
                        Push(a + b);
                        break;
                    case Opcode.Subtract:
                        b = Pop();
                        a = Pop();
                        Push(a - b);
                        break;
                    case Opcode.Multiply:
                        b = Pop();
                        a = Pop();
                        Push(a * b);
                        break;
                    case Opcode.Divide:
                        b = Pop();
                        a = Pop();
                        Push(a / b);
                        break;
                    case Opcode.Negate:
                        Push(-Pop());
                        break;
                    case Opcode.Return:
                        var value = Pop();
                        Console.WriteLine($"Return value is {value}");
                        return InterpretResult.Ok;
                }
            }
        }

        private void Push(in Value value)
        {
            m_stack.Push(value);
        }

        private Value Pop()
        {
            return m_stack.Pop();
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
    }
}
