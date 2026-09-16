using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ravelin.Architecture.Tests;

[TestClass]
public sealed class DomainPurityTests
{
    [TestMethod]
    public void DomainHasNoInfrastructureFrameworkReferenceOrNamespaceUsage()
    {
        string root = FindRepositoryRoot();
        string domainDirectory = Path.Combine(root, "src", "Ravelin.Domain");
        XDocument project = XDocument.Load(Path.Combine(domainDirectory, "Ravelin.Domain.csproj"));

        Assert.IsFalse(
            project.Descendants("FrameworkReference").Any(),
            "Ravelin.Domain must not acquire framework references.");

        string[] prohibitedTokens =
        [
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
        ];

        foreach (string sourceFile in EnumerateSourceFiles(domainDirectory))
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string token in prohibitedTokens)
            {
                Assert.IsFalse(
                    source.Contains(token, StringComparison.Ordinal),
                    $"Domain source must remain infrastructure-free. Found '{token}' in {sourceFile}.");
            }
        }
    }

    [TestMethod]
    public void InfrastructureDoesNotDeclareDomainNamespaces()
    {
        string root = FindRepositoryRoot();
        string infrastructureDirectory = Path.Combine(root, "src", "Ravelin.Infrastructure");

        foreach (string sourceFile in EnumerateSourceFiles(infrastructureDirectory))
        {
            string source = File.ReadAllText(sourceFile);
            Assert.IsFalse(
                source.Contains("namespace Ravelin.Domain", StringComparison.Ordinal),
                $"Product-domain source must remain in Ravelin.Domain: {sourceFile}");
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Ravelin.slnx")) &&
                File.Exists(Path.Combine(current.FullName, "Directory.Build.props")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        Assert.Fail("Unable to locate repository root from test output directory.");
        return string.Empty;
    }
}
