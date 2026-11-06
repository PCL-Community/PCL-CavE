using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.UI;

namespace PCL.Core.Test;

[TestClass]
[TestCategory("Migration")]
[DoNotParallelize]
public class ModalFrameTest
{
    [TestMethod]
    [Timeout(10000)]
    public void NestedLoopProcessesCallbacksAndPreservesTaskResults()
    {
        var dispatcher = Dispatcher.UIThread;
        var frame = new ModalFrame();
        dispatcher.Post(frame.Exit);
        frame.Run();

        var exitedFrame = new ModalFrame();
        exitedFrame.Exit();
        exitedFrame.Run();

        var result = new TaskCompletionSource<int>();
        dispatcher.Post(() => result.SetResult(42));
        Assert.AreEqual(42, SyncHelper.Wait(result.Task));

        var failure = new TaskCompletionSource();
        dispatcher.Post(() => failure.SetException(new InvalidOperationException("test")));
        Assert.Throws<InvalidOperationException>(() => SyncHelper.Wait(failure.Task));

        using var cancellation = new CancellationTokenSource();
        var cancelled = new TaskCompletionSource();
        dispatcher.Post(() => cancelled.SetCanceled(cancellation.Token));
        Assert.Throws<TaskCanceledException>(() => SyncHelper.Wait(cancelled.Task));
    }
}
