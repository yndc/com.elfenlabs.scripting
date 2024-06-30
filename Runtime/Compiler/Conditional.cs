namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        public void ConsumeStatementIf()
        {
            Skip(); // Consume 'if'

            // The condition expression
            ConsumeExpression();
            Consume(TokenType.Then, "Expected 'then' after condition");

            // Ignore the next statement terminator 
            Skip(TokenType.StatementTerminator);

            // Jump to the else block if the condition is false
            var jumpToElseInstructionIndex = CodeBuilder.Add(new Instruction(InstructionType.JumpCondition, 0, 0));

            // Begin the statement block
            BeginScope();
            ConsumeBlock();
            EndScope();
            var jumpToEndInstructionIndex = CodeBuilder.Add(new Instruction(InstructionType.Jump, 0));

            CodeBuilder.Patch(jumpToElseInstructionIndex).ArgSignedShort = (short)(CodeBuilder.InstructionCount - jumpToElseInstructionIndex - 1);

            if (MatchAdvance(TokenType.Else))
            {
                Skip(TokenType.StatementTerminator);
                BeginScope();
                ConsumeBlock();
                EndScope();
            }

            CodeBuilder.Patch(jumpToEndInstructionIndex).ArgSignedShort = (short)(CodeBuilder.InstructionCount - jumpToEndInstructionIndex - 1);
        }
    }
}