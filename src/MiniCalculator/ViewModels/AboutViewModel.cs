using System.Diagnostics;
using System.Reflection;
using Com.MiniCalculator.Helpers;

namespace Com.MiniCalculator.ViewModels;

public sealed class AboutViewModel
{
    private readonly string _productName;
    private readonly string _license;
    private readonly string _privacy;

    public AboutViewModel()
    {
        _productName = Localization.Get("App_Title");
        _license = Localization.Get("About_LicenseValue");
        _privacy = Localization.Get("About_PrivacyValue");

        Assembly assembly = Assembly.GetExecutingAssembly();
        Version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        string? path = Environment.ProcessPath ?? assembly.Location;
        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
            FileVersion = info.FileVersion ?? Version;
        }
        catch
        {
            FileVersion = Version;
        }
    }

    public string ProductName => _productName;

    public string Version { get; }

    public string FileVersion { get; }

    public string License => _license;

    public string Privacy => _privacy;
}
