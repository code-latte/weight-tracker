namespace WeightTracker.Infra.Seed;

/// <summary>
/// Fixed identities and credentials of the seeded dataset.
/// </summary>
/// <remarks>
/// Identifiers are fixed rather than generated so the integration suite can assert on
/// them directly. The shared password is a development and test credential only.
/// </remarks>
public static class SeedData
{
    public const string Password = "Passw0rd!";

    public const string UserEmail = "user@weighttracker.test";
    public const string TrainerEmail = "trainer@weighttracker.test";
    public const string AdminEmail = "admin@weighttracker.test";

    public static readonly Guid UserId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid TrainerId = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid AdminId = new("00000000-0000-0000-0000-000000000003");

    public static readonly Guid PressBancaId = new("01a17aa9-4603-7c62-9606-711a1a1673d9");
    public static readonly Guid SentadillaId = new("01a17aa9-463f-77bd-8839-3304b5121cbd");
    public static readonly Guid PlanchaId = new("01a17aa9-4bd9-7eb1-8ca9-67f1babd1a1e");
    /// <summary>Referenced by no workout, so it can be deleted.</summary>
    public static readonly Guid CurlBicepsId = new("01a17aa9-4606-71dd-a221-282bf8a45905");
    /// <summary>The seeded cardio exercise.</summary>
    public static readonly Guid CorrerEnCintaId = new("01a17aa9-4cb7-7fb2-9840-4559ec09792f");
    /// <summary>The seeded stretching exercise.</summary>
    public static readonly Guid EstiramientoIsquiosId = new("01a17aa9-4585-71c5-a350-6a9c03107e96");

    public const string PressBancaFreeExerciseDbId = "Barbell_Bench_Press_-_Medium_Grip";
    public const string SentadillaFreeExerciseDbId = "Barbell_Squat";
    public const string PlanchaFreeExerciseDbId = "Plank";
    public const string CurlBicepsFreeExerciseDbId = "Barbell_Curl";
    public const string CorrerEnCintaFreeExerciseDbId = "Running_Treadmill";
    public const string EstiramientoIsquiosFreeExerciseDbId = "90_90_Hamstring";

    /// <summary>Translations of the seeded exercises, one in Spanish and one in English for each.</summary>
    /// <remarks>
    /// The seeded exercises are rows of the free exercise catalog that
    /// <c>ImportFreeExerciseCatalog_202610271200</c> inserts: ids, dataset ids and names match it.
    /// </remarks>
    public static readonly Guid PressBancaEsTranslationId = new("01a17aa9-4605-708a-8ee8-7824ddda5df1");
    public static readonly Guid PressBancaEnTranslationId = new("01a17aa9-4604-79cf-98e1-b4f11d9f7594");
    public static readonly Guid SentadillaEsTranslationId = new("01a17aa9-4641-7292-9c39-a2cb9369be1f");
    public static readonly Guid SentadillaEnTranslationId = new("01a17aa9-4640-75eb-8500-63c2fba4ec8c");
    public static readonly Guid PlanchaEsTranslationId = new("01a17aa9-4bdb-7fdd-bda5-c2408f3d05ef");
    public static readonly Guid PlanchaEnTranslationId = new("01a17aa9-4bda-702a-96b1-5edcc63d20b1");
    public static readonly Guid CurlBicepsEsTranslationId = new("01a17aa9-4608-7093-9088-ed119e1ae55c");
    public static readonly Guid CurlBicepsEnTranslationId = new("01a17aa9-4607-7e41-a8dc-059705b080ff");
    public static readonly Guid CorrerEnCintaEsTranslationId = new("01a17aa9-4cb9-7dc3-b962-0bbc6e86d24b");
    public static readonly Guid CorrerEnCintaEnTranslationId = new("01a17aa9-4cb8-712e-b94a-4d77d3c3e8e4");
    public static readonly Guid EstiramientoIsquiosEsTranslationId = new("01a17aa9-4587-7912-b443-aac1b40611b0");
    public static readonly Guid EstiramientoIsquiosEnTranslationId = new("01a17aa9-4586-7f09-ac61-b78f8d6c941a");

