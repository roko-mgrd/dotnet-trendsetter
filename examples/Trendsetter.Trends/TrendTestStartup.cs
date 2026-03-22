using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trendsetter.Example.Configuration;
using Trendsetter.Example.Services;
using Trendsetter.TestAdapter;

namespace Trendsetter.Trends;

public class TrendTestStartup : ITrendTestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<AwsOptions>()
            .AddEnvironmentVariables()
            .Build();

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
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.BedrockToken);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
