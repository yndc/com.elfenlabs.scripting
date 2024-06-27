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
                case TokenType.Function:
                    ConsumeFunctionDeclaration();
                    break;
                case TokenType.Structure:
                    ConsumeStructureDeclaration();
                    break;
                case TokenType.External:
                    ConsumeExternalDeclaration();
                    break;
                default:
                    ConsumeStatement();
                    break;
            }
        }
    }
}