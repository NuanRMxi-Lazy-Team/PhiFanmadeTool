using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace PhiFanmade.Tool.Gui.ViewModels;

/// <summary>侧边栏导航项数据类。</summary>
public sealed partial class NavItem : ObservableObject
{
    public string Title { get; }
    public MaterialIconKind Icon { get; }
    public PageViewModelBase Page { get; }

    /// <summary>是否允许用户点击此项（谱面加载前，非起始页禁用）。</summary>
    [ObservableProperty] private bool _isEnabled;

    public NavItem(string title, MaterialIconKind icon, PageViewModelBase page, bool isEnabled = true)
    {
        Title = title;
        Icon = icon;
        Page = page;
        _isEnabled = isEnabled;
    }
}
