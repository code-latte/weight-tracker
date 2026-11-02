using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers invitation lookups and the persistence of their lifecycle.
/// </summary>
public class TrainerInvitationRepositoryTests : IntegrationTest
{
    public TrainerInvitationRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private Task<TResult> OnRepository<TResult>(Func<ITrainerInvitationRepository, Task<TResult>> action)
        => Factory.WithScope(services => action(services.GetRequiredService<ITrainerInvitationRepository>()));

    private Task<bool> Write(Func<ITrainerInvitationRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<ITrainerInvitationRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    [Fact]
    public async Task GetById_ShouldRestoreASeededInvitation()
    {
        // Arrange & Act
        var invitation = await OnRepository(repository => repository.GetById(
            Id.From(SeedData.UnregisteredInvitationId), CancellationToken.None));

        // Assert
        invitation.Should().NotBeNull();
        invitation!.TrainerId.Should().Be(Id.From(SeedData.TrainerId));
        invitation.InviteEmail.Should().Be(Email.From(SeedData.UnregisteredInvitationEmail));
        invitation.UserId.Should().BeNull();
        invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
        invitation.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_ShouldReturnNullForAnUnknownInvitation()
    {
        // Arrange & Act
        var invitation = await OnRepository(repository => repository.GetById(Id.New(), CancellationToken.None));

        // Assert
        invitation.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPendingByTrainerIdAndEmail_ShouldReturnPendingInvitationsExpiredOrNot()
    {
        // Arrange & Act
        var invitations = await OnRepository(repository => repository.GetAllPendingByTrainerIdAndEmail(
            Id.From(SeedData.TrainerId), Email.From(SeedData.UserEmail), CancellationToken.None));

        // Assert
        invitations.Select(invitation => invitation.Id).Should().BeEquivalentTo(
            [Id.From(SeedData.PendingInvitationId), Id.From(SeedData.ExpiredInvitationId)]);
    }

    [Fact]
    public async Task GetAllPendingByTrainerIdAndEmail_ShouldExcludeAnsweredInvitations()
    {
        // Arrange
        await Write(async repository =>
        {
            var pending = await repository.GetById(Id.From(SeedData.PendingInvitationId), CancellationToken.None);
            pending!.Reject(SeedData.CreatedAt.AddDays(1));
            await repository.Save(pending, CancellationToken.None);
        });

        // Act
        var invitations = await OnRepository(repository => repository.GetAllPendingByTrainerIdAndEmail(
            Id.From(SeedData.TrainerId), Email.From(SeedData.UserEmail), CancellationToken.None));

        // Assert
        invitations.Select(invitation => invitation.Id).Should().BeEquivalentTo([Id.From(SeedData.ExpiredInvitationId)]);
    }

    [Fact]
    public async Task Save_ShouldPersistANewInvitationAndItsAcceptance()
    {
        // Arrange
        var invitation = TrainerInvitation.Create(
            Id.From(SeedData.TrainerId), Email.From("new-athlete@weighttracker.test"), null, "new-invitation-hash",
            SeedData.CreatedAt, SeedData.CreatedAt.AddYears(100));
        await Write(repository => repository.Save(invitation, CancellationToken.None));

        // Act
        await Write(async repository =>
        {
            var stored = await repository.GetById(invitation.Id, CancellationToken.None);
            stored!.Accept(SeedData.CreatedAt.AddDays(1));
            await repository.Save(stored, CancellationToken.None);
        });

        // Assert
        var restored = await OnRepository(repository => repository.GetById(invitation.Id, CancellationToken.None));
        restored!.Status.Should().Be(TrainerInvitationStatus.ACCEPTED);
        restored.AcceptedAtUtc.Should().Be(SeedData.CreatedAt.AddDays(1));
        restored.TokenHash.Should().Be("new-invitation-hash");
    }

    [Fact]
    public async Task GetAll_ShouldReturnEveryInvitationNewestFirst()
    {
        // Arrange
        var newest = TrainerInvitation.Create(
            Id.From(SeedData.AdminId), Email.From("newest@weighttracker.test"), null, "newest-invitation-hash",
            SeedData.CreatedAt.AddDays(1), SeedData.CreatedAt.AddYears(100));
        await Write(repository => repository.Save(newest, CancellationToken.None));

        // Act
        var invitations = (await OnRepository(repository => repository.GetAll(CancellationToken.None))).ToList();

        // Assert
        invitations.Should().HaveCount(4);
        invitations.First().Id.Should().Be(newest.Id);
        invitations.Select(invitation => invitation.CreatedAtUtc).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAllByTrainerId_ShouldExcludeOtherTrainersInvitations()
    {
        // Arrange
        var othersInvitation = TrainerInvitation.Create(
            Id.From(SeedData.AdminId), Email.From("other@weighttracker.test"), null, "other-invitation-hash",
            SeedData.CreatedAt, SeedData.CreatedAt.AddYears(100));
        await Write(repository => repository.Save(othersInvitation, CancellationToken.None));

        // Act
        var invitations = await OnRepository(repository => repository.GetAllByTrainerId(
            Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        invitations.Select(invitation => invitation.Id).Should().BeEquivalentTo([
            Id.From(SeedData.PendingInvitationId),
            Id.From(SeedData.ExpiredInvitationId),
            Id.From(SeedData.UnregisteredInvitationId),
        ]);
        invitations.Select(invitation => invitation.Id).Should().NotContain(othersInvitation.Id);
    }

    [Fact]
    public async Task GetAllByTrainerId_ShouldIncludeAnsweredInvitations()
    {
        // Arrange
        await Write(async repository =>
        {
            var revoked = await repository.GetById(Id.From(SeedData.UnregisteredInvitationId), CancellationToken.None);
            revoked!.Revoke(SeedData.CreatedAt.AddDays(1));
            await repository.Save(revoked, CancellationToken.None);

            var accepted = await repository.GetById(Id.From(SeedData.PendingInvitationId), CancellationToken.None);
            accepted!.Accept(SeedData.CreatedAt.AddDays(1));
            await repository.Save(accepted, CancellationToken.None);
        });

        // Act
        var invitations = await OnRepository(repository => repository.GetAllByTrainerId(
            Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        invitations.Select(invitation => invitation.Status).Should().BeEquivalentTo([
            TrainerInvitationStatus.ACCEPTED,
            TrainerInvitationStatus.REVOKED,
            TrainerInvitationStatus.PENDING,
        ]);
    }
}
