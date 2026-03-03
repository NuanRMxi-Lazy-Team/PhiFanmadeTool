using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Material.Icons;
using PhiFanmade.Tool.Gui.Messages;

namespace PhiFanmade.Tool.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<ChartLoadedMessage>
{
    [ObservableProperty] private NavItem? _selectedNavItem;
    [ObservableProperty] private PageViewModelBase? _currentPage;

    /// <summary>谱面是否已加载（控制受限导航项是否可用）。</summary>
    [ObservableProperty] private bool _chartLoaded;

    public ObservableCollection<NavItem> NavItems { get; } = [];

    /// <summary>需要等待谱面加载后才能使用的导航项。</summary>
    private readonly List<NavItem> _gatedNavItems = [];

    /// <summary>上一个合法的导航项（用于拒绝禁用项时回退）。</summary>
    private NavItem? _lastEnabledNavItem;

    public MainWindowViewModel()
    {
        var start        = new StartViewModel();
        var unbindFather = new RpeUnbindFatherViewModel();
        var layerMerge   = new RpeLayerMergeViewModel();

        var startNav   = new NavItem(Strings.gui_page_start,            MaterialIconKind.Star,    start,        isEnabled: true);
        var unbindNav  = new NavItem(Strings.gui_nav_rpe_unbind_father, MaterialIconKind.LinkOff, unbindFather, isEnabled: false);
        var layerNav   = new NavItem(Strings.gui_nav_rpe_layer_merge,   MaterialIconKind.Layers,  layerMerge,   isEnabled: false);

        NavItems.Add(startNav);
        NavItems.Add(unbindNav);
        NavItems.Add(layerNav);

        _gatedNavItems.Add(unbindNav);
        _gatedNavItems.Add(layerNav);

        SelectedNavItem = NavItems[0];
        _lastEnabledNavItem = NavItems[0];

        // 注册谱面加载消息，用于解锁受限导航项
        WeakReferenceMessenger.Default.Register<ChartLoadedMessage>(this);
    }

    /// <summary>收到谱面加载消息时，解锁所有受限导航项。</summary>
    public void Receive(ChartLoadedMessage message)
    {
        ChartLoaded = true;
        foreach (var item in _gatedNavItems)
            item.IsEnabled = true;
    }

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is { IsEnabled: false })
        {
            // 该项被锁定，异步回退到上一个可用项，避免无限循环
            Dispatcher.UIThread.Post(() => SelectedNavItem = _lastEnabledNavItem);
            return;
        }
        _lastEnabledNavItem = value;
        CurrentPage = value?.Page;
    }
}