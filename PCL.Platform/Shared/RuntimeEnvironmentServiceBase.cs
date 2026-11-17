using PCL.Core.Platform;
using PCL.Core.Utils.OS;

namespace PCL.Platform;

/// <summary>各平台共用的运行环境检查。</summary>
public abstract class RuntimeEnvironmentServiceBase : IRuntimeEnvironmentService
{
    private static readonly string[] _SocialSoftwareFolderNames = ["wechat_files", "WeChat Files", "Tencent Files"];

    public IReadOnlyList<RuntimeEnvironmentIssue> GetIssues(string executablePath)
    {
        var issues = new List<RuntimeEnvironmentIssue>();
        if (IsOutdatedSystem())
            issues.Add(RuntimeEnvironmentIssue.OutdatedSystem);
        if (SystemInfo.Is32BitSystem)
            issues.Add(RuntimeEnvironmentIssue.System32Bit);
        if (IsInTempFolder(executablePath))
            issues.Add(RuntimeEnvironmentIssue.TempFolder);
        if (_SocialSoftwareFolderNames.Any(name => executablePath.Contains(name, StringComparison.OrdinalIgnoreCase)))
            issues.Add(RuntimeEnvironmentIssue.SocialSoftwareFolder);
        return issues;
    }

    protected virtual bool IsOutdatedSystem() => false;

    protected virtual bool IsInTempFolder(string executablePath) =>
        executablePath.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
}
