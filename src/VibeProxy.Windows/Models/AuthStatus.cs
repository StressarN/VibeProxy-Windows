using System;

namespace VibeProxy.Windows.Models;

public enum AuthProviderType
{
    Claude,
    Codex,
    Gemini,
    Qwen
}

public sealed class AuthStatus
{
    public AuthStatus(AuthProviderType type, bool isAuthenticated = false, string? email = null, DateTimeOffset? expiresAt = null)
    {
        Type = type;
        IsAuthenticated = isAuthenticated;
        Email = email;
        ExpiresAt = expiresAt;
    }

    public AuthProviderType Type { get; }

    public bool IsAuthenticated { get; init; }

    public string? Email { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public bool IsExpired => IsAuthenticated && ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

    public string DisplayText
    {
        get
        {
            if (!IsAuthenticated)
            {
                return "Not Connected";
            }

            var label = string.IsNullOrWhiteSpace(Email) ? "Connected" : Email!;
            return IsExpired ? $"{label} (expired)" : label;
        }
    }

    public AuthStatus With(bool? isAuthenticated = null, string? email = null, DateTimeOffset? expiresAt = null)
    {
        return new AuthStatus(
            Type,
            isAuthenticated ?? IsAuthenticated,
            email ?? Email,
            expiresAt ?? ExpiresAt);
    }
}
