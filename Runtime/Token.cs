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
        If, Then, Else,
        True, False, Null,
        Structure, Function,
        And, Or,
        Loop,
        External,
        Return,
        Returns,
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

        public Location Location => new Location { Line = Line, Column = Column };
        public int Length => Value.Length;

        public static Token Invalid => new Token { Type = TokenType.Error, Value = "Invalid token" };
        public static Token TerminatorFromNewline(Token newline) => new Token { Type = TokenType.StatementTerminator, Value = "", Line = newline.Line, Column = newline.Column };
    }
}