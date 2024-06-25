using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        Module module;
        Function currentFunction;
        LinkedListNode<Token> current;
        LinkedListNode<Token> previous;
        ValueType lastValueType;
        Scope globalScope;
        Scope currentScope;

        ByteCodeBuilder builder => currentFunction.Builder;

        public Compiler()
        {
            RegisterBuiltInTypes();
        }

        public void AddModule(Module module)
        {
            this.module = module;

            // Create global scope 
            globalScope = new Scope();
            currentScope = globalScope;

            // Create root function in the global scope
            var globalSubprogram = new Function("global", ValueType.Void);
            functions.Add(globalSubprogram);
            currentFunction = globalSubprogram;

            current = module.Tokens.First;

            while (!MatchAdvance(TokenType.EOF))
            {
                ConsumeDeclaration();
            }
        }

        public Program Build()
        {
            var chunks = new NativeArray<ByteCode>(functions.Count, Allocator.Persistent);
            foreach (var function in functions)
            {
                chunks[function.Index] = function.Builder.Build();
            }
            return new Program
            {
                Chunks = chunks,
                EntryPoint = 0
            };
        }

        Token Consume(TokenType type, string error = null)
        {
            if (current.Value.Type == type)
            {
                Advance();
                return previous.Value;
            }
            else
                throw CreateException(current.Value, error
                    ?? string.Format("Expected token {0} but get '{1}'", type.ToString(), current.Value.Type.ToString()));
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

        void Ignore(TokenType type)
        {
            if (current.Value.Type == type)
                Advance();
        }

        void Expect(TokenType type, int count, string error = null)
        {
            for (int i = 0; i < count; i++)
            {
                Consume(type, error);
            }
        }

        void AssertValueType(ValueType type, params ValueType[] set)
        {
            if (!set.Contains(type))
                throw CreateException(previous.Value, string.Format(
                    "Expected value type {0} to be any of {1}",
                    string.Join(", ", set.Select(x => x.ToString()).ToArray()),
                    type.Identifier));
        }

        void AssertValueTypeEqual(ValueType lhs, ValueType rhs)
        {
            if (lhs != rhs)
                throw CreateException(previous.Value, string.Format(
                    "Expected value type {0} but received {1}",
                    lhs.Identifier, rhs.Identifier));
        }
    }
}