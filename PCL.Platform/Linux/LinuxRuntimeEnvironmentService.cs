namespace PCL.Platform;

/// <summary>Linux 运行环境检查：AppImage 运行时挂载在临时目录下，不视为从压缩包运行。</summary>
public sealed class LinuxRuntimeEnvironmentService : RuntimeEnvironmentServiceBase
{
    protected override bool IsInTempFolder(string executablePath) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE"))
        && base.IsInTempFolder(executablePath);
}
