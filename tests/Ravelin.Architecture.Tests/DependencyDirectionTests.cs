using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ravelin.Architecture.Tests;

[TestClass]
public sealed class DependencyDirectionTests
{
    private static readonly IReadOnlyDictionary<string, int> LayerOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Ravelin.Domain"] = 0,
        ["Ravelin.Application"] = 1,
        ["Ravelin.Infrastructure"] = 2,
        ["Ravelin.Authority"] = 3,
    };

    [TestMethod]
    public void ProductionProjectsOnlyReferenceLowerLayers()
    {
        string repositoryRoot = FindRepositoryRoot();

        foreach ((string projectName, int projectLayer) in LayerOrder)
        {
            string projectPath = Path.Combine(repositoryRoot, "src", projectName, $"{projectName}.csproj");
            XDocument project = XDocument.Load(projectPath);

            IEnumerable<string> references = project
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFileNameWithoutExtension(value!));

            foreach (string reference in references)
            {
                Assert.IsTrue(
                    LayerOrder.TryGetValue(reference, out int referencedLayer),
                    $"{projectName} references unknown production project {reference}.");

                Assert.IsTrue(
                    referencedLayer < projectLayer,
                    $"{projectName} must not reference same/higher layer {reference}.");
            }
        }
    }

    [TestMethod]
    public void DomainHasNoInfrastructureFrameworkOrPackageDependencies()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot, "src", "Ravelin.Domain", "Ravelin.Domain.csproj");
        XDocument project = XDocument.Load(projectPath);

        string[] prohibitedPackagePrefixes =
        [
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
        ];

        IEnumerable<string> packages = project
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!);

        foreach (string package in packages)
        {
            Assert.IsFalse(
                prohibitedPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
                $"Ravelin.Domain must not depend on infrastructure package {package}.");
        }
    }

    [TestMethod]
    public void PackageVersionsAreNotDeclaredInIndividualProjects()
    {
        string repositoryRoot = FindRepositoryRoot();
        IEnumerable<string> projectFiles = Directory.EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories);

        foreach (string projectFile in projectFiles)
        {
            XDocument project = XDocument.Load(projectFile);
            IEnumerable<XElement> versionedReferences = project
                .Descendants("PackageReference")
                .Where(element => element.Attribute("Version") is not null);

            Assert.IsFalse(
                versionedReferences.Any(),
                $"Package versions must be centralized in Directory.Packages.props: {projectFile}");
        }
    }

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
