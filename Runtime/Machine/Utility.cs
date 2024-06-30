using System.Text;

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
            return Encoding.UTF8.GetString((byte*)heapPtr + heapIndex * sizeof(int) + sizeof(int), *(heapPtr + heapIndex));
        }
    }
}