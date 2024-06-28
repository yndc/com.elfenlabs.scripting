using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Description of a value type
    /// </summary>
    public class ValueType : IEquatable<ValueType>
    {
        /// <summary>
        /// Unique fully-qualified identifier for this type
        /// </summary>
        public Path Identifier;

        /// <summary>
        /// Size of this type in words (usually 4 bytes)
        /// </summary>
        public byte WordLength;

        public ValueType(Path identifier, byte wordLength)
        {
            Identifier = identifier;
            WordLength = wordLength;
        }

        public ValueType(string fullyQualifiedPath, byte wordLength)
        {
            Identifier = new Path(fullyQualifiedPath);
            WordLength = wordLength;
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
            return Equals(obj as ValueType);
        }

        public bool Equals(ValueType other)
        {
            return Identifier.Equals(other.Identifier);
        }

        public static bool operator ==(ValueType left, ValueType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueType left, ValueType right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Identifier.ToString();
        }

        public static ValueType Void => new("Void", 0);
        public static ValueType Bool => new("Bool", 1);
        public static ValueType Int => new("Int", 1);
        public static ValueType Float => new("Float", 1);
        public static ValueType String => new("String", 1);
    }

    public class SpanValueType : ValueType
    {
        public ValueType Element;

        public int Length;

        public SpanValueType(ValueType element, int length) : base(new Path($"{element.Identifier.Name}<{length}>"), (byte)(element.WordLength * length))
        {
            Element = element;
            Length = length;
        }

        public override string ToString()
        {
            return $"{Element}<{Length}>";
        }
    }

    public partial class Compiler
    {
        Dictionary<string, ValueType> types;

        public ValueType ConsumeType()
        {
            var typeName = Consume(TokenType.Identifier, $"Expected type name but get {current.Value.Type}").Value;
            var type = GetType(typeName) ?? throw new Exception($"Unknown type {typeName}");
            switch (current.Value.Type)
            {
                case TokenType.Less:
                    Consume(TokenType.Less);
                    var spanSizeToken = Consume(TokenType.Integer, "Expected span size after '<'");
                    Consume(TokenType.Greater, "Expected '>' after span size");
                    return new SpanValueType(type, int.Parse(spanSizeToken.Value));
                default:
                    return type;
            }
        }

        public ValueType GetType(string identifier)
        {
            if (types.TryGetValue(identifier, out var type))
                return type;

            return null;
        }

        void RegisterBuiltInTypes()
        {
            types = new Dictionary<string, ValueType>();
            RegisterType(ValueType.Void);
            RegisterType(ValueType.Bool);
            RegisterType(ValueType.Int);
            RegisterType(ValueType.Float);
            RegisterType(ValueType.String);
        }

        void RegisterType(ValueType type)
        {
            if (types.ContainsKey(type.Identifier))
                throw new Exception($"Type {type.Identifier} already exists");

            types.Add(type.Identifier, type);
        }
    }
}