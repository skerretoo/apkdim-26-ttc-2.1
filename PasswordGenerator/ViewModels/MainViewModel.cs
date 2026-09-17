// MVVM ViewModel (CommunityToolkit.Mvvm 8.4.2 source generators, .NET 9, nullable).
// Privacy contract: history lives ONLY in this in-memory collection (max 3).
// No persistence: no file/registry/Settings/UserSettings code anywhere in the app.
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using PasswordGenerator.Models;
using PasswordGenerator.Services;

namespace PasswordGenerator.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private const int MaxHistory = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LengthText))]
    private int _length = 16;

    [ObservableProperty] private bool _includeLower = true;
    [ObservableProperty] private bool _includeUpper = true;
    [ObservableProperty] private bool _includeDigits = true;
    [ObservableProperty] private bool _includeSymbols = true;
    [ObservableProperty] private bool _excludeAmbiguous;
    [ObservableProperty] private bool _ensureEachCategory = true;
    [ObservableProperty] private bool _isDarkTheme = true;

    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private double _entropyBits;
    [ObservableProperty] private string _strengthText = "—";
    [ObservableProperty] private int _strengthPercent;
    [ObservableProperty] private string _statusMessage = "Ready. Nothing is saved anywhere.";

    public string LengthText => $"{Length} characters";

    /// <summary>In-memory ring of the last 3 passwords. Cleared on exit.</summary>
    public ObservableCollection<PasswordHistoryItem> History { get; } = [];

    public ISnackbarMessageQueue SnackbarQueue { get; } = new SnackbarMessageQueue();

    public MainViewModel() => Generate();

    partial void OnLengthChanged(int value) => RefreshPreview();
    partial void OnIncludeLowerChanged(bool value) => RefreshPreview();
    partial void OnIncludeUpperChanged(bool value) => RefreshPreview();
    partial void OnIncludeDigitsChanged(bool value) => RefreshPreview();
    partial void OnIncludeSymbolsChanged(bool value) => RefreshPreview();
    partial void OnExcludeAmbiguousChanged(bool value) => RefreshPreview();
    partial void OnEnsureEachCategoryChanged(bool value) => RefreshPreview();

    partial void OnIsDarkThemeChanged(bool value)
    {
        var helper = new PaletteHelper();
        var theme = helper.GetTheme();
        theme.SetBaseTheme(value ? BaseTheme.Dark : BaseTheme.Light);
        helper.SetTheme(theme);
    }

    private PasswordOptions Options => new(
        Length, IncludeLower, IncludeUpper, IncludeDigits, IncludeSymbols,
        ExcludeAmbiguous, EnsureEachCategory);

    /// <summary>
    /// Live preview: refreshes the current password as settings change
    /// (e.g. while dragging the slider) WITHOUT touching history.
    /// </summary>
    private void RefreshPreview()
    {
        if (!(IncludeLower || IncludeUpper || IncludeDigits || IncludeSymbols))
        {
            UpdateEntropyOnly();
            return;
        }

        try
        {
            CurrentPassword = PasswordGeneratorService.Generate(Options);
            EntropyBits = PasswordGeneratorService.EntropyBits(Options);
            (StrengthText, StrengthPercent) = StrengthOf(EntropyBits);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            SnackbarQueue.Enqueue(ex.Message);
        }
    }

    private void UpdateEntropyOnly()
    {
        EntropyBits = PasswordGeneratorService.EntropyBits(Options);
        (StrengthText, StrengthPercent) = StrengthOf(EntropyBits);
    }

    /// <summary>
    /// Explicit generation (Generate button): refreshes the password
    /// AND records it in the memory-only history (max 3).
    /// </summary>
    [RelayCommand]
    private void Generate()
    {
        RefreshPreview();
        if (string.IsNullOrEmpty(CurrentPassword))
            return;

        // Avoid a duplicate entry when pressing Generate without changing anything.
        if (History.Count == 0 || History[0].Value != CurrentPassword)
        {
            History.Insert(0, new PasswordHistoryItem(CurrentPassword, DateTime.Now));
            while (History.Count > MaxHistory)
                History.RemoveAt(History.Count - 1);
        }

        StatusMessage = $"Generated. {MaxHistory} kept in RAM only — never saved.";
    }

    [RelayCommand]
    private void CopyCurrent() => CopyToClipboard(CurrentPassword, "Current password copied.");

    [RelayCommand]
    private void CopyFromHistory(PasswordHistoryItem? item)
    {
        if (item is not null)
            CopyToClipboard(item.Value, "Password from history copied.");
    }

    [RelayCommand]
    private void ClearHistory()
    {
        // Overwrite references so strings can be GC'd; then drop them.
        History.Clear();
        StatusMessage = "History cleared from memory.";
        SnackbarQueue.Enqueue("History cleared from memory.");
    }

    private void CopyToClipboard(string value, string confirm)
    {
        if (string.IsNullOrEmpty(value))
        {
            SnackbarQueue.Enqueue("Nothing to copy yet.");
            return;
        }
        try
        {
            Clipboard.SetText(value);
            StatusMessage = $"{confirm} Clipboard holds it until you overwrite it.";
            SnackbarQueue.Enqueue(confirm);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    private static (string Text, int Percent) StrengthOf(double bits) => bits switch
    {
        < 40 => ("Weak", 20),
        < 60 => ("Fair", 45),
        < 80 => ("Strong", 70),
        < 100 => ("Very strong", 88),
        _ => ("Excellent", 100),
    };
}
