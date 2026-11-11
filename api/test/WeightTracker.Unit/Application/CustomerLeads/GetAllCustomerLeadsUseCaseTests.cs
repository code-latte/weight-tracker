using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.CustomerLeads;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.CustomerLeads;

public class GetAllCustomerLeadsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICustomerLeadRepository> _leads = new(MockBehavior.Strict);

    private GetAllCustomerLeadsUseCase AUseCase() => new(_leads.Object);

    [Fact]
    public async Task Admin_ShouldSeeEveryLeadInRepositoryOrder()
    {
        // Arrange
        IReadOnlyList<CustomerLeadView> all =
        [
            new(Email.From("second@example.com"), Now.AddMinutes(1)),
            new(Email.From("first@example.com"), Now),
        ];
        _leads.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var result = await AUseCase().Execute(Role.ADMIN, CancellationToken.None);

        // Assert
        result.Should().Equal(all);
        _leads.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _leads.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task NonAdmin_ShouldBeRejectedAsUnauthorized(Role callerRole)
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(callerRole, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _leads.VerifyNoOtherCalls();
    }
}
