using System.Collections.Generic;
using System;

namespace Elfenlabs.Scripting
{
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

    public partial class Compiler
    {
        void ConsumeBlock()
        {
            BeginScope();

            var depth = currentScope.Depth;

            while (true)
            {
                var indent = ConsumeIndents();
                if (indent < depth)
                {
                    EndScope();
                    return;
                }

                ConsumeStatement();
            }
        }

        void BeginScope()
        {
            var scope = new Scope { Parent = currentScope, Depth = currentScope.Depth + 1 };
            currentScope = scope;
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