using ExileCore;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	static class BuffManager {
		private static bool Paused = true;
		internal static void Initialise() {
			OnRelease(Keys.Pause, () => Paused = !Paused);
			foreach(var item in TextureMapping) {
				TextureReverseMapping[item.Value] = item.Key;
			}
			// PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.72f, .85f), 0);
		}

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

		public static void ReapplyAuras() {
			var game = GetGame();
			var ui = game.IngameState.IngameUi;
			// ui.HiddenSkillBar.Children.Skip(1).Select( (c) => c.Children.ElementAtOrDefault(1)?.Tooltip )


		}

		public static Dictionary<string, string> TextureMapping = new Dictionary<string, string>() {
			{ "Purity of Fire", "aurafireresist.dds" },
			{ "Purity of Lightning", "auralightningresist.dds" },
			{ "Purity of Ice", "auracoldresist.dds" },
			{ "Purity of Elements", "auraresist.dds" },
			{ "Arctic Armour", "Iceshield.dds" },
			{ "Anger", "aurafire.dds" },
			{ "Wrath", "auralightning.dds" },
			{ "Hatred", "auracold.dds" },
			{ "Zealotry", "SpellDamageAura.dds" },
			{ "Malevolence", "DeliriumAura.dds" },
			{ "Determination", "auraarmour.dds" },
			{ "Grace", "auraevasion.dds" },
			{ "Tempest Shield", "lightningshield.dds" },
			{ "Discipline", "auraenergy.dds" },
			{ "Clarity", "auramana.dds" },
			{ "Vitality", "auraregen.dds" },
			{ "Precision", "auracrit.dds" },
			{ "Haste", "auraspeed.dds" },
		};
		public static Dictionary<string, string> TextureReverseMapping = new Dictionary<string, string>();

		/* Disabled for now: needs more updates to Core Element
		public static bool TryGetKeyBinding(string skillName, out string keyBind) {
			keyBind = null;
			if( TextureMapping.TryGetValue(skillName, out string searchTexture) ) {
				searchTexture = "/" + searchTexture;
				foreach ( var child in GetGame().IngameState.IngameUi.SkillBar.Children ) {
					if( child.Texture?.EndsWith(searchTexture) ?? false ) {
						keyBind = child.GetChildFromIndices(0, 0, 0, 1)?.Text;
						return true;
						// DebugWindow.LogDebug($"{child.Texture} {child.GetChildFromIndices(0,0,0,0)?.Texture ?? "null"} {child.GetChildFromIndices(0,0,0,1)?.Text ?? "null"}"); // {child.Children[0]?.Children[0]?.Children[0]?.Children[0]?.Children[1]??? "(none)"}");
					}
				}
			}
			return false;
		}

		public static bool TryGetSkillAtKeyBind(string keyBind, out string skillName) {
			skillName = null;
			foreach ( var child in GetGame().IngameState.IngameUi.SkillBar.Children ) {
				string thisBind = child.GetChildFromIndices(0, 0, 0, 1)?.Text; // the text label like "M5" or "Q" on the skillbar
				if ( thisBind.Equals(keyBind) ) {
					// start with the texture file name, something.dds
					string textureName = child.Texture?.Split('/').Last();
					// try to map it back to a friendly skill name
					if ( TextureReverseMapping.TryGetValue(textureName, out skillName) ) {
						return true;
					}
				}
			}
			return false;
		}

		public static State ClickOnSkillBarSkill(string skillName, State next = null) {
			return State.From((self) => {
				var elem = GetGame().IngameState.IngameUi.SkillBar.Children.Where(c => TextureReverseMapping.TryGetValue(c.Texture?.Split('/').Last() ?? "", out string result) && result.Equals(skillName)).FirstOrDefault();
				if( IsValid(elem) ) {
					return new LeftClickAt(elem, 30, 1, next);
				}
				return null;
			});
		}
		public static State ClickOnHiddenSkillBarSkill(string skillName, State next = null) {
			return State.From((self) => {
				var elem = GetGame().IngameState.IngameUi.HiddenSkillBar.Children.Where(c => TextureReverseMapping.TryGetValue(c.Texture?.Split('/').Last() ?? "", out string result) && result.Equals(skillName)).FirstOrDefault();
				if ( IsValid(elem) ) {
					return new LeftClickAt(elem, 30, 1, next);
				}
				return null;
			});
		}
		public static State ClickOnSkillBarHotkey(string keyBind, State next = null) {
			return State.From((self) => {
				var list = GetGame().IngameState.IngameUi.SkillBar.Children;
				for(int i = 0; i < list.Count; i++) {
					var child = list[i];
					if( child.GetChildFromIndices(0, 0, 0, 1)?.Text?.Equals(keyBind) ?? false ) {
						if ( i > 7 ) { // this is the second page
							Notify("I don't know how to use skills on the second page yet!", Color.Yellow);
							return null;
						} else {
							return new LeftClickAt(child, 30, 1, next);
						}
					}
				}
				return null;
			});
		}

		public static void ChangeKeyBindToSkill(string keyBind, string skillName) {

		}
		*/

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
