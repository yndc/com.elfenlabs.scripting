using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        public T ReadStackAs<T>() where T : unmanaged
        {
            return *(T*)valuesPtr;
        }

        public NativeArray<int> GetStackSnapshot(Allocator allocator)
        {
            var snapshot = new NativeArray<int>(GetStackWordLength(), allocator);
            UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), frameValuesPtr, (int)(valuesPtr - Values.GetUnsafePtr()) * sizeof(int));
            return snapshot;
        }
    }
}