    /// <summary>Owned by the user, no trainer.</summary>
    public static readonly Guid OwnedWorkoutId = new("00000000-0000-0000-0000-000000000021");

    public const string OwnedWorkoutName = "Upper body";

    public const string OwnedWorkoutDescription = "Bench press focus";

    /// <summary>Owned by the user, prescribed by the trainer.</summary>
    public static readonly Guid TrainedWorkoutId = new("00000000-0000-0000-0000-000000000022");

    /// <summary>The trained workout has no description.</summary>
    public const string TrainedWorkoutName = "Leg day";

    /// <summary>Owned by the trainer themselves, no trainer assigned.</summary>
    public static readonly Guid TrainerOwnWorkoutId = new("00000000-0000-0000-0000-000000000023");

    /// <summary>Press Banca in the owned workout, effort HIGH.</summary>
    public static readonly Guid OwnedPressBancaEntryId = new("00000000-0000-0000-0000-000000000031");

    /// <summary>Sentadilla in the trained workout, first, effort MIDHIGH.</summary>
    public static readonly Guid TrainedSentadillaEntryId = new("00000000-0000-0000-0000-000000000032");

    /// <summary>Plancha in the trained workout, second, effort LOW.</summary>
    public static readonly Guid TrainedPlanchaEntryId = new("00000000-0000-0000-0000-000000000033");

    /// <summary>Press Banca in the trainer's own workout, effort MID.</summary>
    public static readonly Guid TrainerOwnPressBancaEntryId = new("00000000-0000-0000-0000-000000000034");

    /// <summary>Owned by the user, prescribed by the trainer. Two ungrouped entries of 3 sets, so they can be grouped.</summary>
    public static readonly Guid PrescribedPairWorkoutId = new("00000000-0000-0000-0000-000000000024");

    /// <summary>Owned by the user, prescribed by the trainer. Press Banca and Sentadilla in superseries 1, Plancha alone.</summary>
    public static readonly Guid PrescribedSuperSeriesWorkoutId = new("00000000-0000-0000-0000-000000000025");

    /// <summary>Owned by the user, no trainer. Two ungrouped entries of 4 sets, so they can be grouped.</summary>
    public static readonly Guid SelfCreatedPairWorkoutId = new("00000000-0000-0000-0000-000000000026");

    /// <summary>Owned by the user, no trainer. Press Banca and Plancha in superseries 1.</summary>
    public static readonly Guid SelfCreatedSuperSeriesWorkoutId = new("00000000-0000-0000-0000-000000000027");

    /// <summary>Press Banca in the prescribed pair workout, 3 x 10 reps.</summary>
    public static readonly Guid PrescribedPairPressBancaEntryId = new("00000000-0000-0000-0000-000000000035");

    /// <summary>Sentadilla in the prescribed pair workout, 3 x 8 reps.</summary>
    public static readonly Guid PrescribedPairSentadillaEntryId = new("00000000-0000-0000-0000-000000000036");

    /// <summary>Press Banca in the prescribed superseries workout, superseries 1.</summary>
    public static readonly Guid PrescribedSuperSeriesPressBancaEntryId = new("00000000-0000-0000-0000-000000000037");

    /// <summary>Sentadilla in the prescribed superseries workout, superseries 1.</summary>
    public static readonly Guid PrescribedSuperSeriesSentadillaEntryId = new("00000000-0000-0000-0000-000000000038");

    /// <summary>Plancha in the prescribed superseries workout, not grouped.</summary>
    public static readonly Guid PrescribedSuperSeriesPlanchaEntryId = new("00000000-0000-0000-0000-000000000039");

