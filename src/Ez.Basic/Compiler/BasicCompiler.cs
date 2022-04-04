using Ez.Basic.Compiler.CodeGen;
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
        private CodeGenerator m_codeGen;
        private Chunk m_compilingChunk;

        public BasicCompiler(ILogger logger)
        {
            m_logger = logger;
            m_parser = default;
            m_codeGen = default;
            m_compilingChunk = null;
        }

        public bool Compile(string source, Chunk chunk)
        {
            var scanner = new Scanner(source);
            m_compilingChunk = chunk;
            m_parser = new BasicParser(m_logger, scanner);

            m_parser.Advance();

            var block = m_parser.Block(TokenType.EoF, TokenType.EoF);

            if (m_parser.HadError)
                return false;

            m_codeGen = new CodeGenerator(m_logger, m_compilingChunk, block);
            if (!m_codeGen.CodeGen())
                return false;

            Console.WriteLine(block.ToString());

            EndCompile();
            return true;
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
