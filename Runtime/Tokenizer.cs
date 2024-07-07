using System;
using System.Collections.Generic;
using Codice.Client.Commands.WkTree;
using Elfenlabs.Strings;

namespace Elfenlabs.Scripting
{
    public class Tokenizer
    {
        Module module;
        readonly Dictionary<string, TokenType> symbols = new();
        Stack<bool> braceInterpolation = new();
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
            AddSymbol("field", TokenType.Field);
            AddSymbol("function", TokenType.Function);
            AddSymbol("and", TokenType.And);
            AddSymbol("or", TokenType.Or);
            AddSymbol("while", TokenType.While);
            AddSymbol("return", TokenType.Return);
            AddSymbol("returns", TokenType.Returns);
            AddSymbol("break", TokenType.Break);
            AddSymbol("continue", TokenType.Continue);
            AddSymbol("external", TokenType.External);
            AddSymbol("global", TokenType.Global);
            AddSymbol("var", TokenType.Variable);
            AddSymbol("module", TokenType.Module);
            AddSymbol("use", TokenType.Module);
            AddSymbol("+", TokenType.Plus);
            AddSymbol("-", TokenType.Minus);
            AddSymbol("=", TokenType.Equal);
            AddSymbol("*", TokenType.Asterisk);
            AddSymbol("%", TokenType.Remainder);
            AddSymbol("/", TokenType.Slash);
            AddSymbol("(", TokenType.LeftParentheses);
            AddSymbol(")", TokenType.RightParentheses);
            AddSymbol("{", TokenType.LeftBrace);
            AddSymbol("}", TokenType.RightBrace);
            AddSymbol("[", TokenType.LeftBracket);
            AddSymbol("]", TokenType.RightBracket);
            AddSymbol(">", TokenType.Greater);
            AddSymbol("<", TokenType.Less);
            AddSymbol("==", TokenType.EqualEqual);
            AddSymbol("!=", TokenType.BangEqual);
            AddSymbol(">=", TokenType.GreaterEqual);
            AddSymbol("<=", TokenType.LessEqual);
            AddSymbol("++", TokenType.Increment);
            AddSymbol("--", TokenType.Decrement);
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

        /// <summary>
        /// Scans the next token
        /// </summary>
        void ScanNextToken()
        {
            if (TryScanIndent())
                return;

            SkipWhitespace();
            SkipComments();

            if (Peek() == '\0')
            {
                AdvanceToken(TokenType.EOF);
                return;
            }

            if (TryScanStringInterpolation())
                return;

            if (TryScanNewLine())
                return;

            if (TryScanLiteralNumeric())
                return;

            if (TryScanSymbol())
                return;

            if (TryScanLiteralString())
                return;

            if (TryScanIdentifier())
                return;

            throw NewException($"Unidentified character: '{module.Source[tail]}'");
        }

        /// <summary>
        /// Adds a token to the tokens chain
        /// </summary>
        /// <param name="type"></param>
        void AdvanceToken(TokenType type)
        {
            var value = module.Source[tail..head];
            var position = tail;
            tail = head;

            module.Tokens.AddLast(new Token
            {
                Module = module,
                Type = type,
                Value = value,
                Line = line,
                Position = position,
                Column = GetColumn() - value.Length
            });
        }

