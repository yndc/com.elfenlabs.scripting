using System.Linq;
using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public class Function
    {
        public string Name;
        public ValueType ReturnType;
        public ValueType[] ParameterTypes;
        public CodeBuilder Builder;
        public byte ParameterWordLength => (byte)ParameterTypes.Sum(x => x.WordLength);
        public Function(string name, ValueType returnType, params ValueType[] parameterTypes)
        {
            Name = name;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
            Builder = new CodeBuilder(Allocator.Temp);
        }
    }
}