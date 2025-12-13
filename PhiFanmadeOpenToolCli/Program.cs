using static PhiFanmade.Core.RePhiEdit.RePhiEdit;
using PhiFanmade.OpenTool.Utils;

Console.WriteLine("PhiFanmade Open Tool CLI");
Console.WriteLine("Please enter the path to the RePhiEdit Chart file:");
var rpeChartPath = Console.ReadLine() ?? throw new InvalidOperationException("No input provided");
// 如果左右有双引号，去掉
rpeChartPath = rpeChartPath.Trim('"');
var rpeChartText = await File.ReadAllTextAsync(rpeChartPath);
var rpeChart = await Chart.LoadFromJsonAsync(rpeChartText);
// 层级合并
foreach (var judgeline in rpeChart.JudgeLineList)
{
    judgeline.EventLayers = new List<EventLayer>(){RePhiEditUtility.LayerMerge(judgeline.EventLayers)};
}
// 加上_PFC文件名后缀导出到同目录
var outputPathRpe = Path.Combine(Path.GetDirectoryName(rpeChartPath) ?? "", Path.GetFileNameWithoutExtension(rpeChartPath) + "_PFC.json");
await File.WriteAllTextAsync(outputPathRpe, await rpeChart.ExportToJsonAsync(true));
/*
Console.WriteLine("Please enter the path to the PhiEditor Chart file:");
var peChartPath = Console.ReadLine() ?? throw new InvalidOperationException("No input provided");
var peChartText = await File.ReadAllTextAsync(peChartPath);
var peChart = await Chart.LoadAsync(peChartText);
// 加上_PFC文件名后缀导出到同目录
var outputPath = Path.Combine(Path.GetDirectoryName(peChartPath) ?? "", Path.GetFileNameWithoutExtension(peChartPath) + "_PFC.pec");
await File.WriteAllTextAsync(outputPath, await peChart.ExportAsync());
*/
Console.WriteLine("Tested. Press any key to exit.");
Console.ReadKey();