using Elfenlabs.Strings;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        public CompilerException CreateException(Token token, string message)
        {
            return new CompilerException(module, token, message);
        }
    }

    public class CompilerException : System.Exception
    {
        public Module Module { get; set; }
        public Token Token { get; set; }

        public override string Message
        {
            get
            {
                return $@"
                    line: {Token.Line}, col: {Token.Column}
                    {CompilerUtility.GenerateSourcePointer(Module, Token.Location, Token.Length)}:
                    {base.Message}  
                ".AutoTrim();
            }
        }

        public CompilerException(Module module, Token token, string message) : base(message)
        {
            Module = module;
            Token = token;
        }
    }
}