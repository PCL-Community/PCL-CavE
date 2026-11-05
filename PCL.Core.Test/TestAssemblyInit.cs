using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PCL.Core.Test;

[TestClass]
public class TestAssemblyInit
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
#if DEBUG
        // 测试宿主的可执行目录与真实用户配置都不属于测试数据。
        var testRoot = Path.Combine(Path.GetTempPath(), "PCLTest", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("PCL_PATH", Path.Combine(testRoot, "data"));
        Environment.SetEnvironmentVariable("PCL_PATH_SHARED", Path.Combine(testRoot, "shared"));
        Environment.SetEnvironmentVariable("PCL_PATH_LOCAL", Path.Combine(testRoot, "local"));
        Environment.SetEnvironmentVariable("PCL_PATH_TEMP", Path.Combine(testRoot, "temp"));
#endif
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
