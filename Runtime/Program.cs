using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public struct ExternalFunctionBinding
    {
        public int InputWordLen;
        public int OutputWordLen;
    }
    
    public struct Program
    {
        public NativeArray<ByteCode> Chunks;
        public NativeArray<ExternalFunctionBinding> ExternalFunctions;
        public int EntryPoint;
    }
}