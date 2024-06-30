using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Elfenlabs.Scripting
{
    public struct EmptyChunk : IComparable<EmptyChunk>
    {
        public int Index;
        public int WordLen;

        public int CompareTo(EmptyChunk other)
        {
            return Index - other.Index;
        }
    }

    public unsafe struct Arena : INativeDisposable
    {
        NativeList<int> container;
        NativePriorityList<EmptyChunk> emptyChunks;
        int biggestEmptyChunk;
        int usedLength;

        public int Length => usedLength;

        public Arena(int initialCapacity, Allocator allocator)
        {
            container = new NativeList<int>(initialCapacity, allocator);
            emptyChunks = new NativePriorityList<EmptyChunk>(initialCapacity, allocator, true);
            biggestEmptyChunk = 0;
            usedLength = 0;
        }

        public void Dispose()
        {
            container.Dispose();
        }

        public JobHandle Dispose(JobHandle deps)
        {
            return container.Dispose(deps);
        }

        /// <summary>
        /// Allocates a block of memory in the heap and returns the index
        /// </summary>
        /// <param name="wordLen"></param>
        /// <returns></returns>
        public int Allocate(int wordLen)
        {
            int index = TryGetEmptyChunk(wordLen);
            if (index > -1)
                return index;

            index = usedLength;
            usedLength += wordLen;
            container.ResizeUninitialized(index + wordLen);
            return index;

        }

        /// <summary>
        /// Deallocates a block of memory in the heap
        /// </summary>
        /// <param name="index"></param>
        /// <param name="wordLen"></param>
        public void Deallocate(int index, int wordLen)
        {
            emptyChunks.Add(new EmptyChunk { Index = index, WordLen = wordLen });
            if (wordLen > biggestEmptyChunk)
                biggestEmptyChunk = wordLen;
        }

        /// <summary>
        /// Get the data as a native array
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<int> ToNativeArray(Allocator allocator)
        {
            return container.ToArray(allocator);
        }

        /// <summary>
        /// Convert the data to a managed array
        /// </summary>
        /// <returns></returns>
        public int[] ToArray()
        {
            return container.ToArray();
        }

        /// <summary>
        /// Sets the size of the heap, but does not initialize the memory
        /// </summary>
        /// <param name="length"></param>
        public void ResizeUninitialized(int length)
        {
            container.ResizeUninitialized(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int* GetUnsafePtr()
        {
            return container.GetUnsafePtr();
        }

        int TryGetEmptyChunk(int wordLen, int maxAttempt = 10)
        {
            for (int i = 0; i < emptyChunks.Length; i++)
            {
                var slice = emptyChunks.ElementAt(i);
                if (slice.WordLen >= wordLen)
                {
                    emptyChunks.RemoveAt(i);
                    return slice.Index;
                }
            }
            return -1;
        }
    }

    /// <summary>
    /// Priority list using a max-heap
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe struct NativePriorityList<T> where T : unmanaged, IComparable<T>
    {
        NativeList<T> container;
        readonly int flip;

        public readonly int Length => container.Length;

        public NativePriorityList(int initialCapacity, Allocator allocator, bool inverted = false)
        {
            container = new NativeList<T>(initialCapacity, allocator);
            if (inverted)
                flip = -1;
            else
                flip = 1;
        }

        public void Dispose()
        {
            container.Dispose();
        }

        public JobHandle Dispose(JobHandle deps)
        {
            return container.Dispose(deps);
        }

        public void Add(T value)
        {
            container.Add(value);
            BubbleUp(container.Length - 1);
        }

        public T PeekTop()
        {
            return container[0];
        }

        public T PeekBottom()
        {
            return container[container.Length - 1];
        }

        public T Pop()
        {
            return RemoveAt(0);
        }

        public T RemoveAt(int index)
        {
            var value = container[index];
            container.RemoveAtSwapBack(index);
            BubbleDown(index);
            return value;
        }

        public T ElementAt(int index)
        {
            return container[index];
        }

        void BubbleUp(int index)
        {
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (container[index].CompareTo(container[parent]) * flip < 0)
                {
                    var temp = container[index];
                    container[index] = container[parent];
                    container[parent] = temp;
                    index = parent;
                }
                else
                {
                    break;
                }
            }
        }

        void BubbleDown(int index)
        {
            while (index < container.Length)
            {
                var left = index * 2 + 1;
                var right = left + 1;
                var smallest = index;

                if (left < container.Length && container[left].CompareTo(container[smallest]) * flip < 0)
                    smallest = left;

                if (right < container.Length && container[right].CompareTo(container[smallest]) * flip < 0)
                    smallest = right;

                if (smallest != index)
                {
                    var temp = container[index];
                    container[index] = container[smallest];
                    container[smallest] = temp;
                    index = smallest;
                }
                else
                {
                    break;
                }
            }
        }
    }

    public unsafe partial struct Machine
    {
        public Arena heap;
        public int* heapPtr;

        /// <summary>
        /// Asserts that the heap has at least the certain length. The heap will be extended if needed. heapPtr will be updated.
        /// </summary>
        /// <param name="words"></param>
        public void EnsureHeapCapacity(int additionalWords)
        {
            heap.ResizeUninitialized(heap.Length + additionalWords);
        }

        /// <summary>
        /// Load data from the heap at the given index with the given length to the stack
        /// </summary>
        /// <param name="index"></param>
        /// <param name="wordLen"></param>
        unsafe void LoadHeap(int index, byte wordLen)
        {
            EnsureStackCapacity(wordLen);
            UnsafeUtility.MemCpy(stackHeadPtr, heapPtr + index, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen;
        }

        /// <summary>
        /// Writes data from the stack to the heap at the given index with the given length
        /// </summary>
        /// <param name="index"></param>
        /// <param name="wordLen"></param>
        unsafe void WriteHeap(int index, byte wordLen)
        {
            UnsafeUtility.MemCpy(heapPtr + index, stackHeadPtr - wordLen, wordLen * CompilerUtility.WordSize);
            stackHeadPtr -= wordLen;
        }

        /// <summary>
        /// Store data from the stack to the heap with the given length, leaving the heap index on the stack.
        /// </summary>
        /// <param name="wordLen"></param>
        unsafe void StoreHeap(byte wordLen)
        {
            var index = heap.Allocate(wordLen);
            stackHeadPtr -= wordLen;
            UnsafeUtility.MemCpy(heapPtr + index, stackHeadPtr, wordLen * CompilerUtility.WordSize);
            Push(index);
        }

        /// <summary>
        /// Copy data from the constant table to the heap with the given length then push the heap index to the stack.
        /// This is optimization for loading constant data to the heap directly without going through the stack.
        /// </summary>
        /// <param name="constantIndex"></param>
        /// <param name="wordLen"></param>
        unsafe void HeapLoadConstant(int constantIndex, byte wordLen)
        {
            EnsureHeapCapacity(wordLen);
            var heapIndex = heap.Allocate(wordLen);
            UnsafeUtility.MemCpy(heapPtr + heapIndex, constantsPtr + constantIndex, wordLen * CompilerUtility.WordSize);
            Push(heapIndex);
        }

        /// <summary>
        /// Frees a block of memory in the heap with the given length
        /// </summary>
        /// <param name="wordLen"></param>
        unsafe void FreeHeap(byte wordLen)
        {
            heap.Deallocate(Pop<int>(), wordLen);
        }
    }
}