using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public class ControlStructure
    {
        List<int> endJumpPatchTargets = new();
        int startInstructionIndex;
        int endInstructionIndex;

        public int StartInstructionIndex => startInstructionIndex;

        public ControlStructure(int startInstructionIndex)
        {
            this.startInstructionIndex = startInstructionIndex;
        }

        public void EndStructure(ByteCodeBuilder builder)
        {
            endInstructionIndex = builder.InstructionCount - 1;
            foreach (var target in endJumpPatchTargets)
            {
                builder.Patch(target).ArgSignedShort = (short)(endInstructionIndex - target);
            }
        }

        public void AddEndJumpPatchTarget(int target)
        {
            endJumpPatchTargets.Add(target);
        }
    }

    public partial class Compiler
    {
        Stack<ControlStructure> controlStructures = new();

        void BeginControlStructure(int startInstructionIndex)
        {
            controlStructures.Push(new ControlStructure(startInstructionIndex));
        }

        void AddEndJumpPatchTarget(int target)
        {
            controlStructures.Peek().AddEndJumpPatchTarget(target);
        }

        void EndControlStructure()
        {
            controlStructures.Pop().EndStructure(CodeBuilder);
        }

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
            BeginControlStructure(conditionExpressionInstructionIndex);
            BeginScope();
            ConsumeBlock();
            EndScope();

            // Jump back to the condition
            CodeBuilder.Add(new Instruction(InstructionType.Jump, (short)(conditionExpressionInstructionIndex - CodeBuilder.InstructionCount - 1)));

            // Patch the jump to the end of the while block
            CodeBuilder.Patch(jumpToEndInstructionIndex).ArgSignedShort = (short)(CodeBuilder.InstructionCount - jumpToEndInstructionIndex - 1);

            // Patch all jump to end statements
            EndControlStructure();
        }

        void ConsumeStatementContinue()
        {
            Skip();
            CodeBuilder.Add(
                new Instruction(
                    InstructionType.Jump,
                    (short)(controlStructures.Peek().StartInstructionIndex - CodeBuilder.InstructionCount - 1)));
        }

        void ConsumeStatementBreak()
        {
            Skip();
            AddEndJumpPatchTarget(CodeBuilder.Add(new Instruction(InstructionType.Jump)));
        }
    }
}