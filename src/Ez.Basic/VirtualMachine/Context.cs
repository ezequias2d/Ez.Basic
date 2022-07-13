namespace Ez.Basic.VirtualMachine
{
    public struct Context
    {
        public int PC;
        public Chunk Chunk;

        public Context(int pc, Chunk chunk)
        {
            PC = pc;
            Chunk = chunk;
        }
    }

    public static class ContextExtensions
    {
        public static int ReadVariant(this ref Context context)
        {
            context.PC += context.Chunk.ReadVariant(context.PC, out var n);
            return n;
        }

        public static Opcode ReadOpcode(this ref Context context)
        {
            return context.Chunk.Read<Opcode>(context.PC++);
        }

        public static int GetLine(this ref Context context)
        {
            return context.Chunk.LineNumberTable.GetLine(context.PC);
        }
    }
}