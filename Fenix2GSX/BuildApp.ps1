# POST
# pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)BuildApp.ps1" $(Configuration) $(SolutionDir) $(ProjectDir) "APPNAME" "AppConfig.json"

######### CONFIG
$cfgDeploy = $false
$cfgCleanDeploy = $false # !!!
$cfgCleanLog = $false
$cfgResetConfig = $false # !!!
$cfgBuildInstaller = $true

if ($args[0] -eq "*Undefined*") {
	exit 0
}

if ($args[1] -eq "*Undefined*") {
	exit 0
}

try {
	$buildConfiguration = $args[0]
	if (-not $buildConfiguration -or $buildConfiguration -eq "Debug") {
		exit 0
	}

	$pathBase = $args[1]
	$pathProject = $args[2]
	$appName = $args[3]
	$appCfg = $args[4]
	$msBuildDir = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64"
	
	$pathPublish = Join-Path $pathProject "bin\publish"
	$pathDeploy = Join-Path (Join-Path ($env:APPDATA) $appName) "bin"
	$pathInstallerPayload = Join-Path $pathBase "Installer\Payload"
	
	$projectFile = Join-Path $pathProject "$appName.csproj"
	[xml]$projectXml = Get-Content $projectFile
	$appVersion = "$($projectXml.Project.ChildNodes.Version)".Trim()

	######### BUILD
	## Create Lock
	Write-Host $pathBase
	cd $pathBase
	if (-not (Test-Path -Path "build.lck")) {
		"lock" | Out-File -File "build.lck"
	}
	else {
		Write-Host "Lock active - sure?"							 
		exit 0
	}

	if (-not $buildConfiguration) {
		$buildConfiguration = "Release"
	}
	Write-Host ("Build Configuration: '$buildConfiguration'")
	
	## Build App
	Write-Host "dotnet publish for $appName (v$appVersion) ..."
	Remove-Item -Recurse -Force -Path ($pathPublish + "\*") -ErrorAction SilentlyContinue | Out-Null
	cd $pathProject
	dotnet publish -p:PublishProfile=FolderProfile$buildConfiguration -c $buildConfiguration --verbosity quiet
	if ($buildConfiguration -eq "Release") {
		Remove-Item -Recurse -Force -Path ($pathPublish + "\*.pdb") -ErrorAction SilentlyContinue | Out-Null
	}
	
	
	######### DEPLOY
	## Stop App
	if ($cfgDeploy) {
		Write-Host "Stopping $appName ..."
		Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process –force -ErrorAction SilentlyContinue
		Sleep(2)
	}

	## Clean Deploy
	if ($cfgDeploy -and $cfgCleanDeploy) {
		Write-Host "Removing old App Files ..."

		New-Item -Type Directory -Path $pathDeploy -ErrorAction SilentlyContinue | Out-Null
		Remove-Item -Recurse -Force -Path ($pathDeploy + "\*") | Out-Null
		Copy-Item -Path ($pathPublish + "\*") -Destination $pathDeploy -Recurse -Force | Out-Null
	}
	## Update Deploy
	elseif ($cfgDeploy) {
		Write-Host "Copy new Binaries ..."
		Copy-Item -Path ($pathPublish + "\*") -Destination $pathDeploy -Force | Out-Null

		if ($cfgResetConfig) {
			$configPath = Join-Path $pathDeploy $appCfg
			Write-Host "Resetting Configuration ..."
			if ((Test-Path $configPath)) {
				Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue | Out-Null
			}
		}

		if ($cfgDeploy -and $cfgCleanLog) {
			Write-Host "Removing Logs ..."				   
			New-Item -Type Directory -Path $pathDeploy -ErrorAction SilentlyContinue | Out-Null																		 
			Remove-Item -Recurse -Force -Path ($pathDeploy + "\log\*") -ErrorAction SilentlyContinue | Out-Null	
			Remove-Item -Recurse -Force -Path ($pathDeploy + "\logs\*") -ErrorAction SilentlyContinue | Out-Null	
		}
	}
	
	
	######### INSTALLER
	if ($cfgBuildInstaller -and $buildConfiguration -eq "Release") {
		Write-Host "msbuild for Installer ..."
		cd $msBuildDir
		.\msbuild.exe (Join-Path $pathBase "$appName.sln") /t:Installer:rebuild /p:Configuration="Release" /p:BuildProjectReferences=false -verbosity:minimal
		if ($cfgDeploy) {
			Write-Host "Copy version.json ..."
			Copy-Item -Path (Join-Path $pathInstallerPayload "version.json") -Destination $pathDeploy -Force -ErrorAction SilentlyContinue | Out-Null
		}
	}
	
	
	## Remove lock
	cd $pathBase
	if ((Test-Path -Path "build.lck")) {
		Remove-Item -Path "build.lck"
	}

	Write-Host "SUCCESS: Build complete!"
	exit 0
}
catch {
	Write-Host "FAILED: Exception in BuildApp.ps1!"
	cd $pathBase
	Remove-Item -Path "build.lck"
	exit -1
}