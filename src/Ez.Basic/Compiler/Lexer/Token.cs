using System;

namespace Ez.Basic.Compiler.Lexer
{
    public readonly struct Token
    {
        public readonly TokenType Type;
        public readonly ReadOnlyMemory<char> Lexeme;
        public readonly int Line;

        public Token(TokenType type, ReadOnlyMemory<char> lexeme, int line)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
        }

        public override string ToString()
        {
            return $"[{Type}, {Lexeme}], line: {Line}";
        }
    }
}