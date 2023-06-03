namespace Elfenlabs.Scripting
{
    public enum Precedence
    {
        None,
        Assignment, // =
        Or,         // or
        And,        // and
        Equality,   // == !=
        Comparison, // < > <= >=
        Term,       // + -
        Factor,     // * /
        Unary,      // ! -
        Call,       // . ()
        Primary
    }

    public enum PrimitiveType : int
    {
        Void,
        Bool,
        Int,
        Float,
        String,
    }

    public enum Handling
    {
        None,
        Group,
        Unary,
        Binary,
        Literal,
        Identifier,
    }

    public class ParseRule
    {
        public Handling Prefix;
        public Handling Infix;
        public Precedence Precedence;
        public ParseRule(Handling prefix = Handling.None, Handling infix = Handling.None, Precedence precedence = Precedence.None)
        {
            Prefix = prefix;
            Infix = infix;
            Precedence = precedence;
        }
    }
}