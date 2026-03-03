﻿using System.Collections.Concurrent;

namespace PhiFanmade.Tool.Gui.Services;

/// <summary>GUI 侧工作区服务（单例），负责管理 RPE 谱面的内存与磁盘缓存。</summary>
public sealed class WorkspaceService
{
    private static readonly Lazy<WorkspaceService> _lazy = new(() => new WorkspaceService());
    public static WorkspaceService Instance => _lazy.Value;

    private readonly ConcurrentDictionary<string, Rpe.Chart> _charts = new();
    private readonly string _rootDir;

    private WorkspaceService()
    {
        _rootDir = Path.Combine(AppContext.BaseDirectory, "workspaces");
        Directory.CreateDirectory(_rootDir);
    }

    public string Root => _rootDir;

    public IEnumerable<string> List() =>
        Directory.EnumerateDirectories(_rootDir)
            .Select(Path.GetFileName)
            .Concat(_charts.Keys)
            .Distinct()!;

    public async Task LoadAsync(string id, string chartPath)
    {
        var text = await File.ReadAllTextAsync(chartPath);
        var chart = await Rpe.Chart.LoadFromJsonAsync(text);
        _charts[id] = chart;
        var dir = Path.Combine(_rootDir, id);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "chart.json"), await chart.ExportToJsonAsync(true));
    }

    public bool Exists(string id) =>
        _charts.ContainsKey(id) || Directory.Exists(Path.Combine(_rootDir, id));

    public async Task<Rpe.Chart?> GetAsync(string id)
    {
        if (_charts.TryGetValue(id, out var chart)) return chart;
        var file = Path.Combine(_rootDir, id, "chart.json");
        if (!File.Exists(file)) return null;
        var text = await File.ReadAllTextAsync(file);
        var c = await Rpe.Chart.LoadFromJsonAsync(text);
        _charts[id] = c;
        return c;
    }

    public async Task SaveAsync(string id, string outputPath)
    {
        var chart = await GetAsync(id)
            ?? throw new InvalidOperationException($"工作区 '{id}' 不存在");
        await File.WriteAllTextAsync(outputPath, await chart.ExportToJsonAsync(true));
    }

    public void Clear(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            if (Directory.Exists(_rootDir)) Directory.Delete(_rootDir, true);
            Directory.CreateDirectory(_rootDir);
            _charts.Clear();
            return;
        }
        _charts.TryRemove(id, out _);
        var dir = Path.Combine(_rootDir, id);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}
