using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhiFanmade.Tool.Gui.Messages;
using PhiFanmade.Tool.Gui.Services;
using PhiFanmade.Tool.Utils;

namespace PhiFanmade.Tool.Gui.ViewModels;

public partial class RpeUnbindFatherViewModel : PageViewModelBase, IRecipient<ChartLoadedMessage>
{
    public override string PageTitle => Strings.gui_nav_rpe_unbind_father;

    public RpeUnbindFatherViewModel()
    {
        WeakReferenceMessenger.Default.Register<ChartLoadedMessage>(this);
    }

    /// <summary>收到谱面加载消息时，自动填充工作区 ID。</summary>
    public void Receive(ChartLoadedMessage message)
    {
        WorkspaceId = message.WorkspaceId;
    }

    // ── 输入来源 ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string  _inputPath   = string.Empty;
    [ObservableProperty] private string  _workspaceId = string.Empty;

    // ── 输出 ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private string  _outputPath  = string.Empty;

    // ── 参数 ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _precision   = 64m;
    [ObservableProperty] private decimal _tolerance   = 5m;
    [ObservableProperty] private bool    _dryRun;
    [ObservableProperty] private bool    _streamOutput;

    // ── 状态 ──────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _status = "就绪";

    // ── 文件浏览 ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task BrowseInputAsync()
    {
        var path = await PickOpenFileAsync("选择 RPE 谱面文件");
        if (path is not null) InputPath = path;
    }

    [RelayCommand]
    private async Task BrowseOutputAsync()
    {
        var path = await PickSaveFileAsync("选择输出位置");
        if (path is not null) OutputPath = path;
    }

    // ── 主操作 ────────────────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputPath) && string.IsNullOrWhiteSpace(WorkspaceId))
        {
            return;
        }

        IsBusy = true;
        Status = "处理中…";

        try
        {
            Rpe.Chart chart;
            if (!string.IsNullOrWhiteSpace(WorkspaceId))
            {
                chart = await WorkspaceService.Instance.GetAsync(WorkspaceId)
                    ?? throw new InvalidOperationException($"工作区 '{WorkspaceId}' 不存在");
            }
            else
            {
                var text = await File.ReadAllTextAsync(InputPath, cancellationToken);
                chart = await Rpe.Chart.LoadFromJsonAsync(text);
            }

            var chartCopy    = chart.Clone();
            var processCount = 0;

            for (var i = 0; i < chart.JudgeLineList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (chart.JudgeLineList[i].Father == -1) continue;
                chartCopy.JudgeLineList[i] = await RePhiEditHelper.FatherUnbindAsync(
                    i, chart.JudgeLineList, (double)Precision, (double)Tolerance);
                processCount++;
            }

            if (!DryRun)
            {
                var output = string.IsNullOrWhiteSpace(OutputPath)
                    ? ResolveOutputPath(InputPath, WorkspaceId)
                    : OutputPath;

                if (StreamOutput)
                {
                    await using var stream = new FileStream(output, FileMode.Create);
                    await chartCopy.ExportToJsonStreamAsync(stream, true);
                }
                else
                {
                    await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(true), cancellationToken);
                }
            }
            else
            {
            }

            Status = "完成";
        }
        catch (OperationCanceledException)
        {
            Status = "已取消";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Status = "出错";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    // ── 内部工具 ──────────────────────────────────────────────────────────────
    private static string ResolveOutputPath(string inputPath, string workspaceId)
    {
        var src = string.IsNullOrWhiteSpace(inputPath) ? $"{workspaceId}_output" : inputPath;
        return Path.Combine(
            Path.GetDirectoryName(src) ?? ".",
            Path.GetFileNameWithoutExtension(src) + "_PFC.json");
    }

    internal static async Task<string?> PickOpenFileAsync(string title)
    {
        if (App.StorageProvider is not { } sp) return null;
        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title        = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON 谱面文件") { Patterns = ["*.json"] },
                new FilePickerFileType("所有文件")      { Patterns = ["*.*"]   }
            ]
        });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    internal static async Task<string?> PickSaveFileAsync(string title)
    {
        if (App.StorageProvider is not { } sp) return null;
        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title            = title,
            DefaultExtension = "json",
            FileTypeChoices  =
            [
                new FilePickerFileType("JSON 谱面文件") { Patterns = ["*.json"] }
            ]
        });
        return file?.Path.LocalPath;
    }
}

