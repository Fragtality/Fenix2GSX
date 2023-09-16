# Fenix2GSX
<img src="img/icon.png" width="196"><br/>
Full and proper GSX Integration and Automation for the Fenix A320! <br/>

- The **Refuel Service is filling the Tanks** as planned (or more correctly GSX and Fenix are "synched")
- Calling **Boarding load's Passengers and Cargo**, as does Deboarding for unloading (or more correctly GSX and Fenix are "synched")
- **Ground Equipment** (GPU, Chocks, PCA) is automatically set or removed
- All **Service Calls** except Push-Back, De-Ice and Gate-Selection **can be automated**
- **GSX Audio** can be controlled via the **INT-Knob** from the ACP in the Cockpit
- **ATC Volume** can be controlled via the **VHF1-Knob** from the ACP in Cockpit (or any other App you wish)
- The other Audio-Channels on the ACP can also be used to control the Volume of even more Apps

<br/><br/>

## Requirements
- Windows 10/11, MSFS, Fenix :wink:
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) x64 Runtime (.NET Runtime and .NET Desktop Runtime. Do not confuse it with arm64!) installed & updated.<br/>Reboot when installing the Runtimes for the first Time.
- MobiFlight [WASM Module](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) installed in your Community Folder.<br/>
Ensure your Archive-Tool does not create an unnecessary Subfolder - the Path to the layout.json should be as follows: `Community\mobiflight-event-module\layout.json`
- A properly working GSX Installation

<br/>

