using Avalonia;
using PCL.UI.SystemIntegration;

namespace PCL;

internal sealed class MacOsPlatformStartup : IPlatformStartup
{
    public IPlatformShellIntegration Shell { get; } = new MacOsShellIntegration();

    public void Initialize(string[] args)
    {
    }

    public AppBuilder Configure()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            // Avalonia 在 AfterSetup 中导出应用菜单，必须先于原生后端的回调安装。
            .AfterSetup(builder => MacOsNativeMenu.Install(builder.Instance!))
            .With(
                new MacOSPlatformOptions
                {
                    ShowInDock = true,
                    DisableDefaultApplicationMenuItems = true,
                    DisableNativeMenus = false
                })
            .WithInterFont()
            .LogToTrace();
    }
}
