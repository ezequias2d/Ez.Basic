using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.Compiler.Lexer
{
    public ref struct Scanner
    {
        private readonly Trie<TokenType> m_keywords;
        private ReadOnlyMemory<char> m_source;
        private int m_current;
        private int m_line; 

        public Scanner(string source)
        {
            m_source = source.AsMemory();
            m_current = 0;
            m_line = 0;
            m_keywords = MakeKeywordsTrie();
        }

        public Token ScanToken()
        {
            SkipWhitespace();
            m_source = m_source.Slice(m_current);
            m_current = 0;

            if (IsAtEnd())
                return MakeToken(TokenType.EoF);

            var c = Advance();

            if (IsAlpha(c)) return Identifier();
            if (char.IsDigit(c)) return Number();

            switch(c)
            {
                case '(': return MakeToken(TokenType.LeftParen);
                case ')': return MakeToken(TokenType.RightParen);
                case '[': return MakeToken(TokenType.LeftBracket);
                case ']': return MakeToken(TokenType.RightBracket);
                case ',': return MakeToken(TokenType.Comma);
                case '.': return MakeToken(TokenType.Dot);
                case '-': return MakeToken(TokenType.Minus);
                case '+': return MakeToken(TokenType.Plus);
                case '/': return MakeToken(TokenType.Slash);
                case '*': return MakeToken(TokenType.Star);
                case '!': return MakeToken(Match('=') 
                                            ? TokenType.BangEqual 
                                            : TokenType.Bang);
                case '=': return MakeToken(Match('=')
                                            ? TokenType.EqualEqual
                                            : TokenType.Equal);
                case '>': return MakeToken(Match('=')
                                            ? TokenType.GreaterEqual
                                            : TokenType.Greater);
                case '<': return MakeToken(Match('=')
                                            ? TokenType.LessEqual
                                            : TokenType.Less);
                case '"': return String();
            }

            return ErrorToken("Unexpected character.");
        }

        private Token Identifier()
        {
            while (IsAlpha(Peek()) || char.IsDigit(Peek()))
                Advance();
            return MakeToken(IdentifierType());
        }

        private TokenType IdentifierType()
        {
            if (m_keywords.TryGet(m_source.Slice(0, m_current), out var result))
                return result;
            return TokenType.Identifier;
        }

        private bool IsAlpha(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private Token Number()
        {
            while (char.IsDigit(Peek()))
                Advance();

            // look for fractional part
            if (Peek() == '.' && char.IsDigit(PeekNext()))
            {
                // consume '.'
                Advance();

                while (char.IsDigit(Peek()))
                    Advance();
            }

            return MakeToken(TokenType.Number);
        }

        private Token String()
        {
            while(Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n')
                    m_line++;
                Advance();
            }

            if (IsAtEnd())
                return ErrorToken("Unterminated string.");

            // the closing quote
            Advance();
            return MakeToken(TokenType.String);
        }

        private void SkipWhitespace()
        {
            while(true)
            {
                var c = Peek();
                switch(c)
                {
                    case ' ':
                    case '\r':
                    case '\t':
                        Advance();
                        break;
                    case '\n':
                        m_line++;
                        Advance();
                        break;
                    case ';':
                        while (Peek() != '\n' && !IsAtEnd())
                            Advance();
                        break;
                    default:
                        return;
                }
            }
        }

        private char Peek()
        {
            if (m_current == m_source.Length)
                return '\0';
            return m_source.Span[m_current];
        }

        private char PeekNext()
        {
            if (IsAtEnd())
                return '\0';
            return m_source.Span[m_current + 1];
        }

        private bool Match(char v)
        {
            if (IsAtEnd() || m_source.Span[m_current] != v)
                return false;
            m_current++;
            return true;
        }

        private char Advance()
        {
            return m_source.Span[m_current++];
        }

        private Token ErrorToken(string message)
        {
            return new Token(TokenType.Error, message.AsMemory(), m_line);
        }

        private Token MakeToken(TokenType type)
        {
            return new Token(type, m_source.Slice(0, m_current), m_line);
        }

        public bool IsAtEnd()
        {
            return m_source.Length == m_current;
        }

        private static Trie<TokenType> MakeKeywordsTrie()
        {
            return new Trie<TokenType>(
                        ("true"  , TokenType.True),
                        ("false" , TokenType.False),
                        ("null"  , TokenType.Null),
                        ("is"    , TokenType.Is),
                        ("and"   , TokenType.And),
                        ("or"    , TokenType.Or),
                        ("xor"   , TokenType.Xor),
                        ("not"   , TokenType.Not),
                        ("def"   , TokenType.Def),
                        ("mould" , TokenType.Mould),
                        ("module", TokenType.Module),
                        ("global", TokenType.Global),
                        ("this"  , TokenType.This),
                        ("base"  , TokenType.Base),
                        ("let"   , TokenType.Let),
                        ("const" , TokenType.Const),
                        ("for"   , TokenType.For),
                        ("do"    , TokenType.Do),
                        ("while" , TokenType.While),
                        ("until" , TokenType.Until),
                        ("goto"  , TokenType.Goto),
                        ("if"    , TokenType.If),
                        ("next"  , TokenType.Next),
                        ("return", TokenType.Return),
                        ("end"   , TokenType.End),
                        ("print" , TokenType.Print),
                        ("input" , TokenType.Input),
                        ("clear" , TokenType.Clear));
        }
    }
}
