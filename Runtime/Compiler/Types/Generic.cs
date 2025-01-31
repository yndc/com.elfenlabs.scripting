using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elfenlabs.Scripting
{
    public enum Constraint
    {
        Numeric,
    }

    public class TypeArguments : IEquatable<TypeArguments>
    {
        readonly Type[] Types;

        public TypeArguments()
        {
            Types = Array.Empty<Type>();
        }

        public TypeArguments(Type[] types)
        {
            Types = types;
        }

        public Type this[int index]
        {
            get { return Types[index]; }
        }

        public int Length => Types.Length;

        public TypeArguments Append(TypeArguments other)
        {
            var types = new Type[Types.Length + other.Types.Length];
            Array.Copy(Types, types, Types.Length);
            Array.Copy(other.Types, 0, types, Types.Length, other.Types.Length);
            return new TypeArguments(types);
        }

        public bool Equals(TypeArguments other)
        {
            if (Types.Length != other.Types.Length)
                return false;

            for (int i = 0; i < Types.Length; i++)
            {
                if (!Types[i].Equals(other.Types[i]))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<");
            for (int i = 0; i < Types.Length; i++)
            {
                sb.Append(Types[i].Identifier.Name);
                if (i < Types.Length - 1)
                    sb.Append(", ");
            }
            sb.Append(">");
            return sb.ToString();
        }
    }

    /// <summary>
    /// A placeholder type for generic types
    /// </summary>
    public class PlaceholderType
    {
        public string Identifier;

        public int Index;

        public HashSet<Constraint> Constraints = new();

        public PlaceholderType(string identifier, int index)
        {
            Identifier = identifier;
            Index = index;
        }
    }
}