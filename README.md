# Fenix2GSX
Full GSX Integration and Automation for the Fenix A320!
<br/><br/>

## Requirements
- Windows 10/11
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) x64 Runtime (Core + Desktop) installed & updated
- MobiFlight [WASM Module](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) installed
- MSFS, Fenix, GSX Pro :wink:

<br/><br/>
## Installation
Extract it anywhere you want, but do not use Application-Folders, User-Folders or even C:\\ <br/>
It may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps.

<br/><br/>
## Configuration
**Fenix**: Disable **Auto-Door** and **Auto-Jetway** Simulation in the EFB!<br/><br/>
**Fenix2GSX**: Change the Configuration to your Needs, depending on how much Automation you want. The Settings are available in the GUI, which is opened by double-clicking the SystemTray Icon of that Tool. But you can also change the Settings in the Fenix2GSX.dll.config File:
* **waitForConnect**		- The Binary will wait until MSFS is started and SimConnect is available. Set it to true, if you want to start the Binary before MSFS. Else start it in the Main Menu.
* **gsxVolumeControl**			- The GSX Volume is controlled via the INT Knob on ACP1.
* **disableCrew**		- Disable Crew boarding and deboarding.
* **repositionPlane**			- The Plane will be repositioned via GSX when you start your Session.
* **autoConnect**		- Automatically connect Jetway/Stairs on Startup and on Arrival.
* **connectPCA**" 		-  The Preconditioned Air will be connected (and disconnected) on Startup and on Arrival.
* **pcaOnlyJetway**" 		-  The Preconditioned Air only connected on Jetways.
* **autoRefuel**		- Call Refueling automatically as soon as an Flightplan was imported on the EFB.
* **callCatering**	- Catering will be called when Refueling is called.
* **autoBoarding**" 		-  Automatically start Boarding when Refueling and Catering (if configured) are finished.
* **autoDeboarding**" 			-  Automaticall start Deboarding on Arrival.
* **refuelRateKGS**" 			-  The Speed at which the Tanks are filled, defaults to 15kg per Second.

<br/><br/>
## Usage
1) Create your SB Flightplan and start MSFS as you normally would. Depending on your Configuration, start the Tool before MSFS or when MSFS is in the Main Menu.
2) When your Session is loaded (Ready to Fly was pressed), wait for the Repositioning and Jetway/Stair Call to happen (if configured).
3) Import your Flightplan on the EFB (wherever you're using it from, does not need to be the EFB in the VC). Refueling and Catering will be called (if configured).
4) When Refueling and Boarding are finished (whoever called it), you will receive your Final Loadsheet after 90-150s. The left Forward Door will be closed when this happens (if not already closed by GSX).
5) When Parking Brake is set, External Power disconnected (on the Overhead) and Beacon Light is On, the Tool will remove all Ground-Equipment: Jetway is disconnected, GPU and PCA (if configured) are removed, Chocks are removed.
6) Happy Flight!
7) When you arrive (on your preselected Gate), the Jetway/Stairs will automatically connect when the Engines are Off and the Parking Brake is set (if configured).
8) When the Beacon Light is off, the other Ground-Equipment will placed: GPU, PCA (if configured) and Chocks. If configured, Deboarding will be called. Calling Deboarding in the EFB is not required, you can dismiss it if you want.
9) It works with Turn-Arounds! As soon as you (re)import a new Flightplan it will start over.
<br/>
If you set every Option for automatic Service Calls, you can also disable GSX in the Toolbar. The Services are still called, but you won't see the Menu. You should open it for Pushback though :sweat_smile:
