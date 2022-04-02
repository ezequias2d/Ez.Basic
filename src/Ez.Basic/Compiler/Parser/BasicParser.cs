using Ez.Basic.Compiler.Lexer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.Compiler.Parser
{
    public ref struct BasicParser
    {
        private readonly ILogger Logger;
        private Scanner Scanner;
        private Token Current;
        private Token Previous;
        public bool HadError;

        public BasicParser(ILogger logger, Scanner scanner)
        {
            Logger = logger;
            Scanner = scanner;
            Current = Previous = default;
            HadError = false;
        }
        
        private void Advance()
        {
            Previous = Current;

            while(true)
            {
                Current = Scanner.ScanToken();

                if (Current.Type != TokenType.Error)
                    break;

                ErrorAtCurrent(Current.Lexeme.ToString());
            }
        }

        private void ErrorAtCurrent(string message)
        {
            ErrorAt(Current, message);
        }

        private void Error(string message)
        {
            ErrorAt(Previous, message);
        }

        private void ErrorAt(Token token, string message)
        {
            var sb = new StringBuilder();

            sb.Append($"[line {token.Line}] Error");

            if (token.Type == TokenType.EoF)
                sb.Append(" at end");
            else if (token.Type == TokenType.Error)
            {

            }
            else
                sb.Append($" at '{token.Lexeme.ToString()}'");

            sb.Append(": ");
            sb.AppendLine(message);
            HadError = true;
        }
    }
}
