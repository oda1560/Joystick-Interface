using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Interceptor;
using WindowsInput;
using WindowsInput.Native;

namespace JoystickInterface
{

    class Program
    {
        const int max = 65535;
        readonly static double notch = max / double.Parse(ConfigurationManager.AppSettings["Notches"]);
        //const ushort down = 0x2C;
        //const ushort up = 0x1E;
        static ushort down;
        static ushort up;

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        static void Main()
        {
            getScanCodes();
            var dinput = new SlimDX.DirectInput.DirectInput();
            var devices = dinput.GetDevices(SlimDX.DirectInput.DeviceClass.GameController, SlimDX.DirectInput.DeviceEnumerationFlags.AttachedOnly);
            if (!devices.Any())
            {
                Console.WriteLine("No devices");
            }
            var device = devices.FirstOrDefault(d => d.InstanceName == "SideWinder Force Feedback 2 Joystick");
            if (device == null)
            {
                Console.WriteLine("SideWinder Force Feedback 2 Joystick not found");
            }
            var joystick = new SlimDX.DirectInput.Joystick(dinput, device.InstanceGuid);
            joystick.Acquire();

            var lastNotch = 0;
            var firstTime = true;
            
            while (true)
            {
                Thread.Sleep(30);
                joystick.Poll();
                var data = joystick.GetCurrentState();
                Console.WriteLine(data.Y);
                var currentNotch = (int)(data.Y / notch);

                ushort key;

                if (currentNotch > lastNotch)
                {
                    Console.WriteLine("Send Down");
                    key = down;
                    lastNotch = currentNotch;
                }
                else if (currentNotch < lastNotch)
                {
                    Console.WriteLine("Send Up");
                    key = up;
                    lastNotch = currentNotch;
                }
                else
                {
                    continue;
                }

                if (firstTime)
                {
                    firstTime = false;
                    continue;
                }

                var InputData = new INPUT[1];

                InputData[0].type = 1;
                InputData[0].wScan = key;
                InputData[0].dwFlags = (uint)SendInputFlags.KEYEVENTF_KEYDOWN | (uint)SendInputFlags.KEYEVENTF_SCANCODE;
                InputData[0].time = 30;
                InputData[0].dwExtraInfo = UIntPtr.Zero;

                SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT)));

                Thread.Sleep(int.Parse(ConfigurationManager.AppSettings["KeyDownTime"]));



                InputData = new INPUT[1];

                InputData[0].type = 1;
                InputData[0].wScan = key;
                InputData[0].dwFlags = (uint)SendInputFlags.KEYEVENTF_KEYUP | (uint)SendInputFlags.KEYEVENTF_SCANCODE;
                InputData[0].time = 30;
                InputData[0].dwExtraInfo = UIntPtr.Zero;

                SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT)));

            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        static void getScanCodes()
        {
            var xDoc = XDocument.Parse(File.ReadAllText("ScanCodes.xml"));
            down = ushort.Parse(xDoc.Root.Elements().FirstOrDefault(e => e.Attribute("key").Value.ToString().Equals(ConfigurationManager.AppSettings["Down"], StringComparison.OrdinalIgnoreCase)).Attribute("value").Value);
            up = ushort.Parse(xDoc.Root.Elements().FirstOrDefault(e => e.Attribute("key").Value.ToString().Equals(ConfigurationManager.AppSettings["Up"], StringComparison.OrdinalIgnoreCase)).Attribute("value").Value);
        }




    }
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public UInt32 type;
        //KEYBDINPUT:
        public ushort wVk;
        public ushort wScan;
        public UInt32 dwFlags;
        public UInt32 time;
        public UIntPtr dwExtraInfo;
        //HARDWAREINPUT:
        public UInt32 uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    enum SendInputFlags
    {
        KEYEVENTF_EXTENDEDKEY = 0x0001,
        KEYEVENTF_KEYUP = 0x0002,
        KEYEVENTF_KEYDOWN = 0x0000,
        KEYEVENTF_UNICODE = 0x0004,
        KEYEVENTF_SCANCODE = 0x0008,
    }
}
