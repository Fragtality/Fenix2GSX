### Run in SolutionDir
$appName = "Fenix2GSX"
$suffix = "latest"
$versionPath = "Installer\Payload\version.json"
$releaseDir = "Releases\"
$includeStamp = $false

Write-Host "Getting App Version ..."
$versionJson = (Get-Content -Raw $versionPath | ConvertFrom-Json)
$version = $versionJson.Version
$version = $version.Substring(0,$version.LastIndexOf('.'))
$timestamp = $versionJson.Timestamp

if (-not (Test-Path $releaseDir)) {
	mkdir $releaseDir | Out-Null
}
$releaseFile = "$appName-Installer-v$version.exe"
if ($includeStamp) {
	$releaseFile = "$appName-Installer-v$version-$timestamp.exe"
}

$releasePath = Join-Path $releaseDir "$releaseFile"

Write-Host "Exporting $releaseFile ..."
if (-not (Test-Path $releasePath)) {
	Copy-Item "$appName-Installer-$suffix.exe" $releasePath | Out-Null
} else {
	Write-Host "WARNING: The File '$releaseFile' already exists!"
}