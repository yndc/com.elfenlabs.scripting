namespace Elfenlabs.Scripting
{
    public enum TokenType
    {
        Invalid,

        // Single character tokens
        LeftParentheses, RightParentheses,
        LeftBrace, RightBrace,
        LeftBracket, RightBracket,
        Comma, Dot,
        Minus, Plus, Slash, Asterisk,

        // One or two character tokens
        Bang, BangEqual,
        Equal, EqualEqual,
        Greater, GreaterEqual,
        Less, LessEqual,
        Increment, Decrement,

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
        Module,
        Use,

        // Formatting
        NewLine, Indent,

        // Structural 
        StatementTerminator,

        // Miscelanous
        Error,

        // End of file
        EOF
    }

    public class Token
    {
        public Module Module;
        public TokenType Type;
        public string Value;
        public int Position;
        public int Line;
        public int Column;

        public Location Location => new Location { Line = Line, Column = Column };
        public int Length => Value.Length;

        public static Token Invalid => new Token { Type = TokenType.Error, Value = "Invalid token" };
        public static Token TerminatorFromNewline(Token newline) => new()
        {
            Type = TokenType.StatementTerminator,
            Value = "",
            Line = newline.Line,
            Column = newline.Column,
            Position = newline.Position,
            Module = newline.Module
        };
    }
}