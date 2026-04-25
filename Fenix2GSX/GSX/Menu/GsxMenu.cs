using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.AppConfig;
using FenixInterface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Menu
{
    public enum GsxMenuState
    {
        UNKNOWN = 0,
        READY = 1,
        HIDE = 2,
        TIMEOUT = 3,
        DISABLED = 4,
        NUM5ALIVE = 5,
        SIXFEETUNDER = 6,
        LUCKYSEVEN = 7
    }

    public class GsxMenu(GsxController gsxController)
    {
        public virtual GsxController Controller { get; } = gsxController;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual Config Config => Controller.Config;
        protected virtual AircraftProfile AircraftProfile => Controller.AircraftProfile;
        public virtual string PathMenu { get { return Path.Join(Controller.PathInstallation, GsxConstants.RelativePathMenu); } }

        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual GsxMenuState MenuState { get; protected set; } = GsxMenuState.DISABLED;
        public virtual string MenuTitle { get; protected set; }
        public virtual int MenuLineCount { get { return MenuLines.Count; } }
        public virtual List<string> MenuLines { get; } = [];

        public virtual bool ReadyReceived { get; protected set; } = false;
        public virtual bool FirstReadyReceived { get; protected set; } = false;
        public virtual bool IsOpeningMenu { get; protected set; } = false;
        protected virtual bool IsSequenceActive { get; set; } = false;
        protected virtual bool IsToolbarEnabled { get; set; } = true;
        protected virtual bool WasOperatorSelected { get; set; } = false;
        public virtual bool IsGateMenu => MatchTitle(GsxConstants.MenuGate);
        public virtual bool IsOperatorMenu => MatchTitle(GsxConstants.MenuOperatorHandling) || MatchTitle(GsxConstants.MenuOperatorCater);
        protected virtual ConcurrentDictionary<string, Func<GsxMenu, Task>> MenuCallbacks { get; } = [];

        protected virtual ISimResourceSubscription SubMenuEvent { get; set; }
        protected virtual ISimResourceSubscription SubMenuOpen { get; set; }
        protected virtual ISimResourceSubscription SubMenuChoice { get; set; }

        protected virtual double LastMenuSelection { get; set; } = -2;
        public virtual bool WaitingForGate { get; protected set; } = false;
        public virtual bool WarpedToGate { get; protected set; } = false;
        protected virtual bool DeIceQuestionAnswered { get; set; } = false;
        protected virtual bool FollowMeAnswered { get; set; } = false;
        public virtual bool SuppressMenuRefresh { get; set; } = false;

        public event Action<string> MenuTitleChanged;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubMenuEvent = Controller.SimStore.AddEvent(GsxConstants.EventMenu);
                SubMenuEvent?.OnReceived += OnMenuEvent;
                SubMenuOpen = Controller.SimStore.AddVariable(GsxConstants.VarMenuOpen);
                SubMenuChoice = Controller.SimStore.AddVariable(GsxConstants.VarMenuChoice);
                SubMenuChoice?.OnReceived += OnMenuSelection;

                Controller.MessageService.Subscribe<MsgGsxCouatlStopped>(OnCouatlStopped);

                MenuCallbacks.Add(GsxConstants.MenuTugAttach, OnTugQuestion);
                MenuCallbacks.Add(GsxConstants.MenuPushbackRequest, OnPushRequestQuestion);
                MenuCallbacks.Add(GsxConstants.MenuFollowMe, OnFollowMeQuestion);
                MenuCallbacks.Add(GsxConstants.MenuDeiceOnPush, OnDeiceQuestion);
                MenuCallbacks.Add(GsxConstants.MenuParkingChange, OnParking);
                MenuCallbacks.Add(GsxConstants.MenuParkingSelect, OnParking);
                MenuCallbacks.Add(GsxConstants.MenuBoardCrew, OnBoardCrew);
                MenuCallbacks.Add(GsxConstants.MenuDeboardCrew, OnDeboardCrew);
                MenuCallbacks.Add(GsxConstants.MenuOperatorHandling, OnOperatorSelection);
                MenuCallbacks.Add(GsxConstants.MenuOperatorCater, OnOperatorSelection);

                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            Controller.MessageService.Unsubscribe<MsgGsxCouatlStopped>(OnCouatlStopped);

            Controller.SimStore.Remove(GsxConstants.EventMenu)?.OnReceived -= OnMenuEvent;
            Controller.SimStore.Remove(GsxConstants.VarMenuOpen);
            SubMenuChoice?.OnReceived -= OnMenuSelection;
            Controller.SimStore.Remove(GsxConstants.VarMenuChoice);

            IsInitialized = false;
        }

        protected virtual Task OnCouatlStopped()
        {
            ReadyReceived = false;
            return Task.CompletedTask;
        }

        public virtual void Reset()
        {
            LastMenuSelection = -2;
            MenuTitle = "";
            MenuLines.Clear();
            MenuState = GsxMenuState.UNKNOWN;
            FirstReadyReceived = false;
            ReadyReceived = false;
            DeIceQuestionAnswered = false;
            FollowMeAnswered = false;
            IsOpeningMenu = false;
            IsSequenceActive = false;
            IsToolbarEnabled = true;
            WaitingForGate = false;
            WarpedToGate = false;
        }

        public virtual void ResetFlight()
        {
            DeIceQuestionAnswered = false;
            FollowMeAnswered = false;
            WaitingForGate = false;
            WarpedToGate = false;
        }

        public virtual void AddMenuCallback(string title, Func<GsxMenu, Task> callback)
        {
            MenuCallbacks.TryAdd(title, callback);
        }

        public virtual void RemoveMenuCallback(string title)
        {
            MenuCallbacks.TryRemove(title, out _);
        }

        public virtual bool MatchTitle(string match)
        {
            return MenuTitle?.StartsWith(match, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        protected virtual async Task OnPushRequestQuestion(GsxMenu menu)
        {
            Logger.Debug($"Request Pushback Question active");
            await Select(1);
        }

        protected virtual async Task OnTugQuestion(GsxMenu menu)
        {
            Logger.Debug($"Tug Question active");
            if (AircraftProfile.AttachTugDuringBoarding == 2)
                await Select(1);
            else if (AircraftProfile.AttachTugDuringBoarding == 1)
                await Select(2);

            if (AircraftProfile.SkipCrewQuestion && AircraftProfile.AttachTugDuringBoarding != 0)
                SuppressMenuRefresh = false;
            else if (AircraftProfile.AttachTugDuringBoarding == 0)
                SuppressMenuRefresh = true;
        }

        protected virtual async Task OnFollowMeQuestion(GsxMenu menu)
        {
            Logger.Debug($"FollowMe Question active");
            if (AircraftProfile.SkipFollowMe && !FollowMeAnswered)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(GsxMenuCommand.Select(2, GsxConstants.MenuFollowMe));
                sequence.Commands.Add(GsxMenuCommand.Wait());
                sequence.Commands.Add(GsxMenuCommand.Operator());
                FollowMeAnswered = await RunSequence(sequence);
            }
        }

        protected virtual async Task OnDeiceQuestion(GsxMenu menu)
        {
            Logger.Debug($"DeIce Question active");
            if (AircraftProfile.KeepDirectionMenuOpen && DeIceQuestionAnswered)
                await Select(2);
        }

        protected virtual async Task OnParking(GsxMenu menu)
        {
            Logger.Debug($"Change/Select Parking active");
            FollowMeAnswered = false;

            if ((MatchTitle(GsxConstants.MenuParkingChange) || MatchTitle(GsxConstants.MenuParkingSelect)) && Controller.AutomationController.State < AutomationState.Departure)
            {
                Logger.Information($"App waiting for Gate to be selected");
                WaitingForGate = true;
            }
        }

        protected virtual async Task OnBoardCrew(GsxMenu menu)
        {
            Logger.Debug($"Board Crew Question active");
            if (AircraftProfile.SkipCrewQuestion)
            {
                await Select(1);
                SuppressMenuRefresh = false;
            }
        }

        protected virtual async Task OnDeboardCrew(GsxMenu menu)
        {
            Logger.Debug($"Deboard Crew Question active");
            if (AircraftProfile.SkipCrewQuestion)
            {
                await Select(1);
                SuppressMenuRefresh = false;
            }
        }

        protected virtual async Task OnOperatorSelection(GsxMenu menu)
        {
            Logger.Debug($"Operator Question active");
            if (!AircraftProfile.OperatorAutoSelect)
                return;

            var gsxOperator = GsxOperator.OperatorSelection(AircraftProfile, MenuLines);
            if (gsxOperator != null)
            {
                Logger.Information($"Selecting Operator '{gsxOperator.Title}' (GSX Choice: {gsxOperator.GsxChoice})");
                await Select(gsxOperator.Number);
            }
            else
            {
                Logger.Warning($"Selecting Operator #1 - no Matches found");
                await Select(1);
            }
        }

        protected virtual Task OnMenuSelection(ISimResourceSubscription sub, object value)
        {
            double num = sub.GetNumber();
            if (num != -2)
            {
                if (MatchTitle(GsxConstants.MenuDeiceOnPush))
                {
                    Logger.Debug($"Deice Question was answered: {num + 1}");
                    DeIceQuestionAnswered = true;
                }
                else if (MatchTitle(GsxConstants.MenuParkingChange) && WaitingForGate && num == 4)
                {
                    Logger.Debug($"Warped to Gate - trigger Menu Refresh");
                    WaitingForGate = false;
                    WarpedToGate = true;
                    Task.Delay(2500, RequestToken).ContinueWith((_) => Open());
                }
                else if (IsOperatorMenu)
                {
                    if (!AircraftProfile.OperatorAutoSelect)
                        Logger.Information($"Manual Operator Selection: {num + 1} - '{MenuLines[(int)(num + 1)]}'");
                    WasOperatorSelected = true;
                }

                ReadyReceived = false;
                LastMenuSelection = num;
                Logger.Verbose($"Menu Selection {LastMenuSelection}");
            }

            return Task.CompletedTask;
        }

        protected virtual async Task OnMenuEvent(ISimResourceSubscription sub, object value)
        {
            try
            {
                var state = sub.GetValue<int>();
                if (!Controller.IsActive || state > 4)
                    return;

                MenuState = (GsxMenuState)state;
                Logger.Debug($"Received Menu Event: {MenuState}");

                if (!FirstReadyReceived && MenuState == GsxMenuState.READY)
                {
                    Logger.Debug($"First Menu Ready received");
                    FirstReadyReceived = true;
                }

                bool menuChanged = false;
                if (MenuState == GsxMenuState.READY)
                {
                    menuChanged = await UpdateMenu();

                    if (!IsOpeningMenu && !IsSequenceActive)
                    {
                        Logger.Debug($"Toolbar enabled by User");
                        IsToolbarEnabled = true;
                    }

                    if (IsGateMenu)
                    {
                        FollowMeAnswered = false;
                        if (WaitingForGate)
                        {
                            Logger.Information($"Gate selected - end waiting");
                            WaitingForGate = false;
                        }
                    }
                }

                IsOpeningMenu = false;
                ReadyReceived = MenuState == GsxMenuState.READY || (ReadyReceived && MenuState == GsxMenuState.HIDE);
                if (ReadyReceived && menuChanged)
                {
                    var matchingCallbacks = MenuCallbacks.Where(c => MatchTitle(c.Key));
                    foreach (var callback in matchingCallbacks)
                    {
                        _ = TaskTools.RunPool(async () =>
                        {
                            await WaitInterval();
                            await callback.Value.Invoke(this);
                        }, RequestToken);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    ReadyReceived = false;
                    IsOpeningMenu = false;
                }
            }
        }

        protected virtual async Task<bool> UpdateMenu()
        {
            string lastTitle = MenuTitle;
            if (File.Exists(PathMenu))
            {
                MenuLines.Clear();

                var fileLines = File.ReadAllLines(PathMenu).ToArray();
                var fileIndex = 0;
                MenuTitle = fileLines[fileIndex++];
                while (fileIndex < fileLines.Length)
                    MenuLines.Add(fileLines[fileIndex++]);
                Logger.Verbose($"Read {MenuLineCount} Lines");
            }
            else
                Logger.Warning($"GSX Menu files does not exist! ({PathMenu})");

            if (lastTitle != MenuTitle)
            {
                Logger.Debug($"Menu Title changed: '{MenuTitle}'");
                await TaskTools.RunPool(() => MenuTitleChanged?.Invoke(MenuTitle), RequestToken);
            }

            return lastTitle != MenuTitle;
        }

        protected virtual async Task<int> WaitInterval(int interval = -1)
        {
            if (interval < 0)
                interval = Config.MenuCheckInterval;
            await Task.Delay(interval, RequestToken);
            return interval;
        }

        public virtual async Task<bool> WaitMenuReady()
        {
            int waitTime = 0;
            if (!ReadyReceived)
                Logger.Debug($"Wait for Menu Ready ...");
            while (!ReadyReceived && !RequestToken.IsCancellationRequested && waitTime <= Config.MenuOpenTimeout)
            {
                await WaitInterval();
                waitTime += Config.MenuCheckInterval;
            }
            if (ReadyReceived && IsOpeningMenu)
                IsOpeningMenu = false;

            if (waitTime < Config.MenuOpenTimeout)
            {
                if (waitTime > 0)
                    Logger.Debug($"Menu was Ready after {waitTime}ms");
                return true;
            }
            else
            {
                Logger.Warning($"Menu Open timed out");
                return false;
            }
        }

        protected virtual async Task<bool> SetMenuState(GsxMenuState state)
        {
            Logger.Debug($"Setting Menu State to {state}");
            ReadyReceived = state == GsxMenuState.READY || state == GsxMenuState.HIDE;
            return await SubMenuEvent.WriteValue((int)state);
        }

        public virtual async Task<bool> Open(bool resetToolbar = true)
        {
            bool result;
            try
            {
                if (IsOpeningMenu)
                {
                    Logger.Debug("MENU OPEN QUEUED AGAIN");
                    return false;
                }

                if (IsToolbarEnabled && resetToolbar)
                {
                    Logger.Information("Disabling GSX Toolbar ...");
                    await SetMenuState(GsxMenuState.DISABLED);
                    await WaitInterval();
                    IsToolbarEnabled = false;
                }
                Logger.Debug($"Open Menu ...");
                ReadyReceived = false;
                IsOpeningMenu = true;
                await SubMenuOpen.WriteValue(1);
                result = await WaitMenuReady();
                if (result)
                    await WaitInterval();
                else
                    await SetMenuState(GsxMenuState.TIMEOUT);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        protected virtual async Task<bool> Select(int number)
        {
            Logger.Debug($"Menu Select Item {number} => Value {number - 1}");
            ReadyReceived = false;
            if (await SubMenuChoice.WriteValue(number - 1))
            {
                await WaitInterval();
                return true;
            }
            else
                return false;
        }

        public virtual async Task<bool> RunSequence(GsxMenuSequence sequence)
        {
            bool result;
            IsSequenceActive = true;
            sequence.IsExecuting = true;
            WasOperatorSelected = false;

            int counter = 0;
            foreach (var command in sequence.Commands)
            {
                if ((!Controller.IsGsxRunning && !sequence.IgnoreGsxState) || RequestToken.IsCancellationRequested)
                    break;

                if (await RunCommand(command) == true)
                    counter++;
                else
                {
                    Logger.Debug("Command failed - break Sequence execution");
                    break;
                }
            }
            result = counter == sequence.Commands.Count;

            sequence.IsExecuting = false;
            sequence.IsSuccess = result;
            IsSequenceActive = false;
            return result;
        }

        public virtual async Task<bool> RunCommand(GsxMenuCommand command)
        {
            Logger.Debug($"Run Cmd Type: {command.Type}");
            if (command.Type == GsxMenuCommandType.Open)
            {
                bool result = true;
                if (!ReadyReceived)
                    result = await Open(command.MenuReset);
                else
                    await WaitInterval();

                return result;
            }
            else if (command.Type == GsxMenuCommandType.State)
            {
                if (await SetMenuState((GsxMenuState)command.Parameter))
                {
                    await WaitInterval();
                    return true;
                }
                else
                    return false;
            }
            else if (command.Type == GsxMenuCommandType.Wait)
            {
                await WaitInterval(Config.MenuCheckInterval * 2);
                return true;
            }
            else if (command.Type == GsxMenuCommandType.Select)
            {
                if (command.WaitReady && !ReadyReceived && !await WaitMenuReady())
                    return false;

                if (command.HasTitle && !MatchTitle(command.Title))
                {
                    Logger.Debug($"Menu Command skipped - Title did not match: '{MenuTitle}' does not start with '{command.Title}'");
                    return false;
                }

                bool result = await Select(command.Parameter);
                if (result && command.MenuReset && await Open())
                    await WaitInterval();
                return result;
            }
            else if (command.Type == GsxMenuCommandType.Operator)
            {
                int timeWaited = 0;
                while (timeWaited <= Config.OperatorWaitTimeout && !ReadyReceived && !IsOperatorMenu && !RequestToken.IsCancellationRequested)
                    timeWaited += await WaitInterval();

                if (timeWaited > Config.OperatorWaitTimeout && !IsOperatorMenu)
                {
                    Logger.Debug("No Operator Menu detected");
                }
                else if (IsOperatorMenu)
                {
                    timeWaited = 0;
                    if (!AircraftProfile.OperatorAutoSelect)
                        Logger.Information($"Waiting for manual Operator Selection ... (Timeout {Config.OperatorSelectTimeout / 1000}s)");
                    else if (!WasOperatorSelected)
                        Logger.Debug("Wait for Operator Selection ...");

                    while (timeWaited <= Config.OperatorSelectTimeout && IsOperatorMenu && !WasOperatorSelected && !RequestToken.IsCancellationRequested)
                        timeWaited += await WaitInterval();

                    if (timeWaited > Config.OperatorSelectTimeout && !WasOperatorSelected)
                    {
                        if (!AircraftProfile.OperatorAutoSelect)
                            Logger.Information("Manual Operator Selection timed out - closing Menu");
                        else
                            Logger.Warning("Automatic Operator Selection timed out - closing Menu");
                        if (await SetMenuState(GsxMenuState.TIMEOUT))
                            await WaitInterval();
                    }
                }
                bool result = !IsOperatorMenu || (IsOperatorMenu && WasOperatorSelected);

                if (command.MenuReset)
                {
                    if (WasOperatorSelected)
                        await WaitInterval(Config.MenuCheckInterval * 2);
                    if (await Open())
                        await WaitInterval();
                }

                return result;
            }
            else
                return false;
        }
    }
}
