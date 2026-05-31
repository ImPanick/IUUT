using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Validation;

namespace IUUT.Core.Services;

/// <summary>
/// The Lazy Max apply pipeline (master doc §13.3): <c>preview → confirm → apply</c>. Preview
/// reads and parses the four core files, runs <see cref="LazyMaxService.MaxAll"/>, validates
/// the result (<see cref="ValidationEngine"/>), and serializes the new content — without
/// writing anything. Apply writes each file through <see cref="ISafeSaveWriter"/> in recovery
/// order (master §12.1), re-parsing every file after the write, and stops at the first failure
/// (each written file has a timestamped backup; CONSTITUTION III).
/// </summary>
public sealed class LazyMaxApplyService
{
    /// <summary>The account-wide profile file.</summary>
    public const string ProfileFile = "Profile.json";

    /// <summary>The character roster file.</summary>
    public const string CharactersFile = "Characters.json";

    /// <summary>The accolade log file.</summary>
    public const string AccoladesFile = "Accolades.json";

    /// <summary>The bestiary scan file.</summary>
    public const string BestiaryFile = "BestiaryData.json";

    private readonly LazyMaxService _lazyMax;
    private readonly ISafeSaveWriter _writer;

    /// <summary>Creates the apply pipeline over the maxing service and the save writer.</summary>
    public LazyMaxApplyService(LazyMaxService lazyMax, ISafeSaveWriter writer)
    {
        ArgumentNullException.ThrowIfNull(lazyMax);
        ArgumentNullException.ThrowIfNull(writer);
        _lazyMax = lazyMax;
        _writer = writer;
    }

    /// <summary>
    /// Builds a <see cref="LazyMaxPlan"/> for <paramref name="saveFolder"/>: reads + parses the
    /// four core files, maxes them in memory, validates, and serializes the new content. Writes
    /// nothing. <see cref="LazyMaxPlan.CanApply"/> is false if any file is missing/unparseable or
    /// validation found a blocking error.
    /// </summary>
    public async Task<LazyMaxPlan> PreviewAsync(string saveFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        var issues = new List<ValidationIssue>();
        var profile = await TryReadParseAsync(saveFolder, ProfileFile, ProfileParser.Parse, issues, cancellationToken).ConfigureAwait(false);
        var characters = await TryReadParseAsync(saveFolder, CharactersFile, CharactersParser.Parse, issues, cancellationToken).ConfigureAwait(false);
        var accolades = await TryReadParseAsync(saveFolder, AccoladesFile, AccoladesParser.Parse, issues, cancellationToken).ConfigureAwait(false);
        var bestiary = await TryReadParseAsync(saveFolder, BestiaryFile, BestiaryParser.Parse, issues, cancellationToken).ConfigureAwait(false);

        // All four files must be present and parseable to max the save.
        if (profile is null || characters is null || accolades is null || bestiary is null)
        {
            return new LazyMaxPlan
            {
                SaveFolder = saveFolder,
                Result = null,
                Validation = ValidationResult.FromIssues(issues),
                Files = [],
                CanApply = false,
            };
        }

        var result = _lazyMax.MaxAll(profile, characters, accolades, bestiary);

        var folderSteamId = Path.GetFileName(Path.TrimEndingDirectorySeparator(saveFolder));
        var validation = ValidationResult.Combine(
            ValidationResult.FromIssues(issues),
            ValidationEngine.ValidateProfile(profile, folderSteamId),
            ValidationEngine.ValidateCharacters(characters, profile));

        // Recovery order (master §12.1): Profile, Characters, Accolades, Bestiary.
        var files = new List<PlannedFileWrite>
        {
            Planned(saveFolder, ProfileFile, ProfileSerializer.Serialize(profile)),
            Planned(saveFolder, CharactersFile, CharactersSerializer.Serialize(characters)),
            Planned(saveFolder, AccoladesFile, AccoladesSerializer.Serialize(accolades)),
            Planned(saveFolder, BestiaryFile, BestiarySerializer.Serialize(bestiary)),
        };

        return new LazyMaxPlan
        {
            SaveFolder = saveFolder,
            Result = result,
            Validation = validation,
            Files = files,
            CanApply = !validation.HasErrors,
        };
    }

    /// <summary>
    /// Applies <paramref name="plan"/>: writes each file through <see cref="ISafeSaveWriter"/> in
    /// order, re-parsing the written content. Refuses to write when
    /// <see cref="LazyMaxPlan.CanApply"/> is false. Stops at the first failed write and reports
    /// how many files were applied (each has a backup for recovery).
    /// </summary>
    public async Task<LazyMaxApplyReport> ApplyAsync(LazyMaxPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.CanApply)
        {
            return new LazyMaxApplyReport
            {
                Applied = false,
                FileResults = [],
                Message = "The plan has blocking errors; nothing was written.",
            };
        }

        var results = new List<SafeSaveResult>(plan.Files.Count);
        foreach (var file in plan.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _writer
                .WriteAsync(file.FilePath, file.NewContent, ReparseValidator(file.FileName), cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);

            if (!result.Ok)
            {
                var applied = results.Count - 1;
                return new LazyMaxApplyReport
                {
                    Applied = false,
                    FileResults = results,
                    Message = $"Write failed at {file.FileName}. {applied} file(s) were applied first — restore them from the timestamped backups if needed.",
                };
            }
        }

        return new LazyMaxApplyReport
        {
            Applied = true,
            FileResults = results,
            Message = $"Applied {results.Count} file(s); a timestamped backup of each was created.",
        };
    }

    private static PlannedFileWrite Planned(string folder, string fileName, string content) =>
        new() { FileName = fileName, FilePath = Path.Combine(folder, fileName), NewContent = content };

    private static async Task<T?> TryReadParseAsync<T>(
        string folder,
        string fileName,
        Func<string, T> parse,
        List<ValidationIssue> issues,
        CancellationToken cancellationToken)
        where T : class
    {
        var path = Path.Combine(folder, fileName);
        if (!File.Exists(path))
        {
            issues.Add(new ValidationIssue(ValidationSeverity.Error, "file-missing", $"{fileName} was not found in the save folder.", fileName));
            return null;
        }

        string text;
        try
        {
            text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            issues.Add(new ValidationIssue(ValidationSeverity.Error, "file-unreadable", $"{fileName} could not be read: {ex.Message}", fileName));
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            issues.Add(new ValidationIssue(ValidationSeverity.Error, "file-unreadable", $"{fileName} could not be read: {ex.Message}", fileName));
            return null;
        }

        try
        {
            return parse(text);
        }
        catch (JsonException ex)
        {
            issues.Add(new ValidationIssue(ValidationSeverity.Error, "file-unparseable", $"{fileName} is not valid JSON: {ex.Message}", fileName));
            return null;
        }
    }

    private static Action<string> ReparseValidator(string fileName) =>
        content => Reparse(fileName, content);

    // Post-write re-parse: throws (JsonException) if the written content is not valid for its file.
    private static void Reparse(string fileName, string content)
    {
        switch (fileName)
        {
            case ProfileFile:
                _ = ProfileParser.Parse(content);
                break;
            case CharactersFile:
                _ = CharactersParser.Parse(content);
                break;
            case AccoladesFile:
                _ = AccoladesParser.Parse(content);
                break;
            case BestiaryFile:
                _ = BestiaryParser.Parse(content);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fileName), fileName, "Unknown save file.");
        }
    }
}
