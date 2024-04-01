using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace BinarySerializationGenerator
{
    [BinarySerializable]
    public partial class TemplateClass
    {
        int field1;
        int field2;
        TemplateClass field3;

        private TemplateClass(int field1, int field2, TemplateClass field3, ConstructorCollisionAvoidanceParameter duplicationAvoidanceParameter)
        {
            this.field1 = field1;
            this.field2 = field2;
            this.field3 = field3;
        }

        public void SerializeToStream(BinaryWriter stream, int maxDepth = 8)
        {
            if (maxDepth <= 0) return;
            stream.Write((int)field1);
            stream.Write((int)field2);
            field3.SerializeToStream(stream, maxDepth - 1);
        }

        public static bool Deserialize(BinaryReader stream, out TemplateClass result, int maxDepth = 8)
        {
            result = default;
            if (maxDepth <= 0) return false;
            //throw new InvalidDataContractException("Deserialization exceeded the maxDepth. This may indicate a circular reference between Types marked with the BinarySerializableAttribute. (The type containing a");
            Int32 field1 = stream.ReadInt32();
            Int32 field2 = stream.ReadInt32();
            if (!TemplateClass.Deserialize(stream, out TemplateClass field3, maxDepth - 1)) return false;
            
            result = new TemplateClass(field1, field2, field3, null);
            return true;
        }

        private sealed class ConstructorCollisionAvoidanceParameter
        {
            private ConstructorCollisionAvoidanceParameter() { }
        }
    }
}
