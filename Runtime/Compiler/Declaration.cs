namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        void ConsumeDeclaration()
        {
            switch (current.Value.Type)
            {
                case TokenType.Variable:
                    ConsumeStatementVariableDeclaration();
                    break;
                default:
                    ConsumeStatement();
                    break;
            }
        }
    }
}