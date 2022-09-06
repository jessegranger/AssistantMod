using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant {
	static class FlaskManager {

		public static bool IsFlask(Entity ent) {
			if ( !IsValid(ent) ) return false;
			string pathName = ent.Path.Substring(ent.Path.LastIndexOf("/") + 1);
			return pathName.StartsWith("Flask");
		}

		private static Dictionary<string, int> baseHealAmount = new Dictionary<string, int>() {
			{ "FlaskLife1", 70 },
			{ "FlaskLife2", 150 },
			{ "FlaskLife3", 250 },
			{ "FlaskLife4", 360 },
			{ "FlaskLife5", 640 },
			{ "FlaskLife6", 830 },
			{ "FlaskLife7", 1000 },
			{ "FlaskLife8", 1200 },
			{ "FlaskLife9", 1990 },
			{ "FlaskLife10", 1460 },
			{ "FlaskLife11", 2400 },
			{ "FlaskLife12", 2080 },
		};
		private static Dictionary<string, int> baseManaAmount = new Dictionary<string, int>() {
			{ "FlaskMana1", 50 },
			{ "FlaskMana2", 70 },
			{ "FlaskMana3", 90 },
			{ "FlaskMana4", 120 },
			{ "FlaskMana5", 170 },
			{ "FlaskMana6", 250 },
			{ "FlaskMana7", 350 },
			{ "FlaskMana8", 480 },
			{ "FlaskMana9", 700 },
			{ "FlaskMana10", 1100 },
			{ "FlaskMana11", 1400 },
			{ "FlaskMana12", 1800 },
		};
		private static Dictionary<string, int> baseDuration = new Dictionary<string, int>() {
			{ "FlaskLife1", 6000 },
			{ "FlaskLife2", 7000 },
			{ "FlaskLife3", 7000 },
			{ "FlaskLife4", 7000 },
			{ "FlaskLife5", 7000 },
			{ "FlaskLife6", 7000 },
			{ "FlaskLife7", 7000 },
			{ "FlaskLife8", 7000 },
			{ "FlaskLife9", 7000 },
			{ "FlaskLife10", 7000 },
			{ "FlaskLife11", 7000 },
			{ "FlaskLife12", 7000 },
			{ "FlaskMana1", 4000 },
			{ "FlaskMana2", 4000 },
			{ "FlaskMana3", 4000 },
			{ "FlaskMana4", 4000 },
			{ "FlaskMana5", 4000 },
			{ "FlaskMana6", 4500 },
			{ "FlaskMana7", 5000 },
			{ "FlaskMana8", 5500 },
			{ "FlaskMana9", 6000 },
			{ "FlaskMana10", 6500 },
			{ "FlaskMana11", 6000 },
			{ "FlaskMana12", 4000 },
			{ "FlaskHybrid1", 5000 }, // Large Hybrid
			{ "FlaskHybrid2", 5000 }, // Large Hybrid
			{ "FlaskHybrid3", 5000 }, // Large Hybrid
			{ "FlaskHybrid4", 5000 }, // Large Hybrid
			{ "FlaskHybrid5", 5000 }, // Large Hybrid
			{ "FlaskHybrid6", 5000 }, // Large Hybrid
			{ "FlaskUtility1", 4000 }, // Diamond Flask
			{ "FlaskUtility2", 4000 }, // Ruby Flask
			{ "FlaskUtility3", 4000 }, // Sapphire Flask
			{ "FlaskUtility4", 4000 }, // Topaz Flask
			{ "FlaskUtility5", 4000 }, // Granite Flask
			{ "FlaskUtility6", 4000 }, // Quicksilver Flask
			{ "FlaskUtility7", 3500 }, // Amethyst Flask
			{ "FlaskUtility8", 4000 }, // Quartz Flask
			{ "FlaskUtility9", 4000 }, // Jade Flask
			{ "FlaskUtility10", 4500 }, // Basalt Flask
			{ "FlaskUtility11", 4000 }, // ???
			{ "FlaskUtility12", 5000 }, // Stibnite Flask
			{ "FlaskUtility13", 4000 }, // Sulphur Flask
			{ "FlaskUtility14", 5000 }, // Silver Flask
			{ "FlaskUtility15", 5000 }, // Bismuth Flask
		};
		private static Dictionary<string, string> baseBuffName = new Dictionary<string, string>() {
			{ "FlaskLife1", "flask_effect_life"},
			{ "FlaskLife2", "flask_effect_life"},
			{ "FlaskLife3", "flask_effect_life"},
			{ "FlaskLife4", "flask_effect_life"},
			{ "FlaskLife5", "flask_effect_life"},
			{ "FlaskLife6", "flask_effect_life"},
			{ "FlaskLife7", "flask_effect_life"},
			{ "FlaskLife8", "flask_effect_life"},
			{ "FlaskLife9", "flask_effect_life"},
			{ "FlaskLife10", "flask_effect_life"},
			{ "FlaskLife11", "flask_effect_life"},
			{ "FlaskLife12", "flask_effect_life"},
			{ "FlaskMana1", "flask_effect_mana"},
			{ "FlaskMana2", "flask_effect_mana"},
			{ "FlaskMana3", "flask_effect_mana"},
			{ "FlaskMana4", "flask_effect_mana"},
			{ "FlaskMana5", "flask_effect_mana"},
			{ "FlaskMana6", "flask_effect_mana"},
			{ "FlaskMana7", "flask_effect_mana"},
			{ "FlaskMana8", "flask_effect_mana"},
			{ "FlaskMana9", "flask_effect_mana"},
			{ "FlaskMana10", "flask_effect_mana"},
			{ "FlaskMana11", "flask_effect_mana"},
			{ "FlaskMana12", "flask_effect_mana"},
			{ "FlaskHybrid1", "flask_effect_mana"}, // Large Hybrid
			{ "FlaskHybrid2", "flask_effect_mana"}, // Large Hybrid
			{ "FlaskHybrid3", "flask_effect_mana"}, // Large Hybrid
			{ "FlaskHybrid4", "flask_effect_mana"}, // Large Hybrid
			{ "FlaskHybrid5", "flask_effect_mana"}, // Large Hybrid
			{ "FlaskHybrid6", "flask_effect_mana"}, // Large Hybrid
			{ "FlaskUtility1", "flask_utility_critical_strike_chance"}, // Diamond Flask
			{ "FlaskUtility2", "flask_utility_resist_fire"}, // Ruby Flask
			{ "FlaskUtility3", "flask_utility_resist_cold"}, // Sapphire Flask
			{ "FlaskUtility4", "flask_utility_resist_lightning"}, // Topaz Flask
			{ "FlaskUtility5", "flask_utility_ironskin"}, // Granite Flask
			{ "FlaskUtility6", "flask_utility_sprint" }, // Quicksilver Flask
			{ "FlaskUtility7", "flask_utility_resist_chaos" }, // Amethyst Flask
			{ "FlaskUtility8", "flask_utility_phase" }, // Quartz Flask
			{ "FlaskUtility9", "flask_utility_evasion" }, // Jade Flask
			{ "FlaskUtility10", "flask_utility_stone" }, // Basalt Flask
			{ "FlaskUtility11", "flask_utility_aquamarine" }, // Aquamarine Flask
			{ "FlaskUtility12", "flask_utility_smoke"}, // Stibnite Flask
			{ "FlaskUtility13", "flask_utility_consecrate" }, // Sulphur Flask
			{ "FlaskUtility14", "flask_utility_haste"}, // Silver Flask
			{ "FlaskUtility15", "flask_utility_prismatic" }, // Bismuth Flask
			{ "FlaskUtility16", "flask_utility_rarity" }, // Gold Flask
			{ "FlaskUtility17", "flask_utility_stun_protection" }, // Corundum Flask
		};
		public class Flask {
			public static Stopwatch GlobalTimer = Stopwatch.StartNew();
			public Stopwatch UseTimer = new Stopwatch();
			public VirtualKeyCode Key;
			public string PathName; // the last part of the full Path
			public int CurrentCharges;
			public int MaxCharges;
			public int ChargesPerUse;
			public int LifeHealAmount;
			public int ManaHealAmount;
			public int Duration;
			public bool IsInstant;
			public bool IsInstantOnLowLife;
			public bool EnchantUseOnFull;
			public bool EnchantUseOnHitRare;
			public bool IsFull => CurrentCharges > 0 && CurrentCharges >= MaxCharges;
			public bool HasEnoughCharge => CurrentCharges >0 && CurrentCharges >= ChargesPerUse;
			public bool IsUsable(int cooldown) => HasEnoughCharge && GlobalTimer.ElapsedMilliseconds > 200 && ((!UseTimer.IsRunning) || UseTimer.ElapsedMilliseconds > cooldown);
			public Flask() {
			}
			public bool UseFlask(int cooldown) {
				if ( IsUsable(cooldown) ) {
					GlobalTimer.Restart();
					UseTimer.Restart();
					Log($"UseFlask: {Key}");
					PressKey(Key, 30, 30);
					return true;
				}
				return false;
			}

			public bool IsActive() => baseBuffName.TryGetValue(PathName, out string buff) && HasBuff(buff);

			public override string ToString() => $"{PathName} [{CurrentCharges}/{MaxCharges}] hp:{LifeHealAmount} mp:{ManaHealAmount} {Duration} ms";


		}
		private static readonly Flask[] flasks = new Flask[]
		{
						new Flask() { Key = VirtualKeyCode.VK_1 },
						new Flask() { Key = VirtualKeyCode.VK_2 },
						new Flask() { Key = VirtualKeyCode.VK_3 },
						new Flask() { Key = VirtualKeyCode.VK_4 },
						new Flask() { Key = VirtualKeyCode.VK_5 },
		};
		public static Flask GetFlask(int i) {
			return flasks[i];
		}

		public static void Initialise() {
			// when the system as a whole comes un-paused refresh the flasks
			OnRelease(VirtualKeyCode.PAUSE, RefreshFlaskMods);
			// bind HOME to just refresh flasks again (maybe the user moves them around)
			OnRelease(VirtualKeyCode.HOME, RefreshFlaskMods);
			PersistedText.Add(GetStatusText, (c) => ScreenRelativeToWindow(.305f, .983f), 0, Color.Orange);
		}

		private static string GetStatusText() => $"[{(IsPaused() ? "=" : ">")}]";

		private static Dictionary<string, Flask> conditionMap = new Dictionary<string, Flask>();
		private static bool TryGetFlaskForCondition(string condition, out Flask flask) => conditionMap.TryGetValue(condition, out flask);
		private static void LinkConditionToFlask(string condition, Flask flask) {
			Log($"Using flask for condition: {condition}");
			conditionMap[condition] = flask;
		}
		private static bool updateFlaskMods = true;
		public static void RefreshFlaskMods() => updateFlaskMods = true;
		private static void UpdateFlaskEnt(Entity flaskEnt, int posX) {
			if ( !IsValid(flaskEnt) ) return;
			Charges charges = flaskEnt.GetComponent<Charges>();
			if ( charges == null ) return;
			Flask flask = flasks[posX];
			flask.CurrentCharges = charges.NumCharges;
			flask.ChargesPerUse = charges.ChargesPerUse;
			flask.MaxCharges = charges.ChargesMax;
			if ( updateFlaskMods ) {
				int amount = 0;
				string pathName = flaskEnt.Path.Substring(flaskEnt.Path.LastIndexOf("/") + 1);
				flask.PathName = pathName;
				var quality = flaskEnt.GetComponent<Quality>();
				if ( baseDuration.TryGetValue(pathName, out flask.Duration) ) {
					if ( quality != null ) {
						flask.Duration = (int)((float)flask.Duration * ((100 + quality.ItemQuality) / 100f));
					}
				} else flask.Duration = 3000;
				// if this is a known Life flask
				if ( baseHealAmount.TryGetValue(pathName, out flask.LifeHealAmount) ) {
					if ( quality != null ) {
						flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * ((100 + quality.ItemQuality) / 100f));
					}
				} else flask.LifeHealAmount = 0;

				// handle Mana flasks
				if ( baseManaAmount.TryGetValue(pathName, out amount) ) {
					flask.ManaHealAmount = amount;
					if ( quality != null ) {
						flask.ManaHealAmount = (int)((float)amount * ((100 + quality.ItemQuality) / 100f));
					}
				} else flask.ManaHealAmount = 0;

				Mods flaskMods = flaskEnt.GetComponent<Mods>();
				foreach ( var mod in flaskMods.ItemMods ) {
					Log($"Flask Mod: {mod.Name} {mod.Value1}");
					switch ( mod.Name ) {
						case "FlaskIncreasedDuration":
							flask.Duration = (int)((float)flask.Duration * ((100 + mod.Value1) / 100f));
							break;
						case "FlaskEnchantmentInjectorOnFullCharges":
							flask.EnchantUseOnFull = true;
							break;
						case "FlaskEnchantmentInjectorOnHittingRareOrUnique":
							flask.EnchantUseOnHitRare = true;
							break;
						case "FlaskExtraCharges":
							flask.MaxCharges += mod.Value1;
							break;
						case "FlaskInstantRecoveryOnLowLife":
							flask.IsInstantOnLowLife = true;
							flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * .75f);
							break;
						case "FlaskFullInstantRecovery":
							flask.IsInstant = true;
							flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * .34f);
							break;
						case "FlaskPartialInstantRecovery":
							flask.IsInstant = true;
							flask.LifeHealAmount = (int)((float)flask.LifeHealAmount * .25f);
							break;
						case "FlaskRemovesBleeding":
						case "FlaskBleedCorruptingBloodImmunity":
							LinkConditionToFlask("bleeding", flask);
							LinkConditionToFlask("corrupted_blood", flask);
							LinkConditionToFlask("corrupting_blood", flask);
							break;
						case "FlaskEffectNotRemovedOnFullMana":
							flask.ManaHealAmount = (int)((float)flask.ManaHealAmount * .70f);
							flask.Duration = (int)((float)flask.Duration * .70f);
							break;
						case "FlaskRemovesShock":
						case "FlaskShockImmunity":
							LinkConditionToFlask("shocked", flask); break;
						case "FlaskCurseImmunity":
							LinkConditionToFlask("cursed", flask); break;
						case "FlaskDispellsChill":
						case "FlaskChillFreezeImmunity":
						case "FlaskFreezeAndChillImmunityDuringEffect":
							LinkConditionToFlask("chilled", flask);
							LinkConditionToFlask("frozen", flask); break;
						case "FlaskIgniteImmunityDuringEffect":
						case "FlaskIgniteImmunity":
						case "FlaskDispellsBurning":
							LinkConditionToFlask("burning", flask);
							LinkConditionToFlask("ignited", flask);
							break;
						case "FlaskDispellsPoison":
						case "FlaskPoisonImmunity":
							LinkConditionToFlask("poisoned", flask);
							break;
					}
				}
				Log($"Flask: {pathName} Charges: [{charges.NumCharges}/{charges.ChargesMax}] Quality: {quality.ItemQuality}% HP:{flask.LifeHealAmount} MP:{flask.ManaHealAmount}");
			}
		}
		private static void UpdateFlasks() {
			var game = GetGame();
			if ( !IsValid(game) ) {
				Log("Failed to update flasks: invalid Game object.");
				return;
			}
			var playerInventories = game.Game.IngameState.Data.ServerData.PlayerInventories;
			if ( playerInventories == null || playerInventories.Count == 0 ) {
				if ( updateFlaskMods ) {
					Log("Failed to update flasks: PlayerInventories is null or empty.");
				}
				updateFlaskMods = false;
				return;
			}
			var flaskInventory = playerInventories.FirstOrDefault(x => x.Inventory != null && x.Inventory.InventType == InventoryTypeE.Flask);
			if ( flaskInventory == null ) {
				if ( updateFlaskMods ) Log("Failed to update flasks: No inventory of flask type was found.");
				updateFlaskMods = false;
				return;
			}
			var serverInventory = flaskInventory.Inventory;
			if ( serverInventory == null || serverInventory.InventorySlotItems == null ) {
				if ( updateFlaskMods ) Log("Failed to update flasks: flaskInventory had no server inventory items.");
				updateFlaskMods = false;
				return;
			}
			if ( updateFlaskMods ) Log("Updating flasks...");
			foreach ( var item in serverInventory.InventorySlotItems ) {
				UpdateFlaskEnt(item.Item, item.PosX);
			}
			updateFlaskMods = false;
		}

		public static void OnTick() {
			UpdateFlasks();
			if ( IsPaused() ) return;
			CheckFlasks();
		}

		public static void Render() {
			var gfx = GetGraphics();
			for ( int i = 0; i < 5; i++ ) {
				Flask f = GetFlask(i);
				var pos = ScreenRelativeToWindow(.178f + (.028f * i), .898f);
				gfx.DrawText($"{(f.LifeHealAmount > 0 ? f.LifeHealAmount : f.ManaHealAmount)}", pos, (f.LifeHealAmount > 0 ? Color.Red : f.ManaHealAmount > 0 ? Color.Teal: Color.Yellow));
				// gfx.DrawText($"[{f.CurrentCharges}/{f.ChargesPerUse}]", pos, f.HasEnoughCharge ? SharpDX.Color.Yellow : SharpDX.Color.Orange);
			}
		}

		private static void CheckFlasks() {
			var api = GetGame();
			if ( !IsValid(api) ) return;
			if ( HasBuff("grace_period") ) return;
			if ( api.Area == null ) return;
			var area = api.Area.CurrentArea;
			if ( area == null ) return;
			if ( area.IsTown || area.IsHideout ) return;
			var player = api.Player;
			if ( !IsValid(player) ) return;
			Life life = player.GetComponent<Life>();
			if ( life == null ) return;
			var settings = GetSettings();
			if ( settings == null ) return;
			if ( settings.AutoCureDebuffs.Value && CheckConditions(life) ) return;
			CheckUseFlasks(life);
		}
		private static readonly Random r = new Random();

		private static bool CheckConditions(Life life) {
			foreach ( var condition in conditionMap.Keys ) {
				if ( HasBuff(condition) ) {
					var flask = conditionMap[condition];
					if ( flask.IsUsable(300) ) {
						return flask.UseFlask(300);
					} else {
						Log($"Cannot use flask for {condition} (not ready)");
					}
				}
			}
			if ( HasAnyBuff("curse_") ) {
				if ( conditionMap.TryGetValue("cursed", out Flask flask) ) {
					if ( flask.IsUsable(4000) ) {
						return flask.UseFlask(4000);
					}
				}
			}
			return false;
		}
		private static bool NeedLifeFlask(Life life) {
			var maxHp = life.MaxHP - life.TotalReservedHP;
			if( HasBuff("petrified_blood") ) {
				maxHp = Math.Min(maxHp, life.MaxHP / 2);
			}
			return life.CurHP <= maxHp / 2;
		}
		private static bool NeedManaFlask(Life life) {
			var maxMana = life.MaxMana - life.TotalReservedMana;
			var manaThreshold = maxMana * .4f;
			var curMana = life.CurMana;
			var settings = GetSettings();
			if( settings?.DebugLife ?? false ) {
				DrawTextAtPlayer($"Mana: {curMana} Threshold: {manaThreshold} Active: {HasAnyBuff("flask_effect_mana")}");
			}
			return curMana < manaThreshold && !HasAnyBuff("flask_effect_mana");
		}

		private static bool CheckUseFlasks(Life life) {
			var settings = GetSettings();
			bool needLifeFlask = NeedLifeFlask(life);
			bool hasActiveLifeFlask = HasBuff("flask_effect_life");
			var isLowLife = life.CurHP <= life.MaxHP / 2;
			Flask slowFlask = null;

			bool needManaFlask = NeedManaFlask(life);
			bool hasActiveManaFlask = HasBuff("flask_effect_mana");
			bool hasHitNearbyRare = settings.MaintainFlasksOnRare && NearbyEnemies(300).Any(e => IsAlive(e) && e.Rarity >= MonsterRarity.Rare && !IsFullLife(e));
			foreach ( var flask in flasks ) {
				if ( settings.UseLifeFlasks && needLifeFlask && (!hasActiveLifeFlask) && flask.LifeHealAmount > 0 && flask.IsUsable(200) ) {
					if ( flask.IsInstant || (flask.IsInstantOnLowLife && isLowLife) ) {
						return flask.UseFlask(200);
					} else {
						slowFlask = slowFlask ?? flask;
					}
				} else if( settings.UseManaFlasks && needManaFlask && (!hasActiveManaFlask) && flask.ManaHealAmount > 0 && flask.IsUsable(200) ) {
					return flask.UseFlask(200);
				} else if( settings.AutoUseFullPotions 
					&& flask.IsFull
					&& flask.LifeHealAmount == 0 && flask.ManaHealAmount == 0
					&& !flask.IsActive()
					&& flask.UseFlask(2000 + r.Next(1,100))) {
					return true;
				} else if( settings.MaintainFlasksOnRare
					&& hasHitNearbyRare
					&& flask.LifeHealAmount == 0
					&& flask.ManaHealAmount == 0
					&& (!flask.EnchantUseOnHitRare)
					&& flask.HasEnoughCharge
					&& (!flask.IsActive())
					&& flask.UseFlask(2000 + r.Next(1,100)) ) {
					return true;
				}
			}
			return slowFlask?.UseFlask(200) ?? false;
		}

	}

}
