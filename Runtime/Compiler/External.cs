using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {   
        public void ConsumeExternalDeclaration()
        {
            Consume(TokenType.External);
            switch (current.Value.Type)
            {
                case TokenType.Function:
                    ConsumeFunctionDeclaration();
                    break;
                case TokenType.Structure:
                    ConsumeExternalVariableDeclaration();
                    break;
                default:
                    throw new CompilerException(current.Value, $"Expected external declaration. Received: {current.Value}");
            }
        }

        void ConsumeExternalVariableDeclaration()
        {

        }
    }
}