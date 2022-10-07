using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	internal static class SkillManager {
		private static Stopwatch globalSkillUseTimer = new Stopwatch();
		internal static void Initialise() {
			foreach(var item in SkillData) {
				SkillDataByDisplayName[item.DisplayName] = item;
				SkillDataByInternalName[item.InternalName] = item;
			}
			// PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.690f, .976f), 0);
		}

		internal class SkillDataRecord {
			public string DisplayName;
			public string InternalName;
			public string Texture;
			public string BuffName;
			public SkillDataRecord(string displayName, string internalName, string texture, string buffName) {
				DisplayName = displayName;
				InternalName = internalName;
				Texture = texture;
				BuffName = buffName;
			}
		}

		// access to the game's data files is unreliable and difficult to navigate,
		// so we keep track of some critical skill data here for now
		public static List<SkillDataRecord> SkillData = new List<SkillDataRecord>() {
			new SkillDataRecord("Purity of Fire",      "FireResistAura",      "aurafireresist.dds",      "player_aura_fire_resist"),
			new SkillDataRecord("Purity of Lightning", "LightningResistAura", "auralightningresist.dds", "player_aura_lightning_resist" ),
			new SkillDataRecord("Purity of Ice",       "LightningResistAura", "auracoldresist.dds",      "player_aura_cold_resist" ),
			new SkillDataRecord("Purity of Elements",  "Purity",              "auraresist.dds",          "player_aura_resists" ),
			new SkillDataRecord("Arctic Armour",       "ArcticArmour",        "Iceshield.dds",           "new_arctic_armour" ),
			new SkillDataRecord("Summon Skitterbots",  "Skitterbots",         "LightningFireSkitterbots.dds", "skitterbots_buff"),
			new SkillDataRecord("Temporal Rift",       "TemporalRift",        "Chronomancer1.dds",       "chronomancer"),
			new SkillDataRecord("Herald of Thunder",   "HeraldOfThunder",     "HeraldOfThunder.dds",     "herald_of_thunder"),
			new SkillDataRecord("Herald of Ash",       "HeraldOfAsh",         "HeraldOfAsh.dds",         "herald_of_ash"),
			new SkillDataRecord("Herald of Ice",       "HeraldOfIce",         "HeraldOfIce.dds",         "herald_of_ice"),
			new SkillDataRecord("Anger",               "Wrath",               "aurafire.dds",            "player_aura_fire_damage" ),
			new SkillDataRecord("Wrath",               "Anger",               "auralightning.dds",       "player_aura_lightning_damage" ),
			new SkillDataRecord("Hatred",              "Hatred",              "auracold.dds",            "player_aura_cold_damage" ),
			new SkillDataRecord("Zealotry",            "SpellDamageAura",     "SpellDamageAura.dds",     "player_aura_spell_damage" ),
			new SkillDataRecord("Malevolence",         "DamageOverTimeAura",  "DeliriumAura.dds",        "player_aura_damage_over_time" ),
			new SkillDataRecord("Determination",       "Determination",       "auraarmour.dds",          "player_aura_armour" ),
			new SkillDataRecord("Grace",               "Grace",               "auraevasion.dds",         "player_aura_evasion" ),
			new SkillDataRecord("Tempest Shield",      "TempestShield",       "lightningshield.dds",     "lightning_shield" ),
			new SkillDataRecord("Discipline",          "Discipline",          "auraenergy.dds",          "player_aura_energy_shield" ),
			new SkillDataRecord("Clarity",             "Clarity",             "auramana.dds",            "player_aura_mana_regen" ),
			new SkillDataRecord("Vitality",            "Vitality",            "auraregen.dds",           "player_aura_life_regen" ),
			new SkillDataRecord("Precision",           "AccuracyAndCritsAura","auracrit.dds",            "player_aura_accuracy_and_crits" ),
			new SkillDataRecord("Haste",               "Haste",               "auraspeed.dds",           "player_aura_speed" ),
			new SkillDataRecord("War Banner",          "BloodstainedBanner",  "WarBanner.dds",           "bloodstained_banner_buff_aura" ),
			new SkillDataRecord("Defiance Banner",     "DefianceBanner",      "ArmourandEvasionBanner.dds","armour_evasion_banner_buff_aura" ),
			new SkillDataRecord("Dread Banner",        "PuresteelBanner",     "DreadBanner.dds",         "puresteel_banner_buff_aura" ),
		};

		public static Dictionary<string, SkillDataRecord> SkillDataByInternalName = new Dictionary<string, SkillDataRecord>();
		public static Dictionary<string, SkillDataRecord> SkillDataByDisplayName = new Dictionary<string, SkillDataRecord>();


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
		public static bool TryUseSkill(string skillName, Keys key) {
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

		public static bool TryUseVaalSkill(string skillName, Keys key) {
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
