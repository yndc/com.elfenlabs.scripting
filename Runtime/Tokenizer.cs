using System;
using System.Collections.Generic;
using Elfenlabs.Strings;

namespace Elfenlabs.Scripting
{
    public class Tokenizer
    {
        Module module;
        readonly Dictionary<string, TokenType> symbols = new();
        int longestSymbolLength = 0;
        int tail;
        int head;
        int line;
        int lastLineCharCum;

        public Tokenizer()
        {
            // Add symbols
            AddSymbol("if", TokenType.If);
            AddSymbol("then", TokenType.Then);
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
            AddSymbol("returns", TokenType.Returns);
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

        public void Tokenize(Module module)
        {
            this.module = module;
            module.Tokens = new LinkedList<Token>();
            tail = 0;
            head = 0;
            line = 1;
            lastLineCharCum = 0;
            do ScanNextToken();
            while (Last().Type != TokenType.EOF && Last().Type != TokenType.Error);
            CleanFormatting();
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

            throw NewException("Unidentified character");
        }

        void AddToken(TokenType type)
        {
            var oldCursor = tail;
            var value = module.Source[oldCursor..head];
            if (head == tail) head++;
            head = Math.Min(head, module.Source.Length);
            tail = head;
            module.Tokens.AddLast(new Token
            {
                Type = type,
                Value = value,
                Line = line,
                Column = GetColumn() - value.Length + 1
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
                    throw NewException("Unterminated string");
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
            for (int length = 1; length < longestSymbolLength; length++)
            {
                if (Peek() == '\0')
                    break;

                var str = CurrentSlice();
                if (symbols.TryGetValue(str, out var match))
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
            var next = head + offset;
            if (next > module.Source.Length - 1)
                return '\0';
            return module.Source[next];
        }

        string PeekSlice(int length, int offset = 0)
        {

            var start = Math.Min(head + offset, module.Source.Length);
            var end = Math.Min(start + length, module.Source.Length);
            return module.Source[start..end];
        }

        string CurrentSlice()
        {
            return module.Source[tail..head];
        }

        void Skip(int length = 1)
        {
            tail += 1;
            head = Math.Max(head, tail);
        }

        bool AdvanceHead(int length = 1)
        {
            head += length;
            if (head >= module.Source.Length)
                return false;

            return true;
        }

        void ResetHead(int offset = 0)
        {
            head = tail + offset;
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
                        line++;
                        lastLineCharCum = tail;
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
            symbols.Add(str, type);
            longestSymbolLength = Math.Max(longestSymbolLength, str.Length);
        }

        Token Last()
        {
            return module.Tokens.Last.Value;
        }

        /// <summary>
        /// Remove unnecessary whitespace and newlines, and convert new lines to statement terminators.
        /// This step is called after parsing.
        /// </summary>
        void CleanFormatting()
        {
            // First pass: normalize base indentation
            NormalizeIndentation();

            // Second pass: remove redundant newlines and replace relevant newlines with terminators
            RemoveRedundantNewLines();

            // Third pass: remove line wrapping
            //RemoveLineWrapping();

            // Fourth pass: remove redundant indentation
            RemoveRedundantIndents();
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
            for (var node = module.Tokens.First; node.Next != null; node = node.Next)
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
            for (var node = module.Tokens.First; node.Next != null; node = node.Next)
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
        /// Remove all indentations that are not at the beginning of a line
        /// </summary>
        void RemoveRedundantIndents()
        {
            var isNewLine = true;
            for (var node = module.Tokens.First; node.Next != null; node = node.Next)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.Indent:
                        if (!isNewLine)
                            node = RemoveNode(node);
                        break;
                    case TokenType.StatementTerminator:
                        isNewLine = true;
                        break;
                    default:
                        isNewLine = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Removes line wrapping on expressions
        /// </summary>
        void RemoveLineWrapping()
        {
            for (var node = module.Tokens.First; node.Next != null; node = node.Next)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.StatementTerminator:
                        if (node.Next.Value.Type == TokenType.Indent)
                        {
                            module.Tokens.Remove(node.Next);
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
            foreach (var token in module.Tokens)
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

        int GetColumn()
        {
            return tail - lastLineCharCum;
        }

        Location GetLocation()
        {
            return new Location { Line = line, Column = GetColumn() };
        }

        LinkedListNode<Token> RemoveNode(LinkedListNode<Token> node)
        {
            var prev = node.Previous;
            module.Tokens.Remove(node);
            prev ??= module.Tokens.First;
            return prev;
        }

        TokenizerException NewException(string message)
        {
            return new TokenizerException(module, GetLocation(), message);
        }
    }

    public struct Location
    {
        public int Line;
        public int Column;
    }

    public class TokenizerException : Exception
    {
        public Module Module { get; set; }
        public Location Location { get; set; }
        public override string Message
        {
            get
            {
                return $@"
                    line: {Location.Line}, col: {Location.Column}
                    {CompilerUtility.GenerateSourcePointer(Module, Location)}
                    {base.Message}  
                ".AutoTrim();
            }
        }

        public TokenizerException(Module module, Location location, string message) : base(message) { }
    }
}