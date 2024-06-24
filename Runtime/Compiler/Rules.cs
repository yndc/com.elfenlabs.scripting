using System;
using System.Collections.Generic;

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
        Composite,
        Identifier,
        And
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

    public partial class Compiler
    {
        static Dictionary<TokenType, ParseRule> parseRules = new()
        {
            // Grouping
            { TokenType.LeftParentheses,        new ParseRule(Handling.Group) },
            { TokenType.RightParentheses,       new ParseRule() },
            { TokenType.LeftBrace,              new ParseRule(Handling.Composite) },
            { TokenType.RightBrace,             new ParseRule() },
            { TokenType.LeftBracket,            new ParseRule(Handling.Composite) },
            { TokenType.RightBracket,           new ParseRule() },

            // Operators
            { TokenType.Minus,                  new ParseRule(Handling.Unary, Handling.Binary, Precedence.Term) },
            { TokenType.Plus,                   new ParseRule(Handling.None, Handling.Binary, Precedence.Term) },
            { TokenType.Slash,                  new ParseRule(Handling.None, Handling.Binary, Precedence.Factor) },
            { TokenType.Asterisk,               new ParseRule(Handling.None, Handling.Binary, Precedence.Factor) },
            { TokenType.Bang,                   new ParseRule(Handling.Unary) },
            { TokenType.And,                    new ParseRule(Handling.None, Handling.And, Precedence.And) },

            // Literal values
            { TokenType.Integer,                new ParseRule(Handling.Literal) },
            { TokenType.Float,                  new ParseRule(Handling.Literal) },
            { TokenType.False,                  new ParseRule(Handling.Literal) },
            { TokenType.True,                   new ParseRule(Handling.Literal) },
            { TokenType.String,                 new ParseRule(Handling.Literal) },

            // Structural
            { TokenType.StatementTerminator,    new ParseRule() },
            { TokenType.EOF,                    new ParseRule() },
            { TokenType.Then,                   new ParseRule() },

            // Comparison 
            { TokenType.BangEqual,              new ParseRule(Handling.None, Handling.Binary, Precedence.Equality) },
            { TokenType.EqualEqual,             new ParseRule(Handling.None, Handling.Binary, Precedence.Equality) },
            { TokenType.Greater,                new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison) },
            { TokenType.GreaterEqual,           new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison) },
            { TokenType.Less,                   new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison) },
            { TokenType.LessEqual,              new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison) },

            // Values 
            { TokenType.Equal,                  new ParseRule(Handling.None, Handling.Binary, Precedence.Assignment) },

            // User defined 
            { TokenType.Identifier,             new ParseRule(Handling.Identifier) },
        };

        public static ParseRule GetRule(TokenType type)
        {
            if (parseRules.TryGetValue(type, out var rule))
                return rule;
            return new ParseRule();
        }
    }
}