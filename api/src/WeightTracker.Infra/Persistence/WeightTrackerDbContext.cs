using Microsoft.EntityFrameworkCore;
using WeightTracker.Domain.Exercises;
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

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    public DbSet<TrainerAthleteEntity> TrainerAthletes => Set<TrainerAthleteEntity>();

    public DbSet<TrainerInvitationEntity> TrainerInvitations => Set<TrainerInvitationEntity>();

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

            entity.HasIndex(exercise => exercise.FreeExerciseDbId).IsUnique();

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
            entity.Property(workoutExercise => workoutExercise.SuperSeriesId).HasColumnName("SuperSeriesId");

            entity.HasIndex(workoutExercise => new { workoutExercise.WorkoutId, workoutExercise.ExerciseId }).IsUnique();

            // Exercise is its own aggregate root: foreign key only, no navigation
            entity.HasOne<ExerciseEntity>()
                .WithMany()
                .HasForeignKey(workoutExercise => workoutExercise.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(workoutExercise => workoutExercise.Sets)
                .WithOne()
                .HasForeignKey(set => set.WorkoutExerciseId)
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
    }
}
