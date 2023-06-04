namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        public void ConsumeStatementIf()
        {
            Advance(); // Consume 'if'

            // The condition expression
            ConsumeExpression();
            Expect(TokenType.Then, "Expected 'then' after condition");

            // Jump to the else block if the condition is false
            var jumpToElseInstructionIndex = builder.Add(new Instruction(InstructionType.JumpIfFalse, 0));

            // Begin the statement block
            ConsumeBlock();
            var jumpToEndInstructionIndex = builder.Add(new Instruction(InstructionType.Jump, 0));

            builder.Patch(jumpToElseInstructionIndex).ArgShort = (ushort)(builder.Length - jumpToElseInstructionIndex);

            if (MatchAdvance(TokenType.Else))
            {
                ConsumeBlock();
            }

            builder.Patch(jumpToEndInstructionIndex).ArgShort = (ushort)(builder.Length - jumpToEndInstructionIndex);
        }
    }
}