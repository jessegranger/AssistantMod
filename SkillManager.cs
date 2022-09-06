using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant {
	internal static class SkillManager {
		private static Stopwatch globalSkillUseTimer = new Stopwatch();
		internal static void Initialise() {
			// PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.690f, .976f), 0);
		}

		// private static string GetStatusText() => $"[{(IsPaused() ? "=" : ">")}]";

		internal static bool TryGetSkill(string skillName, out ActorSkill skill) {
			skill = null;
			var api = GetGame();
			if ( api == null ) return false;
			var actor = api.Player.GetComponent<Actor>();
			foreach ( var s in actor.ActorSkills ) {
				if ( s.Name.Equals(skillName) || s.InternalName.Equals(skillName) ) {
					skill = s;
					return true;
				}
			}
			return false;
		}
		public static bool SkillIsReady(string skillName) {
			if ( IsPaused() ) return false;
			if ( globalSkillUseTimer.IsRunning && globalSkillUseTimer.ElapsedMilliseconds < 133 ) return false;
			return GetSettings().ForceSkillUse || (TryGetSkill(skillName, out ActorSkill skill) && !skill.IsOnCooldown);
		}
		public static bool TryUseSkill(string skillName, VirtualKeyCode key) {
			if ( IsPaused() || !SkillIsReady(skillName) ) return false;
			var api = GetGame();
			if ( api == null ) return false;
			// var player = api.Player;
			// if (!HasEnoughMana(player, 3)) return false; // TODO: find out the real mana cost, for today hardcode to Berserk (4 mana)
			Log($"UseSkill: {skillName}");
			PressKey(key, 30, 30);
			globalSkillUseTimer.Restart();
			return true;
		}
		internal static bool TryGetVaalSkill(string skillName, out ActorVaalSkill skill) {
			skill = null;
			var actor = GetGame()?.Player.GetComponent<Actor>();
			if ( !IsValid(actor) ) return false;
			foreach ( var s in actor.ActorVaalSkills ) {
				if ( !IsValid(s) ) continue;
				if ( s.VaalSkillInternalName.Equals(skillName) ) {
					skill = s;
					return true;
				}
			}
			return false;
		}

		public static bool TryUseVaalSkill(string skillName, VirtualKeyCode key) {
			if ( IsPaused() ) return false;
			if ( globalSkillUseTimer.IsRunning && globalSkillUseTimer.ElapsedMilliseconds < 133 ) return false;
			if ( TryGetVaalSkill(skillName, out ActorVaalSkill skill) && skill.CurrVaalSouls == skill.VaalMaxSouls ) {
				PressKey(key, 30, 30);
				globalSkillUseTimer.Restart();
			}
			return false;
		}
	}
}
