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
            var jumpToElseInstructionIndex = codeBuilder.Add(new Instruction(InstructionType.JumpIfFalse, 0));

            // Begin the statement block
            BeginScope();
            ConsumeBlock();
            EndScope();
            var jumpToEndInstructionIndex = codeBuilder.Add(new Instruction(InstructionType.Jump, 0));

            codeBuilder.Patch(jumpToElseInstructionIndex).ArgShort = (ushort)(codeBuilder.InstructionCount - jumpToElseInstructionIndex - 1);

            if (MatchAdvance(TokenType.Else))
            {
                Ignore(TokenType.StatementTerminator);
                BeginScope();
                ConsumeBlock();
                EndScope();
            }

            codeBuilder.Patch(jumpToEndInstructionIndex).ArgShort = (ushort)(codeBuilder.InstructionCount - jumpToEndInstructionIndex - 1);
        }
    }
}