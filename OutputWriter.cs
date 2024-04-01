using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using static BinarySerializationGenerator.BuiltInTypeHelper;

namespace BinarySerializationGenerator
{
    internal static class OutputWriter
    {
        private const string ConstructorDuplicationAvoidanceParameterClassName = "ConstructorDuplicationAvoidanceParameter";
        public static void WriteOutputForType(GeneratorExecutionContext context, SyntaxTree syntaxTree, SemanticModel semanticModel, TypeDeclarationSyntax typeDeclarationSyntax, AttributeSyntax binarySerializationAttributeSyntax, INamedTypeSymbol skipFieldAttributeSymbol, INamedTypeSymbol includeFieldAttributeSymbol)
        {
            if (!IsDeclaredPartial(typeDeclarationSyntax))
            {
                context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.TypeNotPartialDescriptor, binarySerializationAttributeSyntax.GetLocation()));
                return;
            }
            if (IsDeclaredAbstract(typeDeclarationSyntax))
            {
                context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.TypeMustNotBeAbstractDescriptor, binarySerializationAttributeSyntax.GetLocation()));
                return;
            }
            if (IsTypeInterface(typeDeclarationSyntax))
            {
                context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.TypeMustNotBeAnInterfaceDescriptor, binarySerializationAttributeSyntax.GetLocation()));
                return;
            }
            //TODO: if the target type extends another type, the supertype must provide an empty constructor

            CompilationUnitSyntax rootNode = syntaxTree.GetRoot() as CompilationUnitSyntax;
            OutputWriterData data = new OutputWriterData();
            data.TypeDeclaration = typeDeclarationSyntax;
            data.Usings = rootNode.Usings;

            SyntaxNode parent = typeDeclarationSyntax.Parent;
            while (parent != null)
            {
                if (parent is TypeDeclarationSyntax parentTypeDeclaration)
                {
                    if (!IsDeclaredPartial(parentTypeDeclaration))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.ParentNotPartialDescriptor, binarySerializationAttributeSyntax.GetLocation()));
                        return;
                    }

                    data.ParentTypes.Add(parentTypeDeclaration);
                }
                else if (parent is NamespaceDeclarationSyntax parentNamespaceDeclaration)
                {
                    data.Namespace = parentNamespaceDeclaration;
                    break;
                } else if (parent is CompilationUnitSyntax parentCompilationUnit)
                {

                } else
                {
                    throw new NotImplementedException($"The source generator was not designed with a Parent node of type {parent.GetType()} for a node of type TypeDeclarationNode in mind. Aborting code generation as this may cause unpredictable consequences.");
                }
                parent = parent.Parent;
            }

        
            foreach (FieldDeclarationSyntax fieldDeclarationSyntax in typeDeclarationSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                //ignore constant and static fields (non-instance fields)
                foreach (SyntaxToken modifierToken in fieldDeclarationSyntax.Modifiers)
                {
                    SyntaxKind syntaxKind = modifierToken.Kind();
                    if (syntaxKind == SyntaxKind.ConstKeyword ||
                        syntaxKind == SyntaxKind.StaticKeyword)
                    {
                        goto NextField;
                    }
                }

                TypeSyntax typeSyntax = fieldDeclarationSyntax.Declaration.Type;
                TypeInfo fieldTypeInfo = semanticModel.GetTypeInfo(typeSyntax);
                string resolvedTypeStr = fieldTypeInfo.Type.Name;

                if (BuiltInTypeHelper.TryGetPrimitiveTypeData(resolvedTypeStr, out BuiltInTypeHelper.BuiltInTypeData builtInTypeData))
                {
                    //Check if the field is explicitly marked to be skipped
                    if (AttributeHelper.TryFindAttributeSyntax(semanticModel, fieldDeclarationSyntax, skipFieldAttributeSymbol, out AttributeSyntax skipSerializationAttributeSyntax))
                    {
                        if (!(skipSerializationAttributeSyntax.ArgumentList is AttributeArgumentListSyntax argumentListSyntax))
                            goto NextField;

                        data.AddDebugLine("Found arguments " + argumentListSyntax.ToString());

                        bool stringIsExpression = false;
                        if (argumentListSyntax.Arguments.Count != 1)
                        {
                            if (argumentListSyntax.Arguments.Count != 2)
                            {
                                //TODO: Diagnostic error
                                goto NextField;
                            }
                            
                            foreach (SyntaxNode syntaxNode in argumentListSyntax.Arguments[1].ChildNodes())
                            {
                                if (syntaxNode is LiteralExpressionSyntax literalExpression)
                                {
                                    Optional<object> constValue = semanticModel.GetConstantValue(literalExpression);
                                    if (!constValue.HasValue)
                                    {
                                        //TODO: Diagnostic error
                                        goto NextField;
                                    }
                                    if (!(constValue.Value is bool argument2Value))
                                    {
                                        //TODO: Diagnostic error
                                        goto NextField;
                                    }

                                    stringIsExpression = argument2Value;
                                    if (stringIsExpression)
                                    {
                                        data.AddDebugLine("Handling String as Expression");
                                        ProcessSkippedFieldExpressionDeclaration(context, semanticModel, data, fieldDeclarationSyntax, argumentListSyntax, builtInTypeData);
                                    }
                                    else
                                    {
                                        data.AddDebugLine("Handling String as Constant");
                                        ProcessSkippedFieldConstValueDeclaration(context, semanticModel, data, fieldDeclarationSyntax, argumentListSyntax, builtInTypeData);
                                    }
                                    break;
                                }
                            }

                            //TODO: diagnostic error
                            goto NextField;
                        }

                        ProcessSkippedFieldConstValueDeclaration(context, semanticModel, data, fieldDeclarationSyntax, argumentListSyntax, builtInTypeData);
                        goto NextField;
                    }

                    data.Fields.Add(new SerializedFieldInfo(fieldDeclarationSyntax, builtInTypeData.BinaryReaderReadMethod, null));
                    goto NextField;
                }

                //Non-primitive type
                //Field type is not a native type, only include the field(s) if it is marked
                if (!AttributeHelper.TryFindAttributeSyntax(semanticModel, fieldDeclarationSyntax, includeFieldAttributeSymbol, out _))
                {
                    goto NextField;
                }

                //TODO: check if field type has proper TryDeserialize and Serialize methods
                data.Fields.Add(new SerializedFieldInfo(fieldDeclarationSyntax, null, null));

            NextField:;
            }

            WriteResult(context, semanticModel, data);
        }

        private static void ProcessSkippedFieldConstValueDeclaration(GeneratorExecutionContext context, SemanticModel semanticModel, OutputWriterData data, FieldDeclarationSyntax fieldDeclarationSyntax, AttributeArgumentListSyntax argumentListSyntax, BuiltInTypeData builtInTypeData)
        {
            data.AddDebugLine("First argument type: " + argumentListSyntax.Arguments[0].GetType().Name);
            data.AddDebugLine("First argument: " + argumentListSyntax.Arguments[0].ToString());
            foreach (SyntaxNode syntaxNode in argumentListSyntax.Arguments[0].ChildNodes())
            {
                data.AddDebugLine("Child node type: " + syntaxNode.GetType().Name);
                data.AddDebugLine("Child node: " + syntaxNode.ToString());

                string assignedExpression;
                if (syntaxNode is CastExpressionSyntax castExpression)
                {
                    context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.NotYetSupportedDescriptor, castExpression.GetLocation(), "Cast Expressions are"));
                    return;
                }
                if (syntaxNode is LiteralExpressionSyntax literalExpression)
                {
                    Optional<object> constValue = semanticModel.GetConstantValue(literalExpression);
                    if (!(constValue.HasValue))
                    {
                        //display error: failed to fetch constand value
                        context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.LiteralExpressionNotConstantDescriptor, literalExpression.GetLocation()));
                        return;
                    }
                    object constantSemanticValue = constValue.Value;

                    string argumentExpression = literalExpression.ToString();
                    if (!ValidateAndParseObject(builtInTypeData.BuiltInTypeValidator, constantSemanticValue, argumentExpression, context, literalExpression.GetLocation(), out assignedExpression))
                    {
                        return;
                    }

                    data.AddDebugText("Got Expression: ");
                    data.AddDebugLine(argumentExpression);
                    data.AddDebugText("Of Type: ");
                    data.AddDebugLine(constantSemanticValue.GetType().FullName);

                    data.AddDebugText("Constand Value: \"");
                    data.AddDebugText(assignedExpression);
                    data.AddDebugLine("\"");

                    data.AddDebugLine("Assigned expression: \"" + assignedExpression + "\"");
                    data.Fields.Add(new SerializedFieldInfo(fieldDeclarationSyntax, null, assignedExpression));
                    return;
                }
                else if (syntaxNode is InterpolatedStringExpressionSyntax interpolatedStringExpression)
                {
                    //display error: string must be a compile time constant
                    context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.InterpolatedStringNotAllowedDescriptor, interpolatedStringExpression.GetLocation()));
                    return;
                }
                else
                {
                    //check next syntax node
                    continue;
                }
            }

            //TODO: display error: issue in source gen, this should not be reachable by design
            return;
        }

        private static void ProcessSkippedFieldExpressionDeclaration(GeneratorExecutionContext context, SemanticModel semanticModel, OutputWriterData data, FieldDeclarationSyntax fieldDeclarationSyntax, AttributeArgumentListSyntax argumentListSyntax, BuiltInTypeData builtInTypeData)
        {
            data.AddDebugLine("First argument type: " + argumentListSyntax.Arguments[0].GetType().Name);
            data.AddDebugLine("First argument: " + argumentListSyntax.Arguments[0].ToString());
            foreach (SyntaxNode syntaxNode in argumentListSyntax.Arguments[0].ChildNodes())
            {
                data.AddDebugLine("Child node type: " + syntaxNode.GetType().Name);
                data.AddDebugLine("Child node: " + syntaxNode.ToString());

                string assignedExpression;

                if (syntaxNode is LiteralExpressionSyntax literalExpression)
                {
                    Optional<object> constValue = semanticModel.GetConstantValue(literalExpression);
                    if (!(constValue.HasValue))
                    {
                        //display error: failed to fetch constand value
                        context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.LiteralExpressionNotConstantDescriptor, literalExpression.GetLocation()));
                        return;
                    }
                    object constantSemanticValue = constValue.Value;
                    
                    if (!(constantSemanticValue is string expressionSemanticValue))
                    {
                        //TODO: display error
                        return;
                    }

                    data.Fields.Add(new SerializedFieldInfo(fieldDeclarationSyntax, null, expressionSemanticValue));
                    return;
                }
                else if (syntaxNode is InterpolatedStringExpressionSyntax interpolatedStringExpression)
                {
                    //display error: string must be a compile time constant
                    context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.InterpolatedStringNotAllowedDescriptor, interpolatedStringExpression.GetLocation()));
                    return;
                }
                else
                {
                    //check next syntax node
                    continue;
                }
            }

            //TODO: display error: issue in source gen, this should not be reachable by design
            return;
        }

        private static bool ValidateAndParseObject(BuiltInTypeValidator validator, object constantSemanticValue, string argumentExpression, GeneratorExecutionContext context, Location location, out string assignExpression)
        {
            bool result;
            if (constantSemanticValue is string str)
            {
                result = validator.ValidateString(str, context, location);
                assignExpression = argumentExpression;
            }
            else if (constantSemanticValue is int valueInt)
            {
                result = validator.ValidateSignedIntegral(valueInt, context, location, "int");
                assignExpression = argumentExpression;
            }
            else if (constantSemanticValue is uint valueUInt)
            {
                result = validator.ValidateUnsignedIntegral(valueUInt, context, location, "uint");
                assignExpression = argumentExpression;
            }
            else if (constantSemanticValue is long valueLong)
            {
                result = validator.ValidateSignedIntegral(valueLong, context, location, "long");
                assignExpression = argumentExpression;
            }
            else if (constantSemanticValue is ulong valueULong)
            {
                result = validator.ValidateUnsignedIntegral(valueULong, context, location, "ulong");
                assignExpression = argumentExpression;
            }
            else if (constantSemanticValue is bool valueBool)
            {
                result = validator.ValidateBool(valueBool, context, location);
                assignExpression = argumentExpression;
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(BinarySerializationGenerator.LiteralExpressionTypeNotSupportedDescriptor, location, constantSemanticValue.GetType().FullName));
                assignExpression = "";
                result = false;
            }

            return result;
        }

        private static string ParseStringLiteral(LiteralExpressionSyntax literalExpressionSyntax, string value)
        {
            return value;
        }
        private static string ParseBoolLiteral(LiteralExpressionSyntax literalExpressionSyntax, bool value)
        {
            return value.ToString();
        }
        private static string ParseIntegralLiteral(LiteralExpressionSyntax literalExpressionSyntax, long value)
        {
            return value.ToString();
        }
        private static string ParseIntegralLiteral(LiteralExpressionSyntax literalExpressionSyntax, ulong value)
        {
            return value.ToString();
        }

        private static bool IsTypeInterface(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            return typeDeclarationSyntax is InterfaceDeclarationSyntax;
        }

        private static bool IsDeclaredAbstract(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            foreach (SyntaxToken token in typeDeclarationSyntax.Modifiers)
            {
                if (token.IsKind(SyntaxKind.AbstractKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsDeclaredPartial(TypeDeclarationSyntax typeDeclarationSyntax)
        {
            foreach(SyntaxToken token in typeDeclarationSyntax.Modifiers)
            {
                if (token.IsKind(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        //private static void ExtractAttributeParameters(AttributeSyntax binarySerializableAttributeSyntax, out string serializeMethodName, out string deserializeMethodName)
        //{
        //    serializeMethodName = "Serialize";
        //    deserializeMethodName = "TryDeserialize";
        //    foreach (SyntaxNode attributeChild in binarySerializableAttributeSyntax.ChildNodes())
        //    {
        //        if (!(attributeChild is AttributeArgumentListSyntax attributeArguments))
        //            continue;

        //        foreach(AttributeArgumentSyntax attributeArgumentSyntax in attributeArguments.Arguments)
        //        {
        //            if (attributeArgumentSyntax.NameEquals is null)
        //                continue;

        //            if (attributeArgumentSyntax.NameEquals.Name.Equals("InstanceSerializationMethodName"))
        //            {
        //                serializeMethodName = attributeArgumentSyntax.Expression.NormalizeWhitespace().ToFullString();
        //            } else if (attributeArgumentSyntax.NameEquals.Name.Equals("StaticDeserializationMethodName"))
        //            {
        //                deserializeMethodName = attributeArgumentSyntax.Expression.NormalizeWhitespace().ToFullString();
        //            }
        //            else if (attributeArgumentSyntax.NameEquals.Name.Equals("InstanceSerializationMethodName"))
        //            {

        //            }
        //        }
        //    }
        //}


        const string indentStep = "    ";
        const int indentLength = 4;
        private static void WriteResult(GeneratorExecutionContext context, SemanticModel semanticModel, OutputWriterData data)
        {
            StringBuilder result = new StringBuilder();
            StringBuilder resultFileName = new StringBuilder(data.TypeDeclaration.Identifier.Text);
            string indent = "";

            foreach (UsingDirectiveSyntax usingDirective in data.Usings)
            {
                if (!(usingDirective is null))
                    result.AppendLine(usingDirective.ToString());
            }

            result.AppendLine();

            if (!(data.Namespace is null))
            {
                result.AppendLine($"namespace {data.Namespace.Name}\n{{\n");
                resultFileName.Append(data.Namespace.Name);
                resultFileName.Append('.');
                indent = indentStep;
            }

            //parent types
            foreach (TypeDeclarationSyntax parentTypeDeclaration in data.ParentTypes)
            {
                OpenType(result, ref indent, parentTypeDeclaration);
            }

            //target type
            OpenType(result, ref indent, data.TypeDeclaration);

            ProcessSerializedFields(semanticModel, data, indent + indentStep, out string constructorParams, out string constructorLines, out string serializationLines, out string deserializationLines);

            //constructor
            result.AppendLine("#pragma warning disable IDE0060");
            result.Append(indent);
            result.Append("private ");
            result.Append(data.TypeDeclaration.Identifier.Text);
            result.Append('(');
            result.Append(constructorParams);
            result.AppendLine(")");
            result.AppendLine("#pragma warning restore IDE0060");
            result.Append(indent);
            result.Append("{\n");
            result.AppendLine(constructorLines);
            result.Append(indent);
            result.AppendLine("}\n");

            //serialization
            result.Append(indent);
            result.Append("public void ");
            result.Append(data.SerializationMethodName);
            result.Append("(BinaryWriter stream, int maxDepth = 8)\n");
            result.Append(indent);
            result.Append("{\n");
            result.Append(indent);
            result.Append(indentStep);
            result.AppendLine("if (maxDepth <= 0) return;");
            result.AppendLine(serializationLines);
            result.Append(indent);
            result.AppendLine("}\n");

            //deserialization
            result.Append(indent);
            result.Append("public static bool ");
            result.Append(data.DeserializationMethodName);
            result.Append("(BinaryReader stream, out ");
            result.Append(data.TypeDeclaration.Identifier);
            result.AppendLine(" result, int maxDepth = 8)");
            result.Append(indent);
            result.AppendLine("{");
            result.Append(indent);
            result.Append(indentStep);
            result.AppendLine("result = default!;");
            result.Append(indent);
            result.Append(indentStep);
            result.AppendLine("if (maxDepth <= 0) return false;");
            result.Append(indent);
            result.AppendLine(indentStep);
            result.AppendLine(deserializationLines);
            result.Append(indent);
            result.Append(indentStep);
            result.AppendLine("return true;");
            result.Append(indent);
            result.AppendLine("}\n");

            result.Append(indent);
            result.Append("private sealed class ");
            result.AppendLine(ConstructorDuplicationAvoidanceParameterClassName);
            result.Append(indent);
            result.Append("{\n");
            result.Append(indent);
            result.Append(indentStep);
            result.Append("private ");
            result.Append(ConstructorDuplicationAvoidanceParameterClassName);
            result.AppendLine("() { }");
            result.Append(indent);
            result.AppendLine("}\n");

            CloseType(result, ref indent);

            foreach (TypeDeclarationSyntax _ in data.ParentTypes)
            {
                CloseType(result, ref indent);
            }

            if (!(data.Namespace is null))
            {
                result.AppendLine("\n}");
            }

            context.AddSource(resultFileName.ToString() + "_Debug", SourceText.From(data.GetDebugText(), Encoding.UTF8));
            context.AddSource(resultFileName.ToString(), SourceText.From(result.ToString(), Encoding.UTF8));
        }

        private static void OpenType(StringBuilder result, ref string indent, TypeDeclarationSyntax typeDeclarationSyntax)
        {
            result.Append(indent);
            foreach (SyntaxToken modifierToken in typeDeclarationSyntax.Modifiers)
            {
                result.Append(modifierToken);
                result.Append(' ');
            }

            result.Append(typeDeclarationSyntax.Keyword);
            result.Append(' ');
            result.Append(typeDeclarationSyntax.Identifier);
            result.AppendLine();
            result.Append(indent);
            result.Append("{\n");
            indent += indentStep;
        }

        private static void CloseType(StringBuilder result, ref string indent)
        {
            indent = indent.Substring(indentLength);
            result.Append(indent);
            result.AppendLine("}");
        }

        private static void ProcessSerializedFields(SemanticModel semanticModel, OutputWriterData data, string indent, out string constructorParams, out string constructorLines, out string serializationLines, out string deserializationLines)
        {
            StringBuilder constructorParamsBuilder = new StringBuilder();
            StringBuilder constructorLinesBuilder = new StringBuilder();
            StringBuilder constructorExpressionLinesBuilder = new StringBuilder();
            StringBuilder serializationLinesBuilder = new StringBuilder();
            StringBuilder deserializationLinesBuilder = new StringBuilder();
            StringBuilder deserializationMethodConstructorCallParamsBuilder = new StringBuilder();

            foreach(SerializedFieldInfo serializedFieldInfo in data.Fields)
            {
                FieldDeclarationSyntax fieldDeclaration = serializedFieldInfo.FieldDeclarationSyntax;
                TypeSyntax typeSyntax = fieldDeclaration.Declaration.Type;
                string typeStr = typeSyntax.ToString();
                string readMethodName = serializedFieldInfo.BinaryReaderMethodOrNull;

                foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
                {
                    ISymbol fieldSymbol = semanticModel.GetDeclaredSymbol(variable);
                    string fieldName = fieldSymbol.Name;
                    if (fieldName.Length <= 0)
                        continue;

                    if (!(serializedFieldInfo.DeserializationAssignedDefaultExpressionOrNull is null))
                    {
                        //the field is initialized with an expression, it is not serialized
                        AddConstructorAssignmentToExpression(constructorExpressionLinesBuilder, indent, fieldName, serializedFieldInfo.DeserializationAssignedDefaultExpressionOrNull);
                    }
                    else
                    {
                        //the field is serialized and deserialized
                        if (readMethodName is null)
                        {
                            AddConstructorAssignedParameter(constructorParamsBuilder, constructorLinesBuilder, indent, typeStr, fieldName);
                            AddFieldSerialization(serializationLinesBuilder, indent, fieldName, data.SerializationMethodName);
                            AddFieldDeserialization(deserializationLinesBuilder, deserializationMethodConstructorCallParamsBuilder, indent, typeStr, fieldName, data.DeserializationMethodName);
                        }
                        else
                        {
                            AddConstructorAssignedParameter(constructorParamsBuilder, constructorLinesBuilder, indent, typeStr, fieldName);
                            AddBuiltInFieldSerialization(serializationLinesBuilder, indent, fieldName);
                            AddBuiltInFieldDeserialization(deserializationLinesBuilder, deserializationMethodConstructorCallParamsBuilder, indent, typeStr, fieldName, readMethodName);
                        }
                    }
                }
            }

            constructorParamsBuilder.AppendLine();
            constructorParamsBuilder.Append(indent);
            constructorParamsBuilder.Append(ConstructorDuplicationAvoidanceParameterClassName);
            constructorParamsBuilder.Append(' ');
            constructorParamsBuilder.AppendLine(ConstructorDuplicationAvoidanceParameterClassName);
            constructorParamsBuilder.Append(indent);

            if (constructorLinesBuilder.Length > 0) constructorLinesBuilder.Length -= 1;    // '\n'
            if (serializationLinesBuilder.Length > 0) serializationLinesBuilder.Length -= 1;  // '\n'
            if (deserializationLinesBuilder.Length > 0) deserializationLinesBuilder.Length -= 1;  // '\n'
            if (constructorExpressionLinesBuilder.Length > 0) constructorExpressionLinesBuilder.Length -= 1; //'\n'


            deserializationMethodConstructorCallParamsBuilder.Append("null!");
            

            constructorParams = constructorParamsBuilder.ToString();
            constructorLinesBuilder.Append('\n');
            constructorLinesBuilder.Append(constructorExpressionLinesBuilder.ToString());
            constructorLines = constructorLinesBuilder.ToString();
            serializationLines = serializationLinesBuilder.ToString();

            deserializationLinesBuilder.Append(indent);
            deserializationLinesBuilder.Append("result = new ");
            deserializationLinesBuilder.Append(data.TypeDeclaration.Identifier.Text);
            deserializationLinesBuilder.Append('(');
            deserializationLinesBuilder.Append(deserializationMethodConstructorCallParamsBuilder.ToString());
            deserializationLinesBuilder.Append(");\n");

            deserializationLines = deserializationLinesBuilder.ToString();
        }

        private static void PrintLastXChars(OutputWriterData data, StringBuilder stringBuilder, int count)
        {
            for(int i = 1; i <= count; i++)
            {
                //data.AddDebugLine(char.GetNumericValue(stringBuilder[stringBuilder.Length - 1]).ToString());
                data.AddDebugLine(((int)stringBuilder[stringBuilder.Length - 1]).ToString());
                data.AddDebugLine("char: '" + stringBuilder[stringBuilder.Length - 1] + "'");
            }
        }

        private static void AddBuiltInFieldSerialization(StringBuilder serializationLines, string indent, string fieldName)
        {
            serializationLines.Append(indent);
            serializationLines.Append("stream.Write(");
            serializationLines.Append(fieldName);
            serializationLines.Append(");\n");
        }
        private static void AddFieldSerialization(StringBuilder serializationLines, string indent, string fieldName, string serializationMethodName)
        {
            serializationLines.Append(indent);
            serializationLines.Append(fieldName);
            serializationLines.Append('.');
            serializationLines.Append(serializationMethodName);
            serializationLines.Append("(stream);\n");
        }

        private static void AddBuiltInFieldDeserialization(StringBuilder deserializationLines, StringBuilder deserializationMethodConstructorCallParams, string indent, string typeStr, string fieldName, string readMethodName)
        {
            deserializationLines.Append(indent);
            deserializationLines.Append(typeStr);
            deserializationLines.Append(' ');
            deserializationLines.Append(fieldName);
            deserializationLines.Append(" = stream.");
            deserializationLines.Append(readMethodName);
            deserializationLines.AppendLine("();");
            AddDeserializationConstructorCallParameter(deserializationMethodConstructorCallParams, fieldName);
        }
        private static void AddFieldDeserialization(StringBuilder deserializationLines, StringBuilder deserializationMethodConstructorCallParams, string indent, string typeStr, string fieldName, string deserializationMethodName)
        {
            deserializationLines.Append(indent);
            deserializationLines.Append("if (!");
            deserializationLines.Append(typeStr);
            deserializationLines.Append('.');
            deserializationLines.Append(deserializationMethodName);
            deserializationLines.Append("(stream, out ");
            deserializationLines.Append(typeStr);
            deserializationLines.Append(' ');
            deserializationLines.Append(fieldName);
            deserializationLines.AppendLine(", maxDepth - 1)) return false;");
            AddDeserializationConstructorCallParameter(deserializationMethodConstructorCallParams, fieldName);
        }
        private static void AddDeserializationConstructorCallParameter(StringBuilder deserializationMethodConstructorCallParams, string fieldName)
        {
            deserializationMethodConstructorCallParams.Append(fieldName);
            deserializationMethodConstructorCallParams.Append(", ");
        }

        private static void AddConstructorAssignedParameter(StringBuilder constructorParams, StringBuilder constructorLines, string indent, string typeStr, string fieldName)
        {
            AddParameter(constructorParams, typeStr, fieldName);
            AddConstructorAssignmentToParameter(constructorLines, indent, fieldName);
        }

        private static void AddConstructorAssignmentToParameter(StringBuilder constructorLines, string indent, string fieldName)
        {
            constructorLines.Append(indent);
            constructorLines.Append("this.");
            constructorLines.Append(fieldName);
            constructorLines.Append(" = ");
            constructorLines.Append(fieldName);
            constructorLines.Append(";\n");
        }

        private static void AddConstructorAssignmentToExpression(StringBuilder constructorLines, string indent, string fieldName, string assignedExpression)
        {
            constructorLines.Append(indent);
            constructorLines.Append("this.");
            constructorLines.Append(fieldName);
            constructorLines.Append(" = ");
            constructorLines.Append(assignedExpression);
            constructorLines.Append(";\n");
        }

        private static void AddParameter(StringBuilder parameters, string typeStr, string fieldName)
        {
            parameters.Append(typeStr);
            parameters.Append(' ');
            parameters.Append(fieldName);
            parameters.Append(", ");
        }



        //private static bool TryGetBuiltInBinaryReaderMethod(string typeDeclarationString, out string methodString)
        //{
        //    if (typeDeclarationString.StartsWith("System."))
        //    {
        //        typeDeclarationString = typeDeclarationString.Substring(7);
        //    }
        //    return TypenameToBinaryReaderMethod.TryGetValue(typeDeclarationString, out methodString);
        //}

        //private static Dictionary<string, string> TypenameToBinaryReaderMethod = new Dictionary<string, string>()
        //{
        //    { "bool", "ReadBoolean" },
        //    { "Boolean", "ReadBoolean" },

        //    { "byte",  "ReadByte" },
        //    { "Byte",  "ReadByte" },
        //    { "sbyte", "ReadSByte" },
        //    { "SByte", "ReadSByte" },

        //    { "short", "ReadInt16" },
        //    { "Int16", "ReadInt16" },
        //    { "ushort", "ReadUInt16" },
        //    { "UInt16", "ReadUInt16" },

        //    { "int",   "ReadInt32" },
        //    { "Int32", "ReadInt32" },
        //    { "uint",   "ReadUInt32" },
        //    { "UInt32", "ReadUInt32" },

        //    { "long",  "ReadInt64" },
        //    { "Int64", "ReadInt64" },
        //    { "ulong",  "ReadUInt64" },
        //    { "UInt64", "ReadUInt64" },


        //    { "float",  "ReadSingle" },
        //    { "Single", "ReadSingle" },

        //    { "double", "ReadDouble" },
        //    { "Double", "ReadDouble" },

        //    { "decimal", "ReadDecimal" },
        //    { "Decimal", "ReadDecimal" },

        //    { "char", "ReadChar" },
        //    { "Char", "ReadChar" },

        //    { "string", "ReadString" },
        //    { "String", "ReadString" },
        //};

    }
}
