using System.Globalization;
using System.Resources;

namespace Com.MiniCalculator.Helpers;

public static class Localization
{
    private static readonly ResourceManager ResourceManager =
        new("Com.MiniCalculator.Strings.Resources", typeof(Localization).Assembly);

    public static string Get(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
