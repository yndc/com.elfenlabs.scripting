namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        public void ConsumeStatementIf()
        {
            Advance(); // Consume 'if'

            // The condition expression
            ConsumeExpression();
            Consume(TokenType.Then, "Expected 'then' after condition");

            // Ignore the next statement terminator 
            Ignore(TokenType.StatementTerminator);

            // Jump to the else block if the condition is false
            var jumpToElseInstructionIndex = CodeBuilder.Add(new Instruction(InstructionType.JumpIfFalse, 0));

            // Begin the statement block
            BeginScope();
            ConsumeBlock();
            EndScope();
            var jumpToEndInstructionIndex = CodeBuilder.Add(new Instruction(InstructionType.Jump, 0));

            CodeBuilder.Patch(jumpToElseInstructionIndex).ArgShort = (ushort)(CodeBuilder.InstructionCount - jumpToElseInstructionIndex - 1);

            if (MatchAdvance(TokenType.Else))
            {
                Ignore(TokenType.StatementTerminator);
                BeginScope();
                ConsumeBlock();
                EndScope();
            }

            CodeBuilder.Patch(jumpToEndInstructionIndex).ArgShort = (ushort)(CodeBuilder.InstructionCount - jumpToEndInstructionIndex - 1);
        }
    }
}