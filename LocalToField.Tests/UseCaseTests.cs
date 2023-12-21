using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace LocalToField.Tests
{
    public class UseCaseTests
    {
        private readonly ITestOutputHelper _output;

        public UseCaseTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task StringLocal_InitializedByLiteral()
        {
            const string original =
@"class SomeClass
{
    void SomeMethod()
    {
        string someString = ""Something"";
    }
}";

            const string expected =
@"class SomeClass
{
    private readonly string _someString;

    public SomeClass()
    {
        _someString = ""Something"";
    }

    void SomeMethod()
    {
    }
}";

            await ExecuteExampleAsync(original, expected, "someString");
        }

        [Fact]
        public async Task IntLocal_InitializedByLiteral()
        {
            const string original =
@"class SomeClass
{
    void SomeMethod()
    {
        int someInteger = 32;
    }
}";

            const string expected =
@"class SomeClass
{
    private readonly int _someInteger;

    public SomeClass()
    {
        _someInteger = 32;
    }

    void SomeMethod()
    {
    }
}";

            await ExecuteExampleAsync(original, expected, "someInteger");
        }

        [Fact]
        public async Task LocalVariableInNamespace_ProperlyIndented()
        {
            const string original =
@"namespace SomeNamespace
{
    class SomeClass
    {
        void SomeMethod()
        {
            float someFloat = 16.32f;
        }
    }
}
";

            const string expected =
@"namespace SomeNamespace
{
    class SomeClass
    {
        private readonly float _someFloat;

        public SomeClass()
        {
            _someFloat = 16.32f;
        }

        void SomeMethod()
        {
        }
    }
}
";

            await ExecuteExampleAsync(original, expected, "someFloat");
        }

        [Fact]
        public async Task ClassType_InitializedByNew()
        {
            const string original =
@"namespace SomeNamespace
{
    class SomeClass
    {
        void SomeMethod()
        {
            SomeType someType = new SomeType();
        }
    }
}
";

            const string expected =
@"namespace SomeNamespace
{
    class SomeClass
    {
        private readonly SomeType _someType;

        public SomeClass()
        {
            _someType = new SomeType();
        }

        void SomeMethod()
        {
        }
    }
}
";

            await ExecuteExampleAsync(original, expected, "someType");
        }

        [Fact]
        public async Task ClassType_InitializedByImplicitNew()
        {
            const string original =
@"namespace SomeNamespace
{
    class SomeClass
    {
        void SomeMethod()
        {
            SomeType someType = new();
        }
    }
}
";

            const string expected =
@"namespace SomeNamespace
{
    class SomeClass
    {
        private readonly SomeType _someType;

        public SomeClass()
        {
            _someType = new SomeType();
        }

        void SomeMethod()
        {
        }
    }
}
";

            await ExecuteExampleAsync(original, expected, "someType");
        }

        [Fact]
        public async Task ClassType_InitializedByNew_WithLiteralParameter()
        {
            const string original =
@"namespace SomeNamespace
{
    class SomeClass
    {
        void SomeMethod()
        {
            SomeType someType = new SomeType(10);
        }
    }
}
";

            const string expected =
@"namespace SomeNamespace
{
    class SomeClass
    {
        private readonly SomeType _someType;

        public SomeClass()
        {
            _someType = new SomeType(10);
        }

        void SomeMethod()
        {
        }
    }
}
";

            await ExecuteExampleAsync(original, expected, "someType");
        }

        [Fact]
        public async Task ClassType_InitializedByImplicitNew_WithLiteralParameter()
        {
            const string original =
@"namespace SomeNamespace
{
    class SomeClass
    {
        void SomeMethod()
        {
            SomeType someType = new(10);
        }
    }
}
";

            const string expected =
@"namespace SomeNamespace
{
    class SomeClass
    {
        private readonly SomeType _someType;

        public SomeClass()
        {
            _someType = new SomeType(10);
        }

        void SomeMethod()
        {
        }
    }
}
";

            await ExecuteExampleAsync(original, expected, "someType");
        }

        [Fact]
        public async Task LackOfSpaceAroundAssignmentOperator()
        {
            const string original =
@"namespace SomeNamespace
{
    class SomeClass
    {
        void SomeMethod()
        {
            SomeType someType=new SomeType();
        }
    }
}
";

            const string expected =
@"namespace SomeNamespace
{
    class SomeClass
    {
        private readonly SomeType _someType;

        public SomeClass()
        {
            _someType = new SomeType();
        }

        void SomeMethod()
        {
        }
    }
}
";

            await ExecuteExampleAsync(original, expected, "someType");
        }

        private async Task ExecuteExampleAsync(string original, string expected, string localVariableName)
        {
            AdhocWorkspace workspace = new();
            ProjectId projectId = ProjectId.CreateNewId();
            VersionStamp versionStamp = VersionStamp.Create();
            ProjectInfo projectInfo = ProjectInfo.Create(projectId, versionStamp, "NewProject", "projName", LanguageNames.CSharp);
            Project project = workspace.AddProject(projectInfo);

            SourceText originalSource = SourceText.From(original);

            Document document = workspace.AddDocument(project.Id, "NewFile.cs", originalSource);

            int caretPosition = originalSource.ToString().IndexOf(localVariableName);
            TextSpan span = new(caretPosition, length: 0);

            IntroduceField introduceField = new(document, new TestOutputDebugLog(_output));

            LocalDeclarationStatementSyntax? local = await introduceField.FindLocalDeclarationAsync(span);
            Assert.NotNull(local);

            Document updatedDocument = await introduceField.FromLocalAsync(local);

            SourceText expectedSource = SourceText.From(expected);
            SourceText? updatedSource = await updatedDocument.GetTextAsync();

            try
            {
                Assert.Equal(expectedSource.ToString(), updatedSource.ToString());
            }
            catch (EqualException)
            {
                _output.WriteLine(updatedSource.ToString());

                throw;
            }
        }
    }
}