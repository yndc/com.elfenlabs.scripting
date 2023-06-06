using System.Collections.Generic;
using System;
using UnityEngine;

namespace Elfenlabs.Scripting
{
    public class Scope
    {
        public Scope Parent;
        public Dictionary<string, Variable> Variables = new();
        public Dictionary<string, Function> Functions = new();
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

        public Function DeclareFunction(Function function)
        {
            if (!Functions.TryAdd(function.Name, function))
                throw new Exception($"Function {function.Name} already declared in this scope");

            return function;
        }

        public bool TryGetVariable(string name, out Variable variable)
        {
            if (Variables.TryGetValue(name, out variable))
                return true;

            if (Parent != null)
                return Parent.TryGetVariable(name, out variable);

            return false;
        }

        public bool TryGetFunction(string name, out Function function)
        {
            if (Functions.TryGetValue(name, out function))
                return true;

            if (Parent != null)
                return Parent.TryGetFunction(name, out function);

            return false;
        }

        public Scope CreateChild()
        {
            return new Scope { Parent = this, Depth = Depth + 1 };
        }
    }

    public partial class Compiler
    {
        void ConsumeBlock()
        {
            var depth = currentScope.Depth;

            while (true)
            {
                var indent = ConsumeIndents();
                if (indent < depth)
                {
                    return;
                }

                ConsumeStatement();
            }
        }

        void BeginScope()
        {
            currentScope = currentScope.CreateChild();
        }

        void EndScope()
        {
            builder.Add(new Instruction(InstructionType.Pop, currentScope.WordLength));
            currentScope = currentScope.Parent;
        }

        int ConsumeIndents()
        {
            var count = 0;
            while (MatchAdvance(TokenType.Indent))
                count++;
            return count;
        }
    }
}