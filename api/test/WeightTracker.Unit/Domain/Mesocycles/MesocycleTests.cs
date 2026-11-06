using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using Xunit;

namespace WeightTracker.Unit.Domain.Mesocycles;

public class MesocycleTests
{
    private static readonly DateTime StartDate = new(2026, 11, 4, 15, 30, 0, DateTimeKind.Utc);
    private static readonly MesocycleName Name = MesocycleName.From("Hypertrophy block");
    private static readonly MesocycleObjective Objective = MesocycleObjective.From("Build work capacity");
    private static readonly MesocycleObjective WeekObjective = MesocycleObjective.From("Get used to the volume");
    private static readonly DurationInWeeks FourWeeks = DurationInWeeks.From(4);

    private static Mesocycle NewMesocycle(DurationInWeeks? duration = null)
        => Mesocycle.Create(Id.New(), Id.New(), Name, Objective, StartDate, duration ?? FourWeeks);

    private static Mesocycle MesocycleStartingOn(DateTime startDate)
        => Mesocycle.Create(Id.New(), Id.New(), Name, Objective, startDate, FourWeeks);

    private static MesocycleWeek StoredWeek(int number, TrainingWeekType type = TrainingWeekType.ACCUMULATION)
        => MesocycleWeek.Rehydrate(Id.New(), WeekNumber.From(number), type, WeekObjective);

    [Fact]
    public void Create_ShouldKeepTheTrainerAndTheAthlete()
    {
        // Arrange
        var trainerId = Id.New();
        var athleteId = Id.New();

        // Act
        var mesocycle = Mesocycle.Create(trainerId, athleteId, Name, Objective, StartDate, FourWeeks);

        // Assert
        mesocycle.TrainerId.Should().Be(trainerId);
        mesocycle.AthleteId.Should().Be(athleteId);
        mesocycle.Name.Should().Be(Name);
        mesocycle.Objective.Should().Be(Objective);
        mesocycle.DurationInWeeks.Should().Be(FourWeeks);
    }

    [Fact]
    public void Create_ShouldRejectATrainerWhoIsTheAthlete()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var act = () => Mesocycle.Create(userId, userId, Name, Objective, StartDate, FourWeeks);

