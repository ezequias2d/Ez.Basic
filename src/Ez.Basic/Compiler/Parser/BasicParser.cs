using Ez.Basic.Compiler.Lexer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static Ez.Basic.Compiler.Parser.Ast;

namespace Ez.Basic.Compiler.Parser
{
    public ref struct BasicParser
    {
        private readonly Stack<Ast.Node> m_pool;
        private readonly ILogger Logger;
        private Scanner Scanner;
        private Token m_current;
        private Token m_previous;
        public bool HadError;

        public BasicParser(ILogger logger, Scanner scanner)
        {
            m_pool = new Stack<Ast.Node>();
            Logger = logger;
            Scanner = scanner;
            m_current = m_previous = default;
            HadError = false;
        }

        public Token Current => m_current;
        public Token Previous => m_previous;

        public void Advance()
        {
            m_previous = Current;

            while(true)
            {
                m_current = Scanner.ScanToken();

                if (Current.Type != TokenType.Error)
                    break;

                ErrorAtCurrent(Current.Lexeme.ToString());
            }
        }

        public void Consume(TokenType type, string message)
        {
            if(Current.Type == type)
            {
                Advance();
                return;
            }

            ErrorAtCurrent(message);
        }

        public void ConsumeAny(TokenType type1, TokenType type2, string message)
        {
            if (Current.Type == type1 || Current.Type == type2)
            {
                Advance();
                return;
            }

            ErrorAtCurrent(message);
        }

        private void ErrorAtCurrent(string message)
        {
            ErrorAt(Current, message);
        }

        private void Error(string message)
        {
            ErrorAt(Current, message);
        }

        private ParserException ErrorAt(Token token, string message)
        {
            Debug.Assert(false);
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

            Logger?.LogError(sb.ToString());
            HadError = true;

            return new ParserException();
        }

        private void Synchronize()
        {
            Advance();
            //while(!Scanner.IsAtEnd())
            //{
            //    switch(Current.Type)
            //    {
            //        case TokenType.Module:
            //        case TokenType.Mould:
            //        case TokenType.Def:
            //        case TokenType.Let:
            //        case TokenType.For:
            //        case TokenType.If:
            //        case TokenType.While:
            //        case TokenType.Print:
            //        case TokenType.Return:
            //            return;
            //    }
            //    Advance();
            //}
        }

        internal Node Statement(bool returnValue)
        {
            if (Match(TokenType.For)) return ForStatement(returnValue);
            if (Match(TokenType.If)) return IfStatement(returnValue);
            if (Match(TokenType.Print)) return PrintStatement();
            if (Match(TokenType.Return)) return ReturnStatement(returnValue);
            if (Match(TokenType.While)) return WhileStatement(returnValue);
            if (Match(TokenType.Until)) return UntilStatement(returnValue);
            if (Match(TokenType.Do)) return Block(returnValue, TokenType.Next);

            return ExpressionStatement();
        }

        internal Node ForStatement(bool returnValue)
        {
            var token = Previous;

            Consume(TokenType.Identifier, "Expect a identifier to interate.");
            var name = Previous;

            Consume(TokenType.Equal, "Expect a initializer.");
            var initializer = Expression();

            Consume(TokenType.To, "Expect a limitation");
            var to = Expression();

            Node step = null;
            if(Match(TokenType.Step))
                step = Expression();

            var mainBody = Block(returnValue, TokenType.Next);
            Node body = mainBody;


            body = MakeBlock(token, mainBody.EndToken, new Node[]
            {
                body,
                step != null 
                    ? MakeIncrementStatement(step.Token, name, step) 
                    : MakeIncrementStatement(token, name)
            });

            // create loop while
            var condition = MakeEqualExpression(to.Token, name, to);
            body = MakeUntil(token, condition, body);

            // create let declaration and initializer.
            body = MakeBlock(token, mainBody.EndToken, new Node[]
            {
                MakeLet(name, initializer),
                body
            });

            return body;
        }

        internal Node IfStatement(bool returnValue)
        {
            var token = Previous;
            var condition = Expression();

            var thenBranch = Block(returnValue, TokenType.Next, TokenType.Else);
            Node elseBranch = null;

            if(thenBranch.EndToken.Type == TokenType.Else)
            {
                if(Match(TokenType.If))
                    elseBranch = IfStatement(returnValue);
                else
                    elseBranch = Block(returnValue, TokenType.Next);
            }

            return MakeIf(token, condition, thenBranch, elseBranch);
        }

        internal Node PrintStatement()
        {
            var printToken = Previous;
            Node expr = Expression();
            while (Match(TokenType.Comma))
            {
                var op = Previous;
                var right = Expression();
                expr = MakeBinary(op, expr, right);
            }

            return MakeStatement(NodeKind.Print, printToken, expr);
        }

        internal Node ReturnStatement(bool returnValue)
        {
            var keyword = Previous;

            Node value = null;
            if(returnValue)
                value = Expression();

            return MakeStatement(NodeKind.Return, keyword, value);
        }

        internal Node WhileStatement(bool returnValue) 
        {
            var token = Previous;
            var codition = Expression();
            var body = Block(returnValue, TokenType.Next);

            return MakeWhile(token, codition, body);
        }

        internal Node UntilStatement(bool returnValue)
        {
            var token = Previous;
            var codition = Expression();
            var body = Block(returnValue, TokenType.Next);

            return MakeUntil(token, codition, body);
        }

        internal Node ExpressionStatement()
        {
            var expr = Expression();
            return MakeStatement(NodeKind.Expr, expr.Token, expr);
        }

        internal Node Declaration(bool returnValue)
        {
            try
            {
                if (Match(TokenType.Def))
                    return FunctionDeclaration(NodeKind.Def, true);
                if (Match(TokenType.Sub))
                    return FunctionDeclaration(NodeKind.Sub, false);
                if (Match(TokenType.Let))
                    return LetDeclaration();

                return Statement(returnValue);
            }
            catch (ParserException e)
            {
                Synchronize();
                return null;
            }
        }

        internal Node FunctionDeclaration(NodeKind kind, bool returnValue)
        {
            Consume(TokenType.Identifier, "Expect a identifier for function.");
            var name = Previous;

            Consume(TokenType.LeftParen, "Expect a '(' after function name.");
            var parameters = Array.Empty<Token>();
            if(!Check(TokenType.RightParen))
            {
                var list = new List<Token>();
                do
                {
                    if (list.Count >= 255)
                        throw ErrorAt(Current, "Can't have more than 255 parameters.");

                    Consume(TokenType.Identifier, "Expect parameter name.");
                    list.Add(Previous);
                } while (Match(TokenType.Comma));
                parameters = list.ToArray();
            }

            Consume(TokenType.RightParen, "Expect ')' after parameters.");
            var body = Block(returnValue, TokenType.End);

            return MakeFunction(kind, name, parameters, body);
        }

        internal Node LetDeclaration()
        {
            Consume(TokenType.Identifier, "Expect a identifier for variable.");
            var name = Previous;

            Node initializer = null;
            if (Match(TokenType.Equal))
                initializer = Expression();

            return MakeLet(name, initializer);
        }

        internal Node Block(bool returnValue, TokenType end, TokenType alternativeEnd = TokenType.None)
        {
            var token = Previous;
            var statements = new List<Node>();

            while(!Check(end) && !Check(alternativeEnd) && !Scanner.IsAtEnd())
                statements.Add(Declaration(returnValue));

            ConsumeAny(end, alternativeEnd, $"Expect '{end}' after the code block.");

            return MakeBlock(token, Previous, statements.ToArray());
        }

        internal Node Expression()
        {
            return Assignment();
        }

        internal Node Assignment()
        {
            var expr = Or();
            
            if(Match(TokenType.Equal))
            {
                var equal = Previous;
                var right = Assignment();

                if (expr.Type.Kind == NodeKind.Variable)
                {
                    return MakeAssign(equal, expr, right);
                }

                ErrorAt(equal, "Invalid assignment target.");
            }

            return expr;
        }

        internal Node Or()
        {
            var expr = Xor();

            while(Match(TokenType.Or))
            {
                var op = Previous;
                var right = Xor();
                expr = MakeLogical(op, expr, right);
            }

            return expr;
        }

        internal Node Xor()
        {
            var expr = And();

            while(Match(TokenType.Xor))
            {
                var op = Previous;
                var right = And();
                expr = MakeLogical(op, expr, right);
            }

            return expr;
        }

        internal Node And()
        {
            var expr = Equality();

            while (Match(TokenType.And))
            {
                var op = Previous;
                var right = Equality();
                expr = MakeLogical(op, expr, right);
            }

            return expr;
        }

        internal Node Equality()
        {
            var expr = Comparison();

            while(Match(TokenType.BangEqual, TokenType.EqualEqual))
            {
                var op = Previous;
                var right = Comparison();

                expr = MakeBinary(op, expr, right);
            }

            return expr;
        }

        internal Node Comparison()
        {
            var expr = Term();

            while(Match(TokenType.Greater,
                TokenType.GreaterEqual, 
                TokenType.Less, 
                TokenType.LessEqual))
            {
                var op = Previous;
                var right = Term();

                expr = MakeBinary(op, expr, right);
            }

            return expr;
        }

        internal Node Term()
        {
            var expr = Factor();

            while(Match(TokenType.Minus, TokenType.Plus))
            {
                var op = Previous;
                var right = Factor();
                
                expr = MakeBinary(op, expr, right);
            }

            return expr;
        }

        internal Node Factor()
        {
            var expr = Unary();

            while(Match(TokenType.Slash, TokenType.Star, TokenType.Mod))
            {
                var op = Previous;
                var right = Unary();
                
                expr = MakeBinary(op, expr, right);
            }

            return expr;
        }

        internal Node Unary()
        {
            if(Match(TokenType.Bang, TokenType.Minus))
            {
                var op = Previous;
                var right = Unary();
                var type = new NodeType(NodeClass.Expr, NodeKind.Unary);
                return GetNode(type, op, null, right);
            }

            return Call();
        }

        internal Node Call()
        {
            var expr = Primary();
            while(Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr);
            }

            return expr;
        }