        /// <summary>
        /// Handles braces for string interpolation
        /// </summary>
        /// <returns></returns>
        bool TryScanStringInterpolation()
        {
            if (Peek() == '{')
                braceInterpolation.Push(false);
            if (Peek() == '}')
            {
                var isStringInterpolation = braceInterpolation.Pop();
                if (isStringInterpolation)
                {
                    AdvanceHead();
                    AdvanceToken(TokenType.StringInterpolationTerminator);
                    ScanLiteralString();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Try scanning for indents
        /// </summary>
        /// <returns></returns>
        bool TryScanIndent()
        {
            if (PeekSlice(4) == "    ") // 4 spaces is treated as an indent
            {
                AdvanceHead(4);
                AdvanceToken(TokenType.Indent);
                return true;
            }

            if (Peek() == '\t') // Tab is treated as an indent
            {
                AdvanceHead();
                AdvanceToken(TokenType.Indent);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try scanning for new lines
        /// </summary>
        /// <returns></returns>
        bool TryScanNewLine()
        {
            if (Peek() == '\n')
            {
                AdvanceHead();
                AdvanceToken(TokenType.NewLine);
                line++;
                lastLineCharCum = head;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try scanning for string literal
        /// </summary>
        /// <returns></returns>
        bool TryScanLiteralString()
        {
            // Start parsing characters as string literals when we find backticks OR right-braces 
            if (Peek() != '`')
                return false;

            // Parse characters as string literals until we find backticks to enclose the string
            // Or a single '{' to indicate a string interpolation

            AdvanceTail(1); // Skip the first '`'
            ScanLiteralString();
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
                AdvanceToken(TokenType.Float);
            else
                AdvanceToken(TokenType.Integer);

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

            AdvanceToken(TokenType.Identifier);
            return true;
        }

        bool TryScanSymbol()
        {
            AdvanceHead();

            var longestMatch = TokenType.Invalid;
            var longestMatchLength = 0;

            // Prioritize matching the longest symbol
            for (int length = 1; length <= longestSymbolLength; length++)
            {
                var str = CurrentSlice();
                if (symbols.TryGetValue(str, out var match))
                {
                    longestMatch = match;
                    longestMatchLength = length;
                }

                if (Peek() == '\0')
                    break;

                AdvanceHead();
            }

            if (longestMatch != TokenType.Invalid)
            {
                ResetHead(longestMatchLength);
                AdvanceToken(longestMatch);
                return true;
            }

            ResetHead();
            return false;
        }


        void ScanLiteralString()
        {
            while (true)
            {
                var c = Peek();
                switch (c)
                {
                    case '\0':
                        throw NewException("Unterminated string");
                    case '\n':
                        line++;
                        lastLineCharCum = head;
                        break;
                    case '`':
                        AdvanceToken(TokenType.String);
                        AdvanceTail(); // Skip the last '`'
                        return;
                    case '{':
                        AdvanceToken(TokenType.String);
                        AdvanceHead();
                        AdvanceToken(TokenType.StringInterpolationTerminator);
                        braceInterpolation.Push(true);
                        return;
                }
                AdvanceHead();
            }
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

        void AdvanceTail(int length = 1)
        {
            tail += length;
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
                    case ' ':
                    case '\r':
                        AdvanceTail();
                        break;
                    case '/':
                        if (Peek(1) == '/')
                        {
                            while (Peek() != '\n' && Peek() != '\0') AdvanceTail();
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

        void SkipComments()
        {
            if (PeekSlice(2) == "//")
            {
                while (Peek() != '\n' && Peek() != '\0') AdvanceTail();
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
            module.Tokens.AddBefore(module.Tokens.Last, new Token
            {
                Type = TokenType.NewLine,
                Module = module,
                Position = module.Tokens.Last.Value.Position,
                Column = module.Tokens.Last.Value.Column,
                Line = module.Tokens.Last.Value.Line
            });

            // First pass: normalize base indentation
            //NormalizeIndentation();

            // Second pass: remove redundant newlines and replace relevant newlines with terminators
            RemoveEmptyLines();

            // Third pass: remove line wrapping
            //RemoveLineWrapping();

            // Fourth pass: remove redundant indentation
            RemoveRedundantIndents();
        }

        /// <summary>
        /// Removes empty lines and replaces relevant newlines with terminators
        /// </summary>
        void RemoveEmptyLines()
        {
            var lineHasContent = false;
            var node = module.Tokens.First;
            while (node != null)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.NewLine:
                        node = node.Next;
                        if (!lineHasContent)
                        {
                            // Remove all tokens in this line
                            var deleteNode = node.Previous;
                            while (deleteNode != null && deleteNode.Value.Type != TokenType.StatementTerminator)
                            {
                                var nextDeleteNode = deleteNode.Previous;
                                module.Tokens.Remove(deleteNode);
                                deleteNode = nextDeleteNode;
                            }
                        }
                        else
                        {
                            // Replace newline with statement terminator
                            node.Previous.Value.Type = TokenType.StatementTerminator;
                            node.Previous.Value.Value = "";
                        }
                        lineHasContent = false;
                        break;
                    case TokenType.Indent:
                        node = node.Next;
                        break;
                    default:
                        lineHasContent = true;
                        node = node.Next;
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
            for (var node = module.Tokens.First; node != null; node = node.Next)
            {
                var token = node.Value;
                switch (token.Type)
                {
                    case TokenType.Indent:
                        if (!isNewLine)
                        {
                            node = node.Previous;
                            module.Tokens.Remove(node.Next);
                        }
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

        int GetColumn()
        {
            return tail - lastLineCharCum + 1;
        }

        Location GetLocation()
        {
            return new Location { Line = line, Column = GetColumn() };
        }

        /// <summary>
        /// Removes the given token and returns the previous token
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        LinkedListNode<Token> Remove(LinkedListNode<Token> node)
        {
            var prev = node.Previous;
            module.Tokens.Remove(node);
            return prev ?? module.Tokens.First;
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
                return $"{base.Message} at line {Location.Line}\n{CompilerUtility.GenerateCodeTokenPointer(Module, Location.Line, Location.Column, 1, 3)}";
            }
        }

        public TokenizerException(Module module, Location location, string message) : base(message)
        {
            Module = module;
            Location = location;
        }
    }
}