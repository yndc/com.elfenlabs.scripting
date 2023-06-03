using System;

namespace Elfenlabs.Scripting
{
    public abstract class Node
    {
        public Node Parent;
        public Token Token;
        public Node(Node parent, Token token)
        {
            Parent = parent;
            Token = token;
        }

        abstract public void Compile(CodeBuilder builder);
    }

    public class Statement : Node
    {
        public Statement(Node parent, Token token) : base(parent, token)
        {

        }


        public override void Compile(CodeBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }

    public class Expression : Node
    {
        public Expression Next;
        
        public Expression(Node parent, Token token, Precedence precedence = Precedence.Assignment) : base(parent, token)
        {
            switch (token.Type)
            {
                //case TokenType.Integer:
                //    var value = new Literal(this, token);
            }
        }

        public override void Compile(CodeBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }

    public class Literal : Node
    {
        public Value Value;

        public Literal(Node parent, Token token) : base(parent, token)
        {
            switch (token.Type)
            {
                case TokenType.Integer:
                    Value = ValueType.Int.Parse(token);
                    break;
                case TokenType.Float:
                    Value = ValueType.Float.Parse(token);
                    break;
                case TokenType.String:
                    Value = ValueType.String.Parse(token);
                    break;
                case TokenType.False:
                    Value = ValueType.Bool.Parse(token);
                    break;
                case TokenType.True:
                    Value = ValueType.Bool.Parse(token);
                    break;
                default:
                    throw new ParserException(token, "Unexpected token {0}", token.Value);
            }
        }

        public override void Compile(CodeBuilder builder)
        {
            Value.Compile(builder);
        }
    }

    public class ValueType : IEquatable<ValueType>
    {
        public int Index;
        public int Span;
        public string Name;
        public byte WordLength;

        public Func<Token, int[]> Parser;

        public Value Parse(Token token)
        {
            return new Value()
            {
                Type = this,
                Data = Parser(token)
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Span);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(ValueType other)
        {
            return Index == other.Index && Span == other.Span;
        }

        public static bool operator ==(ValueType left, ValueType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueType left, ValueType right)
        {
            return !(left == right);
        }

        public static ValueType Void => new() { Index = 0, Name = "Void", WordLength = 0 };
        public static ValueType Bool => new()
        {
            Index = 1,
            Name = "Bool",
            WordLength = 1,
            Parser = token => CompilerUtility.ToIntArray(bool.Parse(token.Value))
        };

        public static ValueType Int => new()
        {
            Index = 2,
            Name = "Int",
            WordLength = 1,
            Parser = token => CompilerUtility.ToIntArray(int.Parse(token.Value))
        };

        public static ValueType Float => new()
        {
            Index = 3,
            Name = "Float",
            WordLength = 1,
            Parser = token => CompilerUtility.ToIntArray(float.Parse(token.Value))
        };

        public static ValueType String => new()
        {
            Index = 4,
            Name = "String",
            WordLength = 1,
            Parser = token => CompilerUtility.ToIntArray(token.Value)
        };
    }

    public class Value
    {
        public ValueType Type;
        public int[] Data;
        public void Compile(CodeBuilder builder)
        {
            builder.AddConstant(Data);
        }
    }

    public class ParserException : System.Exception
    {
        public Token Token;

        public ParserException(Token token) : base(token.Value)
        {
            this.Token = token;
        }

        public ParserException(Token token, string message, params string[] args) : base(message)
        {
            message = string.Format(message, args);
            this.Token = token;
            this.Token.Value = message;
        }

        public override string ToString()
        {
            return string.Format("{{0},{1}}: {2}", Token.Line, Token.Column, Token.Value);
        }
    }
}