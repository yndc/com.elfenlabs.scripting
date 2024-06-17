using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static Unity.Entities.SystemBaseDelegates;

namespace Elfenlabs.Scripting
{
    public class Function
    {
        public string Name;
        public ushort Index;
        public ValueType ReturnType;
        public ValueType[] ParameterTypes;
        public ByteCodeBuilder Builder;
        public ByteCode ByteCode;
        public ushort Offset;
        public byte ParameterWordLength => (byte)ParameterTypes.Sum(x => x.WordLength);
        public Function(string name, ValueType returnType, params ValueType[] parameterTypes)
        {
            Name = name;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
            Builder = new ByteCodeBuilder(Allocator.Persistent);
        }
    }

    public partial class Compiler
    {
        Function RegisterFunction(string name, ValueType returnType, params ValueType[] parameterTypes)
        {
            var function = new Function(name, returnType, parameterTypes)
            {
                Index = (ushort)functions.Count,
                Offset = (ushort)currentScope.WordLength,
            };
            functions.Add(function);
            currentScope.DeclareFunction(function);
            return function;
        }

        void ConsumeFunctionDeclaration()
        {
            Advance();

            // Consume function name
            Consume(TokenType.Identifier, "Expected function name");
            var name = previous.Value.Value;

            // A function lives on its own scope
            var functionScope = currentScope.CreateChild();

            var parameters = ConsumeFunctionDeclarationParameters(functionScope);
            var returnType = MatchAdvance(TokenType.Returns) ? ConsumeType() : ValueType.Void;
            Consume(TokenType.StatementTerminator, "Expected new-line after function header");
            var function = RegisterFunction(name, returnType, parameters.ToArray());

            // Consume function body
            var previousFunction = currentFunction;
            currentFunction = function;
            currentScope = functionScope;
            ConsumeBlock();
            currentFunction = previousFunction;
            currentScope = functionScope.Parent;
        }

        List<ValueType> ConsumeFunctionDeclarationParameters(Scope functionScope)
        {
            Consume(TokenType.LeftParentheses, "Expected '(' before function parameters");
            var parameters = new List<ValueType>();
            while (true)
            {
                parameters.Add(ConsumeFunctionDeclarationParameter(functionScope));
                switch (current.Value.Type)
                {
                    case TokenType.Comma:
                        Advance();
                        continue;
                    case TokenType.RightParentheses:
                        Advance();
                        return parameters;
                    default:
                        throw CreateException(current.Value, "Expected ',' or ')' after function parameter");
                }
            }
        }

        ValueType ConsumeFunctionDeclarationParameter(Scope functionScope)
        {
            var type = ConsumeType();
            var name = Consume(TokenType.Identifier, "Expected parameter name").Value;
            functionScope.DeclareVariable(name, type);
            return type;
        }

        ValueType ConsumeFunctionCall(Function function)
        {
            ConsumeFunctionCallParameters(function);
            builder.Add(new Instruction(InstructionType.Call, function.Index, function.ParameterWordLength));
            return function.ReturnType;
        }

        void ConsumeFunctionCallParameters(Function function)
        {
            var parameters = new List<ValueType>();
            var parseParameters = true;
            while (parseParameters)
            {
                parameters.Add(ConsumeExpression());
                switch (current.Value.Type)
                {
                    case TokenType.Comma:
                        Advance();
                        continue;
                    case TokenType.RightParentheses:
                        Advance();
                        parseParameters = false;
                        break;
                    default:
                        throw CreateException(current.Value, "Expected ',' or ')' after function parameter");
                }
            }

            if (parameters.Count != function.ParameterTypes.Length)
                throw CreateException(current.Value,
                    $"Expected {function.ParameterTypes.Length} parameters, got {parameters.Count}");

            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] != function.ParameterTypes[i])
                    throw CreateException(current.Value,
                        $"Expected parameter of type {function.ParameterTypes[i]} but got {parameters[i]}");
            }
        }

        void ConsumeStatementReturn()
        {
            Advance();

            if (currentFunction.ReturnType == ValueType.Void)
            {
                if (MatchAdvance(TokenType.StatementTerminator))
                {
                    builder.Add(new Instruction(InstructionType.Return, 0));
                    return;
                }
                else
                {
                    throw CreateException(current.Value, "This function isn't supposed to return anything. Expected new-line after return statement");
                }
            }

            ConsumeExpression();
            builder.Add(new Instruction(InstructionType.Return, currentFunction.ReturnType.WordLength));
        }

        ValueType ConsumeFunctionPointer(Function function)
        {
            throw CreateException(current.Value, "Function pointers are not supported yet");
        }
    }
}