namespace Trendsetter.Engine.Scorers;

/// <summary>
/// Plug in your own sentence embedding implementation.
/// For local use, wire ML.NET or an ONNX model.
/// For remote use, call OpenAI embeddings or similar.
/// </summary>
public interface ISentenceEncoder
{
    float[] Encode(string text);
}
