using Microsoft.EntityFrameworkCore;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;

namespace WeightTracker.Infra.Persistence;

/// <summary>
/// Maps the EF entities onto the schema owned by FluentMigrator.
/// </summary>
/// <remarks>
/// Every table and column name is declared explicitly so this configuration and the
/// migrations stay in lockstep. EF migrations are never generated for this context.
/// </remarks>
public class WeightTrackerDbContext : DbContext
{
    public WeightTrackerDbContext(DbContextOptions<WeightTrackerDbContext> options) : base(options)
    { }

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<ExerciseEntity> Exercises => Set<ExerciseEntity>();

    public DbSet<ExerciseTranslationEntity> ExerciseTranslations => Set<ExerciseTranslationEntity>();

    public DbSet<WorkoutEntity> Workouts => Set<WorkoutEntity>();

    public DbSet<WorkoutExerciseEntity> WorkoutExercises => Set<WorkoutExerciseEntity>();

    public DbSet<WorkoutExerciseSetEntity> WorkoutExerciseSets => Set<WorkoutExerciseSetEntity>();

    public DbSet<SetRecordEntity> SetRecords => Set<SetRecordEntity>();

    public DbSet<WorkoutTemplateEntity> WorkoutTemplates => Set<WorkoutTemplateEntity>();

    public DbSet<WorkoutTemplateExerciseEntity> WorkoutTemplateExercises => Set<WorkoutTemplateExerciseEntity>();

    public DbSet<WorkoutTemplateExerciseSetEntity> WorkoutTemplateExerciseSets => Set<WorkoutTemplateExerciseSetEntity>();

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    public DbSet<TrainerAthleteEntity> TrainerAthletes => Set<TrainerAthleteEntity>();

    public DbSet<TrainerInvitationEntity> TrainerInvitations => Set<TrainerInvitationEntity>();

    public DbSet<ExerciseFavoriteEntity> FavoriteExercises => Set<ExerciseFavoriteEntity>();

    public DbSet<MesocycleEntity> Mesocycles => Set<MesocycleEntity>();

    public DbSet<MesocycleWeekEntity> MesocycleWeeks => Set<MesocycleWeekEntity>();

