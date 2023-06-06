using System;

namespace Elfenlabs.Scripting
{
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