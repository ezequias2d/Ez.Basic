using Ez.Basic.Compiler.CodeGen;
using Ez.Basic.Compiler.Lexer;
using Ez.Basic.Compiler.Parser;
using Ez.Basic.VirtualMachine;
using Microsoft.Extensions.Logging;

namespace Ez.Basic
{
    public ref struct BasicCompiler
    {
        private ILogger m_logger;

        private BasicParser m_parser;
        private CodeGenerator m_codeGen;

        public BasicCompiler(ILogger logger)
        {
            m_logger = logger;
            m_parser = default;
            m_codeGen = default;
        }

        public Module Compile(string source, GC gc, bool debug = true)
        {
            var module = new Module(gc, debug);
            var scanner = new Scanner(source);
            m_parser = new BasicParser(m_logger, scanner);

            m_parser.Advance();

            var block = m_parser.Block(false, TokenType.EoF, TokenType.EoF);

            if (m_parser.HadError)
                return null;

            m_codeGen = new CodeGenerator(m_logger, module);
            if (!m_codeGen.CodeGen(block))
                return null;

            System.Console.WriteLine(block.ToString());

            EndCompile();
            return module;
        }

        private void EndCompile()
        {
            EmitReturn();
        }

        public void Emit<T>(T value) where T : unmanaged
        {
            //CompilingChunk.Write(value, m_parser.Current.Line);
        }

        private void EmitReturn()
        {
            Emit(Opcode.Return);
        }
    }
}
