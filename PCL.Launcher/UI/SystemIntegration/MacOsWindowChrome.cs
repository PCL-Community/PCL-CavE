using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Threading;

namespace PCL.UI.SystemIntegration;

internal static partial class MacOsWindowChrome
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    public static void Attach(Window window, Control header)
    {
        void Update(object? sender, EventArgs args)
        {
            Dispatcher.UIThread.Post(
                () => CenterTrafficLights(window, header.Bounds.Height),
                DispatcherPriority.Loaded);
        }

        window.Opened += Update;
        window.SizeChanged += Update;
        window.Activated += Update;
        header.SizeChanged += Update;
    }

    private static void CenterTrafficLights(Window window, double headerHeight)
    {
        if (!window.IsVisible || headerHeight <= 0d
            || window.TryGetPlatformHandle() is not { Handle: not 0, HandleDescriptor: "NSWindow" } handle)
            return;

        var closeButton = Send(handle.Handle, Selector("standardWindowButton:"), 0);
        if (closeButton == 0) return;
        var titleBar = Send(closeButton, Selector("superview"));
        var container = Send(titleBar, Selector("superview"));
        if (titleBar == 0 || container == 0) return;

        // Avalonia 的高度提示只调整材质区域，原生按钮仍需按 header 高度居中。
        var containerFrame = GetFrame(container);
        containerFrame.Y = GetFrame(handle.Handle).Height - headerHeight;
        containerFrame.Height = headerHeight;
        SetFrame(container, Selector("setFrame:"), containerFrame);

        var titleFrame = GetFrame(titleBar);
        titleFrame.Y = 0d;
        titleFrame.Height = headerHeight;
        SetFrame(titleBar, Selector("setFrame:"), titleFrame);

        for (nint kind = 0; kind < 3; kind++)
        {
            var button = Send(handle.Handle, Selector("standardWindowButton:"), kind);
            if (button == 0) continue;
            var frame = GetFrame(button);
            frame.Y = (headerHeight - frame.Height) / 2d;
            SetFrame(button, Selector("setFrame:"), frame);
        }
    }

    private static NativeRect GetFrame(nint view)
    {
        // Intel 的 NSRect 返回值使用 stret 入口，Apple Silicon 使用 objc_msgSend。
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            GetFrameX64(out var frame, view, Selector("frame"));
            return frame;
        }

        return GetFrameArm64(view, Selector("frame"));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    [LibraryImport(ObjCLibrary, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Selector(string name);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector, nint argument);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial NativeRect GetFrameArm64(nint receiver, nint selector);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend_stret")]
    private static partial void GetFrameX64(out NativeRect frame, nint receiver, nint selector);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial void SetFrame(nint receiver, nint selector, NativeRect frame);
}
