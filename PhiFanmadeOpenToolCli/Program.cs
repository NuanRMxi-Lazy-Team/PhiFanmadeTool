using PhiFanmade.OpenTool.Cli.Parsing;

Console.WriteLine(Environment.Is64BitProcess);
return await new CommandRouter().RunAsync(args);