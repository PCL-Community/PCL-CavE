using Avalonia.Threading;

namespace PCL.Core.UI;

/// <summary>
///     把 Avalonia 的异步 API 适配成 <see cref="PCL.Core.Platform" /> 中的同步契约。
///     UI 线程上使用嵌套消息循环，保持原生输入与异步回调可用。
/// </summary>
internal static class SyncHelper
{
    /// <summary>同步等待任务完成并返回结果。</summary>
    public static T? Wait<T>(Task<T> task)
    {
        WaitCore(task);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>同步等待任务完成。</summary>
    public static void Wait(Task task)
    {
        WaitCore(task);
        task.GetAwaiter().GetResult();
    }

    private static void WaitCore(Task task)
    {
        if (task.IsCompleted) return;
        if (!Dispatcher.UIThread.CheckAccess())
        {
            task.GetAwaiter().GetResult();
            return;
        }

        var frame = new ModalFrame();
        _ = task.ContinueWith(
            _ => frame.Exit(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        frame.Run();
    }
}
