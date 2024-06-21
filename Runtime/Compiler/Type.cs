using System;

namespace Elfenlabs.Scripting
{
    public enum ValueTypePrimitive : int
    {
        Void,
        Bool,
        Int,
        Float,
        String,
    }

    public class ValueType : IEquatable<ValueType>
    {
        public string Name;
        public int Index;
        public int Span;
        public byte WordLength;
        public bool IsReference;

        public bool IsSpan => Span > 0;

        public ValueType ToRef()
        {
            return new ValueType()
            {
                Name = Name,
                Index = Index,
                Span = Span,
                WordLength = WordLength,
                IsReference = true
            };
        }

        public ValueType ToSpan(int span)
        {
            return new ValueType()
            {
                Name = Name,
                Index = Index,
                Span = span,
                WordLength = WordLength,
                IsReference = IsReference
            };
        }

        public ValueType ToElement()
        {
            return new ValueType()
            {
                Name = Name,
                Index = Index,
                Span = 0,
                WordLength = WordLength,
                IsReference = IsReference
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

        public static ValueType Void => new() { Index = (int)ValueTypePrimitive.Void, Name = "Void", WordLength = 0 };
        public static ValueType Bool => new() { Index = (int)ValueTypePrimitive.Bool, Name = "Bool", WordLength = 1 };
        public static ValueType Int => new() { Index = (int)ValueTypePrimitive.Int, Name = "Int", WordLength = 1 };
        public static ValueType Float => new() { Index = (int)ValueTypePrimitive.Float, Name = "Float", WordLength = 1 };
        public static ValueType String => new() { Index = (int)ValueTypePrimitive.String, Name = "String", WordLength = 1 };
    }

    public partial class Compiler
    {
        public ValueType ConsumeType()
        {
            var identifier = Consume(TokenType.Identifier, $"Expected type name but get {current.Value.Type}").Value;
            var type = GetType(identifier) ?? throw new Exception($"Unknown type {identifier}");
            return type;
        }

        public ValueType GetType(string identifier)
        {
            if (types.TryGetValue(identifier, out var type))
                return type;

            return null;
        }
    }
}