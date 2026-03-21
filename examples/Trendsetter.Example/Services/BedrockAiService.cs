namespace Trendsetter.Example.Services;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trendsetter.Example.Configuration;
using Trendsetter.Example.Models;
using Trendsetter.Example.Services.Dtos;

public sealed class MyAiService : IMyAiService
{
    private readonly HttpClient _httpClient;
    private readonly AwsOptions _options;
    private readonly ILogger<MyAiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public MyAiService(
        HttpClient httpClient,
        IOptions<AwsOptions> options,
        ILogger<MyAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProcedureModel>> ExtractProceduresAsync(string input)
    {
        var result = await CallBedrockAsync<ProcedureListDto>(
            input,
            "Extract all procedures from the provided medical record text. Use the extract_data tool to return the structured data.",
            "Extract and return structured procedure data from medical records.");

        if (result?.Procedures is null)
            return [];

        return result.Procedures.Select(MapProcedure).ToList();
    }

    public async Task<IReadOnlyList<DiagnosisModel>> ExtractDiagnosesAsync(string input)
    {
        var result = await CallBedrockAsync<DiagnosisListDto>(
            input,
            "Extract ALL diagnoses (ICD-10 codes) from the provided medical record text. Include every diagnosis mentioned anywhere in the record. Use the extract_data tool to return the structured data.",
            "Extract and return structured diagnosis data from medical records.");

        if (result?.Diagnoses is null)
            return [];
        return result.Diagnoses
            .Select(dx => new DiagnosisModel(dx.Code ?? string.Empty, dx.Description ?? string.Empty))
            .ToList();
    }

    public async Task<PatientInfoModel> ExtractPatientInfoAsync(string input)
    {
        var result = await CallBedrockAsync<PatientInfoDto>(
            input,
            "Extract patient demographic information from the provided medical record text. Use the extract_data tool to return the structured data.",
            "Extract and return structured patient info from medical records.");

        return new PatientInfoModel(
            FullName: result?.FullName ?? string.Empty,
            DateOfBirth: DateOnly.TryParse(result?.DateOfBirth, out var dob) ? dob : default,
            Gender: result?.Gender ?? string.Empty,
            MemberId: result?.MemberId ?? string.Empty);
    }

    public async Task<PatientModel> ExtractPatientAsync(string input)
    {
        var result = await CallBedrockAsync<PatientDto>(
            input,
            "Extract the complete patient record including patient info, insurance, all procedures, and all diagnoses from the provided medical record text. Use the extract_data tool to return the structured data.",
            "Extract and return a complete structured patient record from medical records.");

        return new PatientModel(
            PatientInfo: new PatientInfoModel(
                FullName: result?.PatientInfo?.FullName ?? string.Empty,
                DateOfBirth: DateOnly.TryParse(result?.PatientInfo?.DateOfBirth, out var dob) ? dob : default,
                Gender: result?.PatientInfo?.Gender ?? string.Empty,
                MemberId: result?.PatientInfo?.MemberId ?? string.Empty),
            Insurance: new InsuranceModel(
                PayerName: result?.Insurance?.PayerName ?? string.Empty,
                PlanType: result?.Insurance?.PlanType ?? string.Empty,
                GroupNumber: result?.Insurance?.GroupNumber ?? string.Empty),
            Procedures: result?.Procedures?.Select(MapProcedure).ToList() ?? [],
            Diagnoses: result?.Diagnoses?
                .Select(dx => new DiagnosisModel(dx.Code ?? string.Empty, dx.Description ?? string.Empty))
                .ToList() ?? []);
    }

    // ── Shared Bedrock call ────────────────────────────────────────

    private async Task<T?> CallBedrockAsync<T>(
        string input,
        string systemPrompt,
        string toolDescription) where T : class
    {
        var toolSchema = JsonSchemaGenerator.Generate<T>();

        var requestBody = new ConverseRequestBody
        {
            System = [new TextBlock { Text = systemPrompt }],
            Messages =
            [
                new ConversationMessage
                {
                    Role = "user",
                    Content = [new ContentBlock { Text = input }],
                },
            ],
            InferenceConfig = new InferenceConfig
            {
                Temperature = 0.0f,
                MaxTokens = 4096,
            },
            ToolConfig = new ToolConfiguration
            {
                Tools =
                [
                    new ToolDefinition
                    {
                        ToolSpec = new ToolSpec
                        {
                            Name = "extract_data",
                            Description = toolDescription,
                            InputSchema = new InputSchemaWrapper { Json = toolSchema },
                        },
                    },
                ],
                ToolChoice = new ToolChoiceWrapper
                {
                    Tool = new ToolChoiceTool { Name = "extract_data" },
                },
            },
        };

        var modelId = _options.BedrockModelId;
        var endpoint = $"/model/{Uri.EscapeDataString(modelId)}/converse";

        _logger.LogInformation("Calling Bedrock model {ModelId} for {Type} extraction", modelId, typeof(T).Name);

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(endpoint, content);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Bedrock returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            return null;
        }

        var converseResponse = JsonSerializer.Deserialize<ConverseResponseBody>(responseBody, JsonOptions);

        var toolInput = converseResponse?.Output?.Message?.Content?
            .Where(c => c.ToolUse is not null)
            .Select(c => c.ToolUse!.Input)
            .FirstOrDefault();

        if (toolInput is null)
        {
            _logger.LogWarning("Bedrock did not return a tool use block");
            return null;
        }

        _logger.LogDebug("Bedrock tool input: {Input}", toolInput);
        return JsonSerializer.Deserialize<T>(toolInput.Value.GetRawText(), JsonOptions);
    }

    // ── Mapping ────────────────────────────────────────────────────

    private static ProcedureModel MapProcedure(ProcedureDto p)
    {
        return new(
        Name: p.Name ?? string.Empty,
        ShortDescription: p.ShortDescription ?? string.Empty,
        DateOfTreatment: DateOnly.TryParse(p.DateOfTreatment, out var d) ? d : default,
        ProviderName: p.ProviderName ?? string.Empty,
        Diagnoses: p.Diagnoses?
            .Select(dx => new DiagnosisModel(dx.Code ?? string.Empty, dx.Description ?? string.Empty))
            .ToList() ?? []);
    }
}
