# PhiFanmadeTool
即使身处无人角落，我仍要继续向前，直到我无法前进。

## 使用
从 `Release` 中获得 nuget 包，或从源码编译。  
<span style="color:yellow">**注意：此项目仍然处于早期阶段，字段名称与行为随时有可能更改，请斟酌后再使用！**</span>  
<span style="color:red">**如果您使用本软件进行低质量创作，本软件将对您进行道德谴责，受限于开源协议，项目维护者无权阻止您的任何行为！**</span>

## CLI使用
打开命令行，进入PhiFanmadeOpenToolCli所在目录，执行以下命令：  
```pwsh
./PhiFanmde.OpenTool.Cli.exe <命令> [参数]
./PhiFanmde.OpenTool.Cli.exe rpe unbind --input <输入文件路径> --output <输出文件路径> #解绑父线，可选参数：--precision <切割精度> --tolerance <拟合容差> --workspace <工作区名称，有此选项时不需要input与output> --dry-run 仅运行不输出
./PhiFanmde.OpenTool.Cli.exe rpe layer-merge --input <输入文件路径> --output <输出文件路径> #合并所有判定线层级，可选参数：--precision <切割精度> --tolerance <拟合容差> --workspace <工作区名称，有此选项时不需要input与output> --dry-run 仅运行不输出
./PhiFanmde.OpenTool.Cli.exe load --input <输入文件路径> --workspace <工作区名称> #加载文件到工作区
./PhiFanmde.OpenTool.Cli.exe save --output <输出文件路径> --workspace <工作区名称> #保存工作区到文件
./PhiFanmde.OpenTool.Cli.exe workspace list #列出所有工作区
./PhiFanmde.OpenTool.Cli.exe workspace clear #清除所有工作区
```

## .NET版本
PhiFanmadeCore: .NETStandard2.1, .NET8.0, .NET9.0, .NET10.0 （基于System.Text.Json的Json序列化功能不在.NETStandard2.1提供）  
PhiFanmadeOpenTool: .NET8.0, .NET9.0, .NET10.0  
PhiFanmadeOpenToolCli: .NET8.0, .NET10.0  

## 招新
本项目需要更多人开发与维护，欢迎发送邮件到 nrlt@nuanr-mxi.com 来加入开发！  
也欢迎加入我的小群！QQ群号: 821169450

## 开源许可证
[GNU AFFERO GENERAL PUBLIC LICENSE](https://www.gnu.org/licenses/agpl-3.0.html)

## Copyright
NuanR_Mxi Copyright © 2025 PhiFanmadeTool Project.  
NuanR_Star Copyright © 2025 PhiFanmadeTool Project.  
Kaede HikariN Copyright © 2025 PhiFanmadeTool Project.  
Kaede NuanR_Mxi Copyright © 2025 PhiFanmadeTool Project.  
Kaede NuanR_Star Copyright © 2025 PhiFanmadeTool Project.  
枫暖日明曦 Copyright © 2025 PhiFanmadeTool Project.  
枫暖日星辉 Copyright © 2025 PhiFanmadeTool Project.  
暖日明曦 Copyright © 2025 PhiFanmadeTool Project.  
暖日星辉 Copyright © 2025 PhiFanmadeTool Project.  
暖日 Copyright © 2025 PhiFanmadeTool Project.  
暖星 Copyright © 2025 PhiFanmadeTool Project.  
NuanR_Mxi Lazy Team Copyright © 2025 PhiFanmadeTool Project.  
NuanR_Star Lazy Team Copyright © 2025 PhiFanmadeTool Project.  
NuanR_Star Ciallo Team Copyright © 2025 PhiFanmadeTool Project.

## 免责声明
本软件与南京鸽游网络有限公司（厦门鸽游网络有限公司）无任何关联。

## 致谢
[cmdysj](https://space.bilibili.com/252635690)  
And you.