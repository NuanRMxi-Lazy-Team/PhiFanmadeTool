﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PhiFanmade.Tool.Gui.Messages;
using PhiFanmade.Tool.Gui.Services;
using PhiFanmade.Tool.Utils;

namespace PhiFanmade.Tool.Gui.ViewModels;

public partial class RpeLayerMergeViewModel : PageViewModelBase, IRecipient<ChartLoadedMessage>
{
    public override string PageTitle => Strings.gui_nav_rpe_layer_merge;

    public RpeLayerMergeViewModel()
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

    // ── 状态 ──────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _status = "就绪";

    // ── 文件浏览 ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task BrowseInputAsync()
    {
        var path = await RpeUnbindFatherViewModel.PickOpenFileAsync("选择 RPE 谱面文件");
        if (path is not null) InputPath = path;
    }

    [RelayCommand]
    private async Task BrowseOutputAsync()
    {
        var path = await RpeUnbindFatherViewModel.PickSaveFileAsync("选择输出位置");
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

            var chartCopy  = chart.Clone();
            var mergeCount = 0;

            await Task.Run(() =>
            {
                foreach (var jl in chartCopy.JudgeLineList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (jl.EventLayers is not { Count: > 1 }) continue;
                    jl.EventLayers =
                    [
                        RePhiEditHelper.LayerMerge(jl.EventLayers, (double)Precision, (double)Tolerance)
                    ];
                    mergeCount++;
                }
            }, cancellationToken);

            if (!DryRun)
            {
                var output = string.IsNullOrWhiteSpace(OutputPath)
                    ? ResolveOutputPath(InputPath, WorkspaceId)
                    : OutputPath;
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(true), cancellationToken);
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
            Status = "出错";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    private static string ResolveOutputPath(string inputPath, string workspaceId)
    {
        var src = string.IsNullOrWhiteSpace(inputPath) ? $"{workspaceId}_output" : inputPath;
        return Path.Combine(
            Path.GetDirectoryName(src) ?? ".",
            Path.GetFileNameWithoutExtension(src) + "_PFC.json");
    }
}

