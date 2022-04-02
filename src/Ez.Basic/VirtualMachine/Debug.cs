using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine
{
    public static class Debug
    {
        public static void DisassembleChunk(this Chunk chunk, StringBuilder sb, string name)
        {
            sb.Append("== ");
            sb.Append(name);
            sb.Append(" ==\n");

            for (var offset = 0; offset < chunk.Count;)
            {
                offset += chunk.DisassembleInstruction(sb, offset);
            }
        }

        internal static int DisassembleInstruction(this Chunk chunk, StringBuilder sb, int offset)
        {
            sb.Append(offset.ToString("X4"));

            // add line number
            var line = chunk.LineNumberTable.GetLine(offset);
            if (offset > 0 &&
                line == chunk.LineNumberTable.GetLine(offset - 1))
                sb.Append("   | ");
            else
                sb.Append(line.ToString("D4"));

            sb.Append(" ");

            var instruction = chunk.Read<Opcode>(offset);
            switch (instruction)
            {
                case Opcode.Constant:
                    return chunk.ConstantInstruction(sb, "OP_CONSTANT", offset);
                case Opcode.Add:
                    return chunk.SimpleInstruction(sb, "OP_ADD", offset);
                case Opcode.Subtract:
                    return chunk.SimpleInstruction(sb, "OP_SUB", offset);
                case Opcode.Multiply:
                    return chunk.SimpleInstruction(sb, "OP_MUL", offset);
                case Opcode.Divide:
                    return chunk.SimpleInstruction(sb, "OP_DIV", offset);
                case Opcode.Negate:
                    return chunk.SimpleInstruction(sb, "OP_NEGATE", offset);
                case Opcode.Return:
                    return chunk.SimpleInstruction(sb, "OP_RETURN", offset);
                default:
                    sb.Append("Unknown opcode ");
                    sb.AppendLine(instruction.ToString());
                    return offset + 1;
            }
        }

        private static int SimpleInstruction(this Chunk chunk, StringBuilder sb, string name, int offset)
        {
            sb.AppendLine(name);
            return 1;
        }

        private static int ConstantInstruction(this Chunk chunk, StringBuilder sb, string name, int offset)
        {
            var count = chunk.ReadVariant(offset, out int constant);
            sb.AppendLine($"{name}\t\t{constant} '{chunk.GetConstant(constant)}'");
            return count + 1;
        }
    }
}
