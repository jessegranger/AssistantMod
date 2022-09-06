using ExileCore;
using ExileCore.Shared.Nodes;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	static class BuffManager {
		private static GameController api;
		private static Graphics Graphics;
		private static AssistantSettings Settings;
		private static bool Paused = true;
		internal static void Initialise(GameController game, Graphics gfx, AssistantSettings settings) {
			api = game;
			Graphics = gfx;
			Settings = settings;
			OnRelease(Keys.Pause, () => Paused = !Paused);
			// PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.72f, .85f), 0);
		}

		private static string GetStatusText() => $"BuffManager[{(Paused ? "Paused" : "Running")}]";

		private static List<BuffToMaintain> vaalBuffsToMaintain = new List<BuffToMaintain>();
		private static List<BuffToMaintain> buffsToMaintain = new List<BuffToMaintain>();
		private static List<BuffToMaintain> buffsToClear = new List<BuffToMaintain>();
		public static void MaintainVaalBuff(ToggleHotkeyNode config, string skillName, string buffName) {
			vaalBuffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName });
		}
		public static void MaintainVaalBuff(ToggleHotkeyNode config, string skillName, string buffName, Func<bool> cond) {
			vaalBuffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Condition = cond });
		}
		public static void MaintainBuff(ToggleHotkeyNode config, string skillName, string buffName, Func<bool> cond) {
			buffsToMaintain.Add(new BuffToMaintain() { Node = config, SkillName = skillName, BuffName = buffName, Condition = cond });
		}
		public static void MaintainBuff(ToggleHotkeyNode config, string skillName, string buffName)
				=> MaintainBuff(config, skillName, buffName, () => true);
		public static void ClearBuff(ToggleHotkeyNode config, string buffName, Func<bool> cond)
				=> buffsToClear.Add(new BuffToMaintain() { Node = config, BuffName = buffName, Condition = cond });
		private class BuffToMaintain {
			public ToggleHotkeyNode Node;
			public string SkillName;
			public string BuffName;
			public Func<bool> Condition = () => true;
		}

		public static void OnTick() {
			if ( Paused ) return;
			if ( HasBuff("grace_period") ) return;
			foreach ( var buff in vaalBuffsToMaintain ) {
				// string status = $"Maintain Vaal: {buff.BuffName} ";
				if ( buff.Node?.Enabled ?? false ) {
					bool needsBuff = false;
					try {
						needsBuff = buff.Condition();
					} catch ( Exception err ) {
						// status += $"Exception: {err.ToString()}";
						Log(err.ToString());
					}
					if ( needsBuff ) {
						if ( !HasBuff(buff.BuffName) ) {
							SkillManager.TryUseVaalSkill(buff.SkillName, buff.Node.Value);
							// } else { status += "Has Buff.";
						}
						// } else { status += "Not needed.";
					}
					// } else { status += "Disabled.";
				}
				// DrawTextAtPlayer(status);
			}
			foreach ( var buff in buffsToMaintain ) {
				if ( !(buff.Node?.Enabled ?? false) ) continue;
				try { if ( !buff.Condition() ) continue; } catch ( Exception err ) {
					Log($"Exception: {err}");
					continue;
				}
				if ( HasBuff(buff.BuffName) ) {
					continue;
				}
				SkillManager.TryUseSkill(buff.SkillName, buff.Node.Value);
			}
			foreach ( var buff in buffsToClear ) {
				if ( !(buff.Node?.Enabled ?? false) ) continue;
				try { if ( !buff.Condition() ) continue; } catch ( Exception err ) {
					Log($"Exception: {err}");
					continue;
				}
				if ( !HasBuff(buff.BuffName) ) continue;
				Globals.PressKey(buff.Node.Value, 40, 1000);
			}
		}
	}
}
