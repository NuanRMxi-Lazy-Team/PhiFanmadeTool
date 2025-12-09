# Unity Package 构建脚本
$ErrorActionPreference = "Stop"

# 定义路径
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageName = "PhiFanmadeCore"
$version = "0.0.1"
$outputPath = Join-Path $scriptPath "Build"
$tempPath = Join-Path $outputPath "Temp"

# 创建输出目录
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null

Write-Host "building Unity Package..." -ForegroundColor Green

# 创建 package.json
$packageJson = @{
    name = "com.phifanmade.core"
    version = $version
    displayName = "PhiFanmade Core"
    description = "PhiFanmade Core Package"
    unity = "2019.4"
    keywords = @("phi", "fanmade", "core")
    author = @{
        name = "NuanR_Star Ciallo Team"
    }
} | ConvertTo-Json -Depth 10

$packageJsonPath = Join-Path $tempPath "package.json"
Set-Content -Path $packageJsonPath -Value $packageJson -Encoding UTF8

Write-Host "created package.json" -ForegroundColor Cyan

# 定义需要排除的文件和目录
$excludePatterns = @(
    "*.ps1",
    "obj",
    "bin",
    "*.csproj",
    "*.csproj.user",
    ".vs",
    "Build"
)

# 复制 Runtime 目录
$runtimeSource = Join-Path $scriptPath "Runtime"
$runtimeDest = Join-Path $tempPath "Runtime"
if (Test-Path $runtimeSource) {
    Copy-Item -Path $runtimeSource -Destination $runtimeDest -Recurse -Force
    Write-Host "success copy Runtime path" -ForegroundColor Cyan
}

# 复制许可证和 README
$parentPath = Split-Path -Parent $scriptPath
foreach ($file in @("LICENSE", "LICENSE.md", "LICENSE.txt", "README", "README.md", "README.txt")) {
    $filePath = Join-Path $parentPath $file
    if (Test-Path $filePath) {
        Copy-Item -Path $filePath -Destination $tempPath -Force
        Write-Host "copyed $file" -ForegroundColor Cyan
    }
}

# 删除排除的文件和目录
Get-ChildItem -Path $tempPath -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($tempPath.Length)
    $shouldExclude = $false

    foreach ($pattern in $excludePatterns) {
        if ($_.Name -like $pattern -or $relativePath -like "*\$pattern\*") {
            $shouldExclude = $true
            break
        }
    }

    if ($shouldExclude) {
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "fucked: $($_.Name)" -ForegroundColor Yellow
    }
}

# 打包为 .unitypackage
$zipPath = Join-Path $outputPath "$packageName-$version.zip"
$packagePath = Join-Path $outputPath "$packageName-$version.unitypackage"

Compress-Archive -Path "$tempPath\*" -DestinationPath $zipPath -Force

# 重命名为 .unitypackage
Move-Item -Path $zipPath -Destination $packagePath -Force

# 清理临时目录
Remove-Item $tempPath -Recurse -Force

Write-Host "build ok!" -ForegroundColor Green
Write-Host "success: $packagePath" -ForegroundColor Green
