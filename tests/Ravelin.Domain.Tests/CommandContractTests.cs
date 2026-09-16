using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ravelin.Application.Commands;
using Ravelin.Domain.Authority;
using Ravelin.Domain.Primitives;
using Ravelin.Domain.Resources;

namespace Ravelin.Domain.Tests;

[TestClass]
public sealed class CommandContractTests
{
    [TestMethod]
    public void CommandsCarryStableCommandIdentityAndAuthorityContext()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext authority = new(ActorId.New(), domainId, [Capability.IncidentCommand]);
        CommandId commandId = CommandId.New();
        OpenIncidentCommand command = new(
            commandId,
            authority,
            IncidentId.New(),
            domainId,
            "Incident",
            DateTimeOffset.UtcNow);

        Assert.AreEqual(commandId, command.CommandId);
        Assert.AreSame(authority, command.Authority);
        Assert.IsInstanceOfType<IApplicationCommand>(command);
    }

    [TestMethod]
    public void MutatingCommandsExposeExpectedAggregateVersionWhereRequired()
    {
        AuthorityDomainId domainId = AuthorityDomainId.New();
        AuthorityContext authority = new(ActorId.New(), domainId, [Capability.ResourceCustody]);
        AggregateVersion expected = new(17);
        AllocateResourceCommand command = new(
            CommandId.New(),
            authority,
            ResourceId.New(),
            AllocationId.New(),
            new AllocationTarget(
                domainId,
                IncidentId.New(),
                AssignmentId.New(),
                new CustodyHolder(authority.ActorId, TeamId.New())),
            expected,
            DateTimeOffset.UtcNow);

        Assert.AreEqual(expected, command.ExpectedResourceVersion);
    }
}
