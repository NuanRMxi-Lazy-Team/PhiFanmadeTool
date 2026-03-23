namespace PhiFanmade.Tool.Cli.Infrastructure;

/// <summary>
/// 工作区服务，仅负责文件的复制与路径管理，不进行任何谱面序列化/反序列化。
/// </summary>
public sealed class WorkspaceService
{
    private const string ChartFileName = "chart.json";
    private readonly string _rootDir;

    public WorkspaceService()
    {
        _rootDir = Path.Combine(AppContext.BaseDirectory, "workspaces");
        Directory.CreateDirectory(_rootDir);
    }

    public string Root => _rootDir;

    public IEnumerable<string> List() =>
        Directory.EnumerateDirectories(_rootDir).Select(Path.GetFileName)!;

    /// <summary>
    /// 将外部谱面文件以流的方式复制到工作区目录，不做任何解析。
    /// </summary>
    public async Task LoadAsync(string id, string chartPath)
    {
        var dir = Path.Combine(_rootDir, id);
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, ChartFileName);
        await using var src = new FileStream(chartPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 65536, useAsync: true);
        await using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize: 65536, useAsync: true);
        await src.CopyToAsync(dst);
    }

    public bool Exists(string id) => Directory.Exists(Path.Combine(_rootDir, id));

    /// <summary>
    /// 返回工作区谱面文件的路径，若工作区不存在则返回 null。
    /// </summary>
    public string? GetChartPath(string id)
    {
        var file = Path.Combine(_rootDir, id, ChartFileName);
        return File.Exists(file) ? file : null;
    }

    /// <summary>
    /// 将工作区谱面文件以流的方式输出到目标路径，不做任何解析。
    /// </summary>
    public async Task SaveAsync(string id, string outputPath)
    {
        var file = GetChartPath(id)
                   ?? throw new InvalidOperationException($"Workspace '{id}' not found");
        await using var src = new FileStream(file, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 65536, useAsync: true);
        await using var dst = new FileStream(outputPath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize: 65536, useAsync: true);
        await src.CopyToAsync(dst);
    }

    public void Clear(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            if (Directory.Exists(_rootDir))
                Directory.Delete(_rootDir, true);
            Directory.CreateDirectory(_rootDir);
            return;
        }

        var dir = Path.Combine(_rootDir, id);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}
