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
            Advance();
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
            Advance();

            switch (current.Value.Type)
            {
                case TokenType.Equal:
                    ConsumeStatementVariableAssignment(variable);
                    break;
                case TokenType.Dot:
                    //ConsumeStatementVariableMember();
                    break;
                default:
                    throw CreateException(current.Value, "Expected operator after variable");
            }
        }

        void ConsumeStatementVariableAssignment(Variable variable)
        {
            Advance();

            var evaluationType = ConsumeExpression();

            if (evaluationType != variable.Type)
                throw CreateException(current.Value, $"Cannot assign {evaluationType.Name} to {variable.Type.Name}");

            builder.Add(new Instruction(
                InstructionType.StoreVariable,
                variable.Position,
                variable.Type.WordLength));
        }
    }
}