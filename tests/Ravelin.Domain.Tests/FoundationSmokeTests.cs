using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class FoundationSmokeTests
{
    [TestMethod]
    public void DomainAssemblyLoads()
    {
        Assembly assembly = Assembly.Load("Ravelin.Domain");

        Assert.AreEqual("Ravelin.Domain", assembly.GetName().Name);
    }
}
