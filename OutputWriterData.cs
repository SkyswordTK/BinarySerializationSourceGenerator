using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    internal partial class OutputWriterData
    {
        public string SerializationMethodName = "Serialize";
        public string DeserializationMethodName = "TryDeserialize";

        public SyntaxList<UsingDirectiveSyntax> Usings;

        public NamespaceDeclarationSyntax Namespace;
        public readonly List<TypeDeclarationSyntax> ParentTypes = new List<TypeDeclarationSyntax>();

        public TypeDeclarationSyntax TypeDeclaration;
        public readonly List<SerializedFieldInfo> Fields = new List<SerializedFieldInfo>();


        private readonly string AccessModifier;
        private readonly string TypeName;
        private readonly bool IncludeExtraConstructorParameter; //TODO: add an option to the Attribute to add another parameter to the constructor in case the user is implementing the exhaustive constructor non-private

        public OutputWriterData()
        {
            _debugStr.AppendLine("/*");
        }

        private readonly StringBuilder _debugStr = new StringBuilder();

        public string GetDebugText()
        {
            _debugStr.AppendLine(" */");
            string result = _debugStr.ToString();
            _debugStr.Length = _debugStr.Length - 4;
            return result;
        }

        public void AddDebugLine(string text)
        {
            _debugStr.AppendLine(text);
        }

        public void AddDebugText(string text)
        {
            _debugStr.Append(text);
        }

        public void ClearDebugText()
        {
            _debugStr.Length = 0;
            _debugStr.AppendLine("/*");
        }

    }

}
