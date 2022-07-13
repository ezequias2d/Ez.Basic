using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    public static class BasicDebug
    {
        public static void Disassemble(this Module module, ILogger logger, string name)
        {
            logger.LogDebug($"== {name} ==");
            var sb = new StringBuilder();
            for (var offset = 0; offset < module.Chunk.Count;)
            {
                offset += module.DisassembleInstruction(sb, offset);
                logger.LogDebug(sb.ToString());
                sb.Clear();
            }
        }

        internal static int DisassembleInstruction(this Module module, StringBuilder sb, int offset)
        {
            sb.Append(offset.ToString("X4"));

            // add line number
            var line = module.Chunk.LineNumberTable.GetLine(offset);
            if (offset > 0 &&
                line == module.Chunk.LineNumberTable.GetLine(offset - 1))
                sb.Append("   | ");
            else if (line < 0)
            {
                sb.Append(" ----");
            }
            else
            {
                sb.Append(' ');
                sb.Append(line.ToString("D4"));
            }

            sb.Append(" ");

            var instruction = module.Chunk.Read<Opcode>(offset);
            switch (instruction)
            {
                #region constants
                case Opcode.Constant:
                    return module.ConstantInstruction(sb, "OP_CONSTANT", offset);
                case Opcode.Null:
                    return module.SimpleInstruction(sb, "OP_NULL", offset);
                case Opcode.True:
                    return module.SimpleInstruction(sb, "OP_TRUE", offset);
                case Opcode.False:
                    return module.SimpleInstruction(sb, "OP_FALSE", offset);
                case Opcode.Pop:
                    return module.SimpleInstruction(sb, "OP_POP", offset);
                case Opcode.PopN:
                    return module.VarintArgumentInstruction(sb, "OP_POP_N", offset);
                #endregion
                #region logical
                case Opcode.Equal:
                    return module.SimpleInstruction(sb, "OP_EQUAL", offset);
                case Opcode.NotEqual:
                    return module.SimpleInstruction(sb, "OP_NOT_EQUAL", offset);
                case Opcode.Greater:
                    return module.SimpleInstruction(sb, "OP_GREATER", offset);
                case Opcode.GreaterEqual:
                    return module.SimpleInstruction(sb, "OP_GREATER_EQUAL", offset);
                case Opcode.Less:
                    return module.SimpleInstruction(sb, "OP_LESS", offset);
                case Opcode.LessEqual:
                    return module.SimpleInstruction(sb, "OP_LESS_EQUAL", offset);
                case Opcode.Not:
                    return module.SimpleInstruction(sb, "OP_NOT", offset);
                #endregion
                #region arithmetic
                case Opcode.Add:
                    return module.SimpleInstruction(sb, "OP_ADD", offset);
                case Opcode.Subtract:
                    return module.SimpleInstruction(sb, "OP_SUB", offset);
                case Opcode.Multiply:
                    return module.SimpleInstruction(sb, "OP_MUL", offset);
                case Opcode.Divide:
                    return module.SimpleInstruction(sb, "OP_DIV", offset);
                case Opcode.Mod:
                    return module.SimpleInstruction(sb, "OP_MOD", offset);
                case Opcode.Negate:
                    return module.SimpleInstruction(sb, "OP_NEGATE", offset);
                #endregion
                case Opcode.Concatenate:
                    return module.SimpleInstruction(sb, "OP_CONCATENATE", offset);
                case Opcode.Print:
                    return module.SimpleInstruction(sb, "OP_PRINT", offset);
                case Opcode.GetVariable:
                    return module.VarintArgumentInstruction(sb, "OP_GET_VARIABLE", offset);
                case Opcode.SetVariable:
                    return module.VarintArgumentInstruction(sb, "OP_SET_VARIABLE", offset);
                case Opcode.BranchTrue:
                    return module.BranchInstruction(sb, "OP_BRANCH_TRUE", offset);
                case Opcode.BranchFalse:
                    return module.BranchInstruction(sb, "OP_BRANCH_FALSE", offset);
                case Opcode.BranchAlways:
                    return module.BranchInstruction(sb, "OP_BRANCH_ALWAYS", offset);
                case Opcode.Call:
                    return module.SimpleInstruction(sb, "OP_CALL", offset);
                case Opcode.Return:
                    return module.SimpleInstruction(sb, "OP_RETURN", offset);
                default:
                    sb.Append("Unknown opcode ");
                    sb.AppendLine(instruction.ToString());
                    return offset + 1;
            }
        }

        private static int SimpleInstruction(this Module module, StringBuilder sb, string name, int offset)
        {
            sb.AppendLine(name);
            return 1;
        }

        private static int ConstantInstruction(this Module module, StringBuilder sb, string name, int offset)
        {
            var count = module.Chunk.ReadVariant(offset + 1, out int constant);
            var value = module.GetConstant(constant);

            string str;
            if (value.IsObject)
                str = module.GC.GetObject(value.ObjectReference).ToString();
            else
                str = value.ToString();

            sb.AppendLine($"{name}\t\t\t{constant} '{str}'");
            return count + 1;
        }

        private static int VarintArgumentInstruction(this Module module, StringBuilder sb, string name, int offset)
        {
            var count = module.Chunk.ReadVariant(offset + 1, out int n);
            sb.AppendLine($"{name}\t\t{n}");
            return count + 1;
        }

        private static int BranchInstruction(this Module module, StringBuilder sb, string name, int offset)
        {
            var count = module.Chunk.ReadVariant(offset + 1, out int n);
            if(n > 0)
                n += count;
            sb.AppendLine($"{name}\t\t{(offset + n).ToString("X4")}");
            return count + 1;
        }
    }
}