        // Assert
        act.Should().Throw<MesocycleTrainerIsAthleteException>();
    }

    [Fact]
    public void Create_ShouldMarkTheMesocycleAsNewWithoutWeeks()
    {
        // Arrange & Act
        var mesocycle = NewMesocycle();

        // Assert
        mesocycle.IsNew.Should().BeTrue();
        mesocycle.Weeks.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldDropTheTimeOfTheStartDate()
    {
        // Arrange & Act
        var mesocycle = NewMesocycle();

        // Assert
        mesocycle.StartDate.Should().Be(new DateTime(2026, 11, 4));
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheStoredStateAsNotNew()
    {
        // Arrange
        var id = Id.New();
        var trainerId = Id.New();
        var athleteId = Id.New();
        var first = StoredWeek(1, TrainingWeekType.ADAPTATION);
        var third = StoredWeek(3, TrainingWeekType.DELOAD);

        // Act
        var mesocycle = Mesocycle.Rehydrate(id, trainerId, athleteId, Name, Objective, StartDate, FourWeeks, [third, first]);

        // Assert
        mesocycle.IsNew.Should().BeFalse();
        mesocycle.Id.Should().Be(id);
        mesocycle.TrainerId.Should().Be(trainerId);
        mesocycle.AthleteId.Should().Be(athleteId);
        mesocycle.Weeks.Should().Equal(first, third);
    }

    [Fact]
    public void Rehydrate_ShouldRejectATrainerWhoIsTheAthlete()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var act = () => Mesocycle.Rehydrate(Id.New(), userId, userId, Name, Objective, StartDate, FourWeeks, []);

        // Assert
        act.Should().Throw<MesocycleTrainerIsAthleteException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectTwoWeeksWithTheSameNumber()
    {
        // Arrange
        var weeks = new[] { StoredWeek(2), StoredWeek(2) };

        // Act
        var act = () => Mesocycle.Rehydrate(Id.New(), Id.New(), Id.New(), Name, Objective, StartDate, FourWeeks, weeks);

        // Assert
        act.Should().Throw<DuplicateMesocycleWeekException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAWeekBeyondTheDuration()
    {
        // Arrange
        var weeks = new[] { StoredWeek(5) };

        // Act
        var act = () => Mesocycle.Rehydrate(Id.New(), Id.New(), Id.New(), Name, Objective, StartDate, FourWeeks, weeks);

        // Assert
        act.Should().Throw<MesocycleWeekOutOfRangeException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnUndefinedTrainingWeekType()
    {
        // Arrange
        var undefined = (TrainingWeekType)99;

        // Act
        var act = () => MesocycleWeek.Rehydrate(Id.New(), WeekNumber.From(1), undefined, WeekObjective);

        // Assert
        act.Should().Throw<InvalidTrainingWeekTypeException>();
    }

    [Fact]
    public void Rename_ShouldReplaceTheName()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        var name = MesocycleName.From("Strength block");

        // Act
        mesocycle.Rename(name);

        // Assert
        mesocycle.Name.Should().Be(name);
    }

    [Fact]
    public void ChangeObjective_ShouldReplaceTheObjective()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        var objective = MesocycleObjective.From("Peak for the meet");

        // Act
        mesocycle.ChangeObjective(objective);

        // Assert
        mesocycle.Objective.Should().Be(objective);
    }

    [Fact]
    public void Reschedule_ShouldReplaceTheStartDateAndKeepTheWeeks()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Act
        mesocycle.Reschedule(new DateTime(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc));

        // Assert
        mesocycle.StartDate.Should().Be(new DateTime(2026, 11, 9));
        mesocycle.Weeks.Should().ContainSingle(week => week.WeekNumber == WeekNumber.From(1));
    }

    [Fact]
    public void AddWeek_ShouldAllowGapsAndAnyOrder()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(3), TrainingWeekType.INTENSIFICATION, WeekObjective);

        // Act
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Assert
        mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1, 3);
    }

    [Fact]
    public void AddWeek_ShouldKeepTheTypeAndTheObjective()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        var objective = MesocycleObjective.From("Heavy triples");

        // Act
        mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.INTENSIFICATION, objective);

        // Assert
        var week = mesocycle.Weeks.Single();
        week.WeekNumber.Should().Be(WeekNumber.From(2));
        week.TrainingWeekType.Should().Be(TrainingWeekType.INTENSIFICATION);
        week.Objective.Should().Be(objective);
        week.IsNew.Should().BeTrue();
    }

    [Fact]
    public void AddWeek_ShouldAcceptTheLastWeekOfTheDuration()
    {
        // Arrange
        var mesocycle = NewMesocycle();

        // Act
        mesocycle.AddWeek(WeekNumber.From(4), TrainingWeekType.DELOAD, WeekObjective);

        // Assert
        mesocycle.Weeks.Should().ContainSingle(week => week.WeekNumber == WeekNumber.From(4));
    }

    [Fact]
    public void AddWeek_ShouldRejectAWeekBeyondTheDurationAndKeepTheWeeks()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Act
        var act = () => mesocycle.AddWeek(WeekNumber.From(5), TrainingWeekType.DELOAD, WeekObjective);

        // Assert
        act.Should().Throw<MesocycleWeekOutOfRangeException>();
        mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1);
    }

    [Fact]
    public void AddWeek_ShouldRejectAnUndefinedTypeAndKeepTheWeeks()
    {
        // Arrange
        var mesocycle = NewMesocycle();

        // Act
        var act = () => mesocycle.AddWeek(WeekNumber.From(1), (TrainingWeekType)99, WeekObjective);

        // Assert
        act.Should().Throw<InvalidTrainingWeekTypeException>();
        mesocycle.Weeks.Should().BeEmpty();
    }

    [Fact]
    public void AddWeek_ShouldRejectAWeekAlreadyDefinedAndKeepIt()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.ACCUMULATION, WeekObjective);

        // Act
        var act = () => mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.DELOAD, MesocycleObjective.From("Back off"));

        // Assert
        act.Should().Throw<DuplicateMesocycleWeekException>();
        var week = mesocycle.Weeks.Single();
        week.TrainingWeekType.Should().Be(TrainingWeekType.ACCUMULATION);
        week.Objective.Should().Be(WeekObjective);
    }

    [Fact]
    public void RedefineWeek_ShouldReplaceTheTypeAndObjectiveKeepingItsIdentity()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.ACCUMULATION, WeekObjective);
        var original = mesocycle.Weeks.Single();
        var objective = MesocycleObjective.From("Back off");

        // Act
        mesocycle.RedefineWeek(WeekNumber.From(2), TrainingWeekType.DELOAD, objective);

        // Assert
        var week = mesocycle.Weeks.Single();
        week.Id.Should().Be(original.Id);
        week.WeekNumber.Should().Be(WeekNumber.From(2));
        week.TrainingWeekType.Should().Be(TrainingWeekType.DELOAD);
        week.Objective.Should().Be(objective);
    }

    [Fact]
    public void RedefineWeek_ShouldRejectAWeekThatIsNotDefinedAndNotCreateIt()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Act
        var act = () => mesocycle.RedefineWeek(WeekNumber.From(3), TrainingWeekType.DELOAD, WeekObjective);

        // Assert
        act.Should().Throw<MesocycleWeekNotFoundException>();
        mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1);
    }

    [Fact]
    public void RedefineWeek_ShouldRejectAnUndefinedTypeAndKeepTheWeek()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.ACCUMULATION, WeekObjective);

        // Act
        var act = () => mesocycle.RedefineWeek(WeekNumber.From(2), (TrainingWeekType)99, MesocycleObjective.From("Other"));

        // Assert
        act.Should().Throw<InvalidTrainingWeekTypeException>();
        var week = mesocycle.Weeks.Single();
        week.TrainingWeekType.Should().Be(TrainingWeekType.ACCUMULATION);
        week.Objective.Should().Be(WeekObjective);
    }

    [Fact]
    public void RemoveWeek_ShouldLeaveTheOtherWeeks()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);
        mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.ACCUMULATION, WeekObjective);
        mesocycle.AddWeek(WeekNumber.From(3), TrainingWeekType.INTENSIFICATION, WeekObjective);

        // Act
        mesocycle.RemoveWeek(WeekNumber.From(3));

        // Assert
        mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1, 2);
    }

    [Fact]
    public void RemoveWeek_ShouldRejectAWeekThatIsNotDefined()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Act
        var act = () => mesocycle.RemoveWeek(WeekNumber.From(4));

        // Assert
        act.Should().Throw<MesocycleWeekNotFoundException>();
        mesocycle.Weeks.Should().HaveCount(1);
    }

    [Fact]
    public void DateOfWeek_ShouldBeTheWednesdayOfTheStartWeekWhenStartingOnMonday()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 2));

        // Act
        var date = mesocycle.DateOfWeek(WeekNumber.From(1));

        // Assert
        date.Should().Be(new DateTime(2026, 11, 4));
    }

    [Fact]
    public void DateOfWeek_ShouldBeTheStartDateWhenStartingOnWednesday()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 4));

        // Act
        var date = mesocycle.DateOfWeek(WeekNumber.From(1));

        // Assert
        date.Should().Be(new DateTime(2026, 11, 4));
    }

    [Fact]
    public void DateOfWeek_ShouldFallBeforeTheStartDateWhenStartingOnSunday()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 8));

        // Act
        var date = mesocycle.DateOfWeek(WeekNumber.From(1));

        // Assert
        date.Should().Be(new DateTime(2026, 11, 4));
    }

    [Fact]
    public void DateOfWeek_ShouldAddOneWeekForEachFollowingWeek()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 2));

        // Act
        var date = mesocycle.DateOfWeek(WeekNumber.From(3));

        // Assert
        date.Should().Be(new DateTime(2026, 11, 18));
    }

    [Fact]
    public void DateOfWeek_ShouldDateAnUndefinedWeekInsideTheDuration()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 2));
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Act
        var date = mesocycle.DateOfWeek(WeekNumber.From(4));

        // Assert
        date.Should().Be(new DateTime(2026, 11, 25));
    }

    [Fact]
    public void DateOfWeek_ShouldRejectAWeekBeyondTheDuration()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 2));

        // Act
        var act = () => mesocycle.DateOfWeek(WeekNumber.From(5));

        // Assert
        act.Should().Throw<MesocycleWeekOutOfRangeException>();
    }

    [Fact]
    public void DateOfWeek_ShouldFollowTheNewStartDateAfterRescheduling()
    {
        // Arrange
        var mesocycle = MesocycleStartingOn(new DateTime(2026, 11, 2));
        mesocycle.Reschedule(new DateTime(2026, 11, 9));

        // Act
        var date = mesocycle.DateOfWeek(WeekNumber.From(1));

        // Assert
        date.Should().Be(new DateTime(2026, 11, 11));
    }

    [Fact]
    public void ChangeDuration_ShouldAcceptADurationThatFitsTheDefinedWeeks()
    {
        // Arrange
        var mesocycle = NewMesocycle(DurationInWeeks.From(6));
        foreach (var number in Enumerable.Range(1, 4))
            mesocycle.AddWeek(WeekNumber.From(number), TrainingWeekType.ACCUMULATION, WeekObjective);

        // Act
        mesocycle.ChangeDuration(FourWeeks);

        // Assert
        mesocycle.DurationInWeeks.Should().Be(FourWeeks);
        mesocycle.Weeks.Should().HaveCount(4);
    }

    [Fact]
    public void ChangeDuration_ShouldAcceptAnyDurationWithoutWeeks()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        var oneWeek = DurationInWeeks.From(1);

        // Act
        mesocycle.ChangeDuration(oneWeek);

        // Assert
        mesocycle.DurationInWeeks.Should().Be(oneWeek);
    }

    [Fact]
    public void ChangeDuration_ShouldRejectADurationBelowADefinedWeekAndKeepEverything()
    {
        // Arrange
        var sixWeeks = DurationInWeeks.From(6);
        var mesocycle = NewMesocycle(sixWeeks);
        mesocycle.AddWeek(WeekNumber.From(5), TrainingWeekType.REALIZATION, WeekObjective);

        // Act
        var act = () => mesocycle.ChangeDuration(FourWeeks);

        // Assert
        act.Should().Throw<MesocycleDurationTooShortException>();
        mesocycle.DurationInWeeks.Should().Be(sixWeeks);
        mesocycle.Weeks.Should().ContainSingle(week => week.WeekNumber == WeekNumber.From(5));
    }

    [Fact]
    public void Weeks_ShouldBeOrderedByWeekNumber()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(3), TrainingWeekType.INTENSIFICATION, WeekObjective);
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        // Act
        mesocycle.AddWeek(WeekNumber.From(2), TrainingWeekType.ACCUMULATION, WeekObjective);

        // Assert
        mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Weeks_ShouldNotExposeTheInternalList()
    {
        // Arrange
        var mesocycle = NewMesocycle();
        mesocycle.AddWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);
        var snapshot = (List<MesocycleWeek>)mesocycle.Weeks;

        // Act
        snapshot.Clear();

        // Assert
        mesocycle.Weeks.Should().HaveCount(1);
    }
}
