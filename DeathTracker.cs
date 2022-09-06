using ExileCore;
using ExileCore.PoEMemory.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Assistant.Globals;

namespace Assistant {
	public static class DeathTracker {
		private static bool init = false;
		private static LinkedList<FrameSample> frameSamples = new LinkedList<FrameSample>();
		public static uint FrameCount = 100;

		private class BuffSample {
			public string Name;
			public int Charges;
			public float Timer;
		}
		private class LifeSample {
			public int HP;
			public int MaxHP;
			public int Mana;
			public int MaxMana;
			public int ES;
			public int MaxES;
			public LifeSample(Life life) {
				HP = life.CurHP;
				MaxHP = life.MaxHP;
				Mana = life.CurMana;
				MaxMana = life.MaxMana;
				ES = life.CurES;
				MaxES = life.MaxES;
			}
		}
		private class FrameSample {
			public LifeSample Life;
			public List<BuffSample> Buffs = new List<BuffSample>();
			public FrameSample(Life life, Buffs buffs) {
				Life = new LifeSample(life);
				foreach ( var buff in buffs.BuffsList ) {
					Buffs.Add(new BuffSample() { Name = buff.Name, Charges = buff.Charges, Timer = buff.Timer });
				}
			}
		}

		public static void Initialise() {
			Input.RegisterKey(System.Windows.Forms.Keys.J);
			Input.RegisterKey(System.Windows.Forms.Keys.K);
			Input.ReleaseKey += (object sender, System.Windows.Forms.Keys key) => {
				switch ( key ) {
					case System.Windows.Forms.Keys.K:
						showFrameIndex = Math.Min(frameSamples.Count, showFrameIndex + 1);
						break;
					case System.Windows.Forms.Keys.J:
						if ( showFrameIndex == 0 ) showFrameIndex = frameSamples.Count;
						showFrameIndex = Math.Max(0, showFrameIndex - 1);
						break;
				}

			};
			GetGame().Area.OnAreaChange += OnAreaChange;
			init = true;
		}

		private static void OnAreaChange(AreaInstance obj) {
			showFrameIndex = 0;
			frameSamples.Clear();
		}

		public static bool IsDead() {
			return false; // as of 3.18 this 107 index is changed and needs updated
										// var ui = gc.IngameState.IngameUi.Root.GetChildFromIndices(1, 107);
										// return ui?.IsVisible ?? false;
		}
		public static void OnTick() {
			if ( !init ) return;
			if ( IsDead() ) return;
			var game = GetGame();
			var life = game.Player.GetComponent<Life>();
			var buffs = game.Player.GetComponent<Buffs>();
			var sample = new FrameSample(life, buffs);
			frameSamples.AddLast(sample);
			if ( frameSamples.Count > FrameCount ) {
				frameSamples.RemoveFirst();
			}
		}

		public static void Render() {
			if ( !init ) return;
			if ( (GetSettings()?.ShowDeathFrames ?? false) && IsDead() ) {
				var cursor = new Vector2(0, 0);
				cursor = DrawText("You are dead.", cursor);
				cursor = DrawText($"Use J/K to see what happened. [Frame: {showFrameIndex}/{frameSamples.Count}]", cursor);
				RenderFrameSample(cursor);
			}
		}

		private static Vector2 DrawText(string text, Vector2 cursor) {
			var pos = new SharpDX.Vector2(cursor.X, cursor.Y);
			var size = GetGraphics().DrawText(text, pos, SharpDX.Color.White, ExileCore.Shared.Enums.FontAlign.Left);
			return cursor + new Vector2(0, size.Y);
			// return new Vector2(cursor.X, cursor.Y + .12f);
		}

		private static int showFrameIndex = 0;
		public static void RenderFrameSample(Vector2 cursor) {
			if ( frameSamples.Count == 0 ) return;
			if ( showFrameIndex < 0 || showFrameIndex >= frameSamples.Count ) {
				showFrameIndex = 0;
			}
			var frameSample = frameSamples.ElementAt(showFrameIndex);
			if ( frameSample == null ) return;
			var life = frameSample.Life;
			var buffs = frameSample.Buffs;
			cursor = DrawText($"Life: {life.HP}/{life.MaxHP} Mana: {life.Mana}/{life.MaxMana} ES: {life.ES}/{life.MaxES}", cursor);
			foreach ( var buff in buffs ) {
				cursor = DrawText($"Buff: {buff.Name} ({buff.Charges}) {buff.Timer:F2}", cursor);
			}
		}
	}
}
