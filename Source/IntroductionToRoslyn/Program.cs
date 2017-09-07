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
            /// Step 1: Building the workspace
            BuildWorkspace();

            /// Step 2: Loading our code
            LoadCodeIntoCompilation();

            /// Step 3: Getting the current compilation state
            RetrieveCompilationState();

            /// Step 4: Exploring our codes syntax tree
            FindClassSyntax();

            /// Step 5: Mutating our syntax tree
            TransformSyntax();

            /// Step 6: Exporting our code
            ExportTransformedSyntax();
        }

        private static void BuildWorkspace()
        {
            // Firstly, we create our workspace.
            Workspace = new AdhocWorkspace();

            // Next, we create the solution and project that houses our source code.
            // A solution and project map to the 
            Solution = Workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
            Project = Solution.AddProject("MyCodeBase", "MyCodeBase", LanguageNames.CSharp);

            // To load the assemblies for our stand-alone Roslyn based app,
            // we are doing a little trick to find the essential assemblies.
            // Here I'm scanning the app domain for mscorlib, System and System.Core 
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var mscorlib = assemblies.FirstOrDefault(a => a.GetName().Name == "mscorlib");
            var system = assemblies.FirstOrDefault(a => a.GetName().Name == "System");
            var systemCore = assemblies.FirstOrDefault(a => a.GetName().Name == "System.Core");

            // As Roslyns solution and project model is immutable, each time we mutate the Project we
            // need to then use the result for future operations.
            Project = Project.AddMetadataReference(MetadataReference.CreateFromFile(mscorlib.Location));
            Project = Project.AddMetadataReference(MetadataReference.CreateFromFile(system.Location));
            Project = Project.AddMetadataReference(MetadataReference.CreateFromFile(systemCore.Location));

            // Lastly, let's set the apps solution to the newly mutated solution that contains the project
            // which references our core assemblies.
            Solution = Project.Solution;

            // Finally, we commit our mutated solution back to the workspace.
            Workspace.TryApplyChanges(Project.Solution);
        }

        private static void LoadCodeIntoCompilation()
        {
            string code = ReadResourceContent(Assembly.GetExecutingAssembly(), "IntroductionToRoslyn.Resources.MyCustomAttribute.cs");

            // Add code to a compilation is as simple as using AddDocument
            // This:
            //  - Creates a new document in the project.
            //  - Readies it for parsing into a SyntaxTree.
            //  - Readies it for parsing into a SyntaxTree.
            Document = Project.AddDocument("MyCustomAttribute.cs", code);

            // Because of Roslyns immutable project model, we need to store our changes back up
            Project = Document.Project;
            Solution = Project.Solution;

            // Commit the mutated solution back into the 
            Workspace.TryApplyChanges(Project.Solution);
        }

        private static void RetrieveCompilationState()
        {
            // Retrieve the compilation, aka the semantic state, for the project.
            // We can use this to resolve symbol information about syntax later on.
            Compilation = Project.GetCompilationAsync().Result;

            // Next, grab our documents SyntaxTree. 
            // We can use this to explore the stucture of our code.
            SyntaxTree = Document.GetSyntaxTreeAsync().Result;

            // Using our documents SyntaxTree and the compilation, let's get the semantic model for our document.
            // We can use the semantic model to perform semantic analysis
            SemanticModel = Compilation.GetSemanticModel(SyntaxTree);
        }

        private static void FindClassSyntax()
        {
            // Firstly, let's use a SyntaxWalker to explore the SyntaxTree for our document.
            // We want to locate the first class declaration in the document so we can perform
            // some semantic analysis on it later.
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

            // Here we use the Compilation to resolve the type symbol for 'System.Attribute'
            // As we loaded a reference to the 'System' assembly to our project, the 'System.Attribute'
            // type will exist in the semantic state of the compilation.
            var attributeType = Compilation.GetTypeByMetadataName("System.Attribute");

            // 
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

            var newSyntax = ClassDeclarationSyntax.AddAttributeLists(attrbuteList);

            var newRoot = rootNode.ReplaceNode(ClassDeclarationSyntax, newSyntax);

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
