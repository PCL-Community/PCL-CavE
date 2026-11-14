using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.UI.Animation;

namespace PCL.Core.Test.Animation;

[TestClass]
public sealed class NColorTest
{
    [TestMethod]
    public void FromHsl2HandlesHueWrapping()
    {
        for (var hue = 0d; hue < 360d; hue += 15d)
        {
            var color = NColor.FromHsl2(hue, 90d, 55d);
            Assert.IsGreaterThan(0d, color.A);
            Assert.IsTrue(color.R >= 0d && color.R <= 255d);
            Assert.IsTrue(color.G >= 0d && color.G <= 255d);
            Assert.IsTrue(color.B >= 0d && color.B <= 255d);
        }
    }

    [TestMethod]
    public void AlphaReturnsIndependentCopy()
    {
        var color = new NColor(10d, 20d, 30d, 40d);
        var transparent = color.Alpha(12d);

        Assert.AreEqual(12d, transparent.A, 1e-6d);
        Assert.AreEqual(40d, color.A, 1e-6d);
    }

    [TestMethod]
    public void ColorConversionClampsChannels()
    {
        var color = new NColor(300d, -10d, 128d, 999d);
        var converted = color.ToColor();

        Assert.AreEqual(255, converted.R);
        Assert.AreEqual(0, converted.G);
        Assert.AreEqual(128, converted.B);
        Assert.AreEqual(255, converted.A);
    }
}