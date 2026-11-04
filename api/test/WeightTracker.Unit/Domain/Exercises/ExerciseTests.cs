using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

/// <summary>
/// Covers restoring exercises, creating custom ones, and the actions that only custom exercises accept.
/// </summary>
public class ExerciseTests
{
    private static readonly FreeExerciseDbId BenchPress = FreeExerciseDbId.From("Barbell_Bench_Press_-_Medium_Grip");
    private static readonly Url Video = Url.From("https://videos.example.com/press-banca");
    private static readonly LanguageCode Spanish = LanguageCode.From("es");
    private static readonly LanguageCode English = LanguageCode.From("en");
    private static readonly ExerciseName PressDeBanca = ExerciseName.From("Press de banca");
    private static readonly ExerciseName BenchPressName = ExerciseName.From("Barbell Bench Press - Medium Grip");

    [Fact]
    public void Rehydrate_ShouldRestoreEveryFieldAndTranslationAndMarkTheExerciseAsNotNew()
    {
        // Arrange
        var id = Id.New();
        var spanish = ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca);
        var english = ExerciseTranslation.Rehydrate(Id.New(), English, BenchPressName);

        // Act
        var exercise = Exercise.Rehydrate(id, BenchPress, Video, Muscles.CHEST, ExerciseTypes.STRENGTH, [spanish, english]);

