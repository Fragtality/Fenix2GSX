﻿using CFIT.AppFramework.MessageService;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.AppConfig;
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
        DISABLED = 4
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
        public virtual bool HasTitle { get { return !string.IsNullOrWhiteSpace(MenuTitle); } }
        public virtual int MenuLineCount { get { return MenuLines.Count; } }
        public virtual List<string> MenuLines { get; } = [];

        public virtual bool FirstReadyReceived { get; protected set; } = false;
        public virtual bool IsSequenceActive { get; protected set; } = false;
        protected virtual bool WasOperatorSelected { get; set; } = false;
        public virtual bool IsMenuReady => MenuState == GsxMenuState.READY || MenuState == GsxMenuState.HIDE;
        public virtual bool IsGateMenu => MatchTitle(GsxConstants.MenuGate);
        public virtual bool IsOperatorMenu => MatchTitle(GsxConstants.MenuOperatorHandling) || MatchTitle(GsxConstants.MenuOperatorCater);
        protected virtual ConcurrentDictionary<string, Func<GsxMenu, Task>> MenuCallbacks { get; } = [];

        protected virtual ISimResourceSubscription SubMenuEvent { get; set; }
        protected virtual ISimResourceSubscription SubMenuOpen { get; set; }
        protected virtual ISimResourceSubscription SubMenuChoice { get; set; }
        protected virtual double LastMenuSelection { get; set; } = -2;
        protected virtual bool DeIceQuestionAnswered { get; set; } = false;
        protected virtual bool FollowMeAnswered { get; set; } = false;
        protected virtual bool MenuOpenRequesting { get; set; } = false;
        protected virtual bool MenuOpenAfterReady { get; set; } = false;
        public virtual bool WaitingForGate { get; protected set; } = false;
        public virtual bool WarpedToGate { get; protected set; } = false;
        public virtual bool SuppressMenuRefresh { get; set; } = false;
        public virtual MessageReceiver<MsgGsxMenuReady> MsgMenuReady { get; protected set; }


        public event Action<string> MenuTitleChanged;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubMenuEvent = Controller.SimStore.AddEvent(GsxConstants.EventMenu);
                SubMenuEvent.OnReceived += OnMenuEvent;
                SubMenuOpen = Controller.SimStore.AddVariable(GsxConstants.VarMenuOpen);
                SubMenuChoice = Controller.SimStore.AddVariable(GsxConstants.VarMenuChoice);
                SubMenuChoice.OnReceived += OnMenuSelection;

                MsgMenuReady = Controller.ReceiverStore.Add<MsgGsxMenuReady>();

                Controller.MsgCouatlStopped.OnMessage += OnCouatlStopped;

                MenuCallbacks.Add(GsxConstants.MenuTugAttach, OnTugQuestion);
                MenuCallbacks.Add(GsxConstants.MenuPushbackRequest, OnPushQuestion);
                MenuCallbacks.Add(GsxConstants.MenuFollowMe, OnFollowMeQuestion);
                MenuCallbacks.Add(GsxConstants.MenuDeiceOnPush, OnDeiceQuestion);
                MenuCallbacks.Add(GsxConstants.MenuParkingChange, OnParking);
                MenuCallbacks.Add(GsxConstants.MenuParkingSelect, OnParking);
                MenuCallbacks.Add(GsxConstants.MenuBoardCrew, OnBoardCrew);
                MenuCallbacks.Add(GsxConstants.MenuDeboardCrew, OnDeboardCrew);

                IsInitialized = true;
            }
        }

        protected virtual void OnMenuSelection(ISimResourceSubscription sub, object value)
        {
            double num = sub.GetNumber();
            if (num != -2)
            {
                if (MatchTitle(GsxConstants.MenuDeiceOnPush))
                {
                    Logger.Debug($"Deice Question was answered: {num}");
                    DeIceQuestionAnswered = true;
                }
                else if (MatchTitle(GsxConstants.MenuParkingChange) && WaitingForGate && num == 4)
                {
                    Logger.Debug($"Warped to Gate - trigger Menu Refresh");
                    WaitingForGate = false;
                    Task.Delay(2000, RequestToken).ContinueWith((_) => OpenHide()).ContinueWith((_) => WarpedToGate = true);
                }
                LastMenuSelection = num;
                Logger.Verbose($"Menu Selection {LastMenuSelection}");
            }
        }

        protected virtual async Task OnPushQuestion(GsxMenu menu)
        {
            Logger.Debug($"Request Pushback Question active");
            await Select(1, false, false, 2);
        }

        protected virtual async Task OnTugQuestion(GsxMenu menu)
        {
            Logger.Debug($"Tug Question active");
            int hide = !AircraftProfile.SkipCrewQuestion ? 0 : 2;
            if (AircraftProfile.AttachTugDuringBoarding == 2)
                await Select(1, true, false, hide);
            else if (AircraftProfile.AttachTugDuringBoarding == 1)
                await Select(2, true, false, hide);

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
                sequence.Commands.Add(new GsxMenuCommand(2, GsxConstants.MenuFollowMe, false) { WaitReady = false});
                sequence.Commands.Add(GsxMenuCommand.CreateOperator());
                sequence.Commands.Add(GsxMenuCommand.CreateReset());
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

            if (MatchTitle(GsxConstants.MenuParkingChange) && Controller.AutomationController.State < AutomationState.Departure)
            {
                Logger.Debug($"App waiting for Gate");
                WaitingForGate = true;
            }
            await Task.Delay(25);
        }

        protected virtual async Task OnBoardCrew(GsxMenu menu)
        {
            Logger.Debug($"Board Crew Question active");
            if (AircraftProfile.SkipCrewQuestion)
            {
                await Select(1, false, false, 2);
                SuppressMenuRefresh = false;
            }
        }

        protected virtual async Task OnDeboardCrew(GsxMenu menu)
        {
            Logger.Debug($"Deboard Crew Question active");
            if (AircraftProfile.SkipCrewQuestion)
            {
                await Select(1, false, false, 2);
                SuppressMenuRefresh = false;
            }            
        }

        protected virtual void OnCouatlStopped(MsgGsxCouatlStopped msg)
        {
            FirstReadyReceived = false;
        }

        public virtual void Reset()
        {
            LastMenuSelection = -2;
            MenuTitle = "";
            MenuLines.Clear();
            MenuState = GsxMenuState.UNKNOWN;
            FirstReadyReceived = false;
            DeIceQuestionAnswered = false;
            FollowMeAnswered = false;
            MenuOpenRequesting = false;
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

        protected virtual async void OnMenuEvent(ISimResourceSubscription sub, object value)
        {
            try
            {
                if (!Controller.IsActive)
                    return;

                MenuState = (GsxMenuState)sub.GetValue<int>();
                Logger.Debug($"Received Menu Event: {MenuState}");

                if (!FirstReadyReceived && MenuState == GsxMenuState.READY)
                {
                    Logger.Debug($"First Menu Ready received");
                    FirstReadyReceived = true;
                }

                WaitingForGate = false;
                if (MenuState == GsxMenuState.READY)
                    await UpdateMenu();

                if (MenuState == GsxMenuState.READY)
                    Controller.MessageService.Send(MessageGsx.Create<MsgGsxMenuReady>(Controller, MenuState));
                else
                    Controller.MessageService.Send(MessageGsx.Create<MsgGsxMenuReceived>(Controller, MenuState));
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual async Task UpdateMenu()
        {
            string lastTitle = MenuTitle;
            MenuOpenAfterReady = false;
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
                await TaskTools.RunLogged(() => MenuTitleChanged?.Invoke(MenuTitle), RequestToken);
                if (IsGateMenu)
                    FollowMeAnswered = false;
            }

            if (IsOperatorMenu && AircraftProfile.OperatorAutoSelect)
            {
                await SelectOperator();
                MenuOpenAfterReady = true;
            }

            var matchingCallbacks = MenuCallbacks.Where(c => MatchTitle(c.Key));
            foreach (var callback in matchingCallbacks)
                await TaskTools.RunLogged(() => callback.Value.Invoke(this), RequestToken);

            if (!SuppressMenuRefresh && MenuOpenAfterReady)
                _ = Task.Delay(1000, RequestToken).ContinueWith((_) => Open());
        }

        public virtual void Hide()
        {
            Logger.Debug($"Hide Menu");
            SubMenuEvent.WriteValue(2);
        }

        public virtual void Timeout()
        {
            Logger.Debug($"Timeout Menu");
            SubMenuEvent.WriteValue(3);
        }

        public virtual async Task<bool> Open(bool waitReady = false)
        {
            if (MenuOpenRequesting)
            {
                Logger.Debug($"Menu Open already requested - delaying ...");
                await Task.Delay(Config.MenuOpenTimeout, RequestToken);
            }
            MenuOpenRequesting = true;

            MsgGsxMenuReady msg = null;
            try
            {
                Logger.Debug($"Open Menu ...");
                MsgMenuReady.Clear();
                await SubMenuOpen.WriteValue(1);
                if (waitReady)
                {
                    msg = await MsgMenuReady.ReceiveAsync(false, Config.MenuOpenTimeout, RequestToken);
                    if (msg == null && !RequestToken.IsCancellationRequested)
                    {
                        Logger.Debug($"Retry Open ...");
                        await SubMenuOpen.WriteValue(1);
                        msg = await MsgMenuReady.ReceiveAsync(false, Config.MenuOpenTimeout, RequestToken);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            MenuOpenRequesting = false;

            return msg != null;
        }

        public virtual async Task<bool> OpenHide()
        {
            bool result = false;
            try
            {
                result = await Open(true);
                await Task.Delay(75);
                Hide();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            return result;
        }

        public virtual async Task Select(int number, bool waitReady = true, bool openMenu = false, int hide = 0)
        {
            if (openMenu && !IsMenuReady)
            {
                Logger.Verbose($"wait open");
                await Open(false);
            }

            if (waitReady && !IsMenuReady)
            {
                Logger.Verbose($"wait menu");
                await MsgMenuReady.ReceiveAsync(false, Config.MenuOpenTimeout, RequestToken);
            }

            Logger.Debug($"Menu Select Item {number} => Value {number - 1}");
            await SubMenuChoice.WriteValue(number - 1);

            if (hide == 1)
                Hide();
            else if (hide == 2)
                await OpenHide();
        }

        public virtual async Task<bool> RunSequence(GsxMenuSequence sequence)
        {
            bool result = false;
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
            }
            result = counter == sequence.Commands.Count;
            if (result)
                sequence.CallbackCompleted?.Invoke(sequence);
            
            sequence.IsExecuting = false;
            IsSequenceActive = false;
            sequence.IsSuccess = result;
            return result;
        }

        protected virtual async Task<bool> RunCommand(GsxMenuCommand command)
        {
            bool result = false;
            Logger.Verbose($"Run Cmd Type: {command.Type}");
            if (command.OpenMenu)
            {
                Logger.Verbose($"wait menu");
                if (command.NoHide)
                {
                    if (await Open(command.WaitReady) == false)
                        return result;
                }
                else
                {
                    if (await OpenHide() == false)
                        return result;
                }
            }
            else if (command.WaitReady && MenuState != GsxMenuState.READY)
            {
                Logger.Verbose($"wait rdy");
                await MsgMenuReady.ReceiveAsync(true, Config.MenuOpenTimeout, RequestToken);
            }

            if (command.HasTitle && !MatchTitle(command.Title))
            {
                Logger.Warning($"Menu Command skipped - Title did not match: '{MenuTitle}' does not start with '{command.Title}'");
                return result;
            }

            if (command.Type == GsxMenuCommandType.Number)
            {
                Logger.Verbose($"op num");
                await Select(command.Number, command.WaitReady);
            }
            else if (command.Type == GsxMenuCommandType.DummyWait)
            {
                await Task.Delay(Config.MenuCheckInterval * 2, RequestToken);
            }
            else if (command.Type == GsxMenuCommandType.Reset)
            {
                await Task.Delay(Config.MenuCheckInterval, RequestToken);
                await OpenHide();
            }
            else if (command.Type == GsxMenuCommandType.Operator)
            {
                Logger.Verbose($"Op Cmd");
                int timeWaited = 0;
                do
                {
                    if (!IsMenuReady || !WasOperatorSelected)
                        await Task.Delay(Config.MenuCheckInterval, RequestToken);
                    timeWaited += Config.MenuCheckInterval;
                }
                while (timeWaited < Config.OperatorWaitTimeout && !IsMenuReady && !WasOperatorSelected && !Controller.Token.IsCancellationRequested && !RequestToken.IsCancellationRequested);
                Logger.Verbose($"Rdy wait ended");
                if (Controller.Token.IsCancellationRequested || RequestToken.IsCancellationRequested)
                    return false;

                if (IsOperatorMenu && !AircraftProfile.OperatorAutoSelect)
                {
                    timeWaited = 0;
                    LastMenuSelection = -2;
                    Logger.Information($"Waiting for manual Operator Selection ... (Timeout {Config.OperatorSelectTimeout / 1000}s)");
                    do
                    {
                        if (LastMenuSelection == -2)
                            await Task.Delay(Config.MenuCheckInterval, Controller.Token);
                        timeWaited += Config.MenuCheckInterval;
                    }
                    while (timeWaited < Config.OperatorSelectTimeout && LastMenuSelection == -2 && !Controller.Token.IsCancellationRequested && !RequestToken.IsCancellationRequested);
                    if (Controller.Token.IsCancellationRequested)
                        return false;
                    Logger.Debug($"Wait ended after {timeWaited}ms - LastSelection {LastMenuSelection}");
                    if (timeWaited >= Config.OperatorSelectTimeout)
                        Timeout();
                    else
                        await OpenHide();
                }
                else if (IsOperatorMenu && WasOperatorSelected)
                    await OpenHide();
            }

            result = true;
            return result;
        }

        public virtual async Task SelectOperator()
        {
            var gsxOperator = GsxOperator.OperatorSelection(AircraftProfile, MenuLines);
            if (gsxOperator != null)
            {
                Logger.Information($"Selecting Operator '{gsxOperator.Title}' (GSX Choice: {gsxOperator.GsxChoice})");
                await Select(gsxOperator.Number, false);
            }
            else
            {
                Logger.Warning($"Selecting Operator #1 - no Matches found");
                await Select(1, false);
            }
        }

        public virtual void FreeResources()
        {
            Controller.MsgCouatlStopped.OnMessage -= OnCouatlStopped;

            Controller.SimStore.Remove(GsxConstants.EventMenu).OnReceived -= OnMenuEvent;
            Controller.SimStore.Remove(GsxConstants.VarMenuOpen);
            SubMenuChoice.OnReceived -= OnMenuSelection;
            Controller.SimStore.Remove(GsxConstants.VarMenuChoice);
            Controller.ReceiverStore.Remove<MsgGsxMenuReady>();
        }

        public virtual bool MatchTitle(string match)
        {
            return MenuTitle?.StartsWith(match, StringComparison.InvariantCultureIgnoreCase) == true;
        }
    }
}