    /// <summary>Sentadilla in the self-created pair workout, 4 x 6 reps.</summary>
    public static readonly Guid SelfCreatedPairSentadillaEntryId = new("00000000-0000-0000-0000-000000000040");

    /// <summary>Plancha in the self-created pair workout, 4 x 45 s.</summary>
    public static readonly Guid SelfCreatedPairPlanchaEntryId = new("00000000-0000-0000-0000-000000000041");

    /// <summary>Press Banca in the self-created superseries workout, superseries 1.</summary>
    public static readonly Guid SelfCreatedSuperSeriesPressBancaEntryId = new("00000000-0000-0000-0000-000000000042");

    /// <summary>Plancha in the self-created superseries workout, superseries 1.</summary>
    public static readonly Guid SelfCreatedSuperSeriesPlanchaEntryId = new("00000000-0000-0000-0000-000000000043");

    /// <summary>Owned by the user, prescribed by the trainer, still a draft. Hidden from the user.</summary>
    public static readonly Guid PrescribedDraftWorkoutId = new("00000000-0000-0000-0000-000000000028");

    /// <summary>Owned by the user, prescribed by the trainer, already started. Locked for editing.</summary>
    public static readonly Guid PrescribedStartedWorkoutId = new("00000000-0000-0000-0000-000000000029");

    /// <summary>Press Banca in the prescribed draft workout, 3 x 10 reps.</summary>
    public static readonly Guid PrescribedDraftPressBancaEntryId = new("00000000-0000-0000-0000-000000000044");

    /// <summary>Press Banca in the prescribed started workout, 3 x 10 reps.</summary>
    public static readonly Guid PrescribedStartedPressBancaEntryId = new("00000000-0000-0000-0000-000000000045");

    /// <summary>Identifier of the superseries seeded in the superseries workouts.</summary>
    public const int SeededSuperSeries = 1;

    /// <summary>The trainer prepares the user, active since <see cref="CreatedAt"/>.</summary>
    public static readonly Guid ActiveTrainerAthleteId = new("00000000-0000-0000-0000-000000000051");

    /// <summary>An earlier, ended relationship between the trainer and the user.</summary>
    public static readonly Guid EndedTrainerAthleteId = new("00000000-0000-0000-0000-000000000052");

    /// <summary>From the trainer to the user, pending and far from expiring.</summary>
    public static readonly Guid PendingInvitationId = new("00000000-0000-0000-0000-000000000061");

    public const string PendingInvitationToken = "seed-pending-invitation-token";

    /// <summary>From the trainer to the user, still pending but already expired.</summary>
    public static readonly Guid ExpiredInvitationId = new("00000000-0000-0000-0000-000000000062");

    public const string ExpiredInvitationToken = "seed-expired-invitation-token";

    /// <summary>From the trainer to an email nobody has registered, pending.</summary>
    public static readonly Guid UnregisteredInvitationId = new("00000000-0000-0000-0000-000000000063");

    public const string UnregisteredInvitationToken = "seed-unregistered-invitation-token";

    public const string UnregisteredInvitationEmail = "invited@weighttracker.test";

    /// <summary>
    /// Four-week mesocycle from the trainer to the user, with weeks 1, 2 and 4 defined and week 3 left undefined.
    /// <see cref="TrainedWorkoutId"/> is placed in week 1 and <see cref="PrescribedStartedWorkoutId"/> in week 2.
    /// </summary>
    public static readonly Guid MesocycleId = new("00000000-0000-0000-0000-000000000071");

    public const string MesocycleName = "Hypertrophy block";

    public const int MesocycleDurationInWeeks = 4;

    public static readonly Guid MesocycleAdaptationWeekId = new("00000000-0000-0000-0000-000000000072");

    public static readonly Guid MesocycleAccumulationWeekId = new("00000000-0000-0000-0000-000000000073");

    public static readonly Guid MesocycleDeloadWeekId = new("00000000-0000-0000-0000-000000000074");

    public static readonly DateTime CreatedAt = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
}
