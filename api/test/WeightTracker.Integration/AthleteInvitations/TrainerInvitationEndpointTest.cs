using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Integration.TrainerAthletes;

namespace WeightTracker.Integration.AthleteInvitations;

/// <summary>
/// Shared arrangements for the invitation endpoint tests. There is no route that reads an invitation, so its stored
/// state is checked in the database.
/// </summary>
public abstract class TrainerInvitationEndpointTest : TrainerAthleteEndpointTest
{
    protected const string InvitationRoute = "/athlete-invitations";

    protected TrainerInvitationEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected Task<TrainerInvitationEntity> StoredInvitation(Guid invitationId)
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .TrainerInvitations.AsNoTracking().SingleAsync(invitation => invitation.Id == invitationId));

    protected Task<int> StoredInvitationCount()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .TrainerInvitations.AsNoTracking().CountAsync());
}
