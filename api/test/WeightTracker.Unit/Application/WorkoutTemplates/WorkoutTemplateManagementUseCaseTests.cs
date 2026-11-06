using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.WorkoutTemplates;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.WorkoutTemplates;

/// <summary>
/// Covers creating, reading, listing, renaming and deleting workout templates.
/// </summary>
public class WorkoutTemplateManagementUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Full body");

    private readonly Mock<IWorkoutTemplateRepository> _templates = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);
    private readonly User _otherTrainer = AStoredUser(Role.TRAINER);
    private readonly User _admin = AStoredUser(Role.ADMIN);
    private readonly User _athlete = AStoredUser(Role.USER);
    private readonly User _deactivatedTrainer = AStoredUser(Role.TRAINER, isActive: false);

    private readonly WorkoutTemplate _template;

    public WorkoutTemplateManagementUseCaseTests()
    {
        _template = WorkoutTemplate.Rehydrate(Id.New(), _trainer.Id, Name, WorkoutDescription.From("Old"), []);

        _templates.Setup(repository => repository.GetById(_template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_template);
        _templates.Setup(repository => repository.Save(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _templates.Setup(repository => repository.Remove(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Task<WorkoutTemplate> Create(Id callerId, Role role, Id? trainerId)
        => new CreateWorkoutTemplateUseCase(
                _templates.Object, new WorkoutTemplateTrainerValidator(UsersHolding(_trainer, _admin, _athlete, _deactivatedTrainer).Object), _unitOfWork.Object)
            .Execute(callerId, role, trainerId, Name, null, CancellationToken.None);

    private Task<WorkoutTemplate> GetById(Id callerId, Role role, Id? templateId = null)
        => new GetWorkoutTemplateByIdUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer())
            .Execute(callerId, role, templateId ?? _template.Id, CancellationToken.None);

    private Task<WorkoutTemplate> Update(Id callerId, Role role, Id? templateId = null)
        => new UpdateWorkoutTemplateUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(callerId, role, templateId ?? _template.Id, WorkoutName.From("Renamed"), null, CancellationToken.None);

    private Task Delete(Id callerId, Role role, Id? templateId = null)
        => new DeleteWorkoutTemplateUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(callerId, role, templateId ?? _template.Id, CancellationToken.None);

    private Task<IEnumerable<WorkoutTemplate>> GetAll(Id callerId, Role role, Id? trainerId = null)
        => new GetAllWorkoutTemplatesUseCase(_templates.Object).Execute(callerId, role, trainerId, CancellationToken.None);

    private Id GivenUnknownTemplate()
    {
        var unknown = Id.New();
        _templates.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkoutTemplate?)null);

        return unknown;
    }

    private void VerifyNothingWritten()
    {
        _templates.Verify(repository => repository.Save(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _templates.Verify(repository => repository.Remove(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldMakeTheTrainerTheOwner()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var template = await Create(trainerId, Role.TRAINER, null);

        // Assert
        template.TrainerId.Should().Be(trainerId);
        template.Exercises.Should().BeEmpty();
        _templates.Verify(repository => repository.Save(template, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldRejectATrainerIdFromATrainer()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var act = () => Create(trainerId, Role.TRAINER, trainerId);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Create_ShouldMakeTheNamedTrainerTheOwnerForAnAdmin()
    {
        // Arrange
        var adminId = _admin.Id;

        // Act
        var template = await Create(adminId, Role.ADMIN, _trainer.Id);

        // Assert
        template.TrainerId.Should().Be(_trainer.Id);
    }

    [Fact]
    public async Task Create_ShouldRequireATrainerIdFromAnAdmin()
    {
        // Arrange
        var adminId = _admin.Id;

        // Act
        var act = () => Create(adminId, Role.ADMIN, null);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Create_ShouldRejectAnAdminAsTrainer()
    {
        // Arrange
        var adminId = _admin.Id;

        // Act
        var act = () => Create(adminId, Role.ADMIN, adminId);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Create_ShouldRejectARegularUserAsTrainer()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Create(_admin.Id, Role.ADMIN, athleteId);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Create_ShouldRejectADeactivatedTrainer()
    {
        // Arrange
        var deactivatedId = _deactivatedTrainer.Id;

        // Act
        var act = () => Create(_admin.Id, Role.ADMIN, deactivatedId);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Create_ShouldRejectAnUnknownTrainer()
    {
        // Arrange
        var unknownId = Id.New();

        // Act
        var act = () => Create(_admin.Id, Role.ADMIN, unknownId);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Create_ShouldRefuseARegularUser()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Create(athleteId, Role.USER, null);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task GetById_ShouldReturnTheTemplateToItsTrainer()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var template = await GetById(trainerId, Role.TRAINER);

        // Assert
        template.Should().BeSameAs(_template);
    }

    [Fact]
    public async Task GetById_ShouldReturnTheTemplateToAnAdmin()
    {
        // Arrange
        var adminId = _admin.Id;

        // Act
        var template = await GetById(adminId, Role.ADMIN);

        // Assert
        template.Should().BeSameAs(_template);
    }

    [Fact]
    public async Task GetById_ShouldHideTheTemplateFromAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = _otherTrainer.Id;

        // Act
        var act = () => GetById(otherTrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
    }

    [Fact]
    public async Task GetById_ShouldRejectAnUnknownTemplate()
    {
        // Arrange
        var unknown = GivenUnknownTemplate();

        // Act
        var act = () => GetById(_admin.Id, Role.ADMIN, unknown);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
    }

    [Fact]
    public async Task Update_ShouldReplaceTheNameAndClearTheDescriptionForAnAdmin()
    {
        // Arrange
        var adminId = _admin.Id;

        // Act
        var template = await Update(adminId, Role.ADMIN);

        // Assert
        template.Name.Value.Should().Be("Renamed");
        template.Description.Should().BeNull();
        template.TrainerId.Should().Be(_trainer.Id);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldHideTheTemplateFromAnotherTrainerAndChangeNothing()
    {
        // Arrange
        var otherTrainerId = _otherTrainer.Id;

        // Act
        var act = () => Update(otherTrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        _template.Name.Should().Be(Name);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Update_ShouldRejectAnUnknownTemplate()
    {
        // Arrange
        var unknown = GivenUnknownTemplate();

        // Act
        var act = () => Update(_trainer.Id, Role.TRAINER, unknown);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheTemplateOfItsTrainer()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        await Delete(trainerId, Role.TRAINER);

        // Assert
        _templates.Verify(repository => repository.Remove(_template, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldHideTheTemplateFromAnotherTrainerAndRemoveNothing()
    {
        // Arrange
        var otherTrainerId = _otherTrainer.Id;

        // Act
        var act = () => Delete(otherTrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Delete_ShouldRejectAnUnknownTemplate()
    {
        // Arrange
        var unknown = GivenUnknownTemplate();

        // Act
        var act = () => Delete(_admin.Id, Role.ADMIN, unknown);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task GetAll_ShouldListOnlyTheTrainerTemplatesAndIgnoreTheirFilter()
    {
        // Arrange
        var own = new[] { _template };
        _templates.Setup(repository => repository.GetAllByTrainerId(_trainer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(own);

        // Act
        var templates = await GetAll(_trainer.Id, Role.TRAINER, _otherTrainer.Id);

        // Assert
        templates.Should().Equal(own);
        _templates.Verify(repository => repository.GetAllByTrainerId(_otherTrainer.Id, It.IsAny<CancellationToken>()), Times.Never);
        _templates.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_ShouldListEveryTemplateForAnAdmin()
    {
        // Arrange
        var every = new[] { _template, WorkoutTemplate.Create(_otherTrainer.Id, Name, null) };
        _templates.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(every);

        // Act
        var templates = await GetAll(_admin.Id, Role.ADMIN);

        // Assert
        templates.Should().Equal(every);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByTrainerForAnAdmin()
    {
        // Arrange
        var others = new[] { WorkoutTemplate.Create(_otherTrainer.Id, Name, null) };
        _templates.Setup(repository => repository.GetAllByTrainerId(_otherTrainer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(others);

        // Act
        var templates = await GetAll(_admin.Id, Role.ADMIN, _otherTrainer.Id);

        // Assert
        templates.Should().Equal(others);
    }

    [Fact]
    public async Task GetAll_ShouldRefuseARegularUser()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => GetAll(athleteId, Role.USER);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }
}
