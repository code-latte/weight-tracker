using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.CustomerLeads;
using WeightTracker.Api.OpenApi;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.AthleteInvitations;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.CustomerLeads;
using WeightTracker.Application.UseCases.ExerciseFavorites;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Application.UseCases.WorkoutTemplates;
using WeightTracker.Application.UseCases.TrainerAthletes;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Repositories;
using WeightTracker.Infra.Seed;
using WeightTracker.Infra.Services;
using WeightTracker.Migrations;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:Default is not configured. "
            + "Supply it through configuration or the ConnectionStrings__Default environment variable."
    );

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<TrainerInvitationOptions>(builder.Configuration.GetSection(TrainerInvitationOptions.SectionName));

var jwtOptions =
    builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        $"The '{JwtOptions.SectionName}' configuration section is missing"
    );

// Fail fast rather than booting with a weak or absent signing key
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < JwtOptions.MinimumSigningKeyBytes)
    throw new InvalidOperationException(
        $"Jwt:SigningKey must be at least {JwtOptions.MinimumSigningKeyBytes} bytes. "
            + "Supply it through configuration or the Jwt__SigningKey environment variable."
    );

var trainerInvitationOptions =
    builder.Configuration.GetSection(TrainerInvitationOptions.SectionName).Get<TrainerInvitationOptions>()
    ?? new TrainerInvitationOptions();

// Fail fast: a lifetime below a day would make every invitation invalid and surface as a caller error
if (trainerInvitationOptions.ExpirationDays < TrainerInvitationOptions.MinimumExpirationDays)
    throw new InvalidOperationException(
        $"TrainerInvitations:ExpirationDays must be at least {TrainerInvitationOptions.MinimumExpirationDays}. "
            + "Supply it through configuration or the TrainerInvitations__ExpirationDays environment variable."
    );

// Bound through options rather than read here, so the limiter sees the host's final configuration.
// Fail fast on start: a zero limit or window would reject every lead or never reset.
builder.Services.AddOptions<CustomerLeadsRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(CustomerLeadsRateLimitOptions.SectionName))
    .Validate(
        options => options.PermitLimit >= 1 && options.WindowSeconds >= 1,
        "RateLimiting:CustomerLeads:PermitLimit and RateLimiting:CustomerLeads:WindowSeconds must be at least 1. "
            + "Supply them through configuration or the RateLimiting__CustomerLeads__* environment variables."
    )
    .ValidateOnStart();

builder.Services.AddDbContext<WeightTrackerDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IExerciseFavoriteRepository, ExerciseFavoriteRepository>();
builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITrainerAthleteRepository, TrainerAthleteRepository>();
builder.Services.AddScoped<ITrainerInvitationRepository, TrainerInvitationRepository>();
builder.Services.AddScoped<IMesocycleRepository, MesocycleRepository>();
builder.Services.AddScoped<IWorkoutTemplateRepository, WorkoutTemplateRepository>();
builder.Services.AddScoped<ICustomerLeadRepository, CustomerLeadRepository>();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IInvitationTokenService, InvitationTokenService>();
builder.Services.AddScoped<WorkoutEditAuthorizer>();
builder.Services.AddScoped<WorkoutRecordingAuthorizer>();
builder.Services.AddScoped<WorkoutVisibility>();
builder.Services.AddScoped<WorkoutListLoader>();
builder.Services.AddScoped<ExerciseVisibility>();
builder.Services.AddScoped<ExerciseEditAuthorizer>();
builder.Services.AddScoped<WorkoutStatusAuthorizer>();
builder.Services.AddScoped<WorkoutAssignmentValidator>();
builder.Services.AddScoped<InvitationViewLoader>();
builder.Services.AddScoped<TrainerAthleteViewLoader>();
builder.Services.AddScoped<MesocycleViewLoader>();
builder.Services.AddScoped<MesocycleVisibility>();
builder.Services.AddScoped<MesocycleEditAuthorizer>();
builder.Services.AddScoped<MesocycleAssignmentValidator>();
builder.Services.AddScoped<MesocyclePlacementValidator>();
builder.Services.AddScoped<WorkoutTemplateEditAuthorizer>();
builder.Services.AddScoped<WorkoutTemplateTrainerValidator>();