        private Node FinishCall(Node callee)
        {
            if (Match(TokenType.RightParen))
                return MakeCall(Previous, callee, Array.Empty<Node>());

            var arguments = new List<Node>();
            do
            {
                if (arguments.Count >= 255)
                    throw ErrorAt(m_current, "Can't have more than 255 arguments.");

                arguments.Add(Expression());
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Expect ')' after artuments.");
            return MakeCall(Previous, callee, arguments.ToArray());
        }

        internal Node Primary()
        {
            if (Match(TokenType.False, TokenType.True))
                return MakeBool(Previous);

            if (Match(TokenType.Null))
                return MakeNull(Previous);

            if (Match(TokenType.Number))
                return MakeNumber(Previous);

            if (Match(TokenType.String))
                return MakeString(Previous);

            if (Match(TokenType.Identifier))
                return MakeVariable(Previous);

            if(Match(TokenType.LeftParen))
            {
                var expr = Expression(); 
                Consume(TokenType.RightParen, "Expect ')' after expression.");
                return expr;
            }

            throw ErrorAt(Current, "Expect expression");
        }

        private Node GetNode(NodeType type, Token token, Node childLeft = null, Node childRight = null, Node condition = null)
        {
            if (m_pool.TryPop(out var result))
            {
                result.Type = type;
                result.Token = token;
                result.ChildLeft = childLeft;
                result.ChildRight = childRight;
                result.Condition = condition;

                return result;
            }
            return new Node(type, token, childLeft, childRight, condition);
        }

        private bool Match(TokenType type1, TokenType type2, TokenType type3, TokenType type4)
        {
            if (Check(type1) || Check(type2) || Check(type3) || Check(type4))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Match(TokenType type1, TokenType type2, TokenType type3)
        {
            if (Check(type1) || Check(type2) || Check(type3))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Match(TokenType type1, TokenType type2)
        {
            if (Check(type1) || Check(type2))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Match(TokenType type)
        {
            if(Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            return m_current.Type == type;
        }

        private Node MakeStatement(NodeKind kind, Token token, Node args)
        {
            var type = new NodeType(NodeClass.Stmt, kind);
            return GetNode(type, token, args);
        }

        private Node MakeIf(Token token, Node condition, Node thenBranch, Node elseBranch)
        {
            var type = new NodeType(NodeClass.Stmt, NodeKind.If);
            return GetNode(type, token, thenBranch, elseBranch, condition);
        }

        private Node MakeFunction(NodeKind kind, Token name, Token[] parameters, Node body)
        {
            var type = new NodeType(NodeClass.Stmt, kind);
            var node = GetNode(type, name, null, body);
            node.Parameters = parameters;
            return node;
        }

        private Node MakeLet(Token name, Node initializer)
        {
            return MakeStatement(NodeKind.Let, name, initializer);
        }

        private Node MakeWhile(Token token, Node condition, Node body)
        {
            var type = new NodeType(NodeClass.Stmt, NodeKind.While);
            return GetNode(type, token, body, null, condition);
        }

        private Node MakeUntil(Token token, Node condition, Node body)
        {
            var type = new NodeType(NodeClass.Stmt, NodeKind.Until);
            return GetNode(type, token, body, null, condition);
        }

        private Node MakeBlock(Token token, Token endToken, Node[] statements)
        {
            var type = new NodeType(NodeClass.Stmt, NodeKind.Block);
            var node = GetNode(type, token);
            node.EndToken = endToken;
            node.Data = statements;
            return node;
        }

        private Node MakeCall(Token token, Node callee, Node[] arguments)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Call);
            var node = GetNode(type, token, callee);
            node.Data = arguments;
            return node;
        }

        private Node MakeBinary(Token op, Node left, Node right)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Binary);
            return GetNode(type, op, childLeft: left, childRight: right);
        }

        private Node MakeLogical(Token op, Node left, Node right)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Logical);
            return GetNode(type, op, childLeft: left, childRight: right);
        }

        private Node MakeAssign(Token op, Node left, Node right)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Assign);
            return GetNode(type, op, left, right);
        }

        private Node MakeBool(Token token)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Literal, LiteralType.Bool);
            return GetNode(type, token);
        }

        private Node MakeNull(Token token)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Literal, LiteralType.Null);
            return GetNode(type, token);
        }

        private Node MakeNumber(Token token)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Literal, LiteralType.Number);
            return GetNode(type, token);
        }

        private Node MakeString(Token token)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Literal, LiteralType.String);
            return GetNode(type, token);
        }

        private Node MakeVariable(Token token)
        {
            var type = new NodeType(NodeClass.Expr, NodeKind.Variable);
            return GetNode(type, token);
        }

        private Node MakeIncrementStatement(Token mainToken, Token name, Node expr)
        {
            var variable = MakeVariable(name);
            var sum = MakeBinary(new Token(TokenType.Plus, mainToken.Lexeme, mainToken.Line), variable, expr);
            var assign = MakeAssign(new Token(TokenType.Plus, mainToken.Lexeme, mainToken.Line), variable, sum);

            return MakeStatement(NodeKind.Expr, mainToken, assign);
        }

        private Node MakeIncrementStatement(Token mainToken, Token name)
        {
            var expr = MakeNumber(new Token(TokenType.Number, "1".AsMemory(), mainToken.Line));
            return MakeIncrementStatement(mainToken, name, expr);
        }

        private Node MakeEqualExpression(Token mainToken, Token name, Node expr)
        {
            var variable = MakeVariable(name);
            var comparison = MakeBinary(new Token(TokenType.EqualEqual, mainToken.Lexeme, mainToken.Line), variable, expr);
            return comparison;
        }
    }
}
