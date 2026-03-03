using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhiFanmade.Tool.Gui.Messages;
using PhiFanmade.Tool.Gui.Services;

namespace PhiFanmade.Tool.Gui.ViewModels;

public partial class StartViewModel : PageViewModelBase
{
    public override string PageTitle => Strings.gui_page_start;

    // ── 文件路径 ──────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadChartCommand))]
    private string _chartPath = string.Empty;

    // ── 状态 ──────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadChartCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _status = Strings.gui_status_ready;
    [ObservableProperty] private bool   _isChartLoaded;

    // ── 文件浏览 ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task BrowseAsync()
    {
        var path = await RpeUnbindFatherViewModel.PickOpenFileAsync(Strings.gui_watermark_select_json);
        if (path is not null) ChartPath = path;
    }

    // ── 载入谱面 ──────────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadChartAsync()
    {
        if (string.IsNullOrWhiteSpace(ChartPath)) return;

        IsBusy = true;
        Status = Strings.ResourceManager.GetString("gui_status_loading", Strings.Culture) ?? "正在加载…";
        try
        {
            await WorkspaceService.Instance.LoadAsync("main", ChartPath);
            var chart = await WorkspaceService.Instance.GetAsync("main");

            IsChartLoaded = true;
            Status = Strings.ResourceManager.GetString("gui_status_load_success", Strings.Culture) ?? "谱面加载成功！";

            // 广播谱面加载完成消息，通知其他页面和主窗口
            WeakReferenceMessenger.Default.Send(new ChartLoadedMessage(chart!, "main", ChartPath));
        }
        catch (Exception ex)
        {
            Status = string.Format(
                Strings.ResourceManager.GetString("gui_status_load_error", Strings.Culture) ?? "加载失败：{0}",
                ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLoad() => !IsBusy && !string.IsNullOrWhiteSpace(ChartPath);
}