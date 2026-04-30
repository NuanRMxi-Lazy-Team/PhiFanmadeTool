using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.KaedePhi;
using KaedePhi.Tool.Converter.PhiEdit;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.RePhiEdit;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.Cli.Infrastructure;

/// <summary>
/// 谱面加载、格式检测与导出服务。
/// </summary>
public sealed class ChartService
{
    private readonly WorkspaceService _workspace = new();

    /// <summary>从文件路径或工作区加载原始文本。</summary>
    public async Task<string> LoadChartTextAsync(string? input, string? workspace, CancellationToken ct = default)
    {
        string path;
        if (!string.IsNullOrWhiteSpace(workspace))
        {
            path = _workspace.GetChartPath(workspace)
                   ?? throw new InvalidOperationException(
                       string.Format(Strings.cli_err_workspace_missing, workspace));
        }
        else
        {
            path = input ?? throw new InvalidOperationException(Strings.cli_err_input_required);
        }

        return await File.ReadAllTextAsync(path, ct);
    }

    /// <summary>将输入谱面统一转换为中间类型。</summary>
    public async Task<Chart?> LoadKpcAsync(string? input, string? workspace, CancellationToken ct = default)
    {
        var text = await LoadChartTextAsync(input, workspace, ct);
        var chartType = ChartGetType.GetType(text);

        switch (chartType)
        {
            case ChartType.RePhiEdit:
            {
                var rePhiEditConverter = new RePhiEditConverter();
                var kaedePhiConverter = new KaedePhiConverter();
                var rePhiEditChart = await Core.RePhiEdit.Chart.LoadFromJsonAsync(text);

                return ChartPipeline
                    .From(rePhiEditChart, rePhiEditConverter, null)
                    .To(kaedePhiConverter, null);
            }

            case ChartType.PhiEdit:
            {
                var phiEditConverter = new PhiEditConverter();
                var kaedePhiConverter = new KaedePhiConverter();
                var phiEditChart = await Core.PhiEdit.Chart.LoadAsync(text);
                return ChartPipeline
                    .From(phiEditChart, phiEditConverter, null)
                    .To(kaedePhiConverter, null);
                //return new PhiEditConverter().ToKpc(await global::KaedePhi.Core.PhiEdit.Chart.LoadAsync(text));
            }

        
            default:
                return null;
        }
    }

    /// <summary>根据输入路径或工作区自动计算输出路径。</summary>
    public string ResolveOutputPath(string? input, string? output, string? workspace)
    {
        if (!string.IsNullOrWhiteSpace(output)) return output;
        if (string.IsNullOrWhiteSpace(workspace))
            return Path.Combine(
                Path.GetDirectoryName(input!) ?? ".",
                Path.GetFileNameWithoutExtension(input!) + "_PFC.json");
        return Path.Combine(_workspace.Root, workspace, "chart.json");
    }

    /// <summary>将 KPC 谱面导出为 RPE 格式并写入。</summary>
    public async Task<string> SaveAsRpeAsync(Chart chart, string outputPath, bool dryRun,
        CancellationToken ct = default)
    {
        var rpeChart = new RePhiEditConverter().FromKpc(chart, new ConvertOption());
        if (dryRun) return outputPath;
        var json = await rpeChart.ExportToJsonAsync(false);
        await File.WriteAllTextAsync(outputPath, json, ct);
        return outputPath;
    }

    /// <summary>将 KPC 谱面导出为目标格式并写入。</summary>
    public async Task<string?> SaveAsAsync(Chart chart, string outputPath, ChartType target,
        bool stream, bool format, bool dryRun, CancellationToken ct = default)
    {
        switch (target)
        {
            case ChartType.RePhiEdit:
            {
                var rpeChart = new RePhiEditConverter().FromKpc(chart, new ConvertOption());
                if (dryRun) return outputPath;
                if (stream)
                {
                    await using var s = new FileStream(outputPath, FileMode.Create);
                    await rpeChart.ExportToJsonStreamAsync(s, format);
                }
                else
                {
                    await File.WriteAllTextAsync(outputPath, await rpeChart.ExportToJsonAsync(format), ct);
                }

                return outputPath;
            }
            case ChartType.PhiEdit:
            {
                var peChart = new PhiEditConverter().FromKpc(chart, new PhiEditConvertOptions());
                if (dryRun) return outputPath;
                if (stream)
                {
                    await using var s = new FileStream(outputPath, FileMode.Create);
                    await peChart.ExportToStreamAsync(s);
                }
                else
                {
                    await File.WriteAllTextAsync(outputPath, await peChart.ExportAsync(), ct);
                }

                return outputPath;
            }
            default:
                return null;
        }
    }
}