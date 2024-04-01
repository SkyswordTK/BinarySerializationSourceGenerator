using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    internal static class BuiltInTypeValidators
    {
        public sealed class BoolValidator : BuiltInTypeValidator
        {
            public BoolValidator() : base(typeof(bool))
            {

            }

            public override bool ValidateBool(bool value, GeneratorExecutionContext context, Location location) { return true; }
        }
        public sealed class CharValidator : BuiltInTypeValidator
        {
            public CharValidator() : base(typeof(char))
            {

            }

            public override bool ValidateChar(char value, GeneratorExecutionContext context, Location location) { return true; }
        }
        public sealed class StringValidator : BuiltInTypeValidator
        {
            public StringValidator() : base(typeof(string))
            {

            }

            //TODO: report diagnostic warning if null is passed
            public override bool ValidateString(string value, GeneratorExecutionContext context, Location location) { return true; }
        }
        public sealed class FloatValidator : BuiltInTypeValidator
        {
            public FloatValidator() : base(typeof(float))
            {

            }

            public override bool ValidateFloat(float value, GeneratorExecutionContext context, Location location) { return true; }
        }
        public sealed class DoubleValidator : BuiltInTypeValidator
        {
            public DoubleValidator() : base(typeof(double))
            {

            }

            public override bool ValidateFloat(float value, GeneratorExecutionContext context, Location location) { return true; }
            public override bool ValidateDouble(double value, GeneratorExecutionContext context, Location location) { return true; }
        }
        public sealed class DecimalValidator : BuiltInTypeValidator
        {
            public DecimalValidator() : base(typeof(decimal))
            {
            
            }

            public override bool ValidateFloat(float value, GeneratorExecutionContext context, Location location) { return true; }
            public override bool ValidateDouble(double value, GeneratorExecutionContext context, Location location) { return true; }
            public override bool ValidateDecimal(decimal value, GeneratorExecutionContext context, Location location) { return true; }
        }
        public sealed class SignedIntegralValidator : BuiltInTypeValidator
        {
            private readonly long minValue;
            private readonly long maxValue;
            public SignedIntegralValidator(Type type, long minValue, long maxValue) : base(type)
            {
                this.minValue = minValue;
                this.maxValue = maxValue;
            }

            public override bool ValidateSignedIntegral(long value, GeneratorExecutionContext context, Location location, string providedType)
            {
                return minValue <= value && value <= maxValue;
            }
            public override bool ValidateUnsignedIntegral(ulong value, GeneratorExecutionContext context, Location location, string providedType)
            {
                if (value > long.MaxValue)
                {
                    return false;
                }
                long lValue = (long)value;
                return minValue <= lValue && lValue <= maxValue;
            }
        }
        public sealed class UnsignedIntegralValidator : BuiltInTypeValidator
        {
            private readonly ulong minValue;
            private readonly ulong maxValue;

            public UnsignedIntegralValidator(Type type, ulong minValue, ulong maxValue) : base(type)
            {
                this.minValue = minValue;
                this.maxValue = maxValue;
            }

            public override bool ValidateSignedIntegral(long value, GeneratorExecutionContext context, Location location, string providedType)
            {
                return 0 <= value && (ulong)value <= maxValue;
            }
            public override bool ValidateUnsignedIntegral(ulong value, GeneratorExecutionContext context, Location location, string providedType)
            {
                return minValue <= value && value <= maxValue;
            }
        }
    }
}
