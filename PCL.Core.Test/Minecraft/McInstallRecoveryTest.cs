using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PCL.Core.Test.Minecraft;

[TestClass]
[TestCategory("Migration")]
public class McInstallRecoveryTest
{
    private string _folder = null!;
    private string Json => Path.Combine(_folder, "test.json");
    private string Jar => Path.Combine(_folder, "test.jar");
    private string Backup => Path.Combine(_folder, McInstallRecovery.BackupFolderName);

    [TestInitialize]
    public void Initialize()
    {
        _folder = Path.Combine(Path.GetTempPath(), "PCLTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        McInstallRecovery.UnregisterSession(_folder);
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private void Finish(LoadState state = LoadState.Failed)
    {
        McInstallRecovery.HandleTerminalState(state, _folder);
    }

    [TestMethod]
    public void BackupFailureThrowsAndRollbackKeepsOriginalFiles()
    {
        File.WriteAllText(Json, "original json");
        File.WriteAllText(Jar, "original jar");
        File.WriteAllText(Backup, "blocks backup directory creation");
        McInstallRecovery.RegisterSession(_folder, "test");

        Assert.Throws<IOException>(() => McInstallRecovery.PrepareBackup(_folder, "test"));
        Finish();

        Assert.AreEqual("original json", File.ReadAllText(Json));
        Assert.AreEqual("original jar", File.ReadAllText(Jar));
    }

    [TestMethod]
    [DataRow(LoadState.Failed, true, false)]
    [DataRow(LoadState.Aborted, true, false)]
    [DataRow(LoadState.Failed, false, true)]
    [DataRow(LoadState.Failed, true, true)]
    public void RollbackRestoresExistingFilesAndDeletesNewFiles(LoadState state, bool jsonExisted, bool jarExisted)
    {
        if (jsonExisted) File.WriteAllText(Json, "original json");
        if (jarExisted) File.WriteAllText(Jar, "original jar");
        File.WriteAllText(Path.Combine(_folder, "save.dat"), "user data");
        McInstallRecovery.RegisterSession(_folder, "test");
        McInstallRecovery.PrepareBackup(_folder, "test");
        File.WriteAllText(Json, "replacement json");
        File.WriteAllText(Jar, "new jar");

        Finish(state);

        if (jsonExisted) Assert.AreEqual("original json", File.ReadAllText(Json));
        else Assert.IsFalse(File.Exists(Json));
        if (jarExisted) Assert.AreEqual("original jar", File.ReadAllText(Jar));
        else Assert.IsFalse(File.Exists(Jar));
        Assert.AreEqual("user data", File.ReadAllText(Path.Combine(_folder, "save.dat")));
        Assert.IsFalse(Directory.Exists(Backup));
        McInstallRecovery.RegisterSession(_folder, "test");
    }

    [TestMethod]
    public void FailedRestorePreservesFilesAndRecoveryBackup()
    {
        File.WriteAllText(Json, "original json");
        File.WriteAllText(Jar, "original jar");
        McInstallRecovery.RegisterSession(_folder, "test");
        McInstallRecovery.PrepareBackup(_folder, "test");
        File.WriteAllText(Json, "replacement json");
        File.WriteAllText(Jar, "replacement jar");
        File.Delete(Path.Combine(Backup, "test.jar"));

        Finish();

        Assert.IsTrue(File.Exists(Json));
        Assert.AreEqual("replacement jar", File.ReadAllText(Jar));
        Assert.AreEqual("original json", File.ReadAllText(Path.Combine(Backup, "test.json")));
    }

    [TestMethod]
    public void MissingSessionDoesNotDeleteFilesOrBackups()
    {
        File.WriteAllText(Json, "user json");
        Directory.CreateDirectory(Backup);
        File.WriteAllText(Path.Combine(Backup, "test.json"), "recoverable json");

        Finish();
        Finish(LoadState.Finished);

        Assert.AreEqual("user json", File.ReadAllText(Json));
        Assert.IsTrue(File.Exists(Path.Combine(Backup, "test.json")));
    }

    [TestMethod]
    public void InstallCheckAndNewBackupPreservePendingRecovery()
    {
        Directory.CreateDirectory(Backup);
        File.WriteAllText(Path.Combine(Backup, "test.json"), "recoverable json");
        Assert.IsFalse(McInstallRecovery.CanInstallToFolder(_folder, "test"));
        McInstallRecovery.RegisterSession(_folder, "test");
        Assert.Throws<IOException>(() => McInstallRecovery.PrepareBackup(_folder, "test"));
        File.WriteAllText(Json, "current json");

        Assert.Throws<IOException>(() => McInstallRecovery.PrepareBackup(_folder, "test"));
        Assert.AreEqual("recoverable json", File.ReadAllText(Path.Combine(Backup, "test.json")));
    }

    [TestMethod]
    public void InstallCheckPreservesExistingJarWithoutJson()
    {
        File.WriteAllText(Jar, "original jar");

        Assert.IsTrue(McInstallRecovery.CanInstallToFolder(_folder, "test"));
        Assert.AreEqual("original jar", File.ReadAllText(Jar));
    }

    [TestMethod]
    public void NewInstanceRollbackRemovesOnlyItsOwnDirectory()
    {
        Directory.Delete(_folder);
        McInstallRecovery.RegisterSession(_folder, "test");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Json, "new json");

        Finish();

        Assert.IsFalse(Directory.Exists(_folder));
    }

    [TestMethod]
    public void ExistingEmptyDirectoryIsPreserved()
    {
        McInstallRecovery.RegisterSession(_folder, "test");
        File.WriteAllText(Json, "new json");

        Finish();

        Assert.IsTrue(Directory.Exists(_folder));
        Assert.IsFalse(File.Exists(Json));
    }

    [TestMethod]
    public void SuccessfulInstallKeepsNewFilesAndReleasesSession()
    {
        File.WriteAllText(Json, "original json");
        McInstallRecovery.RegisterSession(_folder, "test");
        McInstallRecovery.PrepareBackup(_folder, "test");
        File.WriteAllText(Json, "installed json");

        Finish(LoadState.Finished);

        Assert.AreEqual("installed json", File.ReadAllText(Json));
        Assert.IsFalse(Directory.Exists(Backup));
        McInstallRecovery.RegisterSession(_folder, "test");
    }

    [TestMethod]
    public void ConcurrentInstallCannotReplaceExistingSession()
    {
        McInstallRecovery.RegisterSession(_folder, "test");
        Assert.Throws<InvalidOperationException>(() => McInstallRecovery.RegisterSession(_folder, "test"));
    }
}
