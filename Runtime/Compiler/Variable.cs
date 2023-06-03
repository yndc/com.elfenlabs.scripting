namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        void ConsumeStatementVariableDeclaration()
        {
            Advance();
            Expect(TokenType.Identifier, "Expected variable name");
            var variableName = previous.Value.Value;

            Expect(TokenType.Equal, "Expected '=' after variable name");

            var valueType = ConsumeExpression();
            if (valueType == ValueType.Void)
                throw new CompilerException(previous.Value, "Cannot declare variable of type void");

            currentScope.DeclareVariable(variableName, valueType);
            Expect(TokenType.StatementTerminator, "Expected new-line after declaration");
        }
    }
}