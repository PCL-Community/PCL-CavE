using Avalonia.Threading;
using PCL.Core.Platform;

namespace PCL.Core.UI;

/// <summary>
///     使用 Avalonia 的嵌套消息循环，在同步等待期间继续处理原生输入。
/// </summary>
internal sealed class ModalFrame : IUiModalFrame
{
    private readonly DispatcherFrame _frame = new();

    public void Exit()
    {
        _frame.Continue = false;
    }

    public void Run()
    {
        Dispatcher.UIThread.PushFrame(_frame);
    }
}

/// <summary>界面不可用时的空模态消息泵。</summary>
internal sealed class NoopModalFrame : IUiModalFrame
{
    public void Exit()
    {
    }

    public void Run()
    {
    }
}
