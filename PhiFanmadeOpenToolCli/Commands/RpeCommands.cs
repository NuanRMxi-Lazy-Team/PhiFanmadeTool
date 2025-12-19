using PhiFanmade.OpenTool.Cli.Infrastructure;
using PhiFanmade.OpenTool.Cli.Parsing;
using PhiFanmade.OpenTool.Localization;
using PhiFanmade.OpenTool.Utils;
using static PhiFanmade.Core.RePhiEdit.RePhiEdit;

namespace PhiFanmade.OpenTool.Cli.Commands;

public sealed class RpeUnbindFatherCommand : ICommandHandler
{
    public async Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        var input = OptionParser.GetOption(args, "-i", "--input", "--输入");
        var output = OptionParser.GetOption(args, "-o", "--output", "--输出");
        var workspace = OptionParser.GetOption(args, "--workspace", "-wk", "--工作区");
        var precision = OptionParser.GetOption(args, "--precision", "-p", "--精度");
        var tolerance = OptionParser.GetOption(args, "--tolerance", "-t", "--容差");
        var dryRun = OptionParser.HasFlag(args, "--dry-run");

        Chart? chart;
        if (!string.IsNullOrWhiteSpace(workspace))
        {
            var ws = new WorkspaceService();
            chart = await ws.GetAsync(workspace!) ?? throw new InvalidOperationException(
                loc["cli.err.workspace.missing"].Replace("{workspace}", workspace!));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(loc["cli.err.input.required"]);
            var text = await File.ReadAllTextAsync(input);
            chart = await Chart.LoadFromJsonStjAsync(text);
        }

        // 检查precision和tolerance参数，如果有则解析为double，没有则使用默认值64和5
        double precisionValue = 64d;
        double toleranceValue = 5d;
        if (!string.IsNullOrWhiteSpace(precision) && double.TryParse(precision, out var p))
            precisionValue = p;
        if (!string.IsNullOrWhiteSpace(tolerance) && double.TryParse(tolerance, out var t))
            toleranceValue = t;


        var chartCopy = chart.Clone();
        for (var index = 0; index < chart.JudgeLineList.Count; index++)
        {
            Console.WriteLine(index);
            var jl = chart.JudgeLineList[index];
            if (jl.Father != -1)
            {
                chartCopy.JudgeLineList[index] = await RePhiEditHelper.FatherUnbindAsync(index, chart.JudgeLineList,
                    precisionValue, toleranceValue);
            }
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            var source = input ?? "workspace";
            output = Path.Combine(Path.GetDirectoryName(source) ?? ".",
                Path.GetFileNameWithoutExtension(source) + "_PFC.json");
        }

        // 使用流式序列化（ExportToJsonStjStreamAsync）防止OOM
        var stream = new FileStream(output, FileMode.Create);
        if (!dryRun)
            await chartCopy.ExportToJsonStjStreamAsync(stream, true);
        writer.Info(loc["cli.msg.written"].Replace("{path}", output!));
        return 0;
    }
}

public sealed class RpeLayerMergeCommand : ICommandHandler
{
    public async Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        try
        {
            var input = OptionParser.GetOption(args, "-i", "--input", "--输入");
            var output = OptionParser.GetOption(args, "-o", "--output", "--输出");
            var workspace = OptionParser.GetOption(args, "--workspace", "-wk", "--工作区");
            var precision = OptionParser.GetOption(args, "--precision", "-p", "--精度");
            var tolerance = OptionParser.GetOption(args, "--tolerance", "-t", "--容差");
            var dryRun = OptionParser.HasFlag(args, "--dry-run");

            Chart? chart;
            if (!string.IsNullOrWhiteSpace(workspace))
            {
                var ws = new WorkspaceService();
                chart = await ws.GetAsync(workspace) ?? throw new InvalidOperationException(
                    loc["cli.err.workspace.missing"].Replace("{workspace}", workspace!));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentException(loc["cli.err.input.required"]);
                var text = await File.ReadAllTextAsync(input);
                chart = await Chart.LoadFromJsonStjAsync(text);
            }

            // 检查precision和tolerance参数，如果有则解析为double，没有则使用默认值64和5
            double precisionValue = 64d;
            double toleranceValue = 5d;
            if (!string.IsNullOrWhiteSpace(precision) && double.TryParse(precision, out var p))
                precisionValue = p;
            if (!string.IsNullOrWhiteSpace(tolerance) && double.TryParse(tolerance, out var t))
                toleranceValue = t;

            var chartCopy = chart.Clone();
            foreach (var jl in chartCopy.JudgeLineList)
            {
                if (jl.EventLayers is not { Count: > 1 }) continue;
                var merged = RePhiEditHelper.LayerMerge(jl.EventLayers, precisionValue, toleranceValue);
                jl.EventLayers = [merged];
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                var source = input ?? "workspace";
                output = Path.Combine(Path.GetDirectoryName(source) ?? ".",
                    Path.GetFileNameWithoutExtension(source) + "_PFC.json");
            }

            if (!dryRun)
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonStjAsync(true));
            writer.Info(loc["cli.msg.written"].Replace("{path}", output!));
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

public sealed class RpeConvertCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        writer.Warn(loc["cli.warn.rpe.convert"]);
        return Task.FromResult(2);
    }
}

public sealed class UnknownCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        writer.Error(loc["cli.err.unknown"]);
        return Task.FromResult(1);
    }
}