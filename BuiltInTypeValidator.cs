using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    internal abstract class BuiltInTypeValidator
    {
        public readonly string ExpectedTypeString;

        protected BuiltInTypeValidator(Type expectedType)
        {
            ExpectedTypeString = expectedType.FullName;
        }

        private void ReportTypeMismatch(GeneratorExecutionContext context, Location location, string providedType)
        {
            context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.LiteralExpressionTypeMismatchDescriptor, location, ExpectedTypeString, providedType));
        }
        public virtual bool ValidateBool(bool value, GeneratorExecutionContext context, Location location)
        {
            ReportTypeMismatch(context, location, "bool");
            return false;
        }
        public virtual bool ValidateSignedIntegral(long value, GeneratorExecutionContext context, Location location, string providedType)
        {
            ReportTypeMismatch(context, location, providedType);
            return false;
        }
        public virtual bool ValidateUnsignedIntegral(ulong value, GeneratorExecutionContext context, Location location, string providedType)
        {
            ReportTypeMismatch(context, location, providedType);
            return false;
        }
        public virtual bool ValidateFloat(float value, GeneratorExecutionContext context, Location location)
        {
            ReportTypeMismatch(context, location, "float");
            return false;
        }
        public virtual bool ValidateDouble(double value, GeneratorExecutionContext context, Location location)
        {
            ReportTypeMismatch(context, location, "double");
            return false;
        }
        public virtual bool ValidateDecimal(decimal value, GeneratorExecutionContext context, Location location)
        {
            ReportTypeMismatch(context, location, "decimal");
            return false;
        }
        public virtual bool ValidateString(string value, GeneratorExecutionContext context, Location location)
        {
            ReportTypeMismatch(context, location, "string");
            return false;
        }
        public virtual bool ValidateChar(char value, GeneratorExecutionContext context, Location location)
        {
            ReportTypeMismatch(context, location, "char");
            return false;
        }
    }
}