        // Assert
        exercise.IsNew.Should().BeFalse();
        exercise.Id.Should().Be(id);
        exercise.FreeExerciseDbId.Should().Be(BenchPress);
        exercise.VideoUrl.Should().Be(Video);
        exercise.Muscle.Should().Be(Muscles.CHEST);
        exercise.Translations.Should().Equal(spanish, english);
        exercise.Translations.Should().OnlyContain(translation => translation.IsNew == false);
    }

    [Fact]
    public void Rehydrate_ShouldAcceptAMissingVideoUrlFreeExerciseDbIdAndTranslations()
    {
        // Arrange & Act
        var exercise = Exercise.Rehydrate(Id.New(), null, null, Muscles.NECK, ExerciseTypes.STRENGTH, []);

        // Assert
        exercise.VideoUrl.Should().BeNull();
        exercise.FreeExerciseDbId.Should().BeNull();
        exercise.Translations.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_ShouldRejectTwoTranslationsForTheSameLanguage()
    {
        // Arrange
        var first = ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca);
        var second = ExerciseTranslation.Rehydrate(Id.New(), Spanish, ExerciseName.From("Press banca"));

        // Act
        var act = () => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [first, second]);

        // Assert
        act.Should().Throw<DuplicateExerciseTranslationException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnUndefinedMuscle()
    {
        // Arrange
        var undefined = (Muscles)99;

        // Act
        var act = () => Exercise.Rehydrate(Id.New(), null, null, undefined, ExerciseTypes.STRENGTH, []);

        // Assert
        act.Should().Throw<InvalidMuscleException>();
    }

    [Theory]
    [InlineData(ExerciseTypes.STRENGTH)]
    [InlineData(ExerciseTypes.CARDIO)]
    [InlineData(ExerciseTypes.STRETCHING)]
    public void Rehydrate_ShouldKeepTheGivenType(ExerciseTypes type)
    {
        // Arrange
        var id = Id.New();

        // Act
        var exercise = Exercise.Rehydrate(id, null, null, Muscles.CHEST, type, []);

        // Assert
        exercise.Type.Should().Be(type);
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnUndefinedType()
    {
        // Arrange
        var undefined = (ExerciseTypes)99;

        // Act
        var act = () => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, undefined, []);

        // Assert
        act.Should().Throw<InvalidExerciseTypeException>();
    }

    [Fact]
    public void Type_ShouldNotChangeTheMuscleOrTheBodyPart()
    {
        // Arrange
        var exercise = Exercise.Rehydrate(Id.New(), null, null, Muscles.QUADRICEPS, ExerciseTypes.CARDIO, []);

        // Act
        var bodyPart = exercise.BodyPart;

        // Assert
        exercise.Muscle.Should().Be(Muscles.QUADRICEPS);
        bodyPart.Should().Be(BodyParts.LEGS);
    }

    [Theory]
    [InlineData(Muscles.CHEST, BodyParts.CHEST)]
    [InlineData(Muscles.HAMSTRINGS, BodyParts.LEGS)]
    [InlineData(Muscles.ABDOMINALS, BodyParts.CORE)]
    public void BodyPart_ShouldBeDerivedFromTheStoredMuscle(Muscles muscle, BodyParts expected)
    {
        // Arrange
        var exercise = Exercise.Rehydrate(Id.New(), null, null, muscle, ExerciseTypes.STRENGTH, []);

        // Act
        var bodyPart = exercise.BodyPart;

        // Assert
        bodyPart.Should().Be(expected);
    }

    [Fact]
    public void Translations_ShouldBeASnapshotThatCannotChangeTheAggregate()
    {
        // Arrange
        var spanish = ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca);
        var exercise = Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [spanish]);

        // Act
        ((List<ExerciseTranslation>)exercise.Translations).Clear();

        // Assert
        exercise.Translations.Should().Equal(spanish);
    }

    private static readonly Id OwnerId = Id.New();

    private static Exercise ACustomExercise(DateTime? archivedAtUtc = null)
        => Exercise.Rehydrate(
            Id.New(), null, null, Muscles.MIDDLE_BACK, ExerciseTypes.STRENGTH,
            [ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca)], OwnerId, archivedAtUtc);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(
            Id.New(), BenchPress, Video, Muscles.CHEST, ExerciseTypes.STRENGTH,
            [ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca)]);

    [Fact]
    public void Rehydrate_ShouldTreatAnExerciseWithoutOwnerAsASystemExercise()
    {
        // Arrange & Act
        var exercise = ASystemExercise();

        // Assert
        exercise.IsSystem.Should().BeTrue();
        exercise.OwnerId.Should().BeNull();
        exercise.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldBuildANewCustomExerciseOwnedByTheCallerWithASingleTranslation()
    {
        // Arrange
        var translation = ExerciseTranslation.Create(Spanish, PressDeBanca);

        // Act
        var exercise = Exercise.Create(OwnerId, translation, Muscles.MIDDLE_BACK, ExerciseTypes.STRENGTH, Video);

        // Assert
        exercise.IsNew.Should().BeTrue();
        exercise.IsSystem.Should().BeFalse();
        exercise.OwnerId.Should().Be(OwnerId);
        exercise.FreeExerciseDbId.Should().BeNull();
        exercise.IsArchived.Should().BeFalse();
        exercise.BodyPart.Should().Be(BodyParts.BACK);
        exercise.VideoUrl.Should().Be(Video);
        exercise.Translations.Should().Equal(translation);
    }

    [Fact]
    public void Create_ShouldRejectAnUndefinedMuscle()
    {
        // Arrange
        var translation = ExerciseTranslation.Create(Spanish, PressDeBanca);

        // Act
        var act = () => Exercise.Create(OwnerId, translation, (Muscles)999, ExerciseTypes.STRENGTH, null);

        // Assert
        act.Should().Throw<InvalidMuscleException>();
    }

    [Fact]
    public void UpdateDetails_ShouldReplaceTheTranslationOfTheSameLanguage()
    {
        // Arrange
        var exercise = ACustomExercise();
        var renamed = ExerciseTranslation.Create(Spanish, ExerciseName.From("Remo con goma"));

        // Act
        exercise.UpdateDetails(renamed, Muscles.LATS, ExerciseTypes.CARDIO, Video);

        // Assert
        exercise.Translations.Should().Equal(renamed);
        exercise.Muscle.Should().Be(Muscles.LATS);
        exercise.Type.Should().Be(ExerciseTypes.CARDIO);
        exercise.VideoUrl.Should().Be(Video);
    }

    [Fact]
    public void UpdateDetails_ShouldAddANewLanguageAndKeepTheOthers()
    {
        // Arrange
        var exercise = ACustomExercise();
        var original = exercise.Translations.Single();
        var english = ExerciseTranslation.Create(English, BenchPressName);

        // Act
        exercise.UpdateDetails(english, Muscles.MIDDLE_BACK, ExerciseTypes.STRENGTH, null);

        // Assert
        exercise.Translations.Should().BeEquivalentTo([original, english]);
    }

    [Fact]
    public void UpdateDetails_ShouldChangeNothingWhenTheMuscleIsInvalid()
    {
        // Arrange
        var exercise = ACustomExercise();
        var original = exercise.Translations.Single();

        // Act
        var act = () => exercise.UpdateDetails(
            ExerciseTranslation.Create(Spanish, BenchPressName), (Muscles)999, ExerciseTypes.CARDIO, Video);

        // Assert
        act.Should().Throw<InvalidMuscleException>();
        exercise.Translations.Should().Equal(original);
        exercise.Muscle.Should().Be(Muscles.MIDDLE_BACK);
        exercise.Type.Should().Be(ExerciseTypes.STRENGTH);
        exercise.VideoUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ShouldRejectASystemExercise()
    {
        // Arrange
        var exercise = ASystemExercise();

        // Act
        var act = () => exercise.UpdateDetails(
            ExerciseTranslation.Create(Spanish, BenchPressName), Muscles.LATS, ExerciseTypes.CARDIO, null);

        // Assert
        act.Should().Throw<SystemExerciseNotEditableException>();
        exercise.Muscle.Should().Be(Muscles.CHEST);
    }

    [Fact]
    public void UpdateDetails_ShouldRejectAnArchivedExercise()
    {
        // Arrange
        var exercise = ACustomExercise(archivedAtUtc: new DateTime(2026, 11, 1));

        // Act
        var act = () => exercise.UpdateDetails(
            ExerciseTranslation.Create(Spanish, BenchPressName), Muscles.LATS, ExerciseTypes.CARDIO, null);

        // Assert
        act.Should().Throw<ExerciseArchivedException>();
        exercise.Muscle.Should().Be(Muscles.MIDDLE_BACK);
    }

    [Fact]
    public void Archive_ShouldRecordWhenTheExerciseWasArchived()
    {
        // Arrange
        var exercise = ACustomExercise();
        var now = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

        // Act
        exercise.Archive(now);

        // Assert
        exercise.IsArchived.Should().BeTrue();
        exercise.ArchivedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Archive_ShouldRejectASystemExercise()
    {
        // Arrange
        var exercise = ASystemExercise();

        // Act
        var act = () => exercise.Archive(new DateTime(2026, 11, 3));

        // Assert
        act.Should().Throw<SystemExerciseNotEditableException>();
        exercise.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void Archive_ShouldRejectAnAlreadyArchivedExerciseAndKeepTheFirstDate()
    {
        // Arrange
        var first = new DateTime(2026, 11, 1);
        var exercise = ACustomExercise(archivedAtUtc: first);

        // Act
        var act = () => exercise.Archive(new DateTime(2026, 11, 3));

        // Assert
        act.Should().Throw<ExerciseArchivedException>();
        exercise.ArchivedAtUtc.Should().Be(first);
    }
}
