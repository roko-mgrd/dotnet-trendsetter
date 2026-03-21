using System.Net.Http.Headers;
using Scalar.AspNetCore;
using Trendsetter.Example.Configuration;
using Trendsetter.Example.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// AWS Bedrock configuration
builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection(AwsOptions.SectionName));

builder.Services.AddHttpClient<IMyAiService, MyAiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>()
        .GetSection(AwsOptions.SectionName).Get<AwsOptions>()!;

    client.BaseAddress = new Uri($"https://bedrock-runtime.{config.Region}.amazonaws.com");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.BedrockToken);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
