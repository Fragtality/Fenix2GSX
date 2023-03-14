# Fenix2GSX
<br/>

## Requirements
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) x64 Runtime (Core + Desktop) installed & updated
- MobiFlight [WASM Module](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) installed
- MSFS, Fenix, GSX Pro :wink:

## Installation
Extract it anywhere you want, but do not use Application-Folders, User-Folders or even C:\<br/>
It may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps.

## Configuration
Configure GSX to use Ctrl+Shift+F12 as the Menu Hotkey!<br/>

Change the Configuration to your Needs, all the Variables are in the File "Fenix2GSX.dll.config":
* **waitForConnect**		- The Binary will wait until MSFS is started and SimConnect is available. Set it to true, if you want to start the Binary before MSFS. Else start it in the Main Menu.
* **gsxVolumeControl**			- The GSX Volume is controlled via the INT Knob on ACP1.
* **disableCrew**		- Disable Crew boarding and deboarding.
* **repositionPlane**			- The Plane will be repositioned via GSX when you start your Session.
* **autoConnect**		- Automatically connect Jetway/Stairs on Startup and on Arrival.
* **connectPCA**" 		-  The Preconditioned Air will be connected (and disconnected) on Startup and on Arrival.
* **autoRefuel**		- Call Refueling automatically as soon as an Flightplan was imported on the EFB.
* **callCatering**	- Catering will be called when Refueling is called.
* **autoBoarding**" 		-  Automatically start Boarding when Refueling and Catering (if configured) are finished.
* **autoDeboarding**" 			-  Automaticall start Deboarding on Arrival.
* **refuelRateKGS**" 			-  The Speed at which the Tanks are filled, defaults to 15kg per Second.

## Usage
1) Create your SB Flightplan and start MSFS as you normally would. Depending on your Configuration, start the Tool before MSFS or when MSFS is in the Main Menu.
2) When your Session is loaded (Ready to Fly was pressed), wait for the Repositioning and Jetway/Stair Call to happen (if configured)
3) Import your Flightplan on the EFB (wherever you're using it from, does not need to be the EFB in the VC). Refueling and Catering will be called (if configured).
4) When Refueling and Boarding are finished (whoever called it), you will receive your Final Loadsheet after 90-150s. The left Forward Door will be closed when this happens.
5) When Parking Brake is set, External Power disconnected (on the Overhead) and Beacon Light is On, the Tool will remove all Ground-Equipment: Jetway is disconnected, GPU and PCA (if configured) are removed, Chocks are removed.
6) Happy Flight!
7) When you arrive (on your preselected Gate), the Jetway/Stairs will automatically connect when the Engines are Off and the Parking Brake is set (if configured).
8) When the Beacon Light is off, the other Ground-Equipment will placed: GPU, PCA (if configured) and Chocks. If configured, Deboarding will be called.
9) It works with Turn-Arrounds! As soon as you import a new Flightplan it will start over.

Important: make sure GSX is active, else the Tool will not work! (The GSX Logo in the Menu is white and GSX reacts to the Hotkey)
