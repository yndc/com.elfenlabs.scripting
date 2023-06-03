namespace Elfenlabs.Scripting
{
    public class CompilerException : System.Exception
    {
        public Token Token;

        public CompilerException(Token token) : base(token.Value)
        {
            this.Token = token;
        }

        public CompilerException(Token token, string message, params string[] args) : base(message)
        {
            message = string.Format(message, args);
            this.Token = token;
            this.Token.Value = message;
        }

        public override string ToString()
        {
            return string.Format("{{0},{1}}: {2}", Token.Line, Token.Column, Token.Value);
        }
    }
}