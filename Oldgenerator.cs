//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Text;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;

//namespace BinarySerializationGenerator
//{
//    [Generator]
//    public class BinarySerializationGenerator : ISourceGenerator
//    {
//        public void Initialize(GeneratorInitializationContext context)
//        {
//            // No initialization required for this one
//        }

//        public void Execute(GeneratorExecutionContext context)
//        {
//            //Console.WriteLine("Source Generator Debug Output");
//            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(BinarySerializableAttribute).FullName);

//            IEnumerable<SyntaxTree> classesWithAttributes = GetClassesWithAttrubutes(context);

//            foreach (SyntaxTree tree in classesWithAttributes)
//            {

//                context.AddSource($"init", SourceText.From("", Encoding.UTF8));
//                SemanticModel semanticModel = context.Compilation.GetSemanticModel(tree);

//                context.AddSource($"init2", SourceText.From("", Encoding.UTF8));

//                foreach (StructDeclarationSyntax declaredStruct in tree
//                    .GetRoot()
//                    .DescendantNodes()
//                    .OfType<StructDeclarationSyntax>()
//                    .Where(cd => cd.DescendantNodes().OfType<AttributeSyntax>().Any()))
//                {

//                }

//                int i = 0;
//                foreach (ClassDeclarationSyntax declaredClass in tree
//                    .GetRoot()
//                    .DescendantNodes()
//                    .OfType<ClassDeclarationSyntax>()
//                    .Where(cd => cd.DescendantNodes().OfType<AttributeSyntax>().Any()))
//                {
//                    context.AddSource($"{++i}", SourceText.From("/* " + declaredClass.ToString() + " */", Encoding.UTF8));
//                    //List<SyntaxToken> nodes = declaredClass
//                    //.DescendantNodes()
//                    //.OfType<AttributeSyntax>()
//                    //.FirstOrDefault(a => a.DescendantTokens().Any(dt => dt.IsKind(SyntaxKind.IdentifierToken) && semanticModel.GetTypeInfo(dt.Parent).Type.Name == attributeSymbol.Name))
//                    //?.DescendantTokens()
//                    //?.Where(dt => dt.IsKind(SyntaxKind.IdentifierToken))
//                    //?.ToList();

//                    //if (nodes == null)
//                    //{
//                    //    continue;
//                    //}

//                    if (!Util.TryParseClassTokens(declaredClass.ChildTokens(), out string accessModifier, out string typeName))
//                    {
//                        continue;
//                    }

//                    StringBuilder debugStr = new StringBuilder();
//                    debugStr.AppendLine("/*");
//                    debugStr.AppendLine("Access Modifier: " + accessModifier);
//                    debugStr.AppendLine("TypeName: " + typeName);
//                    debugStr.AppendLine("Tokens:");
//                    foreach (SyntaxToken syntaxToken in declaredClass.ChildTokens())
//                    {
//                        debugStr.AppendLine(syntaxToken.ToString());
//                    }
//                    debugStr.AppendLine("");
//                    debugStr.AppendLine("Nodes:");
//                    foreach (SyntaxNode node in declaredClass.DescendantNodesAndSelf())
//                    {
//                        debugStr.AppendLine(node.ToString());
//                    }
//                    debugStr.AppendLine("*/");

//                    //TypeInfo relatedClass = semanticModel.GetTypeInfo(nodes.Last().Parent);
//                    //TypeInfo relatedClass = semanticModel.GetTypeInfo(declaredClass.ChildNodes().First().Parent);
//                    string containingNamespace = "SerializationGeneratorTest"; // relatedClass.Type.ContainingNamespace?.Name ?? null;
//                    //string typeName = "TestClass"; // relatedClass.Type.Name;
//                    //string accessModifier = "public";

//                    context.AddSource($"{i}_1", SourceText.From(debugStr.ToString(), Encoding.UTF8));

//                    //foreach(AttributeData attributeData in relatedClass.Type.GetAttributes())
//                    //{
//                    //    if (SymbolEqualityComparer.Equals(attributeData.AttributeClass == attributeSymbol)
//                    //    {

//                    //    }
//                    //}


//                    PartialTypedefHelper partialTypedefHelper = new PartialTypedefHelper(containingNamespace, typeIsClass: true, typeName, accessModifier, "Serialize", "Deserialize");

//                    //context.AddSource($"{i}_2", SourceText.From("", Encoding.UTF8));



//                    //foreach (IFieldSymbol fieldSymbol in relatedClass.Type.GetMembers().OfType<IFieldSymbol>())
//                    //{
//                    //    //partialTypedefHelper.AddField(fieldSymbol.Name, fieldSymbol.Type);
//                    //    partialTypedefHelper.AddField("intField", typeof(int));
//                    //    partialTypedefHelper.AddField("stringField", typeof(string));
//                    //}

//                    partialTypedefHelper.AddField("intField", typeof(int));
//                    partialTypedefHelper.AddField("stringField", typeof(string));

//                    //context.AddSource($"{i}_3", SourceText.From("", Encoding.UTF8));



//                    //StringBuilder generatedClass = this.GenerateClass(relatedClass);

//                    //foreach (MethodDeclarationSyntax classMethod in declaredClass.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>())
//                    //{
//                    //    this.GenerateMethod(declaredClass.Identifier, relatedClass, classMethod, ref generatedClass);
//                    //}

//                    //this.CloseClass(generatedClass);

//                    context.AddSource($"{declaredClass.Identifier}_", SourceText.From(partialTypedefHelper.BuildClassFile(), Encoding.UTF8));
//                }
//            }
//        }

//        public static IEnumerable<SyntaxTree> GetClassesWithAttrubutes(GeneratorExecutionContext context)
//        {
//            return context.Compilation.SyntaxTrees.Where(
//                st => st.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
//                    .Any(p => p.DescendantNodes().OfType<AttributeSyntax>().Any()));
//        }


//        //        private void GenerateMethod(SyntaxToken moduleName, TypeInfo relatedClass, MethodDeclarationSyntax methodDeclaration, ref StringBuilder builder)
//        //        {
//        //            var signature = $"{methodDeclaration.Modifiers} {relatedClass.Type.Name} {methodDeclaration.Identifier}(";

//        //            var parameters = methodDeclaration.ParameterList.Parameters.Skip(1);

//        //            signature += string.Join(", ", parameters.Select(p => p.ToString())) + ")";

//        //            var methodCall = $"return this._wrapper.{moduleName}.{methodDeclaration.Identifier}(this, {string.Join(", ", parameters.Select(p => p.Identifier.ToString()))});";

//        //            builder.AppendLine(@"
//        //        " + signature + @"
//        //        {
//        //            " + methodCall + @"
//        //        }");
//        //        }

//        //        private StringBuilder GenerateClass(TypeInfo relatedType)
//        //        {
//        //            var sb = new StringBuilder();

//        //            sb.Append(@"
//        //using System;
//        //using System.Collections.Generic;
//        //using SpeedifyCliWrapper.Common;

//        //namespace SpeedifyCliWrapper.ReturnTypes
//        //{
//        //    public partial class " + relatedType.Type.Name);
//        //            sb.Append(@"
//        //    {");

//        //            return sb;
//        //        }

//        //        private void CloseClassOrStruct(StringBuilder generatedType)
//        //        {
//        //            generatedType.AppendLine("    }");
//        //            generatedType.AppendLine("}");
//        //        }

//        //    }
//    }
//}
