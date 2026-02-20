using MaterialDesignThemes.Wpf;
using System.Windows;

namespace SmartAlloc.Services;

public class ThemeService
{
    private bool _isDark = true;
    public bool IsDark => _isDark;

    public event Action? ThemeChanged;

    public void ToggleTheme()
    {
        _isDark = !_isDark;
        Apply(_isDark);
        ThemeChanged?.Invoke();
    }

    private void Apply(bool dark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("Colors.xaml") ||
             d.Source.OriginalString.Contains("ColorsLight.xaml")));

        if (existing != null)
            dicts.Remove(existing);

        dicts.Insert(0, new ResourceDictionary
        {
            Source = new Uri(
                dark ? "Themes/Colors.xaml" : "Themes/ColorsLight.xaml",
                UriKind.Relative)
        });

        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }
}
