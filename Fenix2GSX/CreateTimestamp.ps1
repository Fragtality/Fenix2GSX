### PRE
### pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)CreateTimestamp.ps1" $(SolutionDir) $(ProjectDir) "APPNAME" "PAYLOADPATH"

$basePath = $args[0]
$pathProject = $args[1]
$appName = $args[2]
$appPayloadPath = $args[3]
if ($args[0] -eq "*Undefined*" -or $args[1] -eq "*Undefined*") {
	exit 0
}
$pathPayload = Join-Path $basePath $appPayloadPath

if ((Test-Path -Path (Join-Path $basePath "build.lck"))) {
	exit 0
}

$projectFile = Join-Path $pathProject "$appName.csproj"
[xml]$projectXml = Get-Content $projectFile
$version = "$($projectXml.Project.ChildNodes.Version)".Trim()
$timestamp = $([System.DateTime]::UtcNow.ToString("yyyy.MM.dd.HHmm"))
$build = $version + "+build" + $timestamp
Write-Host "Create version.json for $build ..."
@"
{
	"Version": "$version",
	"Timestamp": "$timestamp"
}
"@ | Out-File (Join-Path $pathPayload "version.json") -Encoding utf8NoBOM