[Download here](https://github.com/Fragtality/Fenix2GSX/releases/latest)

(Under Assests, the Fenix2GSX... zip-File)


<br/><br/>
## Installation / Update
Extract it anywhere you want, but **do not use** Application-Folders, User-Folders, the Windows' Program File Folders or even C:\\ <br/><br/>
Please **remove the old Version** completely before updating / extracting the new Version (don't just overwrite)!<br/>Keep in Mind that this **resets all Settings** to the Defaults. (You can keep your old .config, but only as Reference - *do not replace* the newer with an old one)<br/>
The Program will display a **little Exclamation Mark** on the SysTray/Notification Area Icon and in the GUI to inform on a **new Version**. There is no Pop-Up and there will never be.<br/>
It may be blocked by **Windows Security** or your **AV-Scanner**, try if *unblocking* and/or *setting an Exception* helps.<br/><br/><br/>

## Auto-Start
The Tool can be automatically started by the usual Ways out there.<br/>
If you wish to do so - it is not needed, you can also just place a Shortcut to Binary somewhere and start it manually! (When starting it manually, do so either **before MSFS** or when MSFS is in the **Main Menu**)<br/>

### FSUIPC
If you own a registered Copy of FSUIPC, you can start it automatically through that. Add this to your FSUIPC7.ini:
```
[Programs]
RunIf1=READY,KILL,X:\PATH\YOU\USED\Fenix2GSX.exe
```
The ini-File is in the Folder where FSUIPC was installed to, remember to change the Path to the Binary. If there are multiple RunIf-Entries, make sure they are numbered uniquely and that the [Programs] Section only exists once.<br/>

### exe.xml
You can also start it via the Simulator's exe.xml, but make sure to set the Working Directory correctly like so:
```xml
  <Launch.Addon>
    <Name>Fenix2GSX</Name>
    <Disabled>false</Disabled>
    <Path>X:\PATH\YOU\USED\Fenix2GSX.exe</Path>
    <CommandLine>-path "X:\PATH\YOU\USED"</CommandLine> 
  </Launch.Addon>
```

### Addon Linker
If you use Addon Linker to start your Addons/Tools, you can also add it there:<br/>
**Program to launch** X:\PATH\YOU\USED\Fenix2GSX.exe<br/>
**Optional arguments** -path "X:\PATH\YOU\USED"<br/>
**Wait for simconnect** checked<br/>
The Rest can be left at Default.<br/>

### Axis and Ohs
You can also use AAO to autostart Fenix2GSX if you have that.<br/>
Configure it as *One Shot* / *Beginning* with a Delay of *10000ms*. The Content of the Script is:<br/>
`(EXEC:X:\PATH\YOU\USED\Fenix2GSX.exe)`

### Custom Scripts
You can also start it via Script, but make sure you set the **Working Directory** correctly!<br/>
Either you change the Directory (cd) to the Fenix2GSX Folder and start the Binary:
```batch
cd X:\PATH\YOU\USED
Fenix2GSX.exe
```
Note that it is .\Fenix2GSX.exe when using a Powershell Script.<br/><br/>
Or you pass the Working Directory with the -path Command Line Switch:<br/>
`start "" "X:\PATH\YOU\USED\Fenix2GSX.exe" -path "X:\PATH\YOU\USED\Fenix2GSX"`

<br/><br/>

## Configuration

### Fenix

Disable **Auto-Door** and **Auto-Jetway** Simulation in the EFB!<br/>
Make sure your **Default State** is set to either Cold & Dark or Turn-Around with APU. GSX won't provide any Services when Engines are running and Fenix2GSX will directly go to the Taxi-Out State (=does not provide any Departure Services).

<br/>

### GSX Pro

- Make sure you do not have a customized Aircraft Config (GSX In-Game Menu -> Customize Aircraft -> should show only "Internal GSX Database"). If you want to keep your customized Config for whatever Reason, make sure the Option **"Show MSFS Fuel and Cargo during refueling"** is disabled!
- If using any Automation Option from Fenix2GSX, make sure **"Assistance services Auto Mode"** is disabled in the GSX Settings (GSX In-Game Menu -> GSX Settings -> Simulation)
- If you have troubles with Refueling, try if disabling "Always refuel progressively" and "Detect custom aircraft system refueling" in the GSX Settings helps. (Though it should work with these Settings)
- Please ensure you have entered your **SimBrief Username** and have **Ignore Time** checked. Some Users have reported they also need to disable *Estimate passengers number*.
- For **Automated staircases** semi-automatic (half-checked) is recommended - but it should work with all Modes
- The De-/Boarding Speed of Passengers is dependant on the Passenger Density Setting (GSX In-Game Menu -> GSX Settings -> Timings). Higher Density => faster Boarding. *BUT*: The Setting **Extreme** is too extreme! Boarding does not work with this Setting.
- Ensure the other two Settings under Timings are on their Default (15s, 1x).
- As with GSX itself, Fenix2GSX runs best when you have a proper Airport Profile installed!
- Up to everyone's *Preference*, but disabling the **Aural Cues** (GSX In-Game Menu -> GSX Settings -> Audio) and setting **Message verbosity** to "*only Important*" (GSX In-Game Menu -> GSX Settings -> Simulation) can improve Immersion! 😉

<br/>

### Fenix2GSX

The Configuration is done through the **GUI**, open it by **clicking on the System-Tray/Notification-Icon**. The UI does not open from itself! The Settings are stored persistently in the *Fenix2GSX.dll.config* File - so set them once to your Preference and you should be fine :smiley:<br/>
All Options have ToolTips which explains them further. When changing/adding something in the Text-Boxes: please either hit *Enter* or click in another Text-Box (so the one you changed loses the Input-Focus).<br/>
You can close the Windows/UI without Problems, Fenix2GSX will continue to run. The UI is only there for Configuration, you don't need to have it open to have Fenix2GSX doing its Work.
<br/><br/>
<img src="img/ui2.png" width="400"><br/><br/>
All Settings **can be changed dynamically** on the Fly if needed. But do that **before a Service/Feature** starts or **after** it has ended. For example, don't disable "Automatic Jetway/Stair Operation" while the Jetway is connected. Do it before the Tool calls the Jetway or after it was disconnected by the Tool.<br/><br/>
In general, it is up to **your Preference how much Automation** you want. If you want to keep Control of when Services are Called and/or the Jetway is connected, you **can still enjoy the (De-)Boarding and Refueling Syncronization** when the Automation-Options are disabled. The only Automation which **can not be disabled**: The **Removal of the Ground-Equipment and Jetway-Disconnection** (if still connected) is always active on Depature.<br/><br/>
A Note on the **Audio-Control**: The Tool does not control Audio until the Plane is **powered** (=FCU is On). Be aware, that the Fenix defaults to 50% Volume on INT and VHF1 when loaded - that is why Fenix2GSX defaults to set them to 100% on Startup. You can disable that if you want.<br/>When you end your Session, Fenix2GSX will try to reset the Application-Audio to unmuted and last set Volume (before it started controlling the Volume). But that does not really work on GSX because it is resetting at the same Time. So **GSX can stay muted** when switching to another Plane (if it was muted) - keep that in Mind.<br/><br/>
The new **"Advanced Audio Control"** can be used to Control other Apps with the other Channels (VHF2, VHF3, HF1, HF2, CAB, PA) - one App, one Channel. Their knobs are not moved on Start-Up and the Record Latch always mutes/unmutes the App. But that Settings follow the configured Seat-Position and therefore which ACP is used for Audio-Control.
<br/><br/>
Advanced Options not available in the GUI - can be changed in *Fenix2GSX.dll.config* (Fenix2GSX restart needed. Don't touch any other Option):

- **logLevel**: When I request a "Verbose-Log" on Support-Requests, change the Value to "Verbose" here
- **ignoreAudioDevice**: Ignore a certain Sound Device when searching for Audio-Sessions for Volume-Control (Device Name as seen in your Windows Volume Control / Device List). Certain Sound Cards / Applications (e.g. Sonic Studio Virtual Mixer) mirror the Sound-Sessions to a virtual Device which messes up the Volume-Control Feature.
- **autoConnectDelay**: Delay in Seconds before Jetway/Stairs are called on Session Start (only there). Delay can be cancelled with the INT/RAD Switch.
- **ignorePaxSafeguard**: Boarding and Deboarding have Safeguard that ignores any Change greater 15 Passengers. Set this only to "true" if you have Problems with Boarding/Deboarding (that is, the Plane is not fully boarded or deboarded).
- **finalDelayMin**: Minimum Delay in Seconds before the Final LS is transmitted after Boarding.
- **finalDelayMax**: Maximum Delay in Seconds before the Final LS is transmitted after Boarding.
- **gpuAndChockInterception**: Intercept (and remove) the automatic placed GPU and Chocks (by Fenix) upon Arrival (so they do not appear before the Beacon is off and Jetway/Stair connected).
- **removeAftStairAfterBoard**: Automatically remove the Aft Stair (if available) on Jetway-Stands after Boarding was finished.

<br/><br/>

## Usage

### General Service Flow
Note that Fenix2GSX **does not open a Window** when started - it is designed to run in the Background, the Window/GUI is only there for Configuration! There is no Need to have the GUI opened while running the Sim. When you do open the GUI, it can safely be closed (it only closes that Windows, it does not Stop the Binary/App itself)<br/>

1) Create your SB Flightplan and start MSFS as you normally would. Make sure you use the **correct SimBrief Airframe** Configuration provided by Fenix.
2) If not Auto-Starting it, start the Tool **before MSFS** or when MSFS is in the **Main Menu**.
3) Ensure the Fenix default State is either **Cold & Dark** or **Turn-Around with APU**!
5) When your Session is loaded (Ready to Fly was pressed), **wait for the Repositioning**, Jetway/Stair Call and Ground-Equipment Placement to happen (if configured). When the Tool is done with its Preparation, it will trigger the **"Ding" Sound** (the Sound when you press the ALL Call Button)<br/>Make sure to **wait for that Signal** before powering the Plane and import the Flightplan.
6) **Import your Flightplan** on the EFB (wherever you're using it from, does not need to be the EFB in the VC). **Refueling and Catering** will be called (if configured).<br/>*Always* import a Flightplan on the EFB, regardless of Configuration. Power up the Plane from Cold & Dark before importing the Flightplan!
7) When **Refueling and Boarding are finished** (whoever called it), you will receive your **Final Loadsheet after 90-150s** (Boarding finished = GSX reports the Service as completed). The **Doors** will be **closed** when this happens (if not already closed by GSX) and also the **"Ding" Sound** is triggered.<br/>
When both Services are finished and if the APU is Avail and the APU Bleed is switched ON, then Fenix2GSX will remove the PCA (if configured to connect).
8) When the **Parking Brake is set**, **External Power is disconnected** (on the Overhead) and **Beacon Light is On**, the Tool will **remove all Ground-Equipment**: Jetway/Stairs (if not already removed) and GPU, PCA & Chocks (always, to be safe).<br/>Ground-Equipment is **also removed when Push-Back is** called - make sure you have set you're Parking Brake and your APU is running :wink:
9) Happy Flight! Note that Fenix2GSX can be restarted in Flight if you need/wish to do so.
10) When you arrive (**pre-select your Gate**), the **Jetway/Stairs will automatically connect** as soon as the **Engines are Off** and the **Parking Brake is set** (if configured).
11) When the **Beacon Light is off**, the other **Ground-Equipment will placed**: GPU, PCA (if configured) and Chocks (it may be placed earlier, I can't change the Behavior of the Fenix). If configured, **Deboarding will be called**. Calling **Deboarding in the EFB** is not required, it is best to **dismiss that**. Only generate a new Flightplan in SimBrief until **after Deboarding is already running**!
12) It works with Turn-Arounds! As soon as you (re)import a new Flightplan the Cycle starts over (**after Deboarding has completely finished**).

