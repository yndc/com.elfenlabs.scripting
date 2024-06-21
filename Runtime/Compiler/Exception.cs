using Elfenlabs.Strings;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        public CompilerException CreateException(Token token, string message)
        {
            return new CompilerException(token, message);
        }
    }

    public class CompilerException : System.Exception
    {
        public Token Token { get; set; }

        public override string Message
        {
            get
            {
                return $"{base.Message} at line {Token.Line}\n{CompilerUtility.GenerateCodeTokenPointer(Token, 3)}";
            }
        }

        public CompilerException(Token token, string message) : base(message)
        {
            Token = token;
        }
    }
}