using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

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
// CONTROLLERS (Session 3 Requirement)
// ======================================================
builder.Services.AddControllers();

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
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
// ======================================================
// OPTIONS PATTERN (Exercise 3)
// ======================================================
builder.Services.AddOptions<PaymentOptions>()
.BindConfiguration("Payments")
.ValidateDataAnnotations()
.ValidateOnStart();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ======================================================
// MIDDLEWARE PIPELINE
// ======================================================

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseExceptionHandler("/error");

app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();


// ======================================================
// ENDPOINTS
// ======================================================

app.MapGet("/error", () =>
{
return Results.Problem("An unexpected error occurred.");
})
.AllowAnonymous();

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

app.MapGet("/api/enrollments/worker-smoke",
(EnrollmentWorker worker) =>
{
worker.ProcessBatch();
return Results.Ok("processed");
});

app.MapGet("/api/payments/config",
(IOptions<PaymentOptions> options) =>
{
return Results.Ok(options.Value);
});

app.MapGet("/api/enrollments/test",
async (IEnrollmentService service) =>
{
await service.EnrollAsync("S-001", "CS-101");

await service.EnrollAsync("S-001", "CS-101");

await service.GetByIdAsync("xyz");

await service.DeleteAsync("xyz");

return Results.Ok("Logging test complete");


});

// ======================================================
// SESSION 3 CONTROLLERS
// ======================================================
app.MapControllers();

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException(
        "Simulated database failure for ProblemDetails testing");
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
