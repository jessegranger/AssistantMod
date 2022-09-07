using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	public class InputSimulator {
		[DllImport("user32.dll", SetLastError = true)] private static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);
		[DllImport("user32.dll")] private static extern UInt32 MapVirtualKey(UInt32 uCode, UInt32 uMapType);
		// sends one or more input events, returns the number successful
		private static uint Dispatch(params INPUT[] inputs) => SendInput((UInt32)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

		[StructLayout(LayoutKind.Explicit)]
		private struct MOUSEKBHW_UNION {
			[FieldOffset(0)] public MOUSEINPUT Mouse;
			[FieldOffset(0)] public KEYBDINPUT Keyboard;
			[FieldOffset(0)] public HARDWAREINPUT HW;
		}
		private struct MOUSEINPUT {
			public Int32 X;
			public Int32 Y;
			public UInt32 MouseData;
			public MouseFlag Flags;
			public UInt32 Time;
			public IntPtr ExtraInfo;
		}
		[Flags]
		private enum MouseFlag : UInt32 {
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
		private struct KEYBDINPUT {
			public UInt16 KeyCode;
			public UInt16 ScanCode;
			public KeyboardFlag Flags;
			public UInt32 Time;
			public IntPtr ExtraInfo;
		}
		[Flags]
		private enum KeyboardFlag : UInt32 {
			ExtendedKey = 0x0001,
			KeyUp = 0x0002,
			Unicode = 0x0004,
			ScanCode = 0x0008
		}
		private struct HARDWAREINPUT {
			public UInt32 Msg;
			public UInt16 ParamL;
			public UInt16 ParamH;
		}
		private struct INPUT {
			public InputType Type;
			public MOUSEKBHW_UNION Data;
		}
		private enum InputType : UInt32 {
			Mouse = 0,
			Keyboard = 1,
			Hardware = 2
		}
		public static void LeftDown() {
			var msg = new INPUT { Type = InputType.Mouse };
			msg.Data.Mouse.Flags = MouseFlag.LeftDown;
			Dispatch(msg);
		}
		public static void LeftUp() {
			var msg = new INPUT { Type = InputType.Mouse };
			msg.Data.Mouse.Flags = MouseFlag.LeftUp;
			Dispatch(msg);
		}
		public static void RightDown() {
			var msg = new INPUT { Type = InputType.Mouse };
			msg.Data.Mouse.Flags = MouseFlag.RightDown;
			Dispatch(msg);
		}
		public static void RightUp() {
			var msg = new INPUT { Type = InputType.Mouse };
			msg.Data.Mouse.Flags = MouseFlag.RightUp;
			Dispatch(msg);
		}
		public static void KeyDown(Keys key) {
			int vkey = (int)(key & Keys.KeyCode);
			var msg = new INPUT { Type = InputType.Keyboard };
			msg.Data.Keyboard.KeyCode = (UInt16)vkey;
			msg.Data.Keyboard.ScanCode = (UInt16)(MapVirtualKey((UInt32)vkey, 0) & 0xFFU);
			msg.Data.Keyboard.Flags = 0;
			msg.Data.Keyboard.Time = 0;
			msg.Data.Keyboard.ExtraInfo = IntPtr.Zero;
			Dispatch(msg);
		}
		public static void KeyUp(Keys key) {
			int vkey = (int)(key & Keys.KeyCode);
			var msg = new INPUT { Type = InputType.Keyboard };
			msg.Data.Keyboard.KeyCode = (UInt16)vkey;
			msg.Data.Keyboard.ScanCode = (UInt16)(MapVirtualKey((UInt32)vkey, 0) & 0xFFU);
			msg.Data.Keyboard.Flags = KeyboardFlag.KeyUp;
			msg.Data.Keyboard.Time = 0;
			msg.Data.Keyboard.ExtraInfo = IntPtr.Zero;
			Dispatch(msg);
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

	}
}
