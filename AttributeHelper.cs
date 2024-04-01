using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinarySerializationGenerator
{
    internal static class AttributeHelper
    {
        public static bool TryFindAttributeSyntax(SemanticModel semanticModel, SyntaxNode syntaxNode, INamedTypeSymbol attributeSymbol, out AttributeSyntax attributeSyntax)
        {
            foreach (SyntaxNode node in syntaxNode.ChildNodes())
            {
                if (node is AttributeListSyntax attributeList)
                {
                    foreach (AttributeSyntax attributeNode in attributeList.Attributes)
                    {
                        //debugBuilder?.AppendLine("\n\n\nChildren:");
                        //foreach(SyntaxNode child in attributeNode.ChildNodes())
                        //{
                        //    debugBuilder?.Append(child.GetType().Name);
                        //    debugBuilder?.AppendLine(child.ToString());
                        //}
                        //debugBuilder?.AppendLine("\n\n\nDescendants:");
                        //foreach (SyntaxNode descendant in attributeNode.DescendantNodes())
                        //{
                        //    debugBuilder?.Append(descendant.GetType().Name);
                        //    debugBuilder?.AppendLine(descendant.ToString());
                        //}
                        if (semanticModel.GetTypeInfo(attributeNode).Type.Name.Equals(attributeSymbol.Name))
                        {
                            attributeSyntax = attributeNode;
                            return true;
                        }
                    }
                    //return false; //TODO: if there can only be one AttributeListSyntax: return false early
                }
                else
                {
                    continue;
                }
            }
            attributeSyntax = null;
            return false;
        }
    }
}
