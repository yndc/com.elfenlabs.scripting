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
        /// Initialize this type
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Instantiate this type into a bytecode builder
        /// </summary>
        public virtual void Instantiate(ByteCodeBuilder builder)
        {
            builder.AddConstant(new byte[WordLength * sizeof(int)]);
        }

        /// <summary>
        /// Check if this type has unresolved generic types
        /// </summary>
        /// <returns></returns>
        public virtual bool HasUnresolved()
        {
            return false;
        }

        /// <summary>
        /// Clone this type
        /// </summary>
        /// <returns></returns>
        public virtual Type Clone()
        {
            return (Type)MemberwiseClone();
        }

        /// <summary>
        /// Resolves the generic types with the provided types
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public virtual void Resolve(TypeArguments types = null)
        {
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type.HasUnresolved())
                    throw new CompilerException(null, $"Generic type {type.Identifier} is not resolved");
            }

            return;
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

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is null)
                return false;
            return Equals(other as Type);
        }

        public bool Equals(Type other)
        {
            if (other is null)
                return false;
            return Identifier.Equals(other.Identifier);
        }

        public static bool operator ==(Type left, Type right)
        {
            if (left is null)
            {
                if (right is null)
                    return true;
                return false;
            }
            else
            {
                if (right is null)
                    return false;
                return left.Equals(right);
            }
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

    public partial class Compiler
    {
        Dictionary<string, Type> types;

        Dictionary<string, StructureTypeTemplate> structureTemplates = new();

        public Type ConsumeType(List<PlaceholderType> typeParams = null)
        {
            var baseTypeName = Consume(TokenType.Identifier, $"Expected type name but get {current.Value.Type}").Value;
            var baseType = GetType(baseTypeName, typeParams) ?? throw new CompilerException(current.Value, $"Unknown type {baseTypeName}");

            switch (current.Value.Type)
            {
                case TokenType.Less:
                    Consume(TokenType.Less);
                    if (MatchAdvance(TokenType.Integer))
                    {
                        var spanSizeToken = Consume(TokenType.Integer, "Expected span size after '<'");
                        Consume(TokenType.Greater, "Expected '>' after span size");
                        return new SpanValueType(baseType, int.Parse(spanSizeToken.Value));
                    }
                    else
                    {
                        var types = new List<Type>();
                        while (true)
                        {
                            types.Add(ConsumeType(typeParams));
                            if (MatchAdvance(TokenType.Comma))
                                continue;
                            Consume(TokenType.Greater);
                            break;
                        }

                        var resolvedType = baseType.Clone();
                        var typeArgs = new TypeArguments(types.ToArray());
                        resolvedType.Resolve(typeArgs);

                        return resolvedType;
                    }
                case TokenType.LeftBracket:
                    Consume(TokenType.LeftBracket);
                    Consume(TokenType.RightBracket);
                    return new ListType(baseType);
                default:
                    return baseType;
            }
        }

        public Type GetType(string identifier, List<PlaceholderType> typeParams = null)
        {
            if (types.TryGetValue(identifier, out var type))
                return type;

            if (typeParams != null)
            {
                foreach (var typeParam in typeParams)
                {
                    if (typeParam.Identifier.Name == identifier)
                        return typeParam;
                }
            }

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

        void RegisterStructureTemplate(StructureTypeTemplate template)
        {
            if (structureTemplates.ContainsKey(template.Identifier))
                throw new Exception($"Structure template {template.Identifier} already exists");

            structureTemplates.Add(template.Identifier, template);
        }
    }
}