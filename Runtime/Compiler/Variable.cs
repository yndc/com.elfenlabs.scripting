namespace Elfenlabs.Scripting
{
    public struct Variable
    {
        public ushort Position;
        public string Name;
        public ValueType Type;
    }

    public partial class Compiler
    {
        void ConsumeStatementVariableDeclaration()
        {
            Skip();
            Consume(TokenType.Identifier, "Expected variable name");
            var variableName = previous.Value.Value;

            Consume(TokenType.Equal, "Expected '=' after variable name");

            var valueType = ConsumeExpression();
            if (valueType == ValueType.Void)
                throw CreateException(previous.Value, "Cannot declare variable of type void");

            currentScope.DeclareVariable(variableName, valueType);
            Consume(TokenType.StatementTerminator, "Expected new-line after declaration");
        }

        void ConsumeStatementVariable(Variable variable)
        {
            Skip();

            switch (current.Value.Type)
            {
                case TokenType.Equal:
                    ConsumeStatementVariableAssignment(variable);
                    break;
                case TokenType.Dot:
                    //ConsumeStatementVariableMember();
                    break;
                case TokenType.Increment:
                    ConsumeVariableIncrement(variable);
                    break;
                case TokenType.Decrement:
                    ConsumeVariableDecrement(variable);
                    break;
                default:
                    throw CreateException(current.Value, "Expected operator after variable");
            }
        }

        void ConsumeStatementVariableAssignment(Variable variable)
        {
            Skip();

            var evaluationType = ConsumeExpression();

            if (evaluationType != variable.Type)
                throw CreateException(current.Value, $"Cannot assign {evaluationType.Identifier} type to {variable.Type.Identifier}");

            CodeBuilder.Add(new Instruction(
                InstructionType.StoreVariable,
                variable.Position,
                variable.Type.WordLength));
        }

        void ConsumeVariableIncrement(Variable variable)
        {
            if (MatchAdvance(TokenType.Increment))
            {
                AssertValueType(variable.Type, ValueType.Int);
                CodeBuilder.Add(new Instruction(InstructionType.VariableIncrement, variable.Position));
            }
        }

        void ConsumeVariableDecrement(Variable variable)
        {
            if (MatchAdvance(TokenType.Decrement))
            {
                AssertValueType(variable.Type, ValueType.Int);
                CodeBuilder.Add(new Instruction(InstructionType.VariableDecrement, variable.Position));
            }
        }
    }
}