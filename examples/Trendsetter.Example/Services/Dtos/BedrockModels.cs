namespace Trendsetter.Example.Services.Dtos;

using System.Text.Json;
using System.Text.Json.Nodes;

#region Bedrock Converse API DTOs

internal sealed class ConverseRequestBody
{
    public List<TextBlock>? System { get; init; }
    public List<ConversationMessage>? Messages { get; init; }
    public InferenceConfig? InferenceConfig { get; init; }
    public ToolConfiguration? ToolConfig { get; init; }
}

internal sealed class ConversationMessage
{
    public string? Role { get; init; }
    public List<ContentBlock>? Content { get; init; }
}

internal sealed class ContentBlock
{
    public string? Text { get; init; }
    public ToolUseBlock? ToolUse { get; init; }
}

internal sealed class TextBlock
{
    public string? Text { get; init; }
}

internal sealed class ToolUseBlock
{
    public string? ToolUseId { get; init; }
    public string? Name { get; init; }
    public JsonElement? Input { get; init; }
}

internal sealed class InferenceConfig
{
    public float Temperature { get; init; }
    public int MaxTokens { get; init; }
}

internal sealed class ToolConfiguration
{
    public List<ToolDefinition>? Tools { get; init; }
    public ToolChoiceWrapper? ToolChoice { get; init; }
}

internal sealed class ToolDefinition
{
    public ToolSpec? ToolSpec { get; init; }
}

internal sealed class ToolSpec
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public InputSchemaWrapper? InputSchema { get; init; }
}

internal sealed class InputSchemaWrapper
{
    public JsonNode? Json { get; init; }
}

internal sealed class ToolChoiceWrapper
{
    public ToolChoiceTool? Tool { get; init; }
}

internal sealed class ToolChoiceTool
{
    public string? Name { get; init; }
}

internal sealed class ConverseResponseBody
{
    public OutputBlock? Output { get; init; }
}

internal sealed class OutputBlock
{
    public ConversationMessage? Message { get; init; }
}

#endregion
