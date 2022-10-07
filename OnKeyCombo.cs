using ExileCore;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Assistant {

	public static partial class Globals {

		public static IEnumerable<Keys> GetPressedKeys() => pressedKeys;
		private static HashSet<Keys> pressedKeys = new HashSet<Keys>();
		private static HashSet<Keys> ignoredKeys = new HashSet<Keys>() {
			Keys.None,
			Keys.KeyCode,
			Keys.LButton,
			Keys.RButton,
			Keys.MButton
		};
		private static void updatePressedKeys() {
			pressedKeys.Clear();
			foreach ( var key in Enum.GetValues(typeof(Keys)).Cast<Keys>()
				.Where(k => ! ignoredKeys.Contains(k))
				.Where(IsKeyDown) ) {
				pressedKeys.Add(key);
			}
		}
		public static void OnKeyCombo(string combo, Action action) {
			Keys[] keys = combo.Select(ToKeys).ToArray();
			Run(PlanKeyCombo(combo, keys, action));
		}
		public static void OnRelease(Keys key, Action action) {
			Run(PlanKeyCombo(ToString(key), new Keys[] { key }, action));
		}
		private static State PlanKeyCombo(string combo, Keys[] keys, Action action) {
			uint curStep = 0;
			bool downBefore = false;
			Stopwatch sinceLastRelease = new Stopwatch();
			State doReset(bool downNow, State state) {
				downBefore = downNow;
				curStep = 0;
				sinceLastRelease.Reset();
				return state;
			}
			return State.From($"Combo-{combo}", (state) => {
				var curKey = keys[curStep];
				// DrawTextAtPlayer($"Combo: step {curStep}/{curKey} of {string.Join(" ", keys)}");
				bool downNow = IsKeyDown(curKey);
				if ( downBefore && !downNow ) { // on release:
					// Log($"Combo-{combo} Key {Enum.GetName(typeof(VirtualKeyCode), curKey)} released. Advancing step...");
					curStep += 1;
					sinceLastRelease.Restart();
					if ( curStep >= keys.Length ) {
						try {
							action();
						} catch( Exception e ) {
							DebugWindow.LogError(e.ToString());
							return null; // cancel the failing state
						}
						return doReset(downNow, state);
					}
				} else if ( sinceLastRelease.ElapsedMilliseconds > 1000 ) {
					// Combo expired: resetting
					return doReset(downNow, state);
				} else {
					if ( GetPressedKeys().Any(k => k != curKey) ) {
						return doReset(downNow, state);
					}
				}
				downBefore = downNow;
				return state;
			});
		}

		public static void ConfigureHotkey(ToggleHotkeyNode node, Action action) {
			Run(PlanHotkey(node, action));
		}
		private static State PlanHotkey(ToggleHotkeyNode node, Action action) {
			bool downBefore = false;
			return State.From((state) => {
				if ( !node.Enabled ) return state;
				bool downNow = IsKeyDown(node.Value);
				if ( downBefore && !downNow ) action();
				downBefore = downNow;
				return state;
			});
		}

		internal static State PlanLootKey(ToggleHotkeyNode node) {
			bool downBefore = false;
			return State.From((resume) => {
				State next = resume;
				if ( !node.Enabled ) return next;
				bool downNow = IsKeyDown(node.Value);
				var elem = GetNearestGroundLabel()?.Label;
				if ( downNow ) { // move (lock) mouse to center of the nearest label
					new MoveMouse(elem).OnTick();
				} else if ( downBefore ) { // on release:
					downBefore = false;
					return new LeftClickAt(elem, 50, 1, resume);
				}
				downBefore = downNow;
				return resume;
			});
		}
	}
}
