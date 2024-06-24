using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        public T ReadStackAs<T>() where T : unmanaged
        {
            return *(T*)stackHeadPtr;
        }

        public NativeArray<int> GetStackSnapshot(Allocator allocator)
        {
            var snapshot = new NativeArray<int>(GetStackWordLength(), allocator);
            UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), frameValuesPtr, (int)(stackHeadPtr - Values.GetUnsafePtr()) * sizeof(int));
            return snapshot;
        }

        public NativeArray<byte> GetHeapSnapshot(Allocator allocator)
        {
            var snapshot = new NativeArray<byte>(heap.Length * sizeof(int), allocator);
            UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), heap.GetUnsafePtr(), heap.Length * sizeof(int));
            return snapshot;
        }
    }
}