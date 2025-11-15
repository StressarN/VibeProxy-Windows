namespace VibeProxy.Windows.Services;

public enum AuthCommand
{
    Claude,
    Codex,
    Gemini,
    Qwen
}

public sealed record AuthCommandResult(bool Success, string Message);
