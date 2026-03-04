using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.RePhiEdit;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

// ─── 共享 Settings ────────────────────────────────────────────────────────────

/// <summary>RPE 操作的通用参数，供 unbind-father 和 layer-merge 复用。</summary>
public abstract class RpeOperationSettings : BaseSettings
{
    [CommandOption("-i|--input <PATH>")]
    [LocalizedDescription("cli_opt_input_rpe_desc")]
    public string? Input { get; set; }

    [CommandOption("-o|--output <PATH>")]
    [LocalizedDescription("cli_opt_output_auto_desc")]
    public string? Output { get; set; }

    [CommandOption("-w|--workspace <ID>")]
    [LocalizedDescription("cli_opt_workspace_rpe_desc")]
    public string? Workspace { get; set; }

    [CommandOption("-p|--precision <N>")]
    [LocalizedDescription("cli_opt_precision_desc")]
    public double Precision { get; set; } = 64;

    [CommandOption("-t|--tolerance <N>")]
    [LocalizedDescription("cli_opt_tolerance_desc")]
    public double Tolerance { get; set; } = 5;

    [CommandOption("--dry-run")]
    [LocalizedDescription("cli_opt_dry_run_desc")]
    public bool DryRun { get; set; }

    [CommandOption("--stream")]
    [LocalizedDescription("cli_opt_stream_output_desc")]
    public bool StreamOutput { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Input) && string.IsNullOrWhiteSpace(Workspace))
            return ValidationResult.Error(Strings.cli_err_input_required);
        return base.Validate();
    }

    /// <summary>从文件或工作区加载谱面。</summary>
    public async Task<Rpe.Chart> LoadChartAsync()
    {
        if (!string.IsNullOrWhiteSpace(Workspace))
        {
            var ws = new WorkspaceService();
            return await ws.GetAsync(Workspace) ?? throw new InvalidOperationException(
                string.Format(Strings.cli_err_workspace_missing, Workspace));
        }

        var text = await File.ReadAllTextAsync(Input!);
        return await Rpe.Chart.LoadFromJsonAsync(text);
    }

    /// <summary>根据 Input/Workspace 自动计算输出路径。</summary>
    public string ResolveOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(Output)) return Output;
        var source = Input ?? "workspace";
        return Path.Combine(
            Path.GetDirectoryName(source) ?? ".",
            Path.GetFileNameWithoutExtension(source) + "_PFC.json");
    }
}

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class RpeUnbindFatherCommand : AsyncCommand<RpeUnbindFatherCommand.Settings>
{
    public sealed class Settings : RpeOperationSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chart = await settings.LoadChartAsync();
        var chartCopy = chart.Clone();

        for (var i = 0; i < chart.JudgeLineList.Count; i++)
        {
            //Console.WriteLine(i);
            if (chart.JudgeLineList[i].Father != -1)
                chartCopy.JudgeLineList[i] = await RePhiEditHelper.FatherUnbindAsync(
                    i, chart.JudgeLineList, settings.Precision, settings.Tolerance);
        }

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
        {
            if (settings.StreamOutput)
            {
                await using var stream = new FileStream(output, FileMode.Create);
                await chartCopy.ExportToJsonStreamAsync(stream, true);
            }
            else
            {
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(true), cancellationToken);
            }
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}

/// <summary>
/// 合并层级命令
/// </summary>
public sealed class RpeLayerMergeCommand : AsyncCommand<RpeLayerMergeCommand.Settings>
{
    public sealed class Settings : RpeOperationSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chart = await settings.LoadChartAsync();
        var chartCopy = chart.Clone();

        foreach (var jl in chartCopy.JudgeLineList)
        {
            if (jl.EventLayers is not { Count: > 1 }) continue;
            jl.EventLayers = [RePhiEditHelper.LayerMerge(jl.EventLayers, settings.Precision, settings.Tolerance)];
        }

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
            await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(true), cancellationToken);

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}

// rpe convert 

// Description set via WithDescription(Strings.cli_cmd_rpe_convert_desc) in Program.cs
public sealed class RpeConvertCommand : Command<BaseSettings>
{
    protected override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        settings.CreateWriter().Warn(Strings.cli_warn_rpe_convert);
        return 2;
    }
}

// UnknownCommand（保留兼容

/// <summary>不应在正常路由中被命中，仅作保底。</summary>
public sealed class UnknownCommand : Command<BaseSettings>
{
    protected override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        settings.CreateWriter().Error(Strings.cli_err_unknown);
        return 1;
    }
}