builder.Services.AddScoped<GetAllExercisesUseCase>();
builder.Services.AddScoped<GetExerciseByIdUseCase>();
builder.Services.AddScoped<CreateExerciseUseCase>();
builder.Services.AddScoped<UpdateExerciseUseCase>();
builder.Services.AddScoped<ArchiveExerciseUseCase>();
builder.Services.AddScoped<MarkExerciseFavoriteUseCase>();
builder.Services.AddScoped<UnmarkExerciseFavoriteUseCase>();
builder.Services.AddScoped<GetExerciseFavoritesUseCase>();
builder.Services.AddScoped<GetAllWorkoutsUseCase>();
builder.Services.AddScoped<GetWorkoutByIdUseCase>();
builder.Services.AddScoped<CreateWorkoutUseCase>();
builder.Services.AddScoped<UpdateWorkoutUseCase>();
builder.Services.AddScoped<DeleteWorkoutUseCase>();
builder.Services.AddScoped<AddWorkoutExerciseUseCase>();
builder.Services.AddScoped<UpdateWorkoutExerciseUseCase>();
builder.Services.AddScoped<RemoveWorkoutExerciseUseCase>();
builder.Services.AddScoped<ChangeWorkoutStatusUseCase>();
builder.Services.AddScoped<ReviewWorkoutUseCase>();
builder.Services.AddScoped<CreateSuperSeriesUseCase>();
builder.Services.AddScoped<BreakSuperSeriesUseCase>();
builder.Services.AddScoped<ReorderWorkoutExercisesUseCase>();
builder.Services.AddScoped<AddSetRecordUseCase>();
builder.Services.AddScoped<UpdateSetRecordUseCase>();
builder.Services.AddScoped<RemoveSetRecordUseCase>();
builder.Services.AddScoped<ReorderSetRecordsUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<SignupUseCase>();
builder.Services.AddScoped<RegisterCustomerLeadUseCase>();
builder.Services.AddScoped<GetAllCustomerLeadsUseCase>();
builder.Services.AddScoped<RefreshTokenUseCase>();
builder.Services.AddScoped<GetAuthenticatedUserUseCase>();
builder.Services.AddScoped<UpdateMyProfileUseCase>();
builder.Services.AddScoped<CreateUserUseCase>();
builder.Services.AddScoped<GetAllUsersUseCase>();
builder.Services.AddScoped<ActivateUserUseCase>();
builder.Services.AddScoped<DeactivateUserUseCase>();
builder.Services.AddScoped<LinkAthleteToTrainerUseCase>();
builder.Services.AddScoped<EndTrainerAthleteRelationshipUseCase>();
builder.Services.AddScoped<GetTrainerAthletesUseCase>();
builder.Services.AddScoped<InviteAthleteUseCase>();
builder.Services.AddScoped<RevokeInvitationUseCase>();
builder.Services.AddScoped<AcceptInvitationUseCase>();
builder.Services.AddScoped<RejectInvitationUseCase>();
builder.Services.AddScoped<GetTrainerInvitationsUseCase>();
builder.Services.AddScoped<GetAllMesocyclesUseCase>();
builder.Services.AddScoped<GetMesocycleByIdUseCase>();
builder.Services.AddScoped<GetMesocycleWorkoutsUseCase>();
builder.Services.AddScoped<GetUserMesocyclesUseCase>();
builder.Services.AddScoped<GetUserTrainerUseCase>();
builder.Services.AddScoped<CreateMesocycleUseCase>();
builder.Services.AddScoped<UpdateMesocycleUseCase>();
builder.Services.AddScoped<DefineMesocycleWeekUseCase>();
builder.Services.AddScoped<RedefineMesocycleWeekUseCase>();
builder.Services.AddScoped<RemoveMesocycleWeekUseCase>();
builder.Services.AddScoped<GetAllWorkoutTemplatesUseCase>();
builder.Services.AddScoped<GetWorkoutTemplateByIdUseCase>();
builder.Services.AddScoped<CreateWorkoutTemplateUseCase>();
builder.Services.AddScoped<UpdateWorkoutTemplateUseCase>();
builder.Services.AddScoped<DeleteWorkoutTemplateUseCase>();
builder.Services.AddScoped<AddWorkoutTemplateExerciseUseCase>();
builder.Services.AddScoped<UpdateWorkoutTemplateExerciseUseCase>();
builder.Services.AddScoped<RemoveWorkoutTemplateExerciseUseCase>();
builder.Services.AddScoped<ReorderWorkoutTemplateExercisesUseCase>();
builder.Services.AddScoped<CreateWorkoutTemplateSuperSeriesUseCase>();
builder.Services.AddScoped<BreakWorkoutTemplateSuperSeriesUseCase>();
builder.Services.AddScoped<CreateWorkoutFromTemplateUseCase>();
builder.Services.AddScoped<CreateWorkoutTemplateFromWorkoutUseCase>();

builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<ExceptionMapper>();

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim types exactly as they were issued
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization();

// Applied in Development only, so browsers and devices on the local network can call the API.
const string DevelopmentCorsPolicy = "DevelopmentCors";

// Applied in Production only: the website and the backoffice are the only browser clients.
// Native mobile apps are not subject to CORS.
const string ProductionCorsPolicy = "ProductionCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        DevelopmentCorsPolicy,
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    );
    options.AddPolicy(
        ProductionCorsPolicy,
        policy =>
            policy
                .WithOrigins("https://trainera.es", "https://www.trainera.es", "https://app.trainera.es")
                .AllowAnyHeader()
                .AllowAnyMethod()
    );
});

// Applied per endpoint through [EnableRateLimiting]; endpoints without it are never limited.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // The test server has no remote address, so every test request shares one partition
    options.AddPolicy(CustomerLeadsRateLimitOptions.PolicyName, context =>
    {
        var limit = context.RequestServices.GetRequiredService<IOptions<CustomerLeadsRateLimitOptions>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit.PermitLimit,
                Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                QueueLimit = 0,
            }
        );
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    // The built-in generator ignores the registered authentication schemes, so the bearer
    // scheme and the per-operation requirements have to be contributed explicitly.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<SecurityRequirementOperationTransformer>();
});

var app = builder.Build();

// The document is also served under Testing so the integration suite can assert its contents.
// The reference UI and the seeder stay Development only.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    app.MapOpenApi();

// The integration host migrates its own database, so Testing is skipped
if (!app.Environment.IsEnvironment("Testing"))
{
    DatabaseMigrator.MigrateUp(
        connectionString,
        builder.Configuration.GetSection(AdminUserOptions.SectionName).Get<AdminUserOptions>() ?? new AdminUserOptions()
    );
}

// CORS must stay ahead of UseHttpsRedirection and UseAuthentication so preflight
// requests are answered without a redirect or a bearer token.
if (app.Environment.IsProduction())
    app.UseCors(ProductionCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevelopmentCorsPolicy);

    app.MapScalarApiReference(options =>
        options
            .WithTitle("Weight Tracker API")
            .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName)
            .EnablePersistentAuthentication()
    );

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().Seed(CancellationToken.None);
}

app.UseHttpsRedirection();

// Ahead of UseAuthentication so a rejected request costs no token validation
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Exposed so the integration test host can reference this entry point.
/// </summary>
public partial class Program;
