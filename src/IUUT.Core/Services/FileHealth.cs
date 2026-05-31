namespace IUUT.Core.Services;

/// <summary>Health of one save file within a profile.</summary>
public sealed record FileHealth(string RelativePath, FileHealthStatus Status, string? Detail);
