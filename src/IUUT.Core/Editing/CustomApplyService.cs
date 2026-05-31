using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Services;
using IUUT.Core.Validation;

namespace IUUT.Core.Editing;

/// <summary>
/// The Custom-mode per-category <c>preview → apply</c> pipeline (master doc §10.3, §13.3). A
/// caller supplies an <c>edit</c> delegate that mutates a <see cref="SaveEditBundle"/>; preview
/// loads + parses the four core files, runs the edit, validates, and serializes — writing
/// nothing — and returns only the files whose content actually changed. Apply writes those
/// through <see cref="ISafeSaveWriter"/> (backup + re-parse) in recovery order. This is the
/// granular sibling of the Lazy Max apply pipeline; the WPF Custom shell/nav is parked.
/// </summary>
public sealed class CustomApplyService
{
    private const string ProfileFile = "Profile.json";
    private const string CharactersFile = "Characters.json";
    private const string AccoladesFile = "Accolades.json";
    private const string BestiaryFile = "BestiaryData.json";

    private readonly ISafeSaveWriter _writer;

    /// <summary>Creates the Custom apply pipeline over the save writer.</summary>
    public CustomApplyService(ISafeSaveWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    /// <summary>
    /// Loads the four core files, applies <paramref name="edit"/> in memory, validates, and
    /// returns the changed files + outcome. <see cref="SaveEditPlan.CanApply"/> is false when a
    /// file is missing/unparseable or validation found a blocking error.
    /// </summary>
    public async Task<SaveEditPlan> PreviewAsync(
        string saveFolder,
        Action<SaveEditBundle> edit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        ArgumentNullException.ThrowIfNull(edit);

        var issues = new List<ValidationIssue>();
        var profile = await ReadParseAsync(saveFolder, ProfileFile, ProfileParser.Parse, issues, cancellationToken).ConfigureAwait(false);
        var characters = await ReadParseAsync(saveFolder, CharactersFile, CharactersParser.Parse, issues, cancellationToken).ConfigureAwait(false);
        var accolades = await ReadParseAsync(saveFolder, AccoladesFile, AccoladesParser.Parse, issues, cancellationToken).ConfigureAwait(false);
        var bestiary = await ReadParseAsync(saveFolder, BestiaryFile, BestiaryParser.Parse, issues, cancellationToken).ConfigureAwait(false);

        if (profile is null || characters is null || accolades is null || bestiary is null)
        {
            return new SaveEditPlan
            {
                SaveFolder = saveFolder,
                ChangedFiles = [],
                Validation = ValidationResult.FromIssues(issues),
                CanApply = false,
            };
        }

        // Canonical "before" snapshots (serialized from the parsed models, so only a real edit
        // — not formatting — counts as a change).
        var before = new[]
        {
            ProfileSerializer.Serialize(profile),
            CharactersSerializer.Serialize(characters),
            AccoladesSerializer.Serialize(accolades),
            BestiarySerializer.Serialize(bestiary),
        };

        var bundle = new SaveEditBundle
        {
            Profile = profile,
            Characters = characters,
            Accolades = accolades,
            Bestiary = bestiary,
        };
        edit(bundle);

        var after = new[]
        {
            ProfileSerializer.Serialize(bundle.Profile),
            CharactersSerializer.Serialize(bundle.Characters),
            AccoladesSerializer.Serialize(bundle.Accolades),
            BestiarySerializer.Serialize(bundle.Bestiary),
        };

        var names = new[] { ProfileFile, CharactersFile, AccoladesFile, BestiaryFile };
        var changed = new List<PlannedFileWrite>();
        for (var i = 0; i < names.Length; i++)
        {
            if (!string.Equals(before[i], after[i], StringComparison.Ordinal))
            {
                changed.Add(new PlannedFileWrite
                {
                    FileName = names[i],
                    FilePath = Path.Combine(saveFolder, names[i]),
                    NewContent = after[i],
                });
            }
        }

        var folderSteamId = Path.GetFileName(Path.TrimEndingDirectorySeparator(saveFolder));
        var validation = ValidationResult.Combine(
            ValidationResult.FromIssues(issues),
            ValidationEngine.ValidateProfile(bundle.Profile, folderSteamId),
            ValidationEngine.ValidateCharacters(bundle.Characters, bundle.Profile));

        return new SaveEditPlan
        {
            SaveFolder = saveFolder,
            ChangedFiles = changed,
            Validation = validation,
            CanApply = !validation.HasErrors,
        };
    }

    /// <summary>Applies a confirmed <paramref name="plan"/>'s changed files; refuses when it cannot apply, and stops at the first failed write.</summary>
    public async Task<SaveEditApplyReport> ApplyAsync(SaveEditPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.CanApply)
        {
            return new SaveEditApplyReport
            {
                Applied = false,
                FileResults = [],
                Message = "The edit has blocking errors; nothing was written.",
            };
        }

        if (!plan.HasChanges)
        {
            return new SaveEditApplyReport
            {
                Applied = true,
                FileResults = [],
                Message = "No changes to apply.",
            };
        }

        var results = new List<SafeSaveResult>(plan.ChangedFiles.Count);
        foreach (var file in plan.ChangedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _writer
                .WriteAsync(file.FilePath, file.NewContent, ReparseValidator(file.FileName), cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);

            if (!result.Ok)
            {
                return new SaveEditApplyReport
                {
                    Applied = false,
                    FileResults = results,
                    Message = $"Write failed at {file.FileName}.",
                };
            }
        }

        return new SaveEditApplyReport
        {
            Applied = true,
            FileResults = results,
            Message = $"Applied {results.Count} file(s).",
        };
    }

    private static async Task<T?> ReadParseAsync<T>(
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

    private static Action<string> ReparseValidator(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return content => Reparse(name, content);
    }

    private static void Reparse(string name, string content)
    {
        switch (name)
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
                using (JsonDocument.Parse(content))
                {
                }

                break;
        }
    }
}
