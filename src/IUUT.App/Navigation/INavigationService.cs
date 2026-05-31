namespace IUUT.App.Navigation;

/// <summary>
/// In-window page navigation for the Glass Console shell (docs/UI-DESIGN-CONCEPT.md §8): the
/// single window swaps full-screen pages. View-models depend on this to navigate without
/// referencing the shell directly.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigates to the page with <paramref name="pageKey"/> (e.g. <c>Home</c>, <c>Recovery</c>).</summary>
    void NavigateTo(string pageKey);

    /// <summary>Resets the back stack and navigates Home.</summary>
    void GoHome();

    /// <summary>Navigates to the previous page if there is one.</summary>
    void GoBack();

    /// <summary>Whether a previous page exists on the back stack.</summary>
    bool CanGoBack { get; }
}
