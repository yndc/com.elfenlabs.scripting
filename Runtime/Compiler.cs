using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;

namespace Yonderlabs.Scripting
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

    public class Scope
    {
        public Scope Parent;
        public Dictionary<string, Variable> Variables = new();
        public int Depth;
        public ushort WordLength;

        public ushort DeclareVariable(string name, ValueType type)
        {
            var variable = new Variable()
            {
                Name = name,
                Type = type,
                Position = WordLength
            };

            if (!Variables.TryAdd(name, variable))
                throw new Exception($"Variable {name} already declared in this scope");

            WordLength += type.WordLength;

            return variable.Position;
        }

        public Variable GetVariable(string name)
        {
            if (Variables.TryGetValue(name, out var variable))
                return variable;

            if (Parent != null)
                return Parent.GetVariable(name);

            throw new Exception($"Variable {name} is not declared in this scope");
        }
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

    public class Compiler
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
            catch (Exception exception)
            {
                throw new System.Exception($"{exception.Message}\n at line {exception.Token.Line}, column {exception.Token.Column}");
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
            Expect(TokenType.StatementTerminator, "Expected new-line after statement");
        }

        void ConsumeStatement()
        {
            switch (current.Value.Type)
            {
                default:
                    ConsumeExpression();
                    Expect(TokenType.StatementTerminator, "Expected ';' after expression");
                    builder.Add(new Instruction(InstructionType.Pop));
                    break;
            }
        }

        void ConsumeStatementVariableDeclaration()
        {
            Advance();
            Expect(TokenType.Identifier, "Expected variable name");
            var variableName = previous.Value.Value;

            Expect(TokenType.Equal, "Expected '=' after variable name");

            var valueType = ConsumeExpression();
            if (valueType == ValueType.Void)
                throw new Exception(previous.Value, "Cannot declare variable of type void");

            currentScope.DeclareVariable(variableName, valueType);
        }

        ValueType ConsumeExpression()
        {
            return ConsumeExpressionForward(Precedence.Assignment);
        }

        ValueType ConsumeExpressionForward(Precedence minimumPrecedence)
        {
            Advance();
            var prefixRule = GetRule(previous.Value.Type).Prefix;
            if (prefixRule == Handling.None)
                throw new Exception(previous.Value, "Expected expression.");

            // This is the only place where infix operation is compiled, therefore we need to store the last value type here 
            lastValueType = ConsumeExpression(prefixRule);

            while (GetRule(current.Value.Type).Precedence >= minimumPrecedence)
            {
                Advance();
                var infixRule = GetRule(previous.Value.Type).Infix;
                lastValueType = ConsumeExpression(infixRule);
            }

            return lastValueType;
        }

        ValueType ConsumeExpressionGroup()
        {
            var valueType = ConsumeExpression();
            Expect(TokenType.RightParentheses, "Expected ')' after expression.");
            return valueType;
        }

        ValueType ConsumeExpressionUnary()
        {
            var op = previous.Value.Type;
            var valueType = ConsumeExpressionForward(Precedence.Unary);
            switch (op)
            {
                case TokenType.Minus:
                    switch ((PrimitiveType)valueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntNegate)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatNegate)); break;
                        default: throw new Exception(previous.Value, "Invalid type {0} for symbol {1}", valueType.ToString(), TokenType.Minus.ToString());
                    }
                    break;
                case TokenType.Bang:
                    AssertValueType(valueType, ValueType.Bool);
                    builder.Add(new Instruction(InstructionType.BoolNegate)); break;
                default: throw new Exception(previous.Value, "Invalid unary symbol {0}", op.ToString());
            }

            return valueType;
        }

        ValueType ConsumeExpressionBinary()
        {
            var op = previous.Value.Type;
            var rule = GetRule(op);
            var lhsValueType = lastValueType;
            var rhsValueType = ConsumeExpressionForward(rule.Precedence + 1);
            AssertValueTypeEqual(lhsValueType, rhsValueType);
            switch (op)
            {
                // Arithmetic
                case TokenType.Plus:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntAdd)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatAdd)); break;
                    }
                    break;
                case TokenType.Minus:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntSubstract)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatSubstract)); break;
                    }
                    break;
                case TokenType.Slash:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntDivide)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatDivide)); break;
                    }
                    break;
                case TokenType.Asterisk:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntMultiply)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatMultiply)); break;
                    }
                    break;

                // Comparison
                case TokenType.BangEqual:
                    builder.Add(new Instruction(InstructionType.NotEqual));
                    return ValueType.Bool;
                case TokenType.EqualEqual:
                    builder.Add(new Instruction(InstructionType.Equal));
                    return ValueType.Bool;
                case TokenType.Greater:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntGreaterThan)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatGreaterThan)); break;
                    }
                    return ValueType.Bool;
                case TokenType.GreaterEqual:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntGreaterThanEqual)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatGreaterThanEqual)); break;
                    }
                    return ValueType.Bool;
                case TokenType.Less:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntLessThan)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatLessThan)); break;
                    }
                    return ValueType.Bool;
                case TokenType.LessEqual:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntLessThanEqual)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatLessThanEqual)); break;
                    }
                    return ValueType.Bool;

            }

            return rhsValueType;
        }

        ValueType ConsumeExpressionLiteral()
        {
            var str = previous.Value.Value;

            switch (previous.Value.Type)
            {
                case TokenType.Integer:
                    builder.AddConstant(int.Parse(str));
                    return ValueType.Int;
                case TokenType.Float:
                    builder.AddConstant(float.Parse(str));
                    return ValueType.Float;
                case TokenType.False:
                    builder.AddConstant(0);
                    return ValueType.Bool;
                case TokenType.True:
                    builder.AddConstant(1);
                    return ValueType.Bool;
                default:
                    throw new Exception(
                        previous.Value,
                        "Unknown literal {0} of type {1}",
                        str, previous.Value.Type.ToString());
            };
        }

        ValueType ConsumeExpressionIdentifier()
        {
            var identifier = previous.Value.Value;

            // Check if it refers to a type, replace it as the default value for that type
            if (types.TryGetValue(identifier, out ValueType valueType))
            {
                builder.AddConstant(0);
                return valueType;
            }

            // Check if it refers to a variable
            if (currentScope.Variables.TryGetValue(identifier, out Variable variable))
            {
                builder.Add(new Instruction(InstructionType.LoadVariable, variable.Position, variable.Type.WordLength));
                return variable.Type;
            }

            // Check if it refers to a function
            if (functions.TryGetValue(identifier, out Function function))
            {
                //builder.Add(new Instruction(InstructionType.Call, function.Index));
                return function.ReturnType;
            }

            throw new Exception(previous.Value, "Unknown identifier {0}", identifier);
        }

        ValueType ConsumeExpression(Handling handling)
        {
            return handling switch
            {
                Handling.Group => ConsumeExpressionGroup(),
                Handling.Unary => ConsumeExpressionUnary(),
                Handling.Binary => ConsumeExpressionBinary(),
                Handling.Literal => ConsumeExpressionLiteral(),
                Handling.Identifier => ConsumeExpressionIdentifier(),
                _ => ValueType.Void,
            };
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

        void BeginScope()
        {
            var scope = new Scope { Parent = currentScope, Depth = currentScope.Depth + 1 };
            currentScope = scope;
        }

        void EndScope()
        {
            builder.Add(new Instruction(InstructionType.Pop, (ushort)currentScope.WordLength));
            currentScope = currentScope.Parent;
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
                throw new Exception(current.Value, error ?? string.Format(
                    "Expected token {0} but get {1}",
                    type.ToString(),
                    current.Value.Type.ToString()));
        }

        void AssertValueType(ValueType type, params ValueType[] set)
        {
            if (!set.Contains(type))
                throw new Exception(previous.Value, string.Format(
                    "Expected value type {0} to be any of {1}",
                    string.Join(", ", set.Select(x => x.ToString()).ToArray()),
                    type.Name));
        }

        void AssertValueTypeEqual(ValueType lhs, ValueType rhs)
        {
            if (lhs != rhs)
                throw new Exception(previous.Value, string.Format(
                    "Expected value type {0} but received {1}",
                    lhs.Name, rhs.Name));
        }

        public class Exception : System.Exception
        {
            public Token Token;

            public Exception(Token token) : base(token.Value)
            {
                this.Token = token;
            }

            public Exception(Token token, string message, params string[] args) : base(message)
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

    public static class CompilerUtility
    {
        public const int WordSize = 4;

        public static string Debug(LinkedList<Token> tokens, bool ignoreFormatting = true)
        {
            var text = new StringBuilder();
            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.NewLine: goto formatting;
                    case TokenType.Indent: goto formatting;
                    default:
                        text.Append(token.Value); break;
                    formatting: if (ignoreFormatting) continue; else break;
                }
                text.Append("\t");
                text.Append(token.Type.ToString());
                text.Append("\n");
            }

            return text.ToString();
        }

        public static string Debug(Code code)
        {
            var text = new StringBuilder();
            var constants = code.Constants;
            text.Append("-- Constants:\n");
            for (var i = 0; i < constants.Length; i++)
            {
                text.Append(constants[i]);
                text.Append("\t");
                if ((i + 1) % 4 == 0) text.Append("\n");
            }
            text.Append("\n-- Instructions:\n");
            for (var ip = 0; ip < code.Instructions.Length; ip++)
            {
                var instruction = code.Instructions[ip];
                switch (instruction.Type)
                {
                    case InstructionType.LoadConstant:
                        var index = instruction.ArgShort;
                        var size = instruction.ArgByte1;
                        text.Append("Load");
                        text.Append("\t");
                        text.Append(index);
                        text.Append("\t");
                        text.Append(size);
                        text.Append("\n");
                        break;
                    default: text.Append(instruction.Type.ToString()); text.Append("\n"); break;
                }
            }

            return text.ToString();
        }

        public static int GetWordLength<T>() where T : unmanaged
        {
            return (UnsafeUtility.SizeOf<T>() + WordSize - 1) / WordSize;
        }

        public static unsafe T Read<T>(NativeArray<byte> bytes, int offset) where T : unmanaged
        {
            return *(T*)((byte*)bytes.GetUnsafePtr() + offset);
        }
    }
}