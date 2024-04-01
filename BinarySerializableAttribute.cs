using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class BinarySerializableAttribute : System.Attribute
    {
        //public BinarySerializableAttribute()
        //{
        //}
    }
}
