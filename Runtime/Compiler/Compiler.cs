using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        readonly Dictionary<string, Function> functions;
        readonly Dictionary<string, ValueType> types;
        Module module;
        Function currentFunction;
        Function rootFunction;
        LinkedListNode<Token> current;
        LinkedListNode<Token> previous;
        CodeBuilder builder;
        ValueType lastValueType;
        Scope globalScope;
        Scope currentScope;

        public Compiler()
        {
            functions = new Dictionary<string, Function>();

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
        }

        public void Compile(Module module)
        {
            this.module = module;
            builder = new CodeBuilder(Allocator.Temp);
            current = module.Tokens.First;
            globalScope = new Scope();
            currentScope = globalScope;

            while (!MatchAdvance(TokenType.EOF))
                ConsumeDeclaration();

            module.ByteCode = builder.Build();
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

        void Expect(TokenType type, string error = null)
        {
            if (current.Value.Type == type)
                Advance();
            else
                throw CreateException(current.Value, error ?? string.Format(
                    "Expected token {0} but get {1}",
                    type.ToString(),
                    current.Value.Type.ToString()));
        }

        void Expect(TokenType type, int count, string error = null)
        {
            for (int i = 0; i < count; i++)
            {
                Expect(type, error);
            }
        }

        void AssertValueType(ValueType type, params ValueType[] set)
        {
            if (!set.Contains(type))
                throw CreateException(previous.Value, string.Format(
                    "Expected value type {0} to be any of {1}",
                    string.Join(", ", set.Select(x => x.ToString()).ToArray()),
                    type.Name));
        }

        void AssertValueTypeEqual(ValueType lhs, ValueType rhs)
        {
            if (lhs != rhs)
                throw CreateException(previous.Value, string.Format(
                    "Expected value type {0} but received {1}",
                    lhs.Name, rhs.Name));
        }
    }
}