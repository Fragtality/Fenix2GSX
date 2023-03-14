$projdir = "C:\Users\Fragtality\source\repos\Fenix2GSX"
$outputdir = $projdir + "\Releases\output"
$bindir = $projdir + "\Fenix2GSX\bin\Release\net7.0-windows"
$userdir = "F:\MSFS2020\Fenix2GSX"

cd ($projdir + "\Fenix2GSX")
dotnet publish -p:PublishProfile=FolderProfile

Copy-Item -Path ($bindir + "\Microsoft.FlightSimulator.SimConnect.dll") -Destination $outputdir -Force
Copy-Item -Path ($bindir + "\SimConnect.dll") -Destination $outputdir -Force
Remove-Item -Path ($outputdir + "\runtimes\*") -Force -Recurse
Copy-Item -Path ($bindir + "\runtimes\win-x64") -Destination ($outputdir + "\runtimes") -Force -Recurse

#Copy-Item -Path ($bindir + "\runtimes\win-x64\lib\netcoreapp3.1\CefSharp.Core.Runtime.dll") -Destination $outputdir -Force

#Remove-Item -Path ($outputdir + "\locales\*") -Force -Exclude "en-GB.pak","en-US.pak"

Copy-Item -Path ($outputdir + "\*") -Destination $userdir -Force -Recurse