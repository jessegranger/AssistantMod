using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	public class InputSimulator {
		// this is a bare-bones wrapper around all things SendInput()
		// built in a hurry because I needed to not have the real InputSimulator as a dependency
		[DllImport("user32.dll", SetLastError = true)] private static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);
		[DllImport("user32.dll")] private static extern UInt32 MapVirtualKey(UInt32 uCode, UInt32 uMapType);
		// sends one or more input events, returns the number successful
		public static uint Dispatch(params INPUT[] inputs) => SendInput((UInt32)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

		[StructLayout(LayoutKind.Explicit)]
		public struct MOUSEKBHW_UNION {
			[FieldOffset(0)] public MOUSEINPUT Mouse;
			[FieldOffset(0)] public KEYBDINPUT Keyboard;
			[FieldOffset(0)] public HARDWAREINPUT HW;
		}
		public struct MOUSEINPUT {
			public Int32 X;
			public Int32 Y;
			public UInt32 MouseData;
			public MouseFlag Flags;
			public UInt32 Time;
			public IntPtr ExtraInfo;
		}
		[Flags]
		public enum MouseFlag : UInt32 {
			Move = 0x0001,
			LeftDown = 0x0002,
			LeftUp = 0x0004,
			RightDown = 0x0008,
			RightUp = 0x0010,
			MiddleDown = 0x0020,
			MiddleUp = 0x0040,
			XDown = 0x0080,
			XUp = 0x0100,
			VerticalWheel = 0x0800,
			HorizontalWheel = 0x1000,
			VirtualDesk = 0x4000, // "absolute" motion relative to virtual desktop
			Absolute = 0x8000, // "absolute" relative to a physical screen
		}
		public struct KEYBDINPUT {
			public UInt16 KeyCode;
			public UInt16 ScanCode;
			public KeyboardFlag Flags;
			public UInt32 Time;
			public IntPtr ExtraInfo;
		}
		[Flags]
		public enum KeyboardFlag : UInt32 {
			ExtendedKey = 0x0001,
			KeyUp = 0x0002,
			Unicode = 0x0004,
			ScanCode = 0x0008
		}
		public struct HARDWAREINPUT {
			public UInt32 Msg;
			public UInt16 ParamL;
			public UInt16 ParamH;
		}
		public struct INPUT {
			public InputType Type;
			public MOUSEKBHW_UNION Data;
		}
		public enum InputType : UInt32 {
			Mouse = 0,
			Keyboard = 1,
			Hardware = 2
		}
		public static INPUT MouseMessage(MouseFlag button, uint mouseData = 0) {
			var msg = new INPUT { Type = InputType.Mouse };
			msg.Data.Mouse.Flags = button;
			msg.Data.Mouse.MouseData = mouseData;
			return msg;
		}
		public static void LeftDown() => Dispatch(MouseMessage(MouseFlag.LeftDown));
		public static void LeftUp() => Dispatch(MouseMessage(MouseFlag.LeftUp));
		public static void RightDown() => Dispatch(MouseMessage(MouseFlag.RightDown));
		public static void RightUp() => Dispatch(MouseMessage(MouseFlag.RightUp));
		public static INPUT KeyDownMessage(Keys key) {
			int vkey = (int)(key & Keys.KeyCode);
			var msg = new INPUT { Type = InputType.Keyboard };
			msg.Data.Keyboard.KeyCode = (UInt16)vkey;
			msg.Data.Keyboard.ScanCode = (UInt16)(MapVirtualKey((UInt32)vkey, 0) & 0xFFU);
			msg.Data.Keyboard.Flags = 0;
			msg.Data.Keyboard.Time = 0;
			msg.Data.Keyboard.ExtraInfo = IntPtr.Zero;
			return msg;
		}
		public static void KeyDown(Keys key) {
			Dispatch(KeyDownMessage(key));
		}
		public static INPUT KeyUpMessage(Keys key) {
			int vkey = (int)(key & Keys.KeyCode);
			var msg = new INPUT { Type = InputType.Keyboard };
			msg.Data.Keyboard.KeyCode = (UInt16)vkey;
			msg.Data.Keyboard.ScanCode = (UInt16)(MapVirtualKey((UInt32)vkey, 0) & 0xFFU);
			msg.Data.Keyboard.Flags = KeyboardFlag.KeyUp;
			msg.Data.Keyboard.Time = 0;
			msg.Data.Keyboard.ExtraInfo = IntPtr.Zero;
			return msg;
		}
		public static void KeyUp(Keys key) {
			Dispatch(KeyUpMessage(key));
		}
		private static Vector2 WindowsApiNormalize(Vector2 pos) {
			var game = GetGame();
			if ( game == null ) return Vector2.Zero;
			var window = game.Window;
			if ( window == null ) return Vector2.Zero;
			var w = window.GetWindowRectangleTimeCache;
			// TODO: measure this for performance, not sure if we need to cache PrimaryScreen.Bounds or not
			var bounds = Screen.PrimaryScreen.Bounds;
			float X = pos.X;
			float Y = pos.Y;
			if ( X > bounds.Width || X < 0 || Y > bounds.Height || Y < 0 ) {
				return Vector2.Zero;
			}
			return new Vector2(
					(w.Left + X) * 65535 / bounds.Width,
					(w.Top + Y) * 65535 / bounds.Height);
		}
		public static void MouseMoveTo(double x, double y) => MouseMoveTo(new Vector2((float)x, (float)y));
		public static void MouseMoveTo(Vector2 pos) {
			// x,y and in window coordinates, we need to shift them and normalize to the windows pattern
			// for windows api, mouse events happen on a 65535 x 65535 grid
			pos = WindowsApiNormalize(pos);
			var msg = new INPUT { Type = InputType.Mouse };
			msg.Data.Mouse.Flags = (MouseFlag.Move | MouseFlag.Absolute);
			msg.Data.Mouse.X = (int)Math.Round(pos.X);
			msg.Data.Mouse.Y = (int)Math.Round(pos.Y);
			Dispatch(msg);
		}

		public static INPUT[] TranslateKeyBind(string keyBind, bool keyUp) {
			// takes as input something like "Ctrl+E", or "M5", and returns appropriate INPUT events
			// if keyUp == true, the INPUT events will be of the key up (or mouse up) variety
			// otherwise, they will be keydown (or mouse down) variety
			string[] keys = keyBind.Split('+');
			INPUT[] inputs = new INPUT[keys.Length];
			for(int i = 0; i < keys.Length; i++) {
				string k = keys[i];
				if ( k.Equals("Ctrl") ) {
					inputs[i] = keyUp ? KeyUpMessage(Keys.LControlKey) : KeyDownMessage(Keys.LControlKey);
				} else if ( k.Equals("Shift") ) {
					inputs[i] = keyUp ? KeyUpMessage(Keys.LShiftKey) : KeyDownMessage(Keys.LShiftKey);
				} else if ( k.Equals("Alt") ) {
					inputs[i] = keyUp ? KeyUpMessage(Keys.LMenu) : KeyDownMessage(Keys.LMenu);
				} else if ( k.Length == 2 && k[0] == 'M' && k[1] >= '0' && k[1] <= '9' ) {
					char button = k[1];
					if ( button == '1' ) inputs[i] = MouseMessage(keyUp ? MouseFlag.LeftUp : MouseFlag.LeftDown);
					else if ( button == '2' ) inputs[i] = MouseMessage(keyUp ? MouseFlag.RightUp : MouseFlag.RightDown);
					else if ( button == '3' ) inputs[i] = MouseMessage(keyUp ? MouseFlag.MiddleUp : MouseFlag.MiddleDown);
					else if ( button == '4' ) inputs[i] = MouseMessage(keyUp ? MouseFlag.XUp : MouseFlag.XDown, 0x0001);
					else if ( button == '5' ) inputs[i] = MouseMessage(keyUp ? MouseFlag.XUp : MouseFlag.XDown, 0x0002);
				} else if ( k.Length == 1 && k[0] >= 'A' && k[0] <= 'Z' ) {
					inputs[i] = keyUp ? KeyUpMessage((Keys)(byte)k[0]) : KeyDownMessage((Keys)(byte)k[0]);
				}
			}
			return inputs;
		}

	}
}
