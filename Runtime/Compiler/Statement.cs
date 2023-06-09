using UnityEngine;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        void ConsumeStatement()
        {
            switch (current.Value.Type)
            {
                case TokenType.If:
                    ConsumeStatementIf();
                    break;
                case TokenType.Identifier:
                    ConsumeStatementIdentifier();
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                case TokenType.Function:
                    ConsumeFunctionDeclaration();
                    break;
                case TokenType.Return:
                    ConsumeStatementReturn();
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                default:
                    ConsumeExpression();
                    builder.Add(new Instruction(InstructionType.Pop));
                    Consume(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
            }
        }

        void ConsumeStatementIdentifier()
        {
            var identifier = current.Value.Value;

            // Check if it refers to a variable
            if (currentScope.TryGetVariable(identifier, out var variable))
            {
                ConsumeStatementVariable(variable);
                return;
            }

            // Check if it refers to a function
            if (currentScope.TryGetFunction(identifier, out Function function))
            {

            }

            throw CreateException(previous.Value, $"Unknown statement identifier {identifier}");
        }
    }
}