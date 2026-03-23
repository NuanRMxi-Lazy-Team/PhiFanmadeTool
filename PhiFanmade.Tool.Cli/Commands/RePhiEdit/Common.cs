using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

// 共享 Settings

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
    
    [CommandOption("--format")]
    [LocalizedDescription("cli_opt_format_desc")]
    public bool FormatOutput { get; set; }

    [CommandOption("--no-compress")]
    [LocalizedDescription("cli_opt_compress_desc")]
    public bool DisableCompress { get; set; }
    
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Input) && string.IsNullOrWhiteSpace(Workspace))
            return ValidationResult.Error(Strings.cli_err_input_required);
        return base.Validate();
    }

    /// <summary>从文件或工作区加载谱面。</summary>
    public async Task<Rpe.Chart> LoadChartAsync()
    {
        string path;
        if (!string.IsNullOrWhiteSpace(Workspace))
        {
            var ws = new WorkspaceService();
            path = ws.GetChartPath(Workspace)
                   ?? throw new InvalidOperationException(
                       string.Format(Strings.cli_err_workspace_missing, Workspace));
        }
        else
        {
            path = Input!;
        }

        var text = await File.ReadAllTextAsync(path);
        return await Rpe.Chart.LoadFromJsonAsync(text);
    }

    /// <summary>根据 Input/Workspace 自动计算输出路径。</summary>
    public string ResolveOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(Output)) return Output;
        if (!string.IsNullOrWhiteSpace(Workspace))
        {
            var ws = new WorkspaceService();
            return Path.Combine(ws.Root, Workspace, "chart.json");
        }
        return Path.Combine(
            Path.GetDirectoryName(Input!) ?? ".",
            Path.GetFileNameWithoutExtension(Input!) + "_PFC.json");
    }
}