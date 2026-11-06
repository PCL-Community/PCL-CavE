using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PCL.Core.Test.Minecraft;

[TestClass]
[TestCategory("Migration")]
public class McReinstallRequestTest
{
    private static McInstance CreateInstance()
    {
        var instance = new McInstance(Path.Combine(Path.GetTempPath(), "PCLTest", "reinstall"));
        instance.Info.VanillaName = "1.21.1";
        return instance;
    }

    [TestMethod]
    public void LabyModCannotBecomeVanillaOnReinstall()
    {
        var instance = CreateInstance();
        instance.Info.HasLabyMod = true;
        instance.Info.LabyMod = "4.2.59";

        Assert.Throws<NotSupportedException>(() => McInstallPipeline.CreateReinstallRequest(instance));
    }

    [TestMethod]
    public void QuiltIsRejectedFromBothReinstallEntrypoints()
    {
        var instance = CreateInstance();
        instance.Info.HasQuilt = true;

        Assert.Throws<NotSupportedException>(() => McInstallPipeline.CreateReinstallRequest(instance));
    }

    [TestMethod]
    public void SupportedLoaderAndTargetArePreserved()
    {
        var instance = CreateInstance();
        instance.Info.HasFabric = true;
        instance.Info.Fabric = "0.16.10";

        var request = McInstallPipeline.CreateReinstallRequest(instance);

        Assert.AreEqual(instance.Name, request.targetInstanceName);
        Assert.AreEqual(instance.PathInstance, request.targetInstanceFolder);
        Assert.AreEqual("1.21.1", request.minecraftName);
        Assert.AreEqual("0.16.10", request.fabricVersion);
        Assert.IsNull(request.legacyFabricVersion);
    }
}
