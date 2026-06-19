using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        using var scope = _scopeFactory.CreateScope();

        var enrollmentService =
            scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        Console.WriteLine("Enrollment batch processed.");

        // Example usage
        var records = enrollmentService.GetAllAsync().Result;

        Console.WriteLine($"Records found: {records.Count}");
    }
}