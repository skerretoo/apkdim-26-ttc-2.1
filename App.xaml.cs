using System.Windows;

namespace PasswordGenerator;

/// <summary>App entry. Deliberately NO persistence: no settings load/save anywhere.</summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        // Best effort: drop clipboard if it still holds a generated password?
        // We leave the clipboard alone (user expectation) but all in-app
        // history dies with this process — it was never written to disk.
        base.OnExit(e);
    }
}
