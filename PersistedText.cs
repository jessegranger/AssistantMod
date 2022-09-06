using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using System.Diagnostics;

namespace Assistant {
	internal class PersistedText {
		private class RenderItem {
			public Func<string> Text;
			public Func<Camera, Vector2> Pos;
			public Color Color;
			public Func<long> Remaining;
			public RenderItem(Func<string> text, Func<Camera, Vector2> pos, int duration, Color color) {
				Text = text;
				Pos = pos;
				Color = color;
				if ( duration > 0 ) {
					Stopwatch timer = Stopwatch.StartNew();
					Remaining = () => duration - timer.ElapsedMilliseconds;
				} else {
					Remaining = () => long.MaxValue;
				}
			}
		}

		private static List<RenderItem> instances = new List<RenderItem>();
		public static void Add(Func<string> text, Func<Camera, Vector2> pos, int ms, Color color) => instances.Add(new RenderItem(text, pos, ms, color));
		public static void Add(Func<string> text, Func<Vector2> pos, int ms, Color color) => instances.Add(new RenderItem(text, c => pos(), ms, color));
		public static void Add(Func<string> text, Vector2 pos, int ms, Color color) => instances.Add(new RenderItem(text, c => pos, ms, color));
		public static void Add(string text, Func<Camera, Vector2> pos, int ms, Color color) => instances.Add(new RenderItem(() => text, pos, ms, color));
		public static void Add(string text, Func<Vector2> pos, int ms, Color color) => instances.Add(new RenderItem(() => text, c => pos(), ms, color));
		public static void Add(string text, Vector2 pos, int ms, Color color) => instances.Add(new RenderItem(() => text, c => pos, ms, color));
		public static void Add(string text, Func<Vector3> pos, int ms, Color color) => instances.Add(new RenderItem(() => text, c => c.WorldToScreen(pos()), ms, color));
		public static void Add(string text, Vector3 pos, int ms, Color color) => instances.Add(new RenderItem(() => text, c => c.WorldToScreen(pos), ms, color));
		public static void Render(Camera camera, Graphics G) {
			// clear out expired texts
			instances.RemoveAll(obj => obj.Remaining() <= 0);
			foreach ( var obj in instances ) {
				G.DrawText(obj.Text(), obj.Pos(camera), obj.Color);
			}
		}

	}
}
