using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        public struct Snapshot : INativeDisposable
        {
            public NativeArray<int> Stack;
            public NativeArray<int> Heap;

            public JobHandle Dispose(JobHandle inputDeps)
            {
                inputDeps = Stack.Dispose(inputDeps);
                inputDeps = Heap.Dispose(inputDeps);
                return inputDeps;
            }

            public void Dispose()
            {
                Stack.Dispose();
                Heap.Dispose();
            }
        }

        public NativeArray<int> GetStackSnapshot(Allocator allocator)
        {
            var snapshot = new NativeArray<int>(GetStackWordLength(), allocator);
            UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), frameValuesPtr, (int)(stackHeadPtr - Values.GetUnsafePtr()) * sizeof(int));
            return snapshot;
        }

        public NativeArray<int> GetHeapSnapshot(Allocator allocator)
        {
            var snapshot = new NativeArray<int>(heap.Length, allocator);
            UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), heap.GetUnsafePtr(), heap.Length * sizeof(int));
            return snapshot;
        }

        public Snapshot GetSnapshot(Allocator allocator)
        {
            return new Snapshot
            {
                Stack = GetStackSnapshot(allocator),
                Heap = GetHeapSnapshot(allocator)
            };
        }
    }
}