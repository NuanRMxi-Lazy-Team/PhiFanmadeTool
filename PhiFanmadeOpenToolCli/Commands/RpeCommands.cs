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
        writer.Warn(loc["err.unimplemented"]);
        return 2;
        var input = OptionParser.GetOption(args, "-i", "--input", "--输入");
        var output = OptionParser.GetOption(args, "-o", "--output", "--输出");
        var workspace = OptionParser.GetOption(args, "--workspace", "--工作区");
        var dryRun = OptionParser.HasFlag(args, "--dry-run");

        Chart? chart;
        if (!string.IsNullOrWhiteSpace(workspace))
        {
            var ws = new WorkspaceService();
            chart = await ws.GetAsync(workspace!) ?? throw new InvalidOperationException(
                loc["err.workspace.missing"].Replace("{workspace}", workspace!));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(loc["err.input.required"]);
            var text = await File.ReadAllTextAsync(input);
            chart = await Chart.LoadFromJsonAsync(text);
        }

        var chartCopy = chart.Clone();
        for (var index = 0; index < chart.JudgeLineList.Count; index++)
        {
            var jl = chart.JudgeLineList[index];
            if (jl.Father != -1)
            {
                chartCopy.JudgeLineList[index] = RePhiEditUtility.FatherUnbind(index, chart.JudgeLineList);
            }
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            var source = input ?? "workspace";
            output = Path.Combine(Path.GetDirectoryName(source) ?? ".",
                Path.GetFileNameWithoutExtension(source) + "_PFC.json");
        }

        if (!dryRun)
            await File.WriteAllTextAsync(output!, await chartCopy.ExportToJsonAsync(true));
        writer.Info(loc["msg.written"].Replace("{path}", output!));
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
            var dryRun = OptionParser.HasFlag(args, "--dry-run");

            Chart? chart;
            if (!string.IsNullOrWhiteSpace(workspace))
            {
                var ws = new WorkspaceService();
                chart = await ws.GetAsync(workspace) ?? throw new InvalidOperationException(
                    loc["err.workspace.missing"].Replace("{workspace}", workspace!));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentException(loc["err.input.required"]);
                var text = await File.ReadAllTextAsync(input);
                chart = await Chart.LoadFromJsonStjAsync(text);
            }

            var chartCopy = chart.Clone();
            foreach (var jl in chartCopy.JudgeLineList)
            {
                if (jl.EventLayers is { Count: > 1 })
                {
                    var merged = RePhiEditUtility.LayerMerge(jl.EventLayers);
                    jl.EventLayers = [merged];
                }
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                var source = input ?? "workspace";
                output = Path.Combine(Path.GetDirectoryName(source) ?? ".",
                    Path.GetFileNameWithoutExtension(source) + "_PFC.json");
            }

            if (!dryRun)
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonStjAsync(true));
            writer.Info(loc["msg.written"].Replace("{path}", output!));
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
}

public sealed class PeConvertCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        writer.Warn(loc["warn.pe.convert"]);
        return Task.FromResult(2);
    }
}

public sealed class UnknownCommand : ICommandHandler
{
    public Task<int> ExecuteAsync(string[] args, ConsoleWriter writer, ILocalizer loc)
    {
        writer.Error(loc["err.unknown"]);
        return Task.FromResult(1);
    }
}