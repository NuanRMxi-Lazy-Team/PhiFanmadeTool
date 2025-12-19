using System.Collections.Concurrent;
using static PhiFanmade.Core.RePhiEdit.RePhiEdit;

namespace PhiFanmade.OpenTool.Cli.Infrastructure;

public sealed class WorkspaceService
{
    private readonly ConcurrentDictionary<string, Chart> _charts = new();
    private readonly string _rootDir;

    public WorkspaceService()
    {
        _rootDir = Path.Combine(AppContext.BaseDirectory, "workspaces");
        Directory.CreateDirectory(_rootDir);
    }

    public string Root => _rootDir;

    public IEnumerable<string> List() => Directory.EnumerateDirectories(_rootDir).Select(Path.GetFileName)
        .Concat(_charts.Keys).Distinct()!;

    public async Task LoadAsync(string id, string chartPath)
    {
        var text = await File.ReadAllTextAsync(chartPath);
        var chart = await Chart.LoadFromJsonAsync(text);
        _charts[id] = chart;

        var dir = Path.Combine(_rootDir, id);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "chart.json"), await chart.ExportToJsonAsync(true));
    }

    public bool Exists(string id) => _charts.ContainsKey(id) || Directory.Exists(Path.Combine(_rootDir, id));

    public async Task<Chart?> GetAsync(string id)
    {
        if (_charts.TryGetValue(id, out var chart)) return chart;
        var dir = Path.Combine(_rootDir, id);
        var file = Path.Combine(dir, "chart.json");
        if (File.Exists(file))
        {
            var text = await File.ReadAllTextAsync(file);
            var c = await Chart.LoadFromJsonAsync(text);
            _charts[id] = c;
            return c;
        }
        return null;
    }

    public async Task SaveAsync(string id, string outputPath)
    {
        var chart = await GetAsync(id) ?? throw new InvalidOperationException($"Workspace '{id}' not found");
        await File.WriteAllTextAsync(outputPath, await chart.ExportToJsonAsync(true));
    }

    public void Clear(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            if (Directory.Exists(_rootDir))
                Directory.Delete(_rootDir, true);
            Directory.CreateDirectory(_rootDir);
            _charts.Clear();
            return;
        }

        _charts.TryRemove(id, out _);
        var dir = Path.Combine(_rootDir, id);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}
