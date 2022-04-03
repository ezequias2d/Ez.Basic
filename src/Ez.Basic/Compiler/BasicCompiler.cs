using Ez.Basic.Compiler.Lexer;
using Ez.Basic.Compiler.Parser;
using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;
using System;

namespace Ez.Basic
{
    public ref struct BasicCompiler
    {
        private ILogger m_logger;

        private BasicParser m_parser;
        private Chunk m_compilingChunk;

        public BasicCompiler(ILogger logger)
        {
            m_logger = logger;
            m_parser = default;
            m_compilingChunk = null;
        }

        public bool Compile(string source, Chunk chunk)
        {
            var scanner = new Scanner(source);
            m_compilingChunk = chunk;
            m_parser = new BasicParser(m_logger, scanner);

            m_parser.Advance();
            var block = m_parser.Block(TokenType.EoF, TokenType.EoF);

            Console.WriteLine(block.ToString());

            //m_parser.Consume(TokenType.EoF, "Expect end of expression.");

            //EndCompile();
            return !m_parser.HadError;
        }

        private void EndCompile()
        {
            EmitReturn();
        }

        internal Chunk CompilingChunk => m_compilingChunk;

        public void Emit<T>(T value) where T : unmanaged
        {
            CompilingChunk.Write(value, m_parser.Current.Line);
        }

        private void EmitReturn()
        {
            Emit(Opcode.Return);
        }
    }
}
