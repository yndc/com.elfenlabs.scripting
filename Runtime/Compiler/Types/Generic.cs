using System;
using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public class GenericType : Type
    {
        public static char ToString(int index)
        {
            return (char)('T' + index);
        }

        public int Index;

        public GenericType(int index) : base(ToString(index).ToString(), 0)
        {
            Index = index;
        }
    }
}