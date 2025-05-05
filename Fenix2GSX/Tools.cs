using CFIT.AppLogger;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Fenix2GSX
{
    public static class Tools
    {
        public static string GetMsfsWindowTitle()
        {
            string result = "";
            try
            {
                Process proc = Process.GetProcessesByName(Fenix2GSX.Instance.Config.BinaryMsfs2024)?.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(proc?.MainWindowTitle))
                    result = proc.MainWindowTitle;
                else
                    result = $"{Fenix2GSX.Instance.Config.Msfs2024WindowTitle}{proc?.MainModule?.FileVersionInfo?.FileVersion?.Replace(',', '.')}";
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public static void SendWalkaroundKeystroke()
        {
            _ = SendInput(1, CreateInputStruct(VK_LSHIFT, KeyEventF.KeyDown), Marshal.SizeOf(typeof(Input)));
            _ = SendInput(1, CreateInputStruct(VK_KEY_C, KeyEventF.KeyDown), Marshal.SizeOf(typeof(Input)));
            Thread.Sleep(100);
            _ = SendInput(1, CreateInputStruct(VK_LSHIFT, KeyEventF.KeyUp), Marshal.SizeOf(typeof(Input)));
            _ = SendInput(1, CreateInputStruct(VK_KEY_C, KeyEventF.KeyUp), Marshal.SizeOf(typeof(Input)));
        }
#pragma warning disable
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, MapVirtualKeyMapTypes uMapType);
#pragma warning restore
        public enum MapVirtualKeyMapTypes : uint
        {
            MAPVK_VK_TO_VSC = 0x00,
            MAPVK_VSC_TO_VK = 0x01,
            MAPVK_VK_TO_CHAR = 0x02,
            MAPVK_VSC_TO_VK_EX = 0x03,
            MAPVK_VK_TO_VSC_EX = 0x04
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }

        public struct Input
        {
            public int type;
            public InputUnion u;
        }

        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }

        public static readonly byte VK_LSHIFT = 0xA0;
        public static readonly byte VK_KEY_C = 0x43;

        public static Input[] CreateInputStruct(byte vk, KeyEventF flags)
        {
            return
            [
                new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = (ushort)MapVirtualKey(vk, MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC),
                            dwFlags = (uint)(flags | KeyEventF.Scancode),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            ];
        }
    }
}
