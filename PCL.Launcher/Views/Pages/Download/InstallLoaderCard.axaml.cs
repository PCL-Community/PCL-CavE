using Avalonia;
using Avalonia.Controls;
using PCL.UI.Controls;

namespace PCL.Views.Pages.Download;

/// <summary>
///     下载安装页的加载器卡片：固定卡片几何与头部结构，卡片标题、状态图标、清空按钮和
///     版本行容器均由 XAML 提供，动态版本行仍由 <see cref="InstallLoaderSelector" /> 生成。
/// </summary>
public partial class InstallLoaderCard : UserControl
{
    private bool? _headerShown;
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<InstallLoaderCard, string>(nameof(Title), string.Empty);

    public InstallLoaderCard()
    {
        InitializeComponent();
        CardRoot.SwapControl = BodyHost;
        CardRoot.IsSwapped = true;
    }

    public MyCard Card => CardRoot;

    public StackPanel Body => BodyHost;

    public MyImage Logo => HeaderLogo;

    public TextBlock Info => StatusText;

    public MyIconButton ClearButton => BtnClear;

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty && CardRoot is not null)
            CardRoot.SetCurrentValue(
                MyCard.TitleProperty,
                change.NewValue as string ?? string.Empty);
    }

    public void SetLogo(string? source, double height, Thickness margin)
    {
        HeaderLogo.Height = height;
        HeaderLogo.Margin = margin;
        HeaderLogo.IsVisible = !string.IsNullOrEmpty(source);
        if (!string.IsNullOrEmpty(source))
            HeaderLogo.Source = source;
    }

    public void SetBodyMargin(Thickness margin)
    {
        BodyHost.Margin = margin;
    }

    /// <summary>
    ///     折叠时显示头部版本，展开时淡出。状态没变则不重复播放。
    /// </summary>
    public void SyncCollapsedHeader(bool shown)
    {
        if (_headerShown == shown)
            return;
        var animate = _headerShown is not null && ContainerAnimation.CanAnimate(this);
        _headerShown = shown;
        if (!animate)
        {
            HeaderGrid.Opacity = shown ? 1d : 0d;
            HeaderGrid.RenderTransform = null;
            return;
        }

        HeaderGrid.RenderTransform = new global::Avalonia.Media.TranslateTransform();
        AnimationService.Run(
            new List<IAnimation>
            {
                new OpacityAnimation(HeaderGrid, shown ? 1d : 0d, shown ? 60 : 60),
                new TranslateYAnimation(
                    HeaderGrid,
                    shown ? 0d : 6d,
                    shown ? 150 : 60,
                    ease: CubicEaseOut.Shared)
            },
            "InstallLoaderCard Header " + GetHashCode(),
            true);
    }
}