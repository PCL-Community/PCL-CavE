using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Platform;
using PCL.Platform;

namespace PCL.Core.Test.Platform;

[TestClass]
public sealed class RuntimeEnvironmentServiceTest
{
    private static readonly string _TempExe = Path.Combine(Path.GetTempPath(), "PCL", "PCL.Launcher.exe");

    [TestMethod]
    public void WindowsDetectsTempFolder()
    {
        var service = new WindowsRuntimeEnvironmentService();

        CollectionAssert.Contains(service.GetIssues(_TempExe).ToList(), RuntimeEnvironmentIssue.TempFolder);
        CollectionAssert.Contains(
            service.GetIssues(@"D:\Users\Steve\AppData\Local\Temp\Rar$EXa0.123\PCL.Launcher.exe").ToList(),
            RuntimeEnvironmentIssue.TempFolder);
    }

    [TestMethod]
    public void WindowsDetectsSocialSoftwareFolder()
    {
        var service = new WindowsRuntimeEnvironmentService();

        foreach (var path in new[]
                 {
                     @"C:\Users\Steve\Documents\WeChat Files\wxid_abc\FileStorage\File\PCL.Launcher.exe",
                     @"C:\Users\Steve\Documents\xwechat_files\wxid_abc\msg\file\PCL.Launcher.exe",
                     @"C:\Users\Steve\Documents\Tencent Files\12345\FileRecv\PCL.Launcher.exe"
                 })
            CollectionAssert.Contains(service.GetIssues(path).ToList(), RuntimeEnvironmentIssue.SocialSoftwareFolder);
    }

    [TestMethod]
    public void WindowsIgnoresNormalFolder()
    {
        var issues = new WindowsRuntimeEnvironmentService().GetIssues(@"D:\Games\PCL\PCL.Launcher.exe").ToList();

        CollectionAssert.DoesNotContain(issues, RuntimeEnvironmentIssue.TempFolder);
        CollectionAssert.DoesNotContain(issues, RuntimeEnvironmentIssue.SocialSoftwareFolder);
    }

    [TestMethod]
    public void LinuxIgnoresTempFolderWhenRunningFromAppImage()
    {
        var previous = Environment.GetEnvironmentVariable("APPIMAGE");
        try
        {
            var service = new LinuxRuntimeEnvironmentService();

            Environment.SetEnvironmentVariable("APPIMAGE", null);
            CollectionAssert.Contains(service.GetIssues(_TempExe).ToList(), RuntimeEnvironmentIssue.TempFolder);

            Environment.SetEnvironmentVariable("APPIMAGE", "/home/steve/PCL.AppImage");
            CollectionAssert.DoesNotContain(service.GetIssues(_TempExe).ToList(), RuntimeEnvironmentIssue.TempFolder);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPIMAGE", previous);
        }
    }

    [TestMethod]
    public void MacOsIgnoresTempFolderForTranslocatedApp()
    {
        var service = new MacOsRuntimeEnvironmentService();
        var translocated = Path.GetTempPath()
            + "x/AppTranslocation/0A1B2C3D/d/PCL CE.app/Contents/MacOS/PCL.Launcher";

        CollectionAssert.Contains(service.GetIssues(_TempExe).ToList(), RuntimeEnvironmentIssue.TempFolder);
        CollectionAssert.DoesNotContain(service.GetIssues(translocated).ToList(), RuntimeEnvironmentIssue.TempFolder);
    }
}
