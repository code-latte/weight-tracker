using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.CustomerLeads;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Unit.Application.CustomerLeads;

public class RegisterCustomerLeadUseCaseTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private const string ValidEmail = "  Lead@Example.COM ";

    private readonly Mock<ICustomerLeadRepository> _leads = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RegisterCustomerLeadUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
        _leads.Setup(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    private RegisterCustomerLeadUseCase AUseCase() => new(_leads.Object, _clock.Object, _unitOfWork.Object);

    private void GivenTheEmailIsAlreadyRegistered()
        => _leads.Setup(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

    [Fact]
    public async Task NewEmail_ShouldAddTheNormalizedEmailWithTheClockTimeAndCommit()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        await useCase.Execute(ValidEmail, CancellationToken.None);

        // Assert
        _leads.Verify(repository => repository.Add(
            It.IsAny<Id>(), Email.From("lead@example.com"), Now, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lead@")]
    [InlineData("not-an-email")]
    public async Task InvalidEmail_ShouldThrowBeforeTouchingTheRepository(string? email)
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => useCase.Execute(email!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidEmailException>();
        _leads.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AlreadyRegisteredEmail_ShouldNeitherAddNorCommit()
    {
        // Arrange
        GivenTheEmailIsAlreadyRegistered();
        var useCase = AUseCase();

        // Act
        await useCase.Execute(ValidEmail, CancellationToken.None);

        // Assert
        _leads.Verify(repository => repository.Add(
            It.IsAny<Id>(), It.IsAny<Email>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentDuplicateOnCommit_ShouldBeSwallowed()
    {
        // Arrange
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CustomerLeadAlreadyExistsException(new Exception("unique violation")));
        var useCase = AUseCase();

        // Act
        var act = () => useCase.Execute(ValidEmail, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
