using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class BreakWorkoutTemplateSuperSeriesUseCase
{
    private const string Operation = "DELETE /workout-templates/{templateId}/superseries/{superSeriesId}";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public BreakWorkoutTemplateSuperSeriesUseCase(
        IWorkoutTemplateRepository templates,
        WorkoutTemplateEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    /// <exception cref="SuperSeriesNotFoundException">When no exercise of the template belongs to the superseries.</exception>
    public async Task Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        int superSeriesId,
        CancellationToken cancellationToken)
    {
        var superSeries = SuperSeriesId.From(superSeriesId);

        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        template.BreakSuperSeries(superSeries);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
