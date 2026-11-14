namespace PCL.Views.Pages;

/// <summary>页面生命周期约定：路由在进入/离开页面时调用。</summary>
public interface IPageView
{
    void OnNavigatedTo(object? parameter);

    void OnNavigatedFrom();
}