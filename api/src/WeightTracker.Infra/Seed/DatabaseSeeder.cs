using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Seed;

/// <summary>
/// Populates the development and integration test dataset.
/// </summary>
/// <remarks>
/// Never runs in production. Seed code is the one place allowed to reach the DbContext
/// directly instead of going through the repositories, because it needs to assign fixed
/// identifiers that the normal creation path deliberately generates. Domain objects are
/// still built through the aggregates, so the seeded data respects every invariant.
/// </remarks>
public class DatabaseSeeder
{
    private readonly WeightTrackerDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInvitationTokenService _invitationTokens;

    public DatabaseSeeder(
        WeightTrackerDbContext context,
        IPasswordHasher passwordHasher,
        IInvitationTokenService invitationTokens)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _invitationTokens = invitationTokens;
    }

    public async Task Seed(CancellationToken cancellationToken)
    {
        // Keyed on a seeded row, not on any user: the migrations already insert the configured admin
        if (await _context.Users.AnyAsync(user => user.Id == SeedData.AdminId, cancellationToken))
            return;

        await SeedExercises(cancellationToken);
        await SeedUsers(cancellationToken);
        await SeedMesocycles(cancellationToken);
        await SeedWorkouts(cancellationToken);
        await SeedWorkoutTemplates(cancellationToken);
        await SeedTrainerAthletes(cancellationToken);
        await SeedTrainerInvitations(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedExercises(CancellationToken cancellationToken)
    {
        var exercises = new[]
        {
            SeededExercise(SeedData.PressBancaId, SeedData.PressBancaFreeExerciseDbId, Muscles.CHEST, ExerciseTypes.STRENGTH,
                (SeedData.PressBancaEsTranslationId, "es", "Press de banca"),
                (SeedData.PressBancaEnTranslationId, "en", "Barbell Bench Press - Medium Grip")),
            SeededExercise(SeedData.SentadillaId, SeedData.SentadillaFreeExerciseDbId, Muscles.QUADRICEPS, ExerciseTypes.STRENGTH,
                (SeedData.SentadillaEsTranslationId, "es", "Sentadilla con barra"),
                (SeedData.SentadillaEnTranslationId, "en", "Barbell Squat")),
            SeededExercise(SeedData.PlanchaId, SeedData.PlanchaFreeExerciseDbId, Muscles.ABDOMINALS, ExerciseTypes.STRENGTH,
                (SeedData.PlanchaEsTranslationId, "es", "Plancha"),
                (SeedData.PlanchaEnTranslationId, "en", "Plank")),
            SeededExercise(SeedData.CurlBicepsId, SeedData.CurlBicepsFreeExerciseDbId, Muscles.BICEPS, ExerciseTypes.STRENGTH,
                (SeedData.CurlBicepsEsTranslationId, "es", "Curl de bíceps con barra"),
                (SeedData.CurlBicepsEnTranslationId, "en", "Barbell Curl")),
            SeededExercise(SeedData.CorrerEnCintaId, SeedData.CorrerEnCintaFreeExerciseDbId, Muscles.QUADRICEPS, ExerciseTypes.CARDIO,
                (SeedData.CorrerEnCintaEsTranslationId, "es", "Correr en cinta"),
                (SeedData.CorrerEnCintaEnTranslationId, "en", "Running, Treadmill")),
            SeededExercise(SeedData.EstiramientoIsquiosId, SeedData.EstiramientoIsquiosFreeExerciseDbId, Muscles.HAMSTRINGS, ExerciseTypes.STRETCHING,
                (SeedData.EstiramientoIsquiosEsTranslationId, "es", "Estiramiento de isquiotibiales 90/90"),
                (SeedData.EstiramientoIsquiosEnTranslationId, "en", "90/90 Hamstring")),
        };

        // They are catalog rows: in a migrated database they already exist and must not be inserted
        // twice. Only a truncated database, as the integration tests leave it, is missing them
        var seededIds = exercises.Select(exercise => exercise.Id.Value).ToList();
        var stored = await _context.Exercises
            .Where(exercise => seededIds.Contains(exercise.Id))
            .Select(exercise => exercise.Id)
            .ToListAsync(cancellationToken);

        await _context.Exercises.AddRangeAsync(
            exercises.Where(exercise => stored.Contains(exercise.Id.Value) == false).Select(ExerciseMapper.MapToEntity),
            cancellationToken);
    }

    private static Exercise SeededExercise(
        Guid id,
        string freeExerciseDbId,
        Muscles muscle,
        ExerciseTypes type,
        params (Guid Id, string LanguageCode, string Name)[] translations)
        => Exercise.Rehydrate(
            Id.From(id),
            FreeExerciseDbId.From(freeExerciseDbId),
            null,
            muscle,
            type,
            translations.Select(translation => ExerciseTranslation.Rehydrate(
                Id.From(translation.Id),
                LanguageCode.From(translation.LanguageCode),
                ExerciseName.From(translation.Name))));

    private async Task SeedUsers(CancellationToken cancellationToken)
    {
        var passwordHash = _passwordHasher.Hash(Password.From(SeedData.Password));

        var users = new[]
        {
            BuildUser(SeedData.UserId, SeedData.UserEmail, "Ada", "Lovelace",
                new DateOnly(1990, 4, 12), Role.USER, passwordHash),
            BuildUser(SeedData.TrainerId, SeedData.TrainerEmail, "Grace", "Hopper",
                new DateOnly(1985, 12, 9), Role.TRAINER, passwordHash),
            BuildUser(SeedData.AdminId, SeedData.AdminEmail, "Alan", "Turing",
                new DateOnly(1980, 6, 23), Role.ADMIN, passwordHash),
        };

        foreach (var user in users)
        {
            // Email is a unique business identifier and is checked before persisting
            if (await _context.Users.AnyAsync(stored => stored.Email == user.Email.Value, cancellationToken))
                continue;

            await _context.Users.AddAsync(UserMapper.MapToEntity(user), cancellationToken);
        }
    }

    private async Task SeedWorkouts(CancellationToken cancellationToken)
    {
        var ownedWorkout = Workout.Rehydrate(
            Id.From(SeedData.OwnedWorkoutId),
            Id.From(SeedData.UserId),
            null,
            WorkoutName.From(SeedData.OwnedWorkoutName),
            WorkoutDescription.From(SeedData.OwnedWorkoutDescription),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.DRAFT,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.OwnedPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.HIGH,
                    [new SetPrescription(4, new SetTarget.Reps(10))]),
            ]);

        var trainedWorkout = Workout.Rehydrate(
            Id.From(SeedData.TrainedWorkoutId),
            Id.From(SeedData.UserId),
            Id.From(SeedData.TrainerId),
            WorkoutName.From(SeedData.TrainedWorkoutName),
            null,
            SeedData.TrainedWorkoutDate,
            WorkoutStatus.PUBLISHED,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.TrainedSentadillaEntryId), Id.From(SeedData.SentadillaId), Effort.MIDHIGH,
                    [new SetPrescription(5, new SetTarget.Reps(5))]),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.TrainedPlanchaEntryId), Id.From(SeedData.PlanchaId), Effort.LOW,
                    [new SetPrescription(3, new SetTarget.Duration(40))]),
            ],
            Id.From(SeedData.MesocycleId),
            WeekNumber.From(1));

        var trainerOwnWorkout = Workout.Rehydrate(
            Id.From(SeedData.TrainerOwnWorkoutId),
            Id.From(SeedData.TrainerId),
            null,
            WorkoutName.From("Trainer chest day"),
            null,
            new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.DRAFT,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.TrainerOwnPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.MID,
                    [new SetPrescription(3, new SetTarget.MaxReps())]),
            ]);

        var seededSuperSeries = SuperSeriesId.From(SeedData.SeededSuperSeries);

        var prescribedPairWorkout = Workout.Rehydrate(
            Id.From(SeedData.PrescribedPairWorkoutId),
            Id.From(SeedData.UserId),
            Id.From(SeedData.TrainerId),
            WorkoutName.From("Prescribed pair"),
            null,
            new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.PUBLISHED,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedPairPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.HIGH,
                    [new SetPrescription(3, new SetTarget.Reps(10))]),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedPairSentadillaEntryId), Id.From(SeedData.SentadillaId), Effort.MIDHIGH,
                    [new SetPrescription(3, new SetTarget.Reps(8))]),
            ]);

        var prescribedSuperSeriesWorkout = Workout.Rehydrate(
            Id.From(SeedData.PrescribedSuperSeriesWorkoutId),
            Id.From(SeedData.UserId),
            Id.From(SeedData.TrainerId),
            WorkoutName.From("Prescribed superseries"),
            null,
            new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.PUBLISHED,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedSuperSeriesPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.HIGH,
                    [new SetPrescription(3, new SetTarget.Reps(10))], seededSuperSeries),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedSuperSeriesSentadillaEntryId), Id.From(SeedData.SentadillaId), Effort.MIDHIGH,
                    [new SetPrescription(3, new SetTarget.Reps(8))], seededSuperSeries),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedSuperSeriesPlanchaEntryId), Id.From(SeedData.PlanchaId), Effort.LOW,
                    [new SetPrescription(4, new SetTarget.Duration(30))]),
            ]);

        var selfCreatedPairWorkout = Workout.Rehydrate(
            Id.From(SeedData.SelfCreatedPairWorkoutId),
            Id.From(SeedData.UserId),
            null,
            WorkoutName.From("Self-planned pair"),
            null,
            new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.DRAFT,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.SelfCreatedPairSentadillaEntryId), Id.From(SeedData.SentadillaId), Effort.MID,
                    [new SetPrescription(4, new SetTarget.Reps(6))]),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.SelfCreatedPairPlanchaEntryId), Id.From(SeedData.PlanchaId), Effort.LOW,
                    [new SetPrescription(4, new SetTarget.Duration(45))]),
            ]);

        var selfCreatedSuperSeriesWorkout = Workout.Rehydrate(
            Id.From(SeedData.SelfCreatedSuperSeriesWorkoutId),
            Id.From(SeedData.UserId),
            null,
            WorkoutName.From("Self-planned superseries"),
            null,
            new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.DRAFT,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.SelfCreatedSuperSeriesPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.MID,
                    [new SetPrescription(4, new SetTarget.Reps(8))], seededSuperSeries),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.SelfCreatedSuperSeriesPlanchaEntryId), Id.From(SeedData.PlanchaId), Effort.LOW,
                    [new SetPrescription(4, new SetTarget.Duration(30))], seededSuperSeries),
            ]);

        var prescribedDraftWorkout = Workout.Rehydrate(
            Id.From(SeedData.PrescribedDraftWorkoutId),
            Id.From(SeedData.UserId),
            Id.From(SeedData.TrainerId),
            WorkoutName.From("Prescribed draft"),
            null,
            new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.DRAFT,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedDraftPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.HIGH,
                    [new SetPrescription(3, new SetTarget.Reps(10))]),
            ]);

        var prescribedStartedWorkout = Workout.Rehydrate(
            Id.From(SeedData.PrescribedStartedWorkoutId),
            Id.From(SeedData.UserId),
            Id.From(SeedData.TrainerId),
            WorkoutName.From("Prescribed started"),
            null,
            SeedData.PrescribedStartedWorkoutDate,
            WorkoutStatus.STARTED,
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.PrescribedStartedPressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.HIGH,
                    [new SetPrescription(3, new SetTarget.Reps(10))]),
            ],
            Id.From(SeedData.MesocycleId),
            WeekNumber.From(2));

        var workouts = new[]
        {
            ownedWorkout, trainedWorkout, trainerOwnWorkout,
            prescribedPairWorkout, prescribedSuperSeriesWorkout, selfCreatedPairWorkout, selfCreatedSuperSeriesWorkout,
            prescribedDraftWorkout, prescribedStartedWorkout,
        };

        await _context.Workouts.AddRangeAsync(workouts.Select(WorkoutMapper.MapToEntity), cancellationToken);
    }

    private async Task SeedWorkoutTemplates(CancellationToken cancellationToken)
    {
        var superSeries = SuperSeriesId.From(SeedData.SeededSuperSeries);

        var trainerTemplate = WorkoutTemplate.Rehydrate(
            Id.From(SeedData.TrainerTemplateId),
            Id.From(SeedData.TrainerId),
            WorkoutName.From(SeedData.TrainerTemplateName),
            WorkoutDescription.From(SeedData.TrainerTemplateDescription),
            [
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.TrainerTemplateSentadillaEntryId), Id.From(SeedData.SentadillaId), Effort.HIGH,
                    [new SetPrescription(4, new SetTarget.Reps(8))], superSeries, perSide: true, rest: TimeInMinutes.From(2m)),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.TrainerTemplatePressBancaEntryId), Id.From(SeedData.PressBancaId), Effort.MIDHIGH,
                    [new SetPrescription(4, new SetTarget.RepsRange(8, 12))], superSeries),
                WorkoutExercise.Rehydrate(
                    Id.From(SeedData.TrainerTemplatePlanchaEntryId), Id.From(SeedData.PlanchaId), Effort.LOW,
                    [new SetPrescription(3, new SetTarget.Duration(40))]),
            ]);

        await _context.WorkoutTemplates.AddAsync(WorkoutTemplateMapper.MapToEntity(trainerTemplate), cancellationToken);
    }

    private static User BuildUser(
        Guid id, string email, string name, string surname, DateOnly dateBirth, Role role, PasswordHash passwordHash)
        => User.Rehydrate(
            Id.From(id), Email.From(email), PersonName.From(name), PersonName.From(surname),
            dateBirth, role, passwordHash, SeedData.CreatedAt, true, null);

    private async Task SeedMesocycles(CancellationToken cancellationToken)
    {
        var mesocycle = Mesocycle.Rehydrate(
            Id.From(SeedData.MesocycleId),
            Id.From(SeedData.TrainerId),
            Id.From(SeedData.UserId),
            MesocycleName.From(SeedData.MesocycleName),
            MesocycleObjective.From("Build muscle mass before the strength block"),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            DurationInWeeks.From(SeedData.MesocycleDurationInWeeks),
            [
                MesocycleWeek.Rehydrate(
                    Id.From(SeedData.MesocycleAdaptationWeekId), WeekNumber.From(1), TrainingWeekType.ADAPTATION,
                    MesocycleObjective.From("Learn the technique with moderate loads")),
                MesocycleWeek.Rehydrate(
                    Id.From(SeedData.MesocycleAccumulationWeekId), WeekNumber.From(2), TrainingWeekType.ACCUMULATION,
                    MesocycleObjective.From("Raise the weekly volume")),
                MesocycleWeek.Rehydrate(
                    Id.From(SeedData.MesocycleDeloadWeekId), WeekNumber.From(4), TrainingWeekType.DELOAD,
                    MesocycleObjective.From("Recover before the next block")),
            ]);

        await _context.Mesocycles.AddAsync(MesocycleMapper.MapToEntity(mesocycle), cancellationToken);
    }

    private async Task SeedTrainerAthletes(CancellationToken cancellationToken)
    {
        var relationships = new[]
        {
            TrainerAthlete.Rehydrate(
                Id.From(SeedData.EndedTrainerAthleteId), Id.From(SeedData.TrainerId), Id.From(SeedData.UserId),
                TrainerAthleteStatus.ENDED, SeedData.CreatedAt.AddDays(-90), SeedData.CreatedAt.AddDays(-1)),
            TrainerAthlete.Rehydrate(
                Id.From(SeedData.ActiveTrainerAthleteId), Id.From(SeedData.TrainerId), Id.From(SeedData.UserId),
                TrainerAthleteStatus.ACTIVE, SeedData.CreatedAt, null),
        };

        await _context.TrainerAthletes.AddRangeAsync(
            relationships.Select(TrainerAthleteMapper.MapToEntity), cancellationToken);
    }

    private async Task SeedTrainerInvitations(CancellationToken cancellationToken)
    {
        // Far enough ahead that the seeded pending invitations never expire during development or tests
        var farFuture = SeedData.CreatedAt.AddYears(100);

        var invitations = new[]
        {
            SeededInvitation(SeedData.PendingInvitationId, SeedData.UserEmail, SeedData.UserId,
                SeedData.PendingInvitationToken, farFuture),
            SeededInvitation(SeedData.ExpiredInvitationId, SeedData.UserEmail, SeedData.UserId,
                SeedData.ExpiredInvitationToken, SeedData.CreatedAt.AddDays(7)),
            SeededInvitation(SeedData.UnregisteredInvitationId, SeedData.UnregisteredInvitationEmail, null,
                SeedData.UnregisteredInvitationToken, farFuture),
        };

        await _context.TrainerInvitations.AddRangeAsync(
            invitations.Select(TrainerInvitationMapper.MapToEntity), cancellationToken);
    }

    private TrainerInvitation SeededInvitation(
        Guid id, string email, Guid? userId, string rawToken, DateTime expiresAtUtc)
        => TrainerInvitation.Rehydrate(
            Id.From(id),
            Id.From(SeedData.TrainerId),
            Email.From(email),
            userId is null ? null : Id.From(userId.Value),
            _invitationTokens.Hash(rawToken),
            TrainerInvitationStatus.PENDING,
            SeedData.CreatedAt,
            expiresAtUtc,
            null);
}
