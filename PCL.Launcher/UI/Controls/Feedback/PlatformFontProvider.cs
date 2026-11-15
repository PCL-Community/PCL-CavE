using Avalonia.Media;
using PCL.Core.Platform;

namespace PCL.UI.Controls;

internal static class PlatformFontProvider
{
    public static IPlatformFontProvider Current => PlatformRuntime.Current.Kind switch
    {
        PlatformKind.Windows => new WindowsFontProvider(),
        _ => throw new ArgumentOutOfRangeException()
    };
}

internal sealed class WindowsFontProvider : IPlatformFontProvider
{
    public FontFamily CreateBannerFont() => LaunchFonts.Build("Bahnschrift");
}
