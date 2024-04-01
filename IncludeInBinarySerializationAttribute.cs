using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class IncludeInBinarySerializationAttribute : Attribute
    {
    }
}
