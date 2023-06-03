using System;
using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public enum TokenType
    {
        Invalid,

        // Single character tokens
        LeftParentheses, RightParentheses,
        Comma, Dot,
        Minus, Plus, Slash, Asterisk,

        // One or two character tokens
        Bang, BangEqual,
        Equal, EqualEqual,
        Greater, GreaterEqual,
        Less, LessEqual,

        // Literals
        Identifier, String, Integer, Float,

        // Keywords
        If, Else,
        True, False, Null,
        Structure, Function,
        And, Or,
        Loop,
        External,
        Return,
        Global,
        Variable,

        // Formatting
        NewLine, Indent,

        // Structural 
        StatementTerminator,

        // Miscelanous
        Error,

        // End of file
        EOF
    }

    public struct Token
    {
        public TokenType Type;
        public string Value;
        public int Line;
        public int Column;

        public static Token Invalid => new Token { Type = TokenType.Error, Value = "Invalid token" };
        public static Token TerminatorFromNewline(Token newline) => new Token { Type = TokenType.StatementTerminator, Value = "", Line = newline.Line, Column = newline.Column };
    }

    public class Tokenizer
    {
        string _source;
        int _tail;
        int _head;
        int _line;
        int _longestSymbolLength = 0;
        LinkedList<Token> _tokens = new LinkedList<Token>();
        Dictionary<string, TokenType> _symbols = new();

        public Tokenizer()
        {
            AddSymbol("if", TokenType.If);
            AddSymbol("else", TokenType.Else);
            AddSymbol("true", TokenType.True);
            AddSymbol("false", TokenType.False);
            AddSymbol("nil", TokenType.Null);
            AddSymbol("structure", TokenType.Structure);
            AddSymbol("function", TokenType.Function);
            AddSymbol("and", TokenType.And);
            AddSymbol("or", TokenType.Or);
            AddSymbol("loop", TokenType.Loop);
            AddSymbol("return", TokenType.Return);
            AddSymbol("external", TokenType.External);
            AddSymbol("global", TokenType.Global);
            AddSymbol("var", TokenType.Variable);
            AddSymbol("+", TokenType.Plus);
            AddSymbol("-", TokenType.Minus);
            AddSymbol("=", TokenType.Equal);
            AddSymbol("*", TokenType.Asterisk);
            AddSymbol("/", TokenType.Slash);
            AddSymbol("(", TokenType.LeftParentheses);
            AddSymbol(")", TokenType.RightParentheses);
            AddSymbol(">", TokenType.Greater);
            AddSymbol("<", TokenType.Less);
            AddSymbol("==", TokenType.EqualEqual);
            AddSymbol("!=", TokenType.BangEqual);
            AddSymbol(">=", TokenType.GreaterEqual);
            AddSymbol("<=", TokenType.LessEqual);
            AddSymbol("!", TokenType.Bang);
            AddSymbol(",", TokenType.Comma);
            AddSymbol(".", TokenType.Dot);
        }

        public LinkedList<Token> Tokenize(string source)
        {
            _source = source;
            _tail = 0;
            _head = 0;
            _line = 0;
            _tokens.Clear();
            do ScanNextToken();
            while (Last().Type != TokenType.EOF && Last().Type != TokenType.Error);
            CleanFormatting();
            return _tokens;
        }

        void ScanNextToken()
        {
            SkipWhitespace();

            if (Peek() == '\0')
            {
                AddToken(TokenType.EOF);
                return;
            }

            if (TryScanLiteralNumeric())
                return;

            if (TryScanSymbol())
                return;

            if (TryScanLiteralString())
                return;

            if (TryScanIdentifier())
                return;

            AddError("Unexpected character.");
        }

        void AddToken(TokenType type)
        {
            var oldCursor = _tail;
            if (_head == _tail) _head++;
            _head = Math.Min(_head, _source.Length);
            _tail = _head;
            _tokens.AddLast(new Token
            {
                Type = type,
                Value = _source[oldCursor.._head],
                Line = _line,
                Column = oldCursor
            });
        }

        void AddError(string message)
        {
            _tokens.AddLast(new Token
            {
                Type = TokenType.Error,
                Value = message,
                Line = _line,
                Column = _tail
            });
        }

        bool TryScanLiteralString()
        {
            if (Peek() != '\'')
                return false;

            Skip(1); // Skip the first '''

            while (Peek() != '\'')
            {
                AdvanceHead();
                if (Peek() == '\0')
                {
                    AddError("Unterminated string.");
                    return true;
                }
            }

            AddToken(TokenType.String);

            Skip(1); // Skip the last '''

            return true;
        }

        bool TryScanLiteralNumeric()
        {
            if (!char.IsDigit(Peek()))
                return false;

            AdvanceHead();

            bool isDecimal = false;
            while (true)
            {
                if (Peek() == '.')
                {
                    if (isDecimal)
                        return true;
                    else
                        isDecimal = true;
                }
                else if (!char.IsDigit(Peek())) break;

                AdvanceHead();
            }

            if (isDecimal)
                AddToken(TokenType.Float);
            else
                AddToken(TokenType.Integer);

            return true;
        }

        bool TryScanIdentifier()
        {
            var c = Peek();

            if (!char.IsLetter(c) && c != '_')
                return false;

            do
            {
                AdvanceHead();
            } while (char.IsLetterOrDigit(Peek()) || Peek() == '_');

            AddToken(TokenType.Identifier);
            return true;
        }

        bool TryScanSymbol()
        {
            AdvanceHead();

            var longestMatch = TokenType.Invalid;
            var longestMatchLength = 0;

            // Prioritize matching the longest symbol
            for (int length = 1; length < _longestSymbolLength; length++)
            {
                if (Peek() == '\0')
                    break;

                var str = CurrentSlice();
                if (_symbols.TryGetValue(str, out var match))
                {
                    longestMatch = match;
                    longestMatchLength = length;
                }

                AdvanceHead();
            }

            if (longestMatch != TokenType.Invalid)
            {
                ResetHead(longestMatchLength);
                AddToken(longestMatch);
                return true;
            }

            ResetHead();
            return false;
        }

        bool TryScanStructure(out Token token)
        {
            token = Token.Invalid;

            switch (Peek())
            {
                case '\n': AddToken(TokenType.NewLine); return true;
                case '\t': AddToken(TokenType.Indent); return true;
            }

            if (PeekSlice(4) == "    ") // 4 spaces is treated as an indent
            {
                AdvanceHead(4);
                AddToken(TokenType.Indent);
                return true;
            }

            return false;
        }

        char Peek(int offset = 0)
        {
            var next = _head + offset;
            if (next > _source.Length - 1)
                return '\0';
            return _source[next];
        }

        string PeekSlice(int length, int offset = 0)
        {

            var start = Math.Min(_head + offset, _source.Length);
            var end = Math.Min(start + length, _source.Length);
            return _source[start..end];
        }

        string CurrentSlice()
        {
            return _source[_tail.._head];
        }

        void Skip(int length = 1)
        {
            _tail += 1;
            _head = Math.Max(_head, _tail);
        }

        bool AdvanceHead(int length = 1)
        {
            _head += length;
            if (_head >= _source.Length)
                return false;

            return true;
        }

        void ResetHead(int offset = 0)
        {
            _head = _tail + offset;
        }

        void SkipWhitespace()
        {
            while (true)
            {
                var c = Peek();
                switch (c)
                {
                    case '\n':
                        AddToken(TokenType.NewLine);
                        _line++;
                        Skip();
                        break;
                    case '\t':
                        AddToken(TokenType.Indent);
                        Skip();
                        break;
                    case ' ':
                        if (PeekSlice(4) == "    ") // 4 spaces is treated as an indent
                        {
                            AdvanceHead(4);
                            AddToken(TokenType.Indent);
                            continue;
                        }
                        Skip();
                        break;
                    case '\r':
                        Skip();
                        break;
                    case '/':
                        if (Peek(1) == '/')
                        {
                            while (Peek() != '\n' && Peek() != '\0') Skip();
                        }
                        else
                        {
                            return;
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        void AddSymbol(string str, TokenType type)
        {
            _symbols.Add(str, type);
            _longestSymbolLength = Math.Max(_longestSymbolLength, str.Length);
        }

        Token Last()
        {
            return _tokens.Last.Value;
        }

        /// <summary>
        /// Remove unnecessary whitespace and newlines, and convert new lines to statement terminators.
        /// This step is called after parsing.
        /// </summary>
        void CleanFormatting()
        {
            // First pass: remove redundant indentation
            NormalizeIndentation();

            // Second pass: remove redundant newlines and replace relevant newlines with terminators
            RemoveRedundantNewLines();

            // Third pass: remove line wrapping
            RemoveLineWrapping();
        }

        /// <summary>
        /// Removes indentation that is not at the base level
        /// </summary>
        void NormalizeIndentation()
        {
            var baseDepth = GetBaseIndentation();

            if (baseDepth == 0)
                return;

            var lineDepth = 0;
            for (var node = _tokens.First; node.Next != null; node = node.Next)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.Indent:
                        if (lineDepth == baseDepth)
                            break;
                        node = RemoveNode(node);
                        lineDepth++;
                        break;
                    case TokenType.EOF:
                    case TokenType.NewLine:
                        lineDepth = 0;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Removes empty lines and replaces relevant newlines with terminators
        /// </summary>
        void RemoveRedundantNewLines()
        {
            var lineHasContent = false;
            for (var node = _tokens.First; node.Next != null; node = node.Next)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.Indent:
                        break;
                    case TokenType.EOF:
                    case TokenType.NewLine:
                        if (!lineHasContent)
                            node = RemoveNode(node);
                        else
                            node.Value = Token.TerminatorFromNewline(token);
                        lineHasContent = false;
                        break;
                    default:
                        lineHasContent = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Removes line wrapping on expressions
        /// </summary>
        void RemoveLineWrapping()
        {
            //var expressionTokens = new HashSet<TokenType> { 
            //    TokenType.And, TokenType.Or, TokenType.
            //};
            for (var node = _tokens.First; node.Next != null; node = node.Next)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.StatementTerminator:
                        if (node.Next.Value.Type == TokenType.Indent)
                        {
                            _tokens.Remove(node.Next);
                            node = RemoveNode(node);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        int GetBaseIndentation()
        {
            var baseDepth = 0;
            foreach (var token in _tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Indent:
                        baseDepth++;
                        continue;
                    case TokenType.EOF:
                    case TokenType.NewLine:
                        baseDepth = 0;
                        continue;
                    default:
                        return baseDepth;
                }
            }

            return 0;
        }

        LinkedListNode<Token> RemoveNode(LinkedListNode<Token> node)
        {
            var prev = node.Previous;
            _tokens.Remove(node);
            prev ??= _tokens.First;
            return prev;
        }
    }
}