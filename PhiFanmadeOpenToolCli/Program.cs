using PhiFanmade.Core.PhiEdit;

Console.WriteLine("PhiFanmade Open Tool CLI");
Console.WriteLine("Please enter the path to the PhiEditor Chart file:");
var peChartPath = Console.ReadLine() ?? throw new InvalidOperationException("No input provided");
var peChartText = await File.ReadAllTextAsync(peChartPath);
var peChart = await Chart.LoadAsync(peChartText);
// 加上_PFC文件名后缀导出到同目录
var outputPath = Path.Combine(Path.GetDirectoryName(peChartPath) ?? "", Path.GetFileNameWithoutExtension(peChartPath) + "_PFC.pec");
await File.WriteAllTextAsync(outputPath, await peChart.ExportAsync());
Console.WriteLine("Tested. Press any key to exit.");
Console.ReadKey();