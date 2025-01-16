using System;
using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Description of a value type
    /// </summary>
    public class Type : IEquatable<Type>
    {
        /// <summary>
        /// Unique fully-qualified identifier for this type
        /// </summary>
        public Path Identifier;

        /// <summary>
        /// Size of this type in words (usually 4 bytes)
        /// </summary>
        public byte WordLength;

        /// <summary>
        /// Methods available for this type
        /// </summary>
        public List<FunctionHeader> Methods = new();

        public Type(Path identifier, byte wordLength)
        {
            Identifier = identifier;
            WordLength = wordLength;
        }

        public Type(string fullyQualifiedPath, byte wordLength)
        {
            Identifier = new Path(fullyQualifiedPath);
            WordLength = wordLength;
        }

        /// <summary>
        /// Try getting method by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetMethod(string name, out FunctionHeader result)
        {
            foreach (var method in Methods)
            {
                if (method.Name == name)
                {
                    result = method;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Generates default byte array for this type
        /// </summary>
        /// <returns></returns>
        public byte[] GenerateDefaultValue()
        {
            return new byte[WordLength * sizeof(int)];
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Type);
        }

        public bool Equals(Type other)
        {
            return Identifier.Equals(other.Identifier);
        }

        public static bool operator ==(Type left, Type right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Type left, Type right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Identifier.ToString();
        }

        public static Type Void => new("Void", 0);
        public static Type Bool => new("Bool", 1);
        public static Type Int => new("Int", 1);
        public static Type Float => new("Float", 1);
        public static Type String => new("String", 1);
    }

    public class SpanValueType : Type
    {
        public Type Element;

        public int Length;

        public SpanValueType(Type element, int length) : base(new Path($"{element.Identifier.Name}<{length}>"), (byte)(element.WordLength * length))
        {
            Element = element;
            Length = length;
        }

        public override string ToString()
        {
            return $"{Element}<{Length}>";
        }
    }

    /// <summary>
    /// Reference type holds a reference to a stack value
    /// </summary>
    public class ReferenceType : Type
    {
        public Type Element;

        public ReferenceType(Type element) : base(new Path($"ref {element.Identifier.Name}"), 1)
        {
            Element = element;
        }

        public ReferenceType(Type element, Path identifierOverride) : base(identifierOverride, 1)
        {
            Element = element;
        }
    }

    /// <summary>
    /// Pointer type holds a reference to a heap value
    /// </summary>
    public class PointerType : Type
    {
        public Type Element;

        public PointerType(Type element) : base(new Path($"ptr {element.Identifier.Name}"), 1)
        {
            Element = element;
        }

        public PointerType(Type element, Path identifierOverride) : base(identifierOverride, 1)
        {
            Element = element;
        }
    }

    public partial class Compiler
    {
        Dictionary<string, Type> types;

        public Type ConsumeType()
        {
            var baseTypeName = Consume(TokenType.Identifier, $"Expected type name but get {current.Value.Type}").Value;
            var baseType = GetType(baseTypeName) ?? throw new Exception($"Unknown type {baseTypeName}");
            switch (current.Value.Type)
            {
                case TokenType.Less:
                    Consume(TokenType.Less);
                    var spanSizeToken = Consume(TokenType.Integer, "Expected span size after '<'");
                    Consume(TokenType.Greater, "Expected '>' after span size");
                    return new SpanValueType(baseType, int.Parse(spanSizeToken.Value));
                case TokenType.LeftBracket:
                    Consume(TokenType.LeftBracket);
                    Consume(TokenType.RightBracket);
                    return new ListType(baseType);
                default:
                    return baseType;
            }
        }

        public Type GetType(string identifier)
        {
            if (types.TryGetValue(identifier, out var type))
                return type;

            return null;
        }

        void RegisterBuiltInTypes()
        {
            types = new Dictionary<string, Type>();
            RegisterType(Type.Void);
            RegisterType(Type.Bool);
            RegisterType(Type.Int);
            RegisterType(Type.Float);
            RegisterType(Type.String);
        }

        void RegisterType(Type type)
        {
            if (types.ContainsKey(type.Identifier))
                throw new Exception($"Type {type.Identifier} already exists");

            types.Add(type.Identifier, type);
        }
    }
}