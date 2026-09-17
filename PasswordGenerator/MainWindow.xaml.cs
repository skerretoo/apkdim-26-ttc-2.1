using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PasswordGenerator.ViewModels;
namespace PasswordGenerator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        SourceInitialized += (_, _) => ApplyWindows11Style();
    }

    // Newest-premise native touch: Windows 11 rounded corners + thin border.
    // Pure Win32 via DWM — no extra dependencies, still 100% native WPF.
    private void ApplyWindows11Style()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == nint.Zero) return;

            int corner = (int)DwmWindowCornerPreference.Round;
            DwmSetWindowAttribute(hwnd, DwmWindowAttribute.WindowCornerPreference, ref corner, sizeof(int));

            int borderColor = 0x2B2B2B; // subtle COLORREF (BGR)
            DwmSetWindowAttribute(hwnd, DwmWindowAttribute.BorderColor, ref borderColor, sizeof(int));
        }
        catch
        {
            // Graceful: older Windows simply ignores this.
        }
    }

    private enum DwmWindowAttribute
    {
        WindowCornerPreference = 33,
        BorderColor = 34,
    }

    private enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3,
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint hwnd, DwmWindowAttribute attr, ref int value, int size);
}
