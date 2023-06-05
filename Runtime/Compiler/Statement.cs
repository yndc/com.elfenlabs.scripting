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
                    Expect(TokenType.StatementTerminator, "Expected new-line after statement");
                    break;
                default:
                    ConsumeExpression();
                    builder.Add(new Instruction(InstructionType.Pop));
                    Expect(TokenType.StatementTerminator, "Expected new-line after statement");
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
            if (functions.TryGetValue(identifier, out Function function))
            {

            }

            throw CreateException(previous.Value, $"Unknown statement identifier {identifier}");
        }
    }
}