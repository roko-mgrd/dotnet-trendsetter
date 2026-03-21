using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trendsetter.Engine.Contracts;
using Trendsetter.Engine.Reports;
using Trendsetter.Example.Configuration;
using Trendsetter.Example.Services;
using Trendsetter.Trends.Trends.Services;

// ── Configuration ──────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Trendsetter.Example.Configuration.AwsOptions>()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

// ── DI container ───────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);
services.Configure<AwsOptions>(configuration.GetSection(AwsOptions.SectionName));

services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

services.AddHttpClient<IMyAiService, MyAiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>()
        .GetSection(AwsOptions.SectionName).Get<AwsOptions>()!;

    client.BaseAddress = new Uri($"https://bedrock-runtime.{config.Region}.amazonaws.com");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.BedrockToken);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

services.AddTransient<ProceduresTrendTest>();
services.AddTransient<DiagnosesTrendTest>();
services.AddTransient<PatientInfoTrendTest>();
services.AddTransient<PatientTrendTest>();

await using var sp = services.BuildServiceProvider();

// ── Run trend tests ────────────────────────────────────────────────
var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var baseDirectory = Path.Combine(projectDir, "reports");

var tests = new RunCommand.TestEntry[]
{
    Wrap(sp.GetRequiredService<ProceduresTrendTest>()),
    Wrap(sp.GetRequiredService<DiagnosesTrendTest>()),
    Wrap(sp.GetRequiredService<PatientInfoTrendTest>()),
    Wrap(sp.GetRequiredService<PatientTrendTest>()),
};

await RunCommand.RunAsync(args, tests, baseDirectory);

return;

static RunCommand.TestEntry Wrap<TModel, TResponse>(TrendTest<TModel, TResponse> test)
    => new(test.TestId, history => test.RunAsync(history));
