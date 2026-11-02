using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Users;

/// <summary>
/// A person using the system. Owns the credentials used by the authentication flow.
/// </summary>
/// <remarks>
/// The aggregate never sees a plain-text password: hashing is an infrastructure concern
/// and the already-hashed value is handed in by the Application layer, typed as
/// <see cref="Users.PasswordHash"/>. Strength rules live on <see cref="Password"/>, at the
/// boundary where a plain-text exists, and never reach this aggregate.
/// </remarks>
public sealed class User : AggregateRoot
{
    public Email Email { get; private set; }

    public PersonName Name { get; private set; }

    public PersonName Surname { get; private set; }

    public DateOnly DateBirth { get; private set; }

    public Role Role { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// False once an administrator deactivates the user. Always the opposite of whether
    /// <see cref="DeactivatedAtUtc"/> has a value.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTime? DeactivatedAtUtc { get; private set; }

    private User(
        Id id,
        Email email,
        PersonName name,
        PersonName surname,
        DateOnly dateBirth,
        Role role,
        PasswordHash passwordHash,
        DateTime createdAt,
        bool isActive,
        DateTime? deactivatedAtUtc
    )
    {
        this.Id = id;
        Email = email;
        Name = name;
        Surname = surname;
        DateBirth = dateBirth;
        Role = role;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        IsActive = isActive;
        DeactivatedAtUtc = deactivatedAtUtc;
    }

    /// <summary>
    /// Creates a brand new user. Every user starts active.
    /// </summary>
    /// <param name="passwordHash">Password already hashed by the infrastructure hasher.</param>
    public static User Create(
        Email email,
        PersonName name,
        PersonName surname,
        DateOnly dateBirth,
        Role role,
        PasswordHash passwordHash,
        DateTime createdAtUtc
    )
    {
        return new User(Id.New(), email, name, surname, dateBirth, role, passwordHash, createdAtUtc, true, null)
        {
            IsNew = true,
        };
    }

    /// <summary>
    /// Restores a user already stored in the system.
    /// </summary>
    /// <exception cref="InvalidUserActivationStateException">
    /// When <paramref name="deactivatedAtUtc"/> is set on an active user, or missing on a deactivated one.
    /// </exception>
    public static User Rehydrate(
        Id id,
        Email email,
        PersonName name,
        PersonName surname,
        DateOnly dateBirth,
        Role role,
        PasswordHash passwordHash,
        DateTime createdAt,
        bool isActive,
        DateTime? deactivatedAtUtc
    )
    {
        if (isActive != (deactivatedAtUtc is null))
            throw new InvalidUserActivationStateException(id);

        return new User(id, email, name, surname, dateBirth, role, passwordHash, createdAt, isActive, deactivatedAtUtc)
        {
            IsNew = false,
        };
    }

    /// <summary>
    /// Replaces the name and surname, the only parts of the profile the user owns. Email and
    /// role are never touched here.
    /// </summary>
    public void UpdateProfile(PersonName name, PersonName surname)
    {
        Name = name;
        Surname = surname;
    }

    /// <summary>
    /// True when this user is allowed to be assigned as the trainer of a workout.
    /// </summary>
    public bool CanTrain() => Role is Role.TRAINER or Role.ADMIN;

    /// <summary>
    /// True when this user may be given a new workout, as its owner or its trainer.
    /// </summary>
    public bool CanBeAssignedWorkouts() => IsActive;

    /// <summary>
    /// True when this user may be the trainer in a trainer–athlete relationship. Unlike <see cref="CanTrain"/>,
    /// an administrator does not qualify.
    /// </summary>
    public bool CanBeTrainer() => Role == Role.TRAINER && IsActive;

    /// <summary>
    /// True when this user may be the athlete in a trainer–athlete relationship.
    /// </summary>
    public bool CanBeAthlete() => Role == Role.USER && IsActive;

    /// <summary>
    /// Switches the account off and records when.
    /// </summary>
    /// <exception cref="UserAlreadyDeactivatedException">When the user is already deactivated.</exception>
    public void Deactivate(DateTime nowUtc)
    {
        if (IsActive == false)
            throw new UserAlreadyDeactivatedException(Id);

        IsActive = false;
        DeactivatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Switches a deactivated account back on and clears the deactivation date.
    /// </summary>
    /// <exception cref="UserAlreadyActiveException">When the user is already active.</exception>
    public void Activate()
    {
        if (IsActive)
            throw new UserAlreadyActiveException(Id);

        IsActive = true;
        DeactivatedAtUtc = null;
    }
}
