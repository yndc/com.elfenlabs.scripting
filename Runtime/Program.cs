using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public struct Program
    {
        public NativeArray<ByteCode> Chunks;
        public int EntryPoint;
    }
}