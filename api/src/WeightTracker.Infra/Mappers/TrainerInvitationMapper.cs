using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

public static class TrainerInvitationMapper
{
    public static TrainerInvitationEntity MapToEntity(TrainerInvitation invitation)
    {
        return new TrainerInvitationEntity
        {
            Id = invitation.Id.Value,
            TrainerId = invitation.TrainerId.Value,
            InviteEmail = invitation.InviteEmail.Value,
            UserId = invitation.UserId?.Value,
            TokenHash = invitation.TokenHash,
            Status = (int)invitation.Status,
            CreatedAtUtc = DateTime.SpecifyKind(invitation.CreatedAtUtc, DateTimeKind.Utc),
            ExpiresAtUtc = DateTime.SpecifyKind(invitation.ExpiresAtUtc, DateTimeKind.Utc),
            AcceptedAtUtc = invitation.AcceptedAtUtc is null
                ? null
                : DateTime.SpecifyKind(invitation.AcceptedAtUtc.Value, DateTimeKind.Utc),
        };
    }

    public static TrainerInvitation MapToDomain(TrainerInvitationEntity entity)
    {
        try
        {
            return TrainerInvitation.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.TrainerId),
                Email.From(entity.InviteEmail),
                entity.UserId is null ? null : Id.From(entity.UserId.Value),
                entity.TokenHash,
                MapStatus(entity.Status),
                entity.CreatedAtUtc,
                entity.ExpiresAtUtc,
                entity.AcceptedAtUtc);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored trainer invitation '{entity.Id}' could not be restored", exception);
        }
    }

    private static TrainerInvitationStatus MapStatus(int status)
    {
        if (Enum.IsDefined(typeof(TrainerInvitationStatus), status) == false)
            throw new PersistenceMappingException($"'{status}' is not a known trainer invitation status");

        return (TrainerInvitationStatus)status;
    }
}
