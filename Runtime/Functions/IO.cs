using AOT;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace Elfenlabs.Scripting
{
    public static class IO
    {
        [BurstCompile]
        [MonoPInvokeCallback(typeof(Machine.ExternalFunction))]
        public static unsafe bool Print(Machine machine)
        {
            machine.stackHeadPtr -= 1;
            var heapIndex = *machine.stackHeadPtr;
            var strLen = *machine.heapPtr + heapIndex;
            var str = new string((sbyte*)machine.heapPtr, heapIndex + sizeof(int), strLen);
            Debug.Log(str);
            return true;
        }
    }
}