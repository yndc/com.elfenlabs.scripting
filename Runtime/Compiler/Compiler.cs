using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public struct Function
    {
        public string Name;
        public ValueType ReturnType;
        public ValueType[] ParameterTypes;
        public Function(string name, ValueType returnType, ValueType[] parameterTypes)
        {
            Name = name;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }
    }

    public struct Variable
    {
        public ushort Position;
        public string Name;
        public ValueType Type;
    }

    public struct ValueType : IEquatable<ValueType>
    {
        public int Index;
        public int Span;
        public string Name;
        public byte WordLength;

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
        public static ValueType Bool => new() { Index = 1, Name = "Bool", WordLength = 1 };
        public static ValueType Int => new() { Index = 2, Name = "Int", WordLength = 1 };
        public static ValueType Float => new() { Index = 3, Name = "Float", WordLength = 1 };
        public static ValueType String => new() { Index = 4, Name = "String", WordLength = 1 };
    }

    public class BooleanMatrix
    {
        readonly bool[,] values;
        public BooleanMatrix(int size)
        {
            values = new bool[size, size];
        }

        public void Add(int row, int column)
        {
            values[row, column] = true;
        }

        public bool Get(int row, int column)
        {
            return values[row, column];
        }
    }

    public partial class Compiler
    {
        LinkedListNode<Token> current;
        LinkedListNode<Token> previous;
        CodeBuilder builder;
        ParseRule[] parseRules;
        BooleanMatrix equatableValues;
        ValueType lastValueType;
        Scope globalScope;
        Scope currentScope;
        Dictionary<string, Function> functions;
        Dictionary<string, ValueType> types;

        public static Code Compile(string source)
        {
            return new Compiler(new Tokenizer(source).Tokenize()).Compile();
        }

        public static Code Compile(LinkedList<Token> tokens)
        {
            return new Compiler(tokens).Compile();
        }

        public Compiler(LinkedList<Token> tokens)
        {
            current = tokens.First;
            previous = tokens.First;
            builder = new CodeBuilder(Allocator.Temp);
            parseRules = new ParseRule[Enum.GetValues(typeof(TokenType)).Length];
            functions = new Dictionary<string, Function>();

            globalScope = new Scope();
            currentScope = globalScope;

            // TEST 
            functions["print"] = new Function("print", ValueType.Void, new ValueType[] { ValueType.String });

            // Add built-in types
            types = new Dictionary<string, ValueType>
            {
                { "Void",   ValueType.Void },
                { "Bool",   ValueType.Bool },
                { "Int",    ValueType.Int },
                { "Float",  ValueType.Float },
                { "String", ValueType.String }
            };

            // Grouping
            parseRules[(int)TokenType.LeftParentheses] = new ParseRule(Handling.Group);
            parseRules[(int)TokenType.RightParentheses] = new ParseRule();

            // Operators
            parseRules[(int)TokenType.Minus] = new ParseRule(Handling.Unary, Handling.Binary, Precedence.Term);
            parseRules[(int)TokenType.Plus] = new ParseRule(Handling.None, Handling.Binary, Precedence.Term);
            parseRules[(int)TokenType.Slash] = new ParseRule(Handling.None, Handling.Binary, Precedence.Factor);
            parseRules[(int)TokenType.Asterisk] = new ParseRule(Handling.None, Handling.Binary, Precedence.Factor);
            parseRules[(int)TokenType.Bang] = new ParseRule(Handling.Unary);

            // Literal values
            parseRules[(int)TokenType.Integer] = new ParseRule(Handling.Literal);
            parseRules[(int)TokenType.Float] = new ParseRule(Handling.Literal);
            parseRules[(int)TokenType.False] = new ParseRule(Handling.Literal);
            parseRules[(int)TokenType.True] = new ParseRule(Handling.Literal);

            // Structural
            parseRules[(int)TokenType.StatementTerminator] = new ParseRule();
            parseRules[(int)TokenType.EOF] = new ParseRule();

            // Comparison 
            parseRules[(int)TokenType.BangEqual] = new ParseRule(Handling.None, Handling.Binary, Precedence.Equality);
            parseRules[(int)TokenType.EqualEqual] = new ParseRule(Handling.None, Handling.Binary, Precedence.Equality);
            parseRules[(int)TokenType.Greater] = new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison);
            parseRules[(int)TokenType.GreaterEqual] = new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison);
            parseRules[(int)TokenType.Less] = new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison);
            parseRules[(int)TokenType.LessEqual] = new ParseRule(Handling.None, Handling.Binary, Precedence.Comparison);

            // User defined 
            parseRules[(int)TokenType.Identifier] = new ParseRule(Handling.Identifier);

            equatableValues = new BooleanMatrix(Enum.GetValues(typeof(PrimitiveType)).Length);
            equatableValues.Add((int)PrimitiveType.Int, (int)PrimitiveType.Int);
            equatableValues.Add((int)PrimitiveType.Float, (int)PrimitiveType.Float);
        }

        public Code Compile()
        {
            try
            {
                while (!MatchAdvance(TokenType.EOF))
                    ConsumeDeclaration();
            }
            catch (CompilerException exception)
            {
                throw new System.Exception($"{exception.Message} at line {exception.Token.Line}, column {exception.Token.Column}");
            }

            return builder.Build();
        }

        void ConsumeDeclaration()
        {
            switch (current.Value.Type)
            {
                case TokenType.Variable:
                    ConsumeStatementVariableDeclaration();
                    break;
                default:
                    ConsumeStatement();
                    break;
            }
        }

        void ConsumeStatement()
        {
            switch (current.Value.Type)
            {
                default:
                    ConsumeExpression();
                    builder.Add(new Instruction(InstructionType.Pop));
                    break;
            }
            Expect(TokenType.StatementTerminator, "Expected new-line after statement");
        }

        void Advance()
        {
            if (current.Next != null)
            {
                previous = current;
                current = current.Next;
            }
        }

        bool MatchAdvance(TokenType type)
        {
            if (current.Value.Type != type)
                return false;

            Advance();
            return true;
        }

        ParseRule GetRule(TokenType type)
        {
            return parseRules[(int)type];
        }

        void Expect(TokenType type, string error = null)
        {
            if (current.Value.Type == type)
                Advance();
            else
                throw new CompilerException(current.Value, error ?? string.Format(
                    "Expected token {0} but get {1}",
                    type.ToString(),
                    current.Value.Type.ToString()));
        }

        void AssertValueType(ValueType type, params ValueType[] set)
        {
            if (!set.Contains(type))
                throw new CompilerException(previous.Value, string.Format(
                    "Expected value type {0} to be any of {1}",
                    string.Join(", ", set.Select(x => x.ToString()).ToArray()),
                    type.Name));
        }

        void AssertValueTypeEqual(ValueType lhs, ValueType rhs)
        {
            if (lhs != rhs)
                throw new CompilerException(previous.Value, string.Format(
                    "Expected value type {0} but received {1}",
                    lhs.Name, rhs.Name));
        }
    }
}