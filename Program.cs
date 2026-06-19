using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// DI VALIDATION (Exercise 2)
// ======================================================
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// ======================================================
// AUTHENTICATION / AUTHORIZATION
// ======================================================
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(
        "Bearer",
        null);

builder.Services.AddAuthorization();

// ======================================================
// SERVICES (Exercise 2 + 4)
// ======================================================
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// ======================================================
// OPTIONS PATTERN (Exercise 3)
// ======================================================
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// ======================================================
// MIDDLEWARE PIPELINE
// ======================================================

// MUST be first
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// ======================================================
// ENDPOINTS
// ======================================================

// Error endpoint
app.MapGet("/error", () =>
{
    return Results.Problem("An unexpected error occurred.");
})
.AllowAnonymous();

// Protected endpoint
app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        courseCode = "CS-101",
        studentId = "S-001",
        letterGrade = "A"
    });
})
.RequireAuthorization();

// ======================================================
// EXERCISE 2: Worker Smoke Test
// ======================================================
app.MapGet("/api/enrollments/worker-smoke",
    (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

// ======================================================
// EXERCISE 3: Options Validation Test
// ======================================================
app.MapGet("/api/payments/config",
    (IOptions<PaymentOptions> options) =>
{
    return Results.Ok(options.Value);
});

// ======================================================
// EXERCISE 4: Logging Test Endpoint (IMPORTANT)
// ======================================================
app.MapGet("/api/enrollments/test",
    async (IEnrollmentService service) =>
{
    // Create enrollment
    await service.EnrollAsync("S-001", "CS-101");

    // Duplicate enrollment (triggers warning)
    await service.EnrollAsync("S-001", "CS-101");

    // Missing record lookup (warning)
    await service.GetByIdAsync("xyz");

    // Failed delete (warning)
    await service.DeleteAsync("xyz");

    return Results.Ok("Logging test complete");
});

app.Run();


// ======================================================
// AUTH HANDLER
// ======================================================
public class FakeAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(
            AuthenticateResult.Fail("No token provided."));
    }

    protected override Task HandleChallengeAsync(
        AuthenticationProperties properties)
    {
        Context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        return Task.CompletedTask;
    }
}
