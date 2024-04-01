using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace BinarySerializationGenerator
{
    internal class Util
    {
        public static bool TypeSymbolMatchesType(ITypeSymbol typeSymbol, Type type, SemanticModel semanticModel)
        {
            return SymbolEqualityComparer.Default.Equals(typeSymbol, GetTypeSymbolForType(type, semanticModel));
        }

        public static INamedTypeSymbol GetTypeSymbolForType(Type type, SemanticModel semanticModel)
        {

            if (!type.IsConstructedGenericType)
            {
                return semanticModel.Compilation.GetTypeByMetadataName(type.FullName);
            }

            // get all typeInfo's for the Type arguments 
            IEnumerable<INamedTypeSymbol> typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => GetTypeSymbolForType(a, semanticModel));

            Type openType = type.GetGenericTypeDefinition();
            INamedTypeSymbol typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName);
            return typeSymbol.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>());
        }

        public static bool TryParseTypeTokens(TypeDeclarationSyntax typeDeclarationSyntax, out string accessModifiers, out string typeName)
        {
            return TryParseTypeTokens(typeDeclarationSyntax.Keyword.ToString(), typeDeclarationSyntax.ChildTokens(), out accessModifiers, out typeName);
        }

        public static bool TryParseClassTokens(IEnumerable<SyntaxToken> typeDeclarationTokens, out string accessModifiers, out string typeName)
        {
            return TryParseTypeTokens("class", typeDeclarationTokens, out accessModifiers, out typeName);
        }
        private static bool TryParseTypeTokens(string typeIdentifier, IEnumerable<SyntaxToken> typeDeclarationTokens, out string accessModifierString, out string typename)
        {
            StringBuilder accessModifierBuilder = new StringBuilder();
            IEnumerator<SyntaxToken> tokenEnumerator = typeDeclarationTokens.GetEnumerator();
            while(tokenEnumerator.MoveNext())
            {
                SyntaxToken token = tokenEnumerator.Current;
                string tokenText = token.Text;

                if (tokenText.Equals("partial"))
                {
                    goto FoundPartial;
                }

                accessModifierBuilder.Append(tokenText);
                accessModifierBuilder.Append(' ');
            }

            //TODO: Print Generator error: classes annotated with BinarySerializableAttribute must be partial!
            typename = null;
            accessModifierString = null;
            return false;
        FoundPartial:;
            if (tokenEnumerator.MoveNext())
            {
                SyntaxToken token = tokenEnumerator.Current;
                string tokenText = token.Text;

                if (tokenText.Equals(typeIdentifier))
                {
                    if (tokenEnumerator.MoveNext())
                    {
                        typename = tokenEnumerator.Current.Text;
                        accessModifierString = accessModifierBuilder.ToString();
                        return true;
                    }
                }
            }

            //TODO: Print Generator error: failed to parse type (typeIdentifier)
            typename = null;
            accessModifierString = null;
            return false;
        }
    }
}
