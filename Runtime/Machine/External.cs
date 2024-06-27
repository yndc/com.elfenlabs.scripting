using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{   
    public unsafe partial struct Machine
    {
        public delegate bool ExternalFunction(Machine machine);
        
        public NativeArray<FunctionPointer<ExternalFunction>> ExternalFunctions;
        
        /// <summary>
        /// Asserts that external bindings are provided
        /// </summary>
        /// <returns></returns>
        bool AssertExternalBindings()
        {
            for (int i = 0; i < ExternalFunctions.Length; i++)
            {
                if (ExternalFunctions[i].Value == null)
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Call a function pointer from the external functions
        /// </summary>
        /// <param name="index"></param>
        void CallExternal(ushort index)
        {
            ExternalFunctions[index].Invoke(this);
        }
    }
}