<br/>

NOTE: You can also use the **INT/RAD** Switch on the ACP to trigger some Services in certain Situations. Move the Switch to the **INT Position** and **leave it there**. When Fenix2GSX reacts to the Request it will reset the Switch Postion as Confirmation! Services triggerable:

- **Request Boarding** - regardless if Auto-Boarding is configured and even when Catering & Refueling are still active.
- **Request Push-Back**, after Refueling & Boarding are finished (regardless of who called them). Be aware that calling Push-Back also triggers the removal of Ground-Equipment. And removing the Equipment that Way does not care about the Position of the Parking-Brake, External Power or APU. So nobody holds you back sitting in a free-rolling and unpowered Plane, possibly injuring the Ground-Crew because they unplugged the Cable while being actively used 😅<br/>
Make sure to **enable the GSX Menu** again at that Point! Fenix2GSX only calls the Service, but does not answer any Push-Back related Question in the GSX Menu.
- **Abort/Confirm Push-Back**. While the the Push-Back is running you can abort it (that is Menu Option 1, "Stop here and complete pushback" - the correct Way of aborting the Push). When you have "Good engine start confirmation" enabled in GSX you can answer that GSX Question also with that Switch (after GSX asks you to do so).<br/>
Be aware that this only works if you have "Synchronize By-Pass Pin" enabled.
- **Request Deboarding**, after Parking Brake set, Engines off and Beacon off. If Automatic Jetway/Stair Operation is enabled, wait for them to be called. Works regardless if Auto-Deboarding is enabled (so you can force the Deboard Request if needed)
<br/>

