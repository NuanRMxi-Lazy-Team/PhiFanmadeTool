using System.Reflection;
using PhiFanmade.Tool.Cli.Commands;
using PhiFanmade.Tool.Cli.Commands.RePhiEdit;
using PhiFanmade.Tool.Cli.Commands.Test;
using PhiFanmade.Tool.Cli.Commands.WorkSpace;
using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using Spectre.Console.Cli;

#if !Release
var writer = new ConsoleWriter();
writer.Warn(string.Format(Strings.cli_warn_unstable_version, Strings.cli_app_title));
#endif

var app = new CommandApp();
app.SetDefaultCommand<VersionCommand>();
app.Configure(config =>
{
    config.SetApplicationName(Strings.cli_app_title);
    config.SetApplicationVersion(
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown");

    // 未知命令/参数时立即报错，而非静默跳过
    config.UseStrictParsing();

    // 统一异常处理，保持与原先相同的错误输出风格
    config.SetExceptionHandler((ex, _) =>
    {
        // 未知命令/参数：引导用户使用 --help
        if (ex is CommandParseException)
        {
            writer.Error(Strings.cli_err_unknown);
            writer.Info(Strings.cli_hint_use_help);
            return 1;
        }
        // 如果是out of memory这种错误，应该提示使用--stream选项，而不是让其反馈
        if (ex is OutOfMemoryException)
        {
            new ConsoleWriter().Error(string.Format(Strings.cli_err_out_of_memory,ex));
            return 1;
        }
        new ConsoleWriter().Error(string.Format(Strings.cli_err_ukerr, ex));
        return 1;
    });

    config.AddCommand<VersionCommand>("version")
        .WithDescription(Strings.cli_cmd_version_desc)
        .WithAlias("ver");
    config.AddCommand<GetTypeTestCommand>("test");

    config.AddCommand<LoadCommand>("load")
        .WithDescription(Strings.cli_cmd_load_desc);

    config.AddCommand<SaveCommand>("save")
        .WithDescription(Strings.cli_cmd_save_desc);
    config.AddCommand<UnbindFatherCommand>("unbind-father")
        .WithAlias("unbind")
        .WithDescription(Strings.cli_cmd_rpe_unbind_father_desc);
    config.AddCommand<FitEventCommand>("fit")
        .WithAlias("fit-event")
        .WithDescription("TODO");//TODO: Add Description

    config.AddBranch("workspace", ws =>
    {
        ws.SetDescription(Strings.cli_branch_workspace_desc);
        ws.AddCommand<WorkspaceListCommand>("list")
            .WithDescription(Strings.cli_cmd_workspace_list_desc);
        ws.AddCommand<WorkspaceClearCommand>("clear")
            .WithDescription(Strings.cli_cmd_workspace_clear_desc);
        ws.AddCommand<LoadCommand>("load")
            .WithDescription(Strings.cli_cmd_load_desc);
        ws.AddCommand<SaveCommand>("save")
            .WithDescription(Strings.cli_cmd_save_desc);
    });

    config.AddBranch("rpe", rpe =>
    {
        rpe.SetDescription(Strings.cli_branch_rpe_desc);
        rpe.AddCommand<RpeUnbindFatherCommand>("unbind-father")
            .WithDescription(Strings.cli_cmd_rpe_unbind_father_desc)
            .WithAlias("unbind")
            .IsHidden();
        rpe.AddCommand<RpeLayerMergeCommand>("layer-merge")
            .WithDescription(Strings.cli_cmd_rpe_layer_merge_desc)
            .WithAlias("layer-merge");
        rpe.AddCommand<RpeConvertCommand>("convert")
            .WithDescription(Strings.cli_cmd_rpe_convert_desc);
        rpe.AddCommand<RpeCutEventCommand>("cut")
            .WithAlias("cut-event")
            .WithAlias("cut-all")
            .WithDescription(Strings.cli_cmd_rpe_cut_event_desc);
    });
});

return await app.RunAsync(args);