namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Named stack memory location
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// The name of the variable
        /// </summary>
        public string Name;

        /// <summary>
        /// The data type of the variable
        /// </summary>
        public ValueType Type;

        /// <summary>
        /// The offset position of the variable relative to the frame
        /// </summary>
        public ushort Position;

        /// <summary>
        /// Create a new variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="position"></param>
        public Variable(string name, ValueType type, ushort position)
        {
            Name = name;
            Type = type;
            Position = position;
        }

        /// <summary>
        /// Create a variable for a struct field
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="field"></param>
        public Variable(Variable parent, StructureValueType.Field field)
        {
            Name = $"{parent.Name}.{field.Name}";
            Type = field.Type;
            Position = (ushort)(parent.Position + field.Offset);
        }
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
            var token = Consume();
            switch (token.Type)
            {
                case TokenType.Equal:
                    ConsumeVariableAssignment(variable);
                    break;
                case TokenType.Dot:
                    ConsumeVariableMember(variable);
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

        void ConsumeVariableAssignment(Variable variable)
        {
            var evaluationType = ConsumeExpression();
            if (evaluationType != variable.Type)
                throw CreateException(current.Value, $"Cannot assign {evaluationType.Identifier} type to {variable.Type.Identifier}");

            CodeBuilder.Add(new Instruction(
                InstructionType.WriteStack,
                variable.Position,
                variable.Type.WordLength));
        }

        void ConsumeVariableIncrement(Variable variable)
        {
            AssertValueType(variable.Type, ValueType.Int);
            CodeBuilder.Add(new Instruction(InstructionType.VariableIncrement, variable.Position));
        }

        void ConsumeVariableDecrement(Variable variable)
        {
            AssertValueType(variable.Type, ValueType.Int);
            CodeBuilder.Add(new Instruction(InstructionType.VariableDecrement, variable.Position));
        }

        void ConsumeVariableMember(Variable variable)
        {
            var member = Consume(TokenType.Identifier, "Expected identifier after '.'");
            switch (variable.Type)
            {
                case StructureValueType structureValueType:
                    if (!structureValueType.TryGetFieldByName(member.Value, out var field))
                        throw CreateException(current.Value, $"Unknown member {member} in variable {variable.Name} of type {structureValueType}");
                    ConsumeStatementVariable(new Variable(variable, field));
                    break;
                default:
                    throw CreateException(
                        previous.Value, 
                        $"The member accessor operator '.' can only be used for spans, structs, or module. {variable.Name} is not one of them.");
            }
        }
    }
}