### Addon NOTAMs
There also Issues reported when used together with **Self-Loading Cargo** (SLC). Based on User Reports disabling *"Ground Crew"* in SLC can help! Everything else is mostly SLC missing a proper GSX Integration - so nothing I can solve.<br/><br/>

**FS2Crew (Fenix Edition)**: You basically don't need any Ground- or Door-Handling Features of Fs2Crew. This is what another User recommends as Settings to let Fenix2GSX and FS2Crew work together (thanks for sharing):<br/>
<img src="img/Fs2Crew.png" width="1006"><br/><br/>

<br/>

### Usage Tips / NOTAMs

If you set every Option for **automatic Service Calls**, I'd recommend to **disable the GSX Menu in the Toolbar** (= Icon not white). The Services are still called, but you **won't see the Menu popping-up**.<br/>
So **Push-Back**, **De-Ice** and **Gate-Selection** are the only Situations where you need to **enable the Menu** again (= Icon white. You can close the Menu with "X").<br/><br/>
Be aware that Fenix2GSX automatically selectes the **first Operator** in the List if GSX asks for a Selection (Ground Handling, Catering). If you're picky about which Operator should appear, you have to disable the Automatic Jetway Operation and the Automatic Catering Call!<br/><br/>
Be cautious on the Mass and Balance Page in the EFB: **Don't change the planned Numbers and** and **don't use "Load Aircraft"** and **don't use "Reset All"** - they likely break the Integration (Using **"Resend Loadsheet" is fine** though if needed)!<br/>
That is **IF** you want to Refuel/Board through GSX: When you are in Hurry and want to *skip GSX Refuel/Boarding* but still want to use the other Features, you can use the Fast/Instant Boarding on the EFB. To do so, please *disable* the Option to Call Refuel on EFB import and only then import the Flightplan. The Rest then works the same as before, after the Plane was boarded through the EFB.<br/><br/>
Tip for **VATSIM / IVAO**: Disable the automatic Jetway Operation before loading the Session in MSFS, in Case you need to move to another Gate. If the Gate is free (or you have moved to a free one) you can renable Auto-Connect and the Jetway/Stairs will still connect then (unless the Flightplan was already loaded in the EFB).<br/><br/>
Fenix2GSX has a Detection built-in when starting with **Engines running** (i.e. you selected a Runway as Start). It won't provide any Departure Service apart from removing the Ground-Equipment. It will provide Arrival-Sevices after you've landed again (when you have imported a FlightPlan in the EFB before Take-Off)<br/><br/>

