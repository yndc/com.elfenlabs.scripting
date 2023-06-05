using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public class Module
    {
        public readonly string Name;
        public readonly string Source;
        public LinkedList<Token> Tokens;
        public ByteCode ByteCode;
        public Module(string name, string source)
        {
            Name = name;
            Source = source;
        }
        public Module(string source) : this("global", source) { }
    }
}