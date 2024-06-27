using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Collections;
using static Unity.Entities.SystemBaseDelegates;

namespace Elfenlabs.Scripting
{
    public class FunctionHeader
    {
        public class Parameter
        {
            public string Name;
            public ValueType Type;
            public Parameter(string name, ValueType type)
            {
                Name = name;
                Type = type;
            }
        }

        /// <summary>
        /// Name of this function
        /// </summary>
        public string Name;

        /// <summary>
        /// Return type of this function
        /// </summary>
        public ValueType ReturnType;

        /// <summary>
        /// Parameters of this function
        /// </summary>
        public Parameter[] Parameters;

        /// <summary>
        /// Flags this function as an external function
        /// </summary>
        public bool IsExternal;

        /// <summary>
        /// The index of this function in the function table
        /// </summary>
        public ushort Index;

        /// <summary>
        /// Word length of all parameters combined
        /// </summary>
        public byte ParameterWordLength => (byte)Parameters.Sum(x => x.Type.WordLength);

        /// <summary>
        /// Word length of the return type
        /// </summary>
        public byte ReturnWordLength => (byte)(ParameterWordLength + ReturnType.WordLength);

        public FunctionHeader(string name, ValueType returnType, params Parameter[] parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            Index = 0;
        }
    }

    /// <summary>
    /// User defined function, will be compiled to its own chunk byte code
    /// </summary>
    public class SubProgram
    {
        public FunctionHeader Header;
        public ByteCodeBuilder Builder;
        public ushort Offset;
        public SubProgram(FunctionHeader function)
        {
            Header = function;
            Builder = new ByteCodeBuilder(Allocator.Persistent);
        }
    }

    public partial class Compiler
    {
        readonly List<SubProgram> subPrograms = new();
        readonly List<FunctionHeader> externalFunctions = new();

        /// <summary>
        /// Registers a new function in the current scope and assigns it an index
        /// </summary>
        /// <param name="name"></param>
        /// <param name="returnType"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        void RegisterSubProgram(SubProgram subProgram)
        {
            subProgram.Header.Index = (ushort)subPrograms.Count;
            subPrograms.Add(subProgram);
            currentScope.DeclareFunction(subProgram.Header);
        }

        /// <summary>
        /// Registers a new external function in the current scope and assigns it an index
        /// </summary>
        /// <param name="function"></param>
        void RegisterExternalFunction(FunctionHeader function)
        {
            function.Index = (ushort)externalFunctions.Count;
            function.IsExternal = true;
            externalFunctions.Add(function);
            currentScope.DeclareFunction(function);
        }

        void ConsumeFunctionDeclaration()
        {
            var external = previous?.Value.Type == TokenType.External;
            var function = ConsumeFunctionDeclarationHeader();
            if (external)
                RegisterExternalFunction(function);
            else
            {
                var subProgram = new SubProgram(function);
                RegisterSubProgram(subProgram);
                ConsumeFunctionBody(subProgram);
            }
        }

        FunctionHeader ConsumeFunctionDeclarationHeader()
        {
            Consume(TokenType.Function);
            Consume(TokenType.Identifier, "Expected function name");
            var name = previous.Value.Value;
            var parameters = ConsumeFunctionDeclarationParameters();
            var returnType = MatchAdvance(TokenType.Returns) ? ConsumeType() : ValueType.Void;
            Consume(TokenType.StatementTerminator, "Expected new-line after function header");
            return new FunctionHeader(name, returnType, parameters.ToArray());
        }

        List<FunctionHeader.Parameter> ConsumeFunctionDeclarationParameters()
        {
            Consume(TokenType.LeftParentheses, "Expected '(' before function parameters");
            var parameters = new List<FunctionHeader.Parameter>();
            while (true)
            {
                parameters.Add(ConsumeFunctionDeclarationParameter());
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

        FunctionHeader.Parameter ConsumeFunctionDeclarationParameter()
        {
            var type = ConsumeType();
            var name = Consume(TokenType.Identifier, "Expected parameter name").Value;
            return new FunctionHeader.Parameter(name, type);
        }

        void ConsumeFunctionBody(SubProgram subProgram)
        {
            var functionScope = currentScope.CreateChild();
            var previousFunction = currentSubProgram;

            // Add all parameters as variables to the function scope
            for (int i = 0; i < subProgram.Header.Parameters.Length; i++)
            {
                var p = subProgram.Header.Parameters[i];
                functionScope.DeclareVariable(p.Name, p.Type);
            }
            currentSubProgram = subProgram;
            currentScope = functionScope;
            ConsumeBlock();
            currentSubProgram = previousFunction;
            currentScope = functionScope.Parent;
        }

        ValueType ConsumeFunctionCall(FunctionHeader function)
        {
            ConsumeFunctionCallParameters(function.Parameters);
            if (function.IsExternal)
                CodeBuilder.Add(new Instruction(InstructionType.CallExternal, function.Index));
            else
                CodeBuilder.Add(new Instruction(InstructionType.Call, function.Index, function.ParameterWordLength));
            return function.ReturnType;
        }

        void ConsumeFunctionCallParameters(FunctionHeader.Parameter[] functionParameters)
        {
            var callParameters = new List<ValueType>();
            var parseParameters = true;
            while (parseParameters)
            {
                callParameters.Add(ConsumeExpression());
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

            if (callParameters.Count != functionParameters.Length)
                throw CreateException(current.Value,
                    $"Expected {functionParameters.Length} parameters, got {callParameters.Count}");

            for (int i = 0; i < callParameters.Count; i++)
            {
                if (callParameters[i] != functionParameters[i].Type)
                    throw CreateException(current.Value,
                        $"Expected parameter of type {functionParameters[i]} but got {callParameters[i]}");
            }
        }

        void ConsumeStatementReturn()
        {
            Advance();

            if (currentSubProgram.Header.ReturnType == ValueType.Void)
            {
                if (MatchAdvance(TokenType.StatementTerminator))
                {
                    CodeBuilder.Add(new Instruction(InstructionType.Return, 0));
                    return;
                }
                else
                {
                    throw CreateException(current.Value, "This function isn't supposed to return anything. Expected new-line after return statement");
                }
            }

            ConsumeExpression();
            CodeBuilder.Add(new Instruction(InstructionType.Return, currentSubProgram.Header.ReturnType.WordLength));
        }

        ValueType ConsumeFunctionPointer(FunctionHeader function)
        {
            throw CreateException(current.Value, "Function pointers are not supported yet");
        }
    }
}