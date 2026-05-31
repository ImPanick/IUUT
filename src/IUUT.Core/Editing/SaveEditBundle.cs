using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// The four core save models loaded together for a Custom edit (master doc §10.3). A category
/// edit mutates the relevant model(s) in place; <see cref="CustomApplyService"/> then writes
/// only the files that actually changed. The models are mutable by design.
/// </summary>
public sealed class SaveEditBundle
{
    /// <summary><c>Profile.json</c> — account currencies, flags, workshop/prospect unlocks.</summary>
    public required ProfileModel Profile { get; init; }

    /// <summary><c>Characters.json</c> — the character roster.</summary>
    public required List<CharacterModel> Characters { get; init; }

    /// <summary><c>Accolades.json</c> — the completed-accolade log.</summary>
    public required AccoladesModel Accolades { get; init; }

    /// <summary><c>BestiaryData.json</c> — creature scan progress.</summary>
    public required BestiaryModel Bestiary { get; init; }
}
