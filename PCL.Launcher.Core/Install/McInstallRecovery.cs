using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;

namespace PCL;

/// <summary>
///     安装会话的目录状态记录与失败恢复。
///     安装开始前记录实例目录中哪些路径是本次安装会创建或覆盖的，失败或取消后只回滚这些路径；
///     安装前已存在的实例核心文件会先备份到 <c>PCLInstallBackups</c>，失败时还原、成功时清理。
/// </summary>
public static class McInstallRecovery
{
    public const string BackupFolderName = "PCLInstallBackups";

    private static readonly Lock SyncRoot = new();

    private static readonly Dictionary<string, InstallSession> Sessions =
        new(StringComparer.OrdinalIgnoreCase);

    public static string NormalizeFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return "";
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, "规范化实例目录失败");
            return folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    public static string GetJsonPath(string instanceFolder, string instanceName)
    {
        return Path.Combine(instanceFolder, $"{instanceName}.json");
    }

    public static string GetJarPath(string instanceFolder, string instanceName)
    {
        return Path.Combine(instanceFolder, $"{instanceName}.jar");
    }

    /// <summary>安装开始前调用：记录目录与实例文件的原始状态，供失败清理判断回滚范围。</summary>
    public static void RegisterSession(string instanceFolder, string instanceName)
    {
        var folder = NormalizeFolder(instanceFolder);
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(instanceName))
            return;

        var session = new InstallSession
        {
            InstanceName = instanceName,
            FolderExisted = Directory.Exists(folder),
            JsonExisted = File.Exists(GetJsonPath(folder, instanceName)),
            JarExisted = File.Exists(GetJarPath(folder, instanceName))
        };
        lock (SyncRoot)
            if (!Sessions.TryAdd(folder, session))
                throw new InvalidOperationException($"实例正在安装：{folder}");

        LauncherCommon.Log(
            $"[Install] 记录安装会话：{folder}（目录已存在：{session.FolderExisted}，Json 已存在：{session.JsonExisted}）",
            LauncherLogLevel.Developer);
    }

    /// <summary>安装开始前调用：把已存在的实例核心文件备份到 <c>PCLInstallBackups</c>。</summary>
    public static void PrepareBackup(string instanceFolder, string instanceName)
    {
        var folder = NormalizeFolder(instanceFolder);
        var jsonPath = GetJsonPath(folder, instanceName);
        var jarPath = GetJarPath(folder, instanceName);
        var backupFolder = Path.Combine(folder, BackupFolderName);
        if (Directory.Exists(backupFolder) && Directory.EnumerateFileSystemEntries(backupFolder).Any())
            throw new IOException($"实例仍有安装备份，请先恢复或移走：{backupFolder}");
        if (!File.Exists(jsonPath) && !File.Exists(jarPath))
            return;
        Directory.CreateDirectory(backupFolder);
        if (File.Exists(jsonPath))
            File.Copy(jsonPath, Path.Combine(backupFolder, Path.GetFileName(jsonPath)), true);
        if (File.Exists(jarPath))
            File.Copy(jarPath, Path.Combine(backupFolder, Path.GetFileName(jarPath)), true);
        lock (SyncRoot)
            Sessions[folder].BackupPrepared = true;
        LauncherCommon.Log($"[Install] 已备份实例核心文件：{backupFolder}");
    }

    public static void UnregisterSession(string instanceFolder)
    {
        var folder = NormalizeFolder(instanceFolder);
        lock (SyncRoot)
        {
            Sessions.Remove(folder);
        }
    }

    /// <summary>
    ///     安装加载器进入结束状态后调用：成功时删除备份，失败或取消时回滚到安装前状态。
    /// </summary>
    public static void HandleTerminalState(LoadState state, string instanceFolder)
    {
        if (state == LoadState.Finished)
        {
            var folder = NormalizeFolder(instanceFolder);
            InstallSession? session;
            lock (SyncRoot)
                Sessions.TryGetValue(folder, out session);
            if (session is null) return;
            if (session.BackupPrepared) DeleteBackupQuietly(folder);
            UnregisterSession(folder);
            return;
        }

        if (state is not (LoadState.Failed or LoadState.Aborted))
            return;

        RestoreOrCleanup(instanceFolder);
    }

    public static bool CanInstallToFolder(string instanceFolder, string instanceName)
    {
        var folder = NormalizeFolder(instanceFolder);
        return !File.Exists(GetJsonPath(folder, instanceName))
            && !Directory.Exists(Path.Combine(folder, BackupFolderName));
    }

    private static void RestoreOrCleanup(string instanceFolder)
    {
        var folder = NormalizeFolder(instanceFolder);
        if (string.IsNullOrEmpty(folder))
            return;

        InstallSession? session;
        lock (SyncRoot)
            Sessions.TryGetValue(folder, out session);
        // 没有会话就无法证明路径归本次安装所有。
        if (session is null) return;

        var instanceName = session.InstanceName;
        var backupFolder = Path.Combine(folder, BackupFolderName);
        if (session.BackupPrepared)
            try
            {
                if (session.JsonExisted)
                    File.Copy(Path.Combine(backupFolder, $"{instanceName}.json"), GetJsonPath(folder, instanceName), true);
                if (session.JarExisted)
                    File.Copy(Path.Combine(backupFolder, $"{instanceName}.jar"), GetJarPath(folder, instanceName), true);
                LauncherCommon.Log($"[Install] 已从备份还原实例目录：{folder}");
            }
            catch (Exception ex)
            {
                LauncherCommon.Log(ex, "还原实例备份失败");
                return;
            }
        LauncherCommon.Log(
            $"[Install] 由于安装失败或取消，清理本次安装创建的路径：{folder}",
            LauncherLogLevel.Developer);

        DeleteFileQuietly(Path.Combine(folder, ".pclignore"));
        if (!session.JsonExisted) DeleteFileQuietly(GetJsonPath(folder, instanceName));
        if (!session.JarExisted) DeleteFileQuietly(GetJarPath(folder, instanceName));
        if (session.BackupPrepared) DeleteBackupQuietly(folder);

        if (!session.FolderExisted)
            DeleteFolderQuietly(folder);

        InvalidateInstanceCache(folder);
        UnregisterSession(folder);
    }

    private static void DeleteBackupQuietly(string folder)
    {
        var backupFolder = Path.Combine(folder, BackupFolderName);
        if (!Directory.Exists(backupFolder))
            return;
        try
        {
            LauncherCommon.DeleteDirectory(backupFolder);
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, "清理安装备份目录失败");
        }
    }

    private static void InvalidateInstanceCache(string folder)
    {
        try
        {
            if (ConfigService.GetProvider(ConfigSource.GameInstance)
                is DynamicCacheConfigStorage storage)
                storage.InvalidateCache(folder);
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, "刷新实例配置缓存失败");
        }
    }

    private static void DeleteFileQuietly(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, $"删除安装残留文件失败：{path}");
        }
    }

    private static void DeleteFolderQuietly(string folder)
    {
        try
        {
            if (!Directory.Exists(folder))
                return;
            LauncherCommon.DeleteDirectory(folder);
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, $"删除安装残留目录失败：{folder}");
        }
    }

    private sealed class InstallSession
    {
        public bool BackupPrepared;

        public bool FolderExisted;

        public required string InstanceName;

        /// <summary>安装开始前实例核心 Jar 是否已存在（需要备份）。</summary>
        public bool JarExisted;

        /// <summary>安装开始前实例 Json 是否已存在（需要备份）。</summary>
        public bool JsonExisted;
    }
}
