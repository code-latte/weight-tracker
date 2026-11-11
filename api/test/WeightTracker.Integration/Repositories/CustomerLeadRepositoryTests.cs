using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

public class CustomerLeadRepositoryTests : IntegrationTest
{
    public CustomerLeadRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Email LeadEmail = Email.From("lead@example.com");

    private static readonly DateTime Start = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private Task<bool> Write(Func<ICustomerLeadRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<ICustomerLeadRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    private Task<bool> Exists(Email email)
        => Factory.WithScope(services => services.GetRequiredService<ICustomerLeadRepository>()
            .ExistsByEmail(email, CancellationToken.None));

    private Task<List<CustomerLeadEntity>> StoredLeads()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .CustomerLeads.AsNoTracking().ToListAsync());

    [Fact]
    public async Task Add_ShouldStoreTheLeadWithTheGivenValues()
    {
        // Arrange
        var id = Id.New();

        // Act
        await Write(repository => repository.Add(id, LeadEmail, Start, CancellationToken.None));

        // Assert
        var stored = (await StoredLeads()).Should().ContainSingle().Subject;
        stored.Id.Should().Be(id.Value);
        stored.Email.Should().Be("lead@example.com");
        stored.RegisteredAtUtc.Should().Be(Start);
        stored.RegisteredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ExistsByEmail_ShouldBeTrueForAStoredEmail()
    {
        // Arrange
        await Write(repository => repository.Add(Id.New(), LeadEmail, Start, CancellationToken.None));

        // Act
        var exists = await Exists(LeadEmail);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmail_ShouldBeFalseForAnUnknownEmail()
    {
        // Arrange
        await Write(repository => repository.Add(Id.New(), LeadEmail, Start, CancellationToken.None));

        // Act
        var exists = await Exists(Email.From("other@example.com"));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Commit_ShouldRaiseCustomerLeadAlreadyExistsWhenTheEmailIsAlreadyStored()
    {
        // Arrange
        await Write(repository => repository.Add(Id.New(), LeadEmail, Start, CancellationToken.None));

        // Act
        var act = () => Write(repository => repository.Add(Id.New(), LeadEmail, Start.AddMinutes(1), CancellationToken.None));

        // Assert
        await act.Should().ThrowAsync<CustomerLeadAlreadyExistsException>();
        var stored = (await StoredLeads()).Should().ContainSingle().Subject;
        stored.RegisteredAtUtc.Should().Be(Start);
    }
}
