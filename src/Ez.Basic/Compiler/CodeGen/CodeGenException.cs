using System;

namespace Ez.Basic.Compiler.CodeGen
{
    public class CodeGenException : Exception
    {
        public CodeGenException()
        {
        }

        public CodeGenException(string message) : base(message)
        {
        }
    }
}
