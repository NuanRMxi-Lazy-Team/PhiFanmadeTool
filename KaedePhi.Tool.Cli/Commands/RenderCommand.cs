using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings;
using KaedePhi.Tool.KaedePhi;
using KaedePhi.Tool.Render.KaedePhi;

namespace KaedePhi.Tool.Cli.Commands;

public sealed class RenderCommand : AsyncCommand<RenderCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
        [CommandOption("-r|--pixels-per-beat <N>")]
        [LocalizedDescription("render_opt_pixels_per_beat")]
        public float? PixelsPerBeat { get; set; }

        [CommandOption("--channel-width <N>")]
        [LocalizedDescription("render_opt_channel_width")]
        public int? ChannelWidth { get; set; }

        [CommandOption("--samples <N>")]
        [LocalizedDescription("render_opt_samples")]
        public int? SamplesPerEvent { get; set; }

        [CommandOption("-b|--beat-subdivisions <N>")]
        [LocalizedDescription("render_opt_beat_subdivisions")]
        public int? BeatSubdivisions { get; set; }

        [CommandOption("--line <INDEX>")]
        [LocalizedDescription("render_opt_line")]
        public int? LineIndex { get; set; }

        [CommandOption("--layer <INDEX>")]
        [LocalizedDescription("render_opt_layer")]
        public int? LayerIndex { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings s, CancellationToken ct)
    {
        var c = s.AppConfig.RenderConfig;
        s.PixelsPerBeat ??= c.PixelsPerBeat;
        s.ChannelWidth ??= c.ChannelWidth;
        s.SamplesPerEvent ??= c.SamplesPerEvent;
        s.BeatSubdivisions ??= c.BeatSubdivisions;

        var writer = new ConsoleWriter();
        var svc = new ChartService();
        var nrc = await svc.LoadKpcAsync(s.Input, s.Workspace, ct);
        if (nrc == null) { writer.Error(CliLocalizationString.render_err_load_failed); return 1; }

        var outputDir = !string.IsNullOrWhiteSpace(s.Output) ? s.Output
            : !string.IsNullOrWhiteSpace(s.Input) ? Path.Combine(Path.GetDirectoryName(s.Input) ?? ".", "render_output")
            : Path.Combine(Directory.GetCurrentDirectory(), "render_output");

        writer.Info(string.Format(CliLocalizationString.render_msg_start, outputDir));

        var opts = new KpcRenderOptions
        {
            PixelsPerBeat = s.PixelsPerBeat ?? 100f,
            ChannelWidth = s.ChannelWidth ?? 150,
            SamplesPerEvent = s.SamplesPerEvent ?? 64,
            BeatSubdivisions = s.BeatSubdivisions ?? 2,
        };

        var exporter = new KpcChartRenderExporter();
        using var _ = KpcToolLog.Subscribe(info: writer.Info, warning: writer.Warn, error: writer.Error);

        try
        {
            var files = exporter.ExportChart(nrc, outputDir, opts, s.LineIndex, s.LayerIndex);
            if (files.Count == 0) writer.Warn(CliLocalizationString.render_warn_nothing);
            else writer.Info(string.Format(CliLocalizationString.render_msg_done, files.Count, outputDir));
        }
        catch (Exception ex)
        {
            writer.Error(string.Format(CliLocalizationString.render_err_render_failed, ex.Message));
            return 2;
        }

        return 0;
    }
}