<br/>

### Troubleshooting / FCOM

First Order: Ensure you have fully read and understand the Readme 😉<br/>
If that and any of the Instructions below did not help, please open an Issue with a meaningful Problem Description and attach the Logs to it (Fenix2GSX\<date>.log)!<br/>
I also tend to hang out in the GSX Community of Fenix Discord "from time to time" - you can also ping me there!

<br/>

#### Does not Start

- It does not open a Window if you expect that. The GUI is only needed for Configuration and can be opened by clicking on the Icon in the SysTray / Notification Area (these Icons beside your Clock)
- Ensure you have .NET 7 Installed as described in [Requirements](#requirements) (Reboot!)
- Check if the .NET Runtimes are correctly installed by running the Command `dotnet --list-runtimes` - the .NET Version you downloaded should show up there.
- Please just don't "run as Admin" because you think that is needed. You can try if that helps, but it should run just fine without that!
- Certain AV/Security Software might require setting an Exception

<br/>

#### There are no Log Files

- Regardless of how you start it, please ensure the Working Directory will be set to the Directory where Fenix2GSX is installed - see [Auto-Start](#auto-start). If that is something you can't follow: just make a Shortcut to the Binary on your Desktop (or wherever you placed your Sim and Tools Shortcuts) and run it manually
- Please just don't "run as Admin" because you think that is needed. You can try if that helps, but it should run just fine without that!

<br/>

#### Does not Connect / Exception 31

The MobiFlight WASM Module is not installed correctly or at all:

- Ensure you have downloaded the actual Module (Binary) and not the Source-Code! Go to [Releases](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) and download the File mobiflight-event-module-x.y.z.zip (under Assets)
- Ensure you have unpacked it correctly (or at all). Check the Path as recommended under [Requirements](#requirements). In the End it is installed like any other 3rd Party Scenery in your Community Folder

<br/>

#### Refuel Stops / Problems with Boarding / Jetways do not connect

If it is only Boarding (e.g. only 10 Pax boarded) it might be something with your GSX Settings (In Game Menu -> Settings):
- Ensure your *SimBrief Username* is set
- Ensure *Ignore Time* is set
- Try if disabling (or enabling) *Estimate passengers number* helps anything

<br/>

There have been also Cases where the GSX Installation was somehow "corrupted". You can try to run the Check in the FSDT Installer multiple Times, else a complete fresh / clean Installation of GSX might be required.

<br/>

Else it is likely that you have Issues with the SimConnect Interface (the API which both GSX and Fenix2GSX use) being overloaded by too many SimObjects (one of these Things Asobo is incapable or unwilling of fixing).<br/>
In most Cases this is caused by AI Aircraft or other Tools spawning SimObjects (e.g. Nool VDGS or even GSX itself). Reduce the Number of SimObjects and check if it works better then:

- If only tried while connecting to an Online Network, try if it works in an Offline Session
- Disable Traffic in the MSFS Settings (Airport Vehicle / Ground Aircraft / Worker)
- Reduce the amount of AI Planes in your Tool's Settings
- External AI Tools might have the Option to spawn Ground Services for AI Aircraft (AIG-TC e.g.) - be sure to disable that!
- Disable AI Traffic all together - whether it be MSFS or an external Tool
- Disable "Ground Clutter" in GSX (FSDT Installer -> Config)
- Disable other Addons spawning SimObjects

