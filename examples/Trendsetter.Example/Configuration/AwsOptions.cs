namespace Trendsetter.Example.Configuration;

public sealed class AwsOptions
{
    public const string SectionName = "Aws";

    public string Region { get; set; } = string.Empty;
    public string BedrockToken { get; set; } = string.Empty;
    public string BedrockModelId { get; set; } = string.Empty;
}
