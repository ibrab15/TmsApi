using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Services
// Register our lightweight stub handler to satisfy the pipeline's challenge requirements
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Bearer", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// 1. Custom Logging Middleware (Outer Wrapper)
app.UseMiddleware<RequestLoggingMiddleware>();

// 2. Standard Plumbing
app.UseExceptionHandler("/error"); 
app.UseHttpsRedirection();
app.UseRouting();

// 3. Security Layer
app.UseAuthentication();
app.UseAuthorization();

// 4. Endpoints
app.MapGet("/error", () => Results.Problem("An unexpected error occurred."))
   .AllowAnonymous(); 

app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101", 
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();

// --- IN-LINE STUB AUTHENTICATION HANDLER ---
public class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, // Corrected parameter type
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder) 
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Deny all requests by default so they fall back to anonymous evaluation
        return Task.FromResult(AuthenticateResult.Fail("No token provided."));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Gracefully issue a 401 instead of blowing up the framework
        Context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}