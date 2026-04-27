using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Model;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.PhiEdit.Converters;
using Spectre.Console;
using Chart = KaedePhi.Core.Kpc.Chart;

namespace KaedePhi.Tool.Cli.Settings.Operation;

/// <summary>
/// 所有 CLI 操作的通用基础参数（输入、输出、工作区）
/// </summary>
public abstract class OperationSettingsBase : CommandSettings
{
    [CommandOption("-i|--input <PATH>")]
    [LocalizedDescription("cli_opt_input_rpe_desc")]
    public virtual string? Input { get; set; }

    [CommandOption("-o|--output <PATH>")]
    [LocalizedDescription("cli_opt_output_auto_desc")]
    public string? Output { get; set; }

    [CommandOption("-w|--workspace <ID>")]
    [LocalizedDescription("cli_opt_workspace_rpe_desc")]
    public string? Workspace { get; set; }

    public AppConfig AppConfig { get; set; }

    protected OperationSettingsBase()
    {
        // 检查程序所在目录是否有config.yaml，如果没有，则通过Model中的AppConfig配合YamlDotNet新建一个默认的config.yaml
        AppConfig config;
        if (File.Exists("config.yaml"))
        {
            var configText = File.ReadAllText("config.yaml");
            config = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize<AppConfig>(configText);
        }
        else
        {
            config = new AppConfig();
            var configText = new YamlDotNet.Serialization.SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build()
                .Serialize(config);
            File.WriteAllText("config.yaml", configText);
        }

        AppConfig = config;
    }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Input) && string.IsNullOrWhiteSpace(Workspace))
            return ValidationResult.Error(Strings.cli_err_input_required);
        return base.Validate();
    }

    /// <summary>从文件或工作区加载谱面。</summary>
    public async Task<string> LoadChartAsync()
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
            path = Input ?? throw new InvalidOperationException(Strings.cli_err_input_required);
        }

        var text = await File.ReadAllTextAsync(path);
        return text;
    }

    /// <summary>根据 Input/Workspace 自动计算输出路径。</summary>
    public string ResolveOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(Output)) return Output;
        if (string.IsNullOrWhiteSpace(Workspace))
            return Path.Combine(
                Path.GetDirectoryName(Input!) ?? ".",
                Path.GetFileNameWithoutExtension(Input!) + "_PFC.json");
        var ws = new WorkspaceService();
        return Path.Combine(ws.Root, Workspace, "chart.json");
    }

    /// <summary>将输入谱面统一转换为 NRC 中间类型。</summary>
    public async Task<Chart?> LoadNrcChartAsync(CancellationToken cancellationToken = default)
    {
        var chartText = await LoadChartAsync();
        var chartType = ChartGetType.GetType(chartText);

        return chartType switch
        {
            ChartType.RePhiEdit => RePhiEdit.Converters.RpeToNrc.Convert(
                await global::KaedePhi.Core.RePhiEdit.Chart.LoadFromJsonAsync(chartText)),
            ChartType.PhiEdit => PeToNrc.Convert(
                await global::KaedePhi.Core.PhiEdit.Chart.LoadAsync(chartText)),
            _ => null
        };
    }

    /// <summary>将 NRC 中间类型导出为目标谱面类型，并按当前设置写入。子类可覆写来支持导出功能。</summary>
    public virtual Task<string?> SaveFromNrcAsync(Chart chart, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>应用配置默认值的扩展点。</summary>
    public virtual void ApplyConfigDefaults()
    {
    }
}



