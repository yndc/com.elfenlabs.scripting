using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        public T ReadStackAs<T>() where T : unmanaged
        {
            return *(T*)stackPtr;
        }

        public NativeArray<int> GetStackSnapshot(Allocator allocator)
        {
            var snapshot = new NativeArray<int>(ValueStackPointer, allocator);
            UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), stackPtr, ValueStackPointer * sizeof(int));
            return snapshot;
        }
    }
}