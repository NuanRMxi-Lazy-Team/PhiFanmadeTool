using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings.Operation;
using KaedePhi.Tool.KaedePhi;
using KaedePhi.Tool.KaedePhi.Render;

namespace KaedePhi.Tool.Cli.Commands;

/// <summary>
/// 事件通道渲染命令：将 NRC 谱面各判定线各事件层的通道事件渲染为 PNG 图片。
/// </summary>
public sealed class RenderCommand : AsyncCommand<RenderCommand.Settings>
{
    public sealed class Settings : OperationSettingsForRender
    {
        protected override float? GetConfigPixelsPerBeat() => AppConfig.RenderConfig?.PixelsPerBeat;
        protected override int? GetConfigChannelWidth() => AppConfig.RenderConfig?.ChannelWidth;
        protected override int? GetConfigSamples() => AppConfig.RenderConfig?.SamplesPerEvent;
        protected override int? GetConfigBeatSubdivisions() => AppConfig.RenderConfig?.BeatSubdivisions;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        settings.ApplyConfigDefaults();
        var writer = new ConsoleWriter();

        // 加载谱面
        var nrc = await settings.LoadNrcChartAsync(cancellationToken);
        if (nrc == null)
        {
            writer.Error(CliLocalizationString.render_err_load_failed);
            return 1;
        }

        // 构建渲染配置
        var opts = new RenderOptions
        {
            PixelsPerBeat = settings.PixelsPerBeat,
            ChannelWidth = settings.ChannelWidth,
            SamplesPerEvent = settings.SamplesPerEvent,
            BeatSubdivisions = settings.BeatSubdivisions
        };

        string outputDir = settings.ResolveOutputDir();
        writer.Info(string.Format(CliLocalizationString.render_msg_start, outputDir));

        // 订阅日志
        using var logSub = KpcToolLog.Subscribe(
            info: writer.Info,
            warning: writer.Warn,
            error: writer.Error);

        // 执行渲染导出
        IReadOnlyList<string> files;
        try
        {
            files = KpcRenderExporter.ExportChart(
                nrc, outputDir, opts,
                lineIndex: settings.LineIndex,
                layerIndex: settings.LayerIndex);
        }
        catch (Exception ex)
        {
            writer.Error(string.Format(CliLocalizationString.render_err_render_failed, ex.Message));
            return 2;
        }

        if (files.Count == 0)
            writer.Warn(CliLocalizationString.render_warn_nothing);
        else
            writer.Info(string.Format(CliLocalizationString.render_msg_done, files.Count, outputDir));

        return 0;
    }
}