using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.ExerciseFavorites.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.ExerciseFavorites;
using WeightTracker.Domain.Common;

namespace WeightTracker.Api.Endpoints.ExerciseFavorites;

/// <summary>
/// The favorite exercises of the caller. Every role may keep favorites, and a caller only ever reaches their own.
/// </summary>
[ApiController]
[Route("exercise-favorites")]
public class ExerciseFavoritesController : AuthorizedController
{
    private readonly GetExerciseFavoritesUseCase _getAll;
    private readonly MarkExerciseFavoriteUseCase _mark;
    private readonly UnmarkExerciseFavoriteUseCase _unmark;

    public ExerciseFavoritesController(
        GetExerciseFavoritesUseCase getAll,
        MarkExerciseFavoriteUseCase mark,
        UnmarkExerciseFavoriteUseCase unmark,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAll = getAll;
        _mark = mark;
        _unmark = unmark;
    }

    /// <summary>
    /// Lists the caller's favorite exercises, newest first. A favorite stays listed when its exercise has been
    /// archived since.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExerciseFavoriteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var favorites = await _getAll.Execute(CurrentUser!.Id, cancellationToken);

            return Ok(favorites.Select(ExerciseFavoriteResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Marks an exercise the caller can see as a favorite. Marking one that already is a favorite returns it with
    /// its original date.
    /// </summary>
    [HttpPut("{exerciseId:guid}")]
    [ProducesResponseType<ExerciseFavoriteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Mark(Guid exerciseId, CancellationToken cancellationToken)
    {
        try
        {
            var favorite = await _mark.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(exerciseId), cancellationToken);

            return Ok(ExerciseFavoriteResponse.From(favorite));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Removes an exercise from the caller's favorites. Removing one that is not a favorite is a success too.
    /// </summary>
    [HttpDelete("{exerciseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unmark(Guid exerciseId, CancellationToken cancellationToken)
    {
        try
        {
            await _unmark.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(exerciseId), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
