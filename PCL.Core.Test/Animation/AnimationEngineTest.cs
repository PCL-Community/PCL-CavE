using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.UI.Animation;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.Test.Animation;

[TestClass]
public sealed class AnimationEngineTest
{
    [TestInitialize]
    public void Initialize()
    {
        AnimationService.RegisterValueProcessorsForTest();
        AnimationService.Speed = 1d;
    }

    [TestCleanup]
    public void Cleanup()
    {
        AnimationService.ResetSpeed();
    }

    [TestMethod]
    public void AbsoluteAnimationEndsAtTarget()
    {
        var owner = new NumberOwner
        {
            Value = 4d
        };
        var target = CreateTarget(owner);
        var animation = new DoubleFromToAnimation
        {
            From = 0d,
            HasFrom = true,
            To = 10d,
            Duration = TimeSpan.FromSeconds(1),
            Easing = new LinearEasing()
        };

        RunFrames(animation, target);

        Assert.AreEqual(10d, owner.Value, 1e-6d);
    }

    [TestMethod]
    public void RelativeAnimationAddsDeltaToCurrentValue()
    {
        var owner = new NumberOwner
        {
            Value = 5d
        };
        var target = CreateTarget(owner);
        var animation = new DoubleFromToAnimation
        {
            To = 7d,
            ValueType = AnimationValueType.Relative,
            Duration = TimeSpan.FromSeconds(1),
            Easing = new LinearEasing()
        };

        RunFrames(animation, target);

        Assert.AreEqual(12d, owner.Value, 1e-6d);
    }

    [TestMethod]
    public void ExplicitFromIsNotOverwritten()
    {
        var owner = new NumberOwner
        {
            Value = 5d
        };
        var target = CreateTarget(owner);
        var animation = new DoubleFromToAnimation
        {
            From = 0d,
            HasFrom = true,
            To = 10d,
            Duration = TimeSpan.FromSeconds(1),
            Easing = new LinearEasing()
        };

        animation.PrepareForTest(target);
        animation.CurrentFrame = 0;
        var frame = animation.ComputeNextFrame(target);

        Assert.IsNotNull(frame);
        frame.GetAction()();
        Assert.AreEqual(0d, owner.Value, 1e-6d);
    }

    [TestMethod]
    public void SpeedZeroAppliesFinalValueImmediately()
    {
        AnimationService.Speed = 0d;
        var owner = new NumberOwner
        {
            Value = 5d
        };
        var target = CreateTarget(owner);
        var animation = new DoubleFromToAnimation
        {
            To = 7d,
            ValueType = AnimationValueType.Relative,
            Duration = TimeSpan.FromSeconds(1),
            Easing = new LinearEasing()
        };

        var result = animation.RunFireAndForget(target);

        Assert.AreEqual(12d, owner.Value, 1e-6d);
        Assert.AreEqual(AnimationStatus.Completed, result.Status);
    }

    [TestMethod]
    public void SpeedMappingMatchesLegacyCurve()
    {
        Assert.AreEqual(0d, AnimationService.MapSpeedValue(0));
        Assert.AreEqual(1d, AnimationService.MapSpeedValue(9), 1e-6d);
        Assert.AreEqual(3d, AnimationService.MapSpeedValue(29), 1e-6d);
        Assert.AreEqual(200d, AnimationService.MapSpeedValue(30), 1e-6d);
    }

    private static ClrAnimatable<NumberOwner, double> CreateTarget(NumberOwner owner)
    {
        return new ClrAnimatable<NumberOwner, double>(
            owner,
            static value => value.Value,
            static (value, next) => value.Value = next);
    }

    private static void RunFrames(
        FromToAnimationBase<double> animation,
        IAnimatable target)
    {
        animation.PrepareForTest(target);
        for (var frameIndex = 0; frameIndex < animation.TotalFrames; frameIndex++)
        {
            animation.CurrentFrame = frameIndex;
            var frame = animation.ComputeNextFrame(target);
            Assert.IsNotNull(frame);
            frame.GetAction()();
        }
    }

    [TestMethod]
    public void SuspensionScopeIsReentrant()
    {
        Assert.IsFalse(AnimationService.IsSuspended);
        using (AnimationService.SuspendControl())
        {
            Assert.IsTrue(AnimationService.IsSuspended);
            using (AnimationService.SuspendControl())
            {
                Assert.IsTrue(AnimationService.IsSuspended);
            }

            Assert.IsTrue(AnimationService.IsSuspended);
        }

        Assert.IsFalse(AnimationService.IsSuspended);
    }

    [TestMethod]
    public void ActionAnimationRunsImmediatelyWhenDisabled()
    {
        AnimationService.Speed = 0d;
        var called = 0;
        var animation = new ActionAnimation(() => called++);

        var result = animation.RunFireAndForget(EmptyAnimatable.Instance);

        Assert.AreEqual(1, called);
        Assert.AreEqual(AnimationStatus.Completed, result.Status);
    }

    [TestMethod]
    public void LegacyFluentEasingKeepsEndpointAndPowerCurve()
    {
        var ease = new FluentEaseOut(EasePower.Weak);

        Assert.AreEqual(0d, ease.Ease(0d), 1e-6d);
        Assert.AreEqual(1d, ease.Ease(1d), 1e-6d);
        Assert.AreEqual(0.75d, ease.Ease(0.5d), 1e-6d);
    }

    [TestMethod]
    public void AnimationGroupPreservesAfterOrderingWhenDisabled()
    {
        AnimationService.Speed = 0d;
        var calls = new List<int>();

        AnimationService.Run(
            new List<IAnimation>
            {
                new ActionAnimation(() => calls.Add(1)),
                new ActionAnimation(() => calls.Add(2)),
                new ActionAnimation(() => calls.Add(3), true)
            },
            "test-order");

        Assert.AreSequenceEqual(
            new[]
            {
                1,
                2,
                3
            },
            calls);
    }

    private sealed class NumberOwner
    {
        public double Value { get; set; }
    }
}