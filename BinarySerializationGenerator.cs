using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BinarySerializationGenerator
{
    [Generator]
    public class BinarySerializationGenerator : ISourceGenerator
    {
        public static DiagnosticDescriptor NotYetSupportedDescriptor { get; private set; }
        public static DiagnosticDescriptor TypeNotPartialDescriptor { get; private set; }
        public static DiagnosticDescriptor ParentNotPartialDescriptor { get; private set; }
        public static DiagnosticDescriptor InterpolatedStringNotAllowedDescriptor { get; private set; }
        public static DiagnosticDescriptor LiteralExpressionNotConstantDescriptor { get; private set; }
        public static DiagnosticDescriptor LiteralExpressionTypeNotSupportedDescriptor { get; private set; }
        public static DiagnosticDescriptor LiteralExpressionTypeMismatchDescriptor { get; private set; }
        public static DiagnosticDescriptor TypeMustNotBeAbstractDescriptor { get; private set; }
        public static DiagnosticDescriptor TypeMustNotBeAnInterfaceDescriptor { get; private set; }


        public void Initialize(GeneratorInitializationContext context)
        {
            string descriptorCategory = "Compiler (SourceGen)";
            string[] descriptorTags = new string[] { "generator", "serialize" };

            NotYetSupportedDescriptor = new DiagnosticDescriptor(
                id: "BS0000",
                title: "Feature not yet supported.",
                messageFormat: "{0} not yet supported by the BinarySerializationSourceGenerator. However future support is planned.",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "This feature is not yet supported by the BinarySerializationSourceGenerator.",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            TypeNotPartialDescriptor = new DiagnosticDescriptor(
                id: "BS0001",
                title: "BinarySerializableAttribute requires partial Type.",
                messageFormat: "BinarySerializableAttribute requires the Type to be declared partial.",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "BinarySerializableAttribute requires the Type to be declared partial.",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            ParentNotPartialDescriptor = new DiagnosticDescriptor(
                id: "BS0002",
                title: "BinarySerializableAttribute requires partial parent Types.",
                messageFormat: "BinarySerializableAttribute on nested Types requires the entire parent Type chain to be declared partial.",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "BinarySerializableAttribute on nested Types requires the entire parent Type chain to be declared partial.",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            InterpolatedStringNotAllowedDescriptor = new DiagnosticDescriptor(
                id: "BS0003",
                title: "SkipBinarySerializationAttribute can only assign a compile time constant expression.",
                messageFormat: @"SkipBinarySerializationAttribute can only assign a compile time constant expression. Interpolated strings are not supported. If you intended to assign an interpolated string, try ""$""This is {var} example string."""" instead of $""This is {var} example string."".",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: @"SkipBinarySerializationAttribute can only assign a compile time constant expression. Interpolated strings are not supported. If you intended to use an interpolated string as expression, try ""$\""This is {var} example string.\"""" instead of $""This is {var} example string."".",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            LiteralExpressionNotConstantDescriptor = new DiagnosticDescriptor(
                id: "BS0004",
                title: "SkipBinarySerializationAttribute can only assign a compile time constant expression.",
                messageFormat: @"SkipBinarySerializationAttribute can only assign a compile time constant literal. Failed to parse argument as constant value.",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: @"SkipBinarySerializationAttribute can only assign a compile time constant literal. Failed to parse argument as constant value.",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            LiteralExpressionTypeNotSupportedDescriptor = new DiagnosticDescriptor(
                id: "BS0005",
                title: "Type unknown.",
                messageFormat: @"Support for type {0} is not implemented. This is an error in the SourceGenerator.",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: @"Support for type {0} is not implemented. This is an error in the SourceGenerator.",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            LiteralExpressionTypeMismatchDescriptor = new DiagnosticDescriptor(
                id: "BS0006",
                title: "Type mismatch: Expected type {0} but got {1}.",
                messageFormat: @"Type mismatch: Expected type {0} but got {1}.",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: @"Type mismatch: Expected type {0} but got {1}.",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            TypeMustNotBeAbstractDescriptor = new DiagnosticDescriptor(
                id: "BS0006",
                title: "Types marked with BinarySerializationAttribute must not be abstract.",
                messageFormat: "Types marked with BinarySerializationAttribute must not be abstract. The type must be directly instantiatable!",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Types marked with BinarySerializationAttribute must not be abstract. The type must be directly instantiatable!",
                helpLinkUri: null,
                customTags: descriptorTags
                );

            TypeMustNotBeAnInterfaceDescriptor = new DiagnosticDescriptor(
                id: "BS0006",
                title: "Types marked with BinarySerializationAttribute must not be interfaces.",
                messageFormat: "Types marked with BinarySerializationAttribute must not be an interface. The type must be directly instantiatable!",
                category: descriptorCategory,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "Types marked with BinarySerializationAttribute must not be an interface. The type must be directly instantiatable!",
                helpLinkUri: null,
                customTags: descriptorTags
                );
        }

        public void Execute(GeneratorExecutionContext context)
        {
            INamedTypeSymbol generatorMarkerAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(BinarySerializableAttribute).FullName);
            INamedTypeSymbol skipFieldAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(SkipBinarySerializationAttribute).FullName);
            INamedTypeSymbol includeFieldAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(IncludeInBinarySerializationAttribute).FullName);

            foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
            {
                SyntaxNode rootNode = syntaxTree.GetRoot();
                if (rootNode == null)
                    continue;

                SemanticModel semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                foreach(SyntaxNode syntaxNode in rootNode.DescendantNodes())
                {
                    if (syntaxNode is null)
                        continue;
                    if (!(syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax))
                        continue;

                    if (!AttributeHelper.TryFindAttributeSyntax(semanticModel, typeDeclarationSyntax, generatorMarkerAttributeSymbol, out AttributeSyntax binarySerializationAttributeSyntax))
                        continue;

                    OutputWriter.WriteOutputForType(context, syntaxTree, semanticModel, typeDeclarationSyntax, binarySerializationAttributeSyntax, skipFieldAttributeSymbol, includeFieldAttributeSymbol);
                }
            }
        }

    }
}
