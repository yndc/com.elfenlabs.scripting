namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        /// <summary>
        /// Get string value from heap
        /// </summary>
        /// <param name="heapIndex"></param>
        /// <returns></returns>
        public string GetStringFromHeap(int heapIndex)
        {
            var strLen = *heapPtr + heapIndex;
            return new string((sbyte*)heapPtr, heapIndex + sizeof(int), strLen);
        }
    }
}