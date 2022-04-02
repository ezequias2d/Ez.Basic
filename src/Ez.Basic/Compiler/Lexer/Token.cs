using System;

namespace Ez.Basic.Compiler.Lexer
{
    public readonly ref struct Token
    {
        public readonly TokenType Type;
        public readonly ReadOnlySpan<char> Lexeme;
        public readonly int Line;

        public Token(TokenType type, ReadOnlySpan<char> lexeme, int line)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
        }

        public override string ToString()
        {
            return $"[{Type}, {Lexeme.ToString()}], line: {Line}";
        }
    }
}