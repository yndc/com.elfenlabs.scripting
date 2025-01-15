using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        SubProgram currentSubProgram;
        LinkedListNode<Token> current;
        LinkedListNode<Token> previous;
        Type lastValueType;
        Scope globalScope;
        Scope currentScope;

        ByteCodeBuilder CodeBuilder => currentSubProgram.Builder;

        public Compiler()
        {
            RegisterBuiltInTypes();
        }

        /// <summary>
        /// Adds a module to the compiler
        /// </summary>
        /// <param name="module"></param>
        public void AddModule(Module module)
        {
            // Create global scope 
            globalScope = new Scope() { IsFrame = true };
            currentScope = globalScope;

            // Create root function in the global scope
            var rootFunctionHeader = new FunctionHeader("root", Type.Void);
            var rootSubProgram = new SubProgram(rootFunctionHeader);
            subPrograms.Add(rootSubProgram);
            currentSubProgram = rootSubProgram;
            RegisterBuiltInFunctions();

            current = module.Tokens.First;

            // Consume all declarations until end of file
            while (!MatchAdvance(TokenType.EOF))
            {
                ConsumeDeclaration();
            }
        }

        /// <summary>
        /// Builds the program from the given modules
        /// </summary>
        /// <returns></returns>
        public Program Build()
        {
            var chunks = new NativeArray<ByteCode>(subPrograms.Count, Allocator.Persistent);
            var functionBindings = new NativeArray<ExternalFunctionBinding>(externalFunctions.Count, Allocator.Persistent);
            for (int i = 0; i < subPrograms.Count; i++)
            {
                chunks[i] = subPrograms[i].Builder.Build();
            }
            for (int i = 0; i < externalFunctions.Count; i++)
            {
                functionBindings[i] = new ExternalFunctionBinding
                {
                    InputWordLen = externalFunctions[i].ParameterWordLength,
                    OutputWordLen = externalFunctions[i].ReturnWordLength
                };
            }
            return new Program
            {
                Chunks = chunks,
                EntryPoint = 0,
                ExternalFunctions = functionBindings
            };
        }

        /// <summary>
        /// Consumes current token
        /// </summary>
        /// <returns></returns>
        Token Consume()
        {
            var token = current.Value;
            Skip();
            return token;
        }

        /// <summary>
        /// Consume current token with type assertion
        /// </summary>
        /// <param name="type"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        Token Consume(TokenType type, string error = null)
        {
            if (current.Value.Type == type)
            {
                Skip();
                return previous.Value;
            }
            else
                throw CreateException(current.Value, error
                    ?? string.Format("Expected token {0} but get '{1}'", type.ToString(), current.Value.Type.ToString()));
        }

        /// <summary>
        /// Rewind the current token
        /// </summary>
        void Rewind()
        {
            current = previous;
        }

        /// <summary>
        /// Skips the current token regardless of type
        /// </summary>
        void Skip()
        {
            if (current.Next != null)
            {
                previous = current;
                current = current.Next;
            }
        }

        /// <summary>
        /// Skips the current token if it matches the given type
        /// </summary>
        /// <param name="type"></param>
        void Skip(TokenType type)
        {
            if (current.Value.Type == type)
                Skip();
        }

        bool MatchAdvance(TokenType type)
        {
            if (current.Value.Type != type)
                return false;

            Skip();
            return true;
        }

        void AssertValueType(Type type, params Type[] set)
        {
            if (!set.Contains(type))
                throw CreateException(previous.Value, string.Format(
                    "Expected value type {0} to be any of {1}",
                    string.Join(", ", set.Select(x => x.ToString()).ToArray()),
                    type.Identifier));
        }

        void AssertValueTypeEqual(Type lhs, Type rhs)
        {
            if (lhs != rhs)
                throw CreateException(previous.Value, string.Format(
                    "Expected value type {0} but received {1}",
                    lhs.Identifier, rhs.Identifier));
        }

        void ConsumeDeclaration()
        {
            switch (current.Value.Type)
            {
                case TokenType.Function:
                    ConsumeFunctionDeclaration();
                    break;
                case TokenType.Structure:
                    ConsumeStructureDeclaration();
                    break;
                case TokenType.External:
                    ConsumeExternalDeclaration();
                    break;
                default:
                    ConsumeStatement();
                    break;
            }
        }

        void ConsumeStatement()
        {
            switch (current.Value.Type)
            {
                case TokenType.Variable:
                    ConsumeStatementVariableDeclaration();
                    break;
                case TokenType.If:
                    ConsumeStatementIf();
                    break;
                case TokenType.While:
                    ConsumeStatementWhile();
                    break;
                case TokenType.Continue:
                    ConsumeStatementContinue();
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                case TokenType.Break:
                    ConsumeStatementBreak();
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                case TokenType.Identifier:
                    ConsumeStatementIdentifier();
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                case TokenType.Return:
                    ConsumeStatementReturn();
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                default:
                    ConsumeExpression();
                    CodeBuilder.Add(new Instruction(InstructionType.Pop));
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
            }
        }
    }
}