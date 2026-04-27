using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Settings.Operation;
using KaedePhi.Tool.KaedePhi;
using KaedePhi.Tool.KaedePhi.Events;

namespace KaedePhi.Tool.Cli.Commands;

/// <summary>
/// 事件拟合命令
/// </summary>
public sealed class FitEventCommand : AsyncCommand<FitEventCommand.Settings>
{
    public sealed class Settings : OperationSettingsWithTolerance
    {
        protected override double? GetConfigToleranceDefault() => AppConfig.FitConfig?.Tolerance;
        protected override bool? GetConfigDryRunDefault() => AppConfig.FitConfig?.DryRun;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        settings.ApplyConfigDefaults();
        var writer = new ConsoleWriter();
        var nrc = await settings.LoadNrcChartAsync(cancellationToken);

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_unimplemented));
            return 1;
        }

        var nrcCopy = nrc.Clone();
        using var logSubscription = KpcToolLog.Subscribe(
            info: writer.Info,
            warning: writer.Warn,
            error: writer.Error,
            debug: writer.Info);

        var fitTaskDegree = Math.Max(1, Environment.ProcessorCount);

        for (var i = 0; i < nrc.JudgeLineList.Count; i++)
        {
            var jdl = nrc.JudgeLineList[i];
            for (var index = 0; index < jdl.EventLayers.Count; index++)
            {
                var eventLayer = jdl.EventLayers[index];
                if (eventLayer == null) continue;

                cancellationToken.ThrowIfCancellationRequested();

                // 4 个通道完全独立，并发异步启动；每个通道内部 Phase 1 再利用多核预计算。
                var moveXTask = KpcEventTools.EventListFitAsync(
                    eventLayer.MoveXEvents, settings.Tolerance, fitTaskDegree, cancellationToken);
                var moveYTask = KpcEventTools.EventListFitAsync(
                    eventLayer.MoveYEvents, settings.Tolerance, fitTaskDegree, cancellationToken);
                var alphaTask = KpcEventTools.EventListFitAsync(
                    eventLayer.AlphaEvents, settings.Tolerance, fitTaskDegree, cancellationToken);
                var rotateTask = KpcEventTools.EventListFitAsync(
                    eventLayer.RotateEvents, settings.Tolerance, fitTaskDegree, cancellationToken);

                await Task.WhenAll(moveXTask, moveYTask, alphaTask, rotateTask);

                nrcCopy.JudgeLineList[i].EventLayers[index].MoveXEvents = moveXTask.Result;
                nrcCopy.JudgeLineList[i].EventLayers[index].MoveYEvents = moveYTask.Result;
                nrcCopy.JudgeLineList[i].EventLayers[index].AlphaEvents = alphaTask.Result;
                nrcCopy.JudgeLineList[i].EventLayers[index].RotateEvents = rotateTask.Result;
            }
        }

        var output = await settings.SaveFromNrcAsync(nrcCopy, cancellationToken);
        if (output == null)
        {
            writer.Warn(Strings.cli_warn_rpe_convert);
            return 2;
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}