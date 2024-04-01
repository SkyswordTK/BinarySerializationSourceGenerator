using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BinarySerializationGenerator
{
    internal static class BuiltInTypeHelper
    {
        public sealed class BuiltInTypeData
        {
            public readonly string BinaryReaderReadMethod;
            public readonly TryParseFunction TryParseFunction;
            public readonly BuiltInTypeValidator BuiltInTypeValidator;

            internal BuiltInTypeData(string binaryReaderReadMethod, TryParseFunction tryParseFunction, BuiltInTypeValidator validator)
            {
                BinaryReaderReadMethod = binaryReaderReadMethod;
                TryParseFunction = tryParseFunction;
                BuiltInTypeValidator = validator;
            }
        }

        public delegate bool TryParseFunction(string value, out object result);
        public delegate bool TypedTryParseFunction<T>(string value, out T result);

        private static Dictionary<string, BuiltInTypeData> TypenameToBinaryReaderMethod = new Dictionary<string, BuiltInTypeData>();

        static BuiltInTypeHelper()
        {
            RegisterPrimitiveType<bool>("ReadBoolean",    bool.TryParse,    new BuiltInTypeValidators.BoolValidator(), "bool", "Boolean");
            RegisterPrimitiveType<byte>("ReadByte",       byte.TryParse,    new BuiltInTypeValidators.UnsignedIntegralValidator(typeof(byte), byte.MinValue, byte.MaxValue), "byte", "Byte");
            RegisterPrimitiveType<sbyte>("ReadSByte",     sbyte.TryParse,   new BuiltInTypeValidators.SignedIntegralValidator(typeof(sbyte), sbyte.MinValue, sbyte.MaxValue), "sbyte", "SByte");
            RegisterPrimitiveType<short>("ReadInt16",     short.TryParse,   new BuiltInTypeValidators.SignedIntegralValidator(typeof(short), short.MinValue, short.MaxValue), "short", "Int16");
            RegisterPrimitiveType<ushort>("ReadUInt16",   ushort.TryParse,  new BuiltInTypeValidators.UnsignedIntegralValidator(typeof(ushort), ushort.MinValue, ushort.MaxValue), "ushort", "UInt16");
            RegisterPrimitiveType<int>("ReadInt32",       int.TryParse,     new BuiltInTypeValidators.SignedIntegralValidator(typeof(int), int.MinValue, int.MaxValue), "int", "Int32");
            RegisterPrimitiveType<uint>("ReadUInt32",     uint.TryParse,    new BuiltInTypeValidators.UnsignedIntegralValidator(typeof(uint), uint.MinValue, uint.MaxValue), "uint", "UInt32");
            RegisterPrimitiveType<long>("ReadInt64",      long.TryParse,    new BuiltInTypeValidators.SignedIntegralValidator(typeof(long), long.MinValue, long.MaxValue), "long", "Int64");
            RegisterPrimitiveType<ulong>("ReadUInt64",    ulong.TryParse,   new BuiltInTypeValidators.UnsignedIntegralValidator(typeof(ulong), ulong.MinValue, ulong.MaxValue), "ulong", "UInt64");
            RegisterPrimitiveType<float>("ReadSingle",    float.TryParse,   new BuiltInTypeValidators.FloatValidator(), "float", "Single");
            RegisterPrimitiveType<double>("ReadDouble",   double.TryParse,  new BuiltInTypeValidators.DoubleValidator(), "double", "Double");
            RegisterPrimitiveType<decimal>("ReadDecimal", decimal.TryParse, new BuiltInTypeValidators.DecimalValidator(), "decimal", "Decimal");
            RegisterPrimitiveType<char>("ReadChar",       char.TryParse,    new BuiltInTypeValidators.CharValidator(), "char", "Char");
            RegisterPrimitiveType<string>("ReadString", (string str, out string obj) => { obj = str; return true; }, new BuiltInTypeValidators.StringValidator(), "string", "String");
        }

        private static void RegisterPrimitiveType<T>(string binaryReaderMethod, TypedTryParseFunction<T> typedTryParseFunction, BuiltInTypeValidator validator, params string[] typeIdentifiers)
        {
            foreach(string typeIdentifier in typeIdentifiers)
            {
                TypenameToBinaryReaderMethod.Add(typeIdentifier, new BuiltInTypeData(binaryReaderMethod, WrapTryParseMethod<T>(typedTryParseFunction), validator));
            }
        }

        private static TryParseFunction WrapTryParseMethod<T>(TypedTryParseFunction<T> typedTryParseFunction)
        {
            return (string str, out object obj) =>
            {
                bool result = typedTryParseFunction(str, out T parsedObj);
                obj = parsedObj;
                return result;
            };
        }

        public static bool TryGetPrimitiveTypeData(string typeDeclarationString, out BuiltInTypeData primitiveTypeData)
        {
            if (typeDeclarationString.StartsWith("System."))
            {
                typeDeclarationString = typeDeclarationString.Substring(7);
            }
            return TypenameToBinaryReaderMethod.TryGetValue(typeDeclarationString, out primitiveTypeData);
        }

    }
}
