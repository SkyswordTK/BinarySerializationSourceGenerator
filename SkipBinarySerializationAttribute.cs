using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class SkipBinarySerializationAttribute : Attribute
    {
        public SkipBinarySerializationAttribute() { }
        public SkipBinarySerializationAttribute(string defaultValue, bool stringIsExpression = false) { }
        public SkipBinarySerializationAttribute(bool defaultValue) { }
        public SkipBinarySerializationAttribute(char defaultValue) { }
        public SkipBinarySerializationAttribute(long defaultValue) { }
        public SkipBinarySerializationAttribute(ulong defaultValue) { }
        public SkipBinarySerializationAttribute(float defaultValue) { }
        public SkipBinarySerializationAttribute(double defaultValue) { }
        public SkipBinarySerializationAttribute(decimal defaultValue) { }

    }
}