    public DbSet<CustomerLeadEntity> CustomerLeads => Set<CustomerLeadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(user => user.Email).HasColumnName("Email").HasMaxLength(254).IsRequired();
            entity.Property(user => user.Name).HasColumnName("Name").HasMaxLength(100).IsRequired();
            entity.Property(user => user.Surname).HasColumnName("Surname").HasMaxLength(100).IsRequired();
            entity.Property(user => user.DateBirth).HasColumnName("DateBirth").HasColumnType("date");
            entity.Property(user => user.Role).HasColumnName("Role");
            entity.Property(user => user.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(500).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamptz");
            entity.Property(user => user.IsActive).HasColumnName("IsActive");
            entity.Property(user => user.DeactivatedAtUtc).HasColumnName("DeactivatedAtUtc").HasColumnType("timestamptz");
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<ExerciseEntity>(entity =>
        {
            entity.ToTable("Exercises");
            entity.HasKey(exercise => exercise.Id);
            entity.Property(exercise => exercise.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(exercise => exercise.FreeExerciseDbId).HasColumnName("FreeExerciseDbId").HasMaxLength(FreeExerciseDbId.MaxLength);
            entity.Property(exercise => exercise.VideoUrl).HasColumnName("VideoUrl").HasMaxLength(Url.MaxLength);
            entity.Property(exercise => exercise.Muscle).HasColumnName("Muscle");
            entity.Property(exercise => exercise.Type).HasColumnName("Type");
            entity.Property(exercise => exercise.OwnerId).HasColumnName("OwnerId");
            entity.Property(exercise => exercise.ArchivedAtUtc).HasColumnName("ArchivedAtUtc").HasColumnType("timestamptz");

            entity.HasIndex(exercise => exercise.FreeExerciseDbId).IsUnique();
            entity.HasIndex(exercise => exercise.OwnerId);

            entity.HasMany(exercise => exercise.Translations)
                .WithOne()
                .HasForeignKey(translation => translation.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExerciseTranslationEntity>(entity =>
        {
            entity.ToTable("ExerciseTranslations");
            entity.HasKey(translation => translation.Id);
            entity.Property(translation => translation.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(translation => translation.ExerciseId).HasColumnName("ExerciseId");
            entity.Property(translation => translation.LanguageCode).HasColumnName("LanguageCode").HasMaxLength(LanguageCode.MaxLength).IsRequired();
            entity.Property(translation => translation.Name).HasColumnName("Name").HasMaxLength(ExerciseName.MaxLength).IsRequired();

            entity.HasIndex(translation => new { translation.ExerciseId, translation.LanguageCode }).IsUnique();
        });

        modelBuilder.Entity<WorkoutEntity>(entity =>
        {
            entity.ToTable("Workouts");
            entity.HasKey(workout => workout.Id);
            entity.Property(workout => workout.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(workout => workout.UserId).HasColumnName("UserId");
            entity.Property(workout => workout.TrainerId).HasColumnName("TrainerId");
            entity.Property(workout => workout.WorkoutDate).HasColumnName("WorkoutDate").HasColumnType("date");
            entity.Property(workout => workout.Name).HasColumnName("Name").HasMaxLength(WorkoutName.MaxLength).IsRequired();
            entity.Property(workout => workout.Description).HasColumnName("Description").HasMaxLength(WorkoutDescription.MaxLength);
            entity.Property(workout => workout.Status).HasColumnName("Status");
            entity.Property(workout => workout.MesocycleId).HasColumnName("MesocycleId");
            entity.Property(workout => workout.MesocycleWeekNumber).HasColumnName("MesocycleWeekNumber");
            entity.Property(workout => workout.ReviewedAtUtc).HasColumnName("ReviewedAtUtc");
            entity.Property(workout => workout.ReviewedBy).HasColumnName("ReviewedBy");
            entity.Property(workout => workout.ResultsUpdatedAtUtc).HasColumnName("ResultsUpdatedAtUtc");
            entity.Property(workout => workout.StartedAtUtc).HasColumnName("StartedAtUtc");
            entity.Property(workout => workout.CompletedAtUtc).HasColumnName("CompletedAtUtc");

            // xmin is maintained by Postgres; a save whose original version no longer matches fails instead of overwriting
            entity.Property(workout => workout.Version).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

            // Declared without navigation properties: the User aggregate stays referenced by
            // identifier only, while EF still orders writes to satisfy the real constraints.
            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(workout => workout.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(workout => workout.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(workout => workout.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // The Mesocycle aggregate is referenced by identifier only; future read views join on these columns.
            entity.HasOne<MesocycleEntity>()
                .WithMany()
                .HasForeignKey(workout => workout.MesocycleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(workout => new { workout.MesocycleId, workout.MesocycleWeekNumber });

            // Serves the last-performance read: the caller's workouts newest first, ties broken by the newest id
            entity.HasIndex(workout => new { workout.UserId, workout.WorkoutDate, workout.Id })
                .IsDescending(false, true, true)
                .HasDatabaseName("IX_Workouts_UserId_WorkoutDate_Id");

            entity.HasMany(workout => workout.WorkoutExercises)
                .WithOne()
                .HasForeignKey(workoutExercise => workoutExercise.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExerciseEntity>(entity =>
        {
            entity.ToTable("WorkoutExercises");
            entity.HasKey(workoutExercise => workoutExercise.Id);
            entity.Property(workoutExercise => workoutExercise.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(workoutExercise => workoutExercise.WorkoutId).HasColumnName("WorkoutId");
            entity.Property(workoutExercise => workoutExercise.ExerciseId).HasColumnName("ExerciseId");
            entity.Property(workoutExercise => workoutExercise.Position).HasColumnName("Position");
            entity.Property(workoutExercise => workoutExercise.Effort).HasColumnName("Effort");
            entity.Property(workoutExercise => workoutExercise.PerSide).HasColumnName("PerSide");
            entity.Property(workoutExercise => workoutExercise.RestMinutes).HasColumnName("RestMinutes").HasPrecision(5, 2);
            entity.Property(workoutExercise => workoutExercise.SuperSeriesId).HasColumnName("SuperSeriesId");

            entity.HasIndex(workoutExercise => new { workoutExercise.WorkoutId, workoutExercise.ExerciseId }).IsUnique();

            // Serves the reads that start from an exercise, so a rare or never-done one does not scan the table
            entity.HasIndex(workoutExercise => new { workoutExercise.ExerciseId, workoutExercise.WorkoutId })
                .HasDatabaseName("IX_WorkoutExercises_ExerciseId_WorkoutId");

            // Exercise is its own aggregate root: foreign key only, no navigation
            entity.HasOne<ExerciseEntity>()
                .WithMany()
                .HasForeignKey(workoutExercise => workoutExercise.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(workoutExercise => workoutExercise.Sets)
                .WithOne()
                .HasForeignKey(set => set.WorkoutExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(workoutExercise => workoutExercise.SetRecords)
                .WithOne()
                .HasForeignKey(setRecord => setRecord.WorkoutExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExerciseSetEntity>(entity =>
        {
            entity.ToTable("WorkoutExerciseSets");
            entity.HasKey(set => set.Id);
            entity.Property(set => set.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(set => set.WorkoutExerciseId).HasColumnName("WorkoutExerciseId");
            entity.Property(set => set.Position).HasColumnName("Position");
            entity.Property(set => set.Count).HasColumnName("Count");
            entity.Property(set => set.TargetType).HasColumnName("TargetType").HasMaxLength(20).IsRequired();
            entity.Property(set => set.TargetValue).HasColumnName("TargetValue");
            entity.Property(set => set.TargetMaxValue).HasColumnName("TargetMaxValue");
            entity.Property(set => set.TargetMinutes).HasColumnName("TargetMinutes").HasPrecision(5, 2);
        });

        modelBuilder.Entity<SetRecordEntity>(entity =>
        {
            entity.ToTable("SetRecords");
            entity.HasKey(setRecord => setRecord.Id);
            entity.Property(setRecord => setRecord.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(setRecord => setRecord.WorkoutExerciseId).HasColumnName("WorkoutExerciseId");
            entity.Property(setRecord => setRecord.Position).HasColumnName("Position");
            entity.Property(setRecord => setRecord.Reps).HasColumnName("Reps");
            entity.Property(setRecord => setRecord.DurationSeconds).HasColumnName("DurationSeconds");
            entity.Property(setRecord => setRecord.Weight).HasColumnName("Weight").HasPrecision(6, 2);
            entity.Property(setRecord => setRecord.RegisterDatetimeUtc).HasColumnName("RegisterDatetimeUtc").HasColumnType("timestamptz");
            entity.HasIndex(setRecord => setRecord.WorkoutExerciseId);
        });

        modelBuilder.Entity<WorkoutTemplateEntity>(entity =>
        {
            entity.ToTable("WorkoutTemplates");
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(template => template.TrainerId).HasColumnName("TrainerId");
            entity.Property(template => template.Name).HasColumnName("Name").HasMaxLength(WorkoutName.MaxLength).IsRequired();
            entity.Property(template => template.Description).HasColumnName("Description").HasMaxLength(WorkoutDescription.MaxLength);

            // The User aggregate is referenced by identifier only: foreign key, no navigation
            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(template => template.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(template => template.TrainerId);

            entity.HasMany(template => template.Exercises)
                .WithOne()
                .HasForeignKey(templateExercise => templateExercise.WorkoutTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutTemplateExerciseEntity>(entity =>
        {
            entity.ToTable("WorkoutTemplateExercises");
            entity.HasKey(templateExercise => templateExercise.Id);
            entity.Property(templateExercise => templateExercise.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(templateExercise => templateExercise.WorkoutTemplateId).HasColumnName("WorkoutTemplateId");
            entity.Property(templateExercise => templateExercise.ExerciseId).HasColumnName("ExerciseId");
            entity.Property(templateExercise => templateExercise.Position).HasColumnName("Position");
            entity.Property(templateExercise => templateExercise.Effort).HasColumnName("Effort");
            entity.Property(templateExercise => templateExercise.PerSide).HasColumnName("PerSide");
            entity.Property(templateExercise => templateExercise.RestMinutes).HasColumnName("RestMinutes").HasPrecision(5, 2);
            entity.Property(templateExercise => templateExercise.SuperSeriesId).HasColumnName("SuperSeriesId");

            entity.HasIndex(templateExercise => new { templateExercise.WorkoutTemplateId, templateExercise.ExerciseId }).IsUnique();

            // Exercise is its own aggregate root: foreign key only, no navigation
            entity.HasOne<ExerciseEntity>()
                .WithMany()
                .HasForeignKey(templateExercise => templateExercise.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(templateExercise => templateExercise.Sets)
                .WithOne()
                .HasForeignKey(set => set.WorkoutTemplateExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutTemplateExerciseSetEntity>(entity =>
        {
            entity.ToTable("WorkoutTemplateExerciseSets");
            entity.HasKey(set => set.Id);
            entity.Property(set => set.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(set => set.WorkoutTemplateExerciseId).HasColumnName("WorkoutTemplateExerciseId");
            entity.Property(set => set.Position).HasColumnName("Position");
            entity.Property(set => set.Count).HasColumnName("Count");
            entity.Property(set => set.TargetType).HasColumnName("TargetType").HasMaxLength(20).IsRequired();
            entity.Property(set => set.TargetValue).HasColumnName("TargetValue");
            entity.Property(set => set.TargetMaxValue).HasColumnName("TargetMaxValue");
            entity.Property(set => set.TargetMinutes).HasColumnName("TargetMinutes").HasPrecision(5, 2);
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(token => token.UserId).HasColumnName("UserId");
            entity.Property(token => token.TokenHash).HasColumnName("TokenHash").HasMaxLength(128).IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("timestamptz");
            entity.Property(token => token.RevokedAt).HasColumnName("RevokedAt").HasColumnType("timestamptz");
            entity.Property(token => token.ReplacedByTokenId).HasColumnName("ReplacedByTokenId");
            entity.Property(token => token.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamptz");
            entity.HasIndex(token => token.TokenHash).IsUnique();

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Rotation links a revoked token to its replacement. EF has to know about this
            // self reference, otherwise it can emit the revoking UPDATE before the INSERT
            // of the replacement row and trip the foreign key.
            entity.HasOne<RefreshTokenEntity>()
                .WithMany()
                .HasForeignKey(token => token.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainerAthleteEntity>(entity =>
        {
            entity.ToTable("TrainerAthletes");
            entity.HasKey(relationship => relationship.Id);
            entity.Property(relationship => relationship.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(relationship => relationship.TrainerId).HasColumnName("TrainerId");
            entity.Property(relationship => relationship.UserId).HasColumnName("UserId");
            entity.Property(relationship => relationship.Status).HasColumnName("Status");
            entity.Property(relationship => relationship.StartedAtUtc).HasColumnName("StartedAtUtc").HasColumnType("timestamptz");
            entity.Property(relationship => relationship.FinishedAtUtc).HasColumnName("FinishedAtUtc").HasColumnType("timestamptz");
            entity.HasIndex(relationship => relationship.TrainerId);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(relationship => relationship.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(relationship => relationship.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExerciseFavoriteEntity>(entity =>
        {
            entity.ToTable("FavoriteExercises");
            entity.HasKey(favorite => favorite.Id);
            entity.Property(favorite => favorite.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(favorite => favorite.UserId).HasColumnName("UserId");
            entity.Property(favorite => favorite.ExerciseId).HasColumnName("ExerciseId");
            entity.Property(favorite => favorite.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz");
            entity.HasIndex(favorite => new { favorite.UserId, favorite.ExerciseId }).IsUnique();

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(favorite => favorite.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ExerciseEntity>()
                .WithMany()
                .HasForeignKey(favorite => favorite.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainerInvitationEntity>(entity =>
        {
            entity.ToTable("TrainerInvitations");
            entity.HasKey(invitation => invitation.Id);
            entity.Property(invitation => invitation.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(invitation => invitation.TrainerId).HasColumnName("TrainerId");
            entity.Property(invitation => invitation.InviteEmail).HasColumnName("InviteEmail").HasMaxLength(254).IsRequired();
            entity.Property(invitation => invitation.UserId).HasColumnName("UserId");
            entity.Property(invitation => invitation.TokenHash).HasColumnName("TokenHash").HasMaxLength(128).IsRequired();
            entity.Property(invitation => invitation.Status).HasColumnName("Status");
            entity.Property(invitation => invitation.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz");
            entity.Property(invitation => invitation.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz");
            entity.Property(invitation => invitation.AcceptedAtUtc).HasColumnName("AcceptedAtUtc").HasColumnType("timestamptz");
            entity.HasIndex(invitation => invitation.TokenHash).IsUnique();
            entity.HasIndex(invitation => new { invitation.TrainerId, invitation.InviteEmail });

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(invitation => invitation.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(invitation => invitation.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MesocycleEntity>(entity =>
        {
            entity.ToTable("Mesocycles");
            entity.HasKey(mesocycle => mesocycle.Id);
            entity.Property(mesocycle => mesocycle.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(mesocycle => mesocycle.TrainerId).HasColumnName("TrainerId");
            entity.Property(mesocycle => mesocycle.AthleteId).HasColumnName("AthleteId");
            entity.Property(mesocycle => mesocycle.Name).HasColumnName("Name").HasMaxLength(MesocycleName.MaxLength).IsRequired();
            entity.Property(mesocycle => mesocycle.Objective).HasColumnName("Objective").HasMaxLength(MesocycleObjective.MaxLength).IsRequired();
            entity.Property(mesocycle => mesocycle.StartDate).HasColumnName("StartDate").HasColumnType("date");
            entity.Property(mesocycle => mesocycle.DurationInWeeks).HasColumnName("DurationInWeeks");
            entity.HasIndex(mesocycle => mesocycle.TrainerId);
            entity.HasIndex(mesocycle => mesocycle.AthleteId);

            // Declared without navigation properties: the User aggregate stays referenced by
            // identifier only, while EF still orders writes to satisfy the real constraints.
            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(mesocycle => mesocycle.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(mesocycle => mesocycle.AthleteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(mesocycle => mesocycle.Weeks)
                .WithOne()
                .HasForeignKey(week => week.MesocycleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MesocycleWeekEntity>(entity =>
        {
            entity.ToTable("MesocycleWeeks");
            entity.HasKey(week => week.Id);
            entity.Property(week => week.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(week => week.MesocycleId).HasColumnName("MesocycleId");
            entity.Property(week => week.WeekNumber).HasColumnName("WeekNumber");
            entity.Property(week => week.TrainingWeekType).HasColumnName("TrainingWeekType");
            entity.Property(week => week.Objective).HasColumnName("Objective").HasMaxLength(MesocycleObjective.MaxLength).IsRequired();

            // Lets EF delete a removed week before inserting a new one with the same number
            entity.HasIndex(week => new { week.MesocycleId, week.WeekNumber }).IsUnique();
        });

        modelBuilder.Entity<CustomerLeadEntity>(entity =>
        {
            entity.ToTable("CustomerLeads");
            entity.HasKey(lead => lead.Id);
            entity.Property(lead => lead.Id).HasColumnName("Id").ValueGeneratedNever();
            entity.Property(lead => lead.Email).HasColumnName("Email").HasMaxLength(Email.MaxLength).IsRequired();
            entity.Property(lead => lead.RegisteredAtUtc).HasColumnName("RegisteredAtUtc").HasColumnType("timestamptz");
            entity.HasIndex(lead => lead.Email).IsUnique();
        });
    }
}
