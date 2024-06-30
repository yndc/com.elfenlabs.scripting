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
                case TokenType.While:
                    ConsumeStatementWhile();
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
            if (currentScope.TryGetFunction(identifier, out FunctionHeader function))
            {
                Skip();
                if (MatchAdvance(TokenType.LeftParentheses))
                {
                    ConsumeFunctionCall(function);
                    return;
                }
            }

            throw CreateException(current.Value, $"Unknown statement identifier {identifier}");
        }

        void ConsumeStatementWhile()
        {
            Skip();

            var conditionExpressionInstructionIndex = CodeBuilder.InstructionCount;
            ConsumeExpression();
            Consume(TokenType.StatementTerminator, "Expected new-line after while condition");

            // Jump to the end of the while block if the condition is false
            var jumpToEndInstructionIndex = CodeBuilder.Add(new Instruction(InstructionType.JumpCondition, 0));

            // Begin while statement block
            BeginScope();
            ConsumeBlock();
            EndScope();

            // Jump back to the condition
            CodeBuilder.Add(new Instruction(InstructionType.Jump, (short)(conditionExpressionInstructionIndex - CodeBuilder.InstructionCount - 1)));
        
            // Patch the jump to the end of the while block
            CodeBuilder.Patch(jumpToEndInstructionIndex).ArgSignedShort = (short)(CodeBuilder.InstructionCount - jumpToEndInstructionIndex - 1);
        }
    }
}