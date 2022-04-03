using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.Compiler.Parser
{
    public class ParserException : Exception
    {
        public ParserException()
        {
        }

        public ParserException(string message) : base(message)
        {
        }
    }
}
