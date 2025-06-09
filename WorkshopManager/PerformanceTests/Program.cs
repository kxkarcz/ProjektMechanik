using NBomber.CSharp;
using NBomber.Http.CSharp;

Console.WriteLine("Rozpoczynam NBomber test...");

using var httpClient = new HttpClient();
string url = @"https://localhost:7027/Customer";

var scenario = Scenario.Create("TestGetCustomersWith50UsersAnd100Requests", async context =>
{
    try
    {
        // Wykonaj żądanie HTTP
        var response = await httpClient.GetAsync(url);
        
        Console.WriteLine($"Żądanie przez użytkownika {context.ScenarioInfo.InstanceId} - Status: {response.StatusCode}");
        
        // Małe opóźnienie aby każdy użytkownik mógł wykonać kilka żądań
        await Task.Delay(50);
        
        return response.IsSuccessStatusCode
            ? Response.Ok()
            : Response.Fail();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Błąd żądania: {ex.Message}");
        return Response.Fail();
    }
})
.WithWarmUpDuration(TimeSpan.FromSeconds(1))
.WithLoadSimulations(
    // 50 użytkowników przez czas wystarczający na ~2 żądania każdy
    Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(10))
);

var stats = NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("Reports")
    .Run();

Console.WriteLine($"Test zakończony!");
Console.WriteLine($"Statystyki NBomber - Żądania OK: {stats.AllOkCount}, Żądania Failed: {stats.AllFailCount}");