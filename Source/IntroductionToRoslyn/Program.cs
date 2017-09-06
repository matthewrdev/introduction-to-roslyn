using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace IntroductionToRoslyn.ConsoleSample
{
    class MainClass
    {
		public static AdhocWorkspace Workspace;

        public static Solution Solution;
		public static Project Project;
        public static Document Document;

		public static Compilation Compilation;
        public static SemanticModel SemanticModel;
        public static SyntaxTree SyntaxTree;
        public static ClassDeclarationSyntax ClassDeclarationSyntax;

        public static void Main(string[] args)
        {
            BuildWorkspace();

			LoadCodeIntoCompilation();

			RetrieveCompilationState();

            FindClassSyntax();

            TransformSyntax();

            ExportTransformedSyntax();
        }

		private static void BuildWorkspace()
		{
			Workspace = new AdhocWorkspace();

			Solution = Workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
			Project = Solution.AddProject("MyCodeBase", "MyCodeBase", LanguageNames.CSharp);

			LoadAssemblies();

			Workspace.TryApplyChanges(Project.Solution);

			Solution = Project.Solution;
		}

		private static void LoadAssemblies()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			var mscorlib = assemblies.FirstOrDefault(a => a.GetName().Name == "mscorlib");
			var system = assemblies.FirstOrDefault(a => a.GetName().Name == "System");
			var systemCore = assemblies.FirstOrDefault(a => a.GetName().Name == "System.Core");

            Project = Project.AddMetadataReference(MetadataReference.CreateFromFile(mscorlib.Location));
			Project = Project.AddMetadataReference(MetadataReference.CreateFromFile(system.Location));
			Project = Project.AddMetadataReference(MetadataReference.CreateFromFile(systemCore.Location));
		}

        private static void LoadCodeIntoCompilation()
		{
            string code = ReadResourceContent(Assembly.GetExecutingAssembly(), "IntroductionToRoslyn.Resources.MyCustomAttribute.cs");

            var doc = Project.AddDocument("MyCustomAttribute.cs", code);

            Project = doc.Project;
            Document = doc;

            Workspace.TryApplyChanges(Project.Solution);
            Solution = Project.Solution;
        }

		private static void RetrieveCompilationState()
		{
			SyntaxTree = Document.GetSyntaxTreeAsync().Result;
			Compilation = Project.GetCompilationAsync().Result;
			SemanticModel = Compilation.GetSemanticModel(SyntaxTree);
		}

		private static void FindClassSyntax()
		{
			var walker = new ClassDeclarationSyntaxWalker();

			walker.Visit(SyntaxTree.GetRoot());

			var classDeclaration = walker.ClassDeclaration;
			if (classDeclaration == null)
			{
				return;
			}

			var baseList = classDeclaration.BaseList;
			if (baseList == null || !baseList.Types.Any())
			{
				return;
			}

			var attributeType = SemanticModel.Compilation.GetTypeByMetadataName("System.Attribute");

			bool derivesFromAttribute = false;
			foreach (var type in baseList.Types)
			{
				var baseType = type as SimpleBaseTypeSyntax;

				if (baseType != null)
				{
					var typeInfo = SemanticModel.GetTypeInfo(baseType.Type);

					if (typeInfo.Type != null
						&& typeInfo.Type.TypeKind != TypeKind.Interface
						&& SymbolHelper.DerivesFrom(typeInfo.Type, attributeType))
					{
						derivesFromAttribute = true;
						break;
					}
				}
			}

			if (!derivesFromAttribute)
			{
				return;
			}

			ClassDeclarationSyntax = classDeclaration;
		}

        public static void TransformSyntax()
        {
			var rootNode = SyntaxTree.GetRoot();

            var attrbuteList = AttributeUsageAnnotationGenerator.GenerateSyntax(AttributeTargets.Class);

            var newClassDeclaration = ClassDeclarationSyntax.AddAttributeLists(attrbuteList);

			var newRoot = rootNode.ReplaceNode(ClassDeclarationSyntax, newClassDeclaration);

            Document = Document.WithSyntaxRoot(newRoot);

            SyntaxTree = Document.GetSyntaxTreeAsync().Result;
        }

		private static void ExportTransformedSyntax()
		{
			var rootNode = SyntaxTree.GetRoot();
			var formattedSyntax = Formatter.Format(rootNode, Workspace);

			Document = Document.WithSyntaxRoot(formattedSyntax);

			// We can use the 
			Document = Simplifier.ReduceAsync(Document).Result;

			var code = Document.GetSyntaxRootAsync().Result;

			string fileName = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MyCustomAttribute.cs");
			File.WriteAllText(fileName, code.ToString());
		}

        public static string ReadResourceContent(Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}
    }
}
