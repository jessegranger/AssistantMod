using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Assistant.Globals;

namespace Assistant {
	static class Test {

		internal static void RunAll() {
			var game = GetGame();
			if ( game == null ) {
				Log("No game.");
				return;
			}
			//  InventoryReport();
			//  TestFlaskInventory();
			//  TestPlayerLife();
			//  TestStash();
			Equipment();
			Stats();
			//  TestNearbyBuffs();
			SkillBar();
			//  TestDeployedObjects();
			//  TestLabelsOnGround();
			//  InputManager.Add(InputManager.PlanChangeStashTab(16));

			/*
			InputManager.Add(
					new KeyDown(VirtualKeyCode.VK_1,
					new Delay(20,
					new KeyUp(VirtualKeyCode.VK_1,
					new Delay(20,
					new KeyDown(VirtualKeyCode.VK_2,
					new Delay(20,
					new KeyUp(VirtualKeyCode.VK_2))))))));
			*/

		}

		internal static void PlayerLife() {
			var player = GetGame()?.Player;
			if ( player == null ) {
				Log("No player.");
				return;
			}
			var life = player.GetComponent<Life>();
			if ( life == null ) {
				Log("No Life component.");
				return;
			}
			Log($"Life: {life.CurHP}/{life.MaxHP}-{life.TotalReservedHP} {life.HPPercentage}%");
			Log($"ES: {life.CurES}/{life.MaxES}");
			Log($"Mana: {life.CurMana}/{life.MaxMana}-{life.TotalReservedMana}");
		}

		internal static void FlaskInventory() {
			var game = GetGame();
			var panel = game.IngameState.IngameUi.InventoryPanel;
			var equip = panel[InventoryIndex.Flask];
			var items = equip.VisibleInventoryItems;
			foreach ( var item in items ) {
				var charges = item.Item.GetComponent<Charges>();
				Log($"Flask: x:{item.InventPosX} y:{item.InventPosY} {charges?.NumCharges ?? 0}");
			}
		}

		internal static void SkillBar() {
			var game = GetGame();
			var actor = game.Player.GetComponent<Actor>();
			Log($"Actor Skills: {actor.ActorSkills?.Count ?? 0}");
			foreach ( var skill in actor.ActorSkills ) Log($"Skill: {skill.Name} IsOnSkillBar:{skill.IsOnSkillBar} IsOnCooldown:{skill.IsOnCooldown} IsUsing:{skill.IsUsing} CanBeUsed:{skill.CanBeUsed}");
			Log($"Actor Vaal Skills: {actor.ActorVaalSkills?.Count ?? 0}");
			foreach (var skill in actor.ActorVaalSkills) Log(string.Format("Vaal Skill: {0} Souls:{1}/{2}", skill.VaalSkillInternalName, skill.CurrVaalSouls, skill.VaalMaxSouls));
		}

		internal static void Stash() {
			var stash = GetGame()?.IngameState?.IngameUi?.StashElement;
			if ( stash == null ) {
				Log("No stash (bad game offsets?).");
				return;
			}
			if ( !stash.IsVisible ) {
				Log("Stash not visible.");
				return;
			}
			Log($"Stash: Index:{stash.IndexVisibleStash}");
			var stashInv = stash.VisibleStash;
			if ( stashInv == null ) {
				Log("No VisibleStash inventory.");
				return;
			}
			Log($"Stash: Type: {stashInv.InvType}");
			var invRoot = stashInv.InventoryUIElement;
			if ( invRoot == null ) {
				Log("No InventoryUIElement.");
				return;
			}
			foreach ( var item in stashInv.VisibleInventoryItems ) {
				if ( !IsValid(item.Item) ) continue;
				Log($"Stash Item: {item.Item.Path} {item.LongText}");
			}
		}

		internal static void DeployedObjects() {
			var actor = GetGame()?.Player?.GetComponent<Actor>();
			if( actor == null ) {
				Log("No actor.");
				return;
			}
			Log($"Deployed Objects: {actor.DeployedObjectsCount}");
			foreach (var obj in actor.DeployedObjects) { Log(string.Format("Deployed Object: Id:{0} SkillKey:{1}", obj.ObjectId, obj.SkillKey)); }
		}
		internal static void LabelsOnGround() {
			var ui = GetGame()?.IngameState?.IngameUi;
			if( ui == null ) {
				Log("No UI.");
				return;
			}
			foreach (var label in ui.ItemsOnGroundLabelElement.LabelsOnGround) {
				Log(string.Format("Label: {0} Id:{3} Visible:{1} CanPickUp:{2}", label.Label?.Text ?? "null", label.IsVisible, label.CanPickUp, label.ItemOnGround.Id));
			}
		}
		internal static void Equipment() {
			var game = GetGame();
			var panel = game?.IngameState?.IngameUi?.InventoryPanel;
			if ( panel == null ) {
				Log("No panel.");
				return;
			}
			if ( !panel.IsVisible ) {
				Log("Not visible.");
				return;
			}
			var equippedInventories = new InventoryIndex[] {
						InventoryIndex.Helm,
						InventoryIndex.Amulet,
						InventoryIndex.Chest,
						InventoryIndex.LWeapon,
						InventoryIndex.RWeapon,
						InventoryIndex.LWeapon,
						InventoryIndex.RRing,
						InventoryIndex.LRing,
						InventoryIndex.Gloves,
						InventoryIndex.Belt,
						InventoryIndex.Boots,
						InventoryIndex.PlayerInventory
					};
			foreach ( var equipIndex in equippedInventories ) {
				var equipInventory = panel[equipIndex];
				if ( equipInventory == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} is null");
					continue;
				}
				var equipItems = equipInventory.VisibleInventoryItems;
				if ( equipItems == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} has null items");
					continue;
				}
				Log($"Inventory {equipIndex}: {equipInventory.IsValid} {equipItems.Count}");
				var equipItem = equipItems.FirstOrDefault();
				if ( equipItem == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} has null first item");
					continue;
				}
				var theItem = equipItem.Item;
				if ( theItem == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} does not reference a game item");
					continue;
				}
				var itemMods = theItem.GetComponent<Mods>();
				if ( itemMods == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} item does not have a Mods component");
					continue;
				}
				var theMods = itemMods.ItemMods;
				if ( theMods == null ) {
					Log($"Warn: Equipment Inventory {equipIndex} item Mods component does not have an ItemMods definition");
					continue;
				}
				foreach ( var mod in theMods ) {
					Log($" - Mod: {mod.Name} {mod.RawName} Group:{mod.Group} Value:{string.Join(" ", mod.Values)} Level:{mod.Level}");
				}
				Log($" - Incubator: {itemMods.IncubatorName ?? "null"}");
			}
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) {
				Log("No player inventory.");
				return;
			}
			foreach ( var item in playerInventory.VisibleInventoryItems ) {
				var ent = item.Item;
				if ( !IsValid(ent) ) continue;
				var baseItemType = game.Files.BaseItemTypes.Translate(ent.Path);
				ModDomain searchDomain = ModDomain.Item;
				if ( ent.Path.StartsWith("Metadata/Items/Jewels/JewelAbyss") ) {
					searchDomain = ModDomain.Abyss;
				} else if( ent.Path.StartsWith("Metadata/Items/Jewels/JewelPassiveTree")) {
					searchDomain = ModDomain.ClusterJewel;
				} else if( ent.Path.StartsWith("Metadata/Items/Jewels/") ) {
					searchDomain = ModDomain.Jewel;
				}
				
				Log($"Item: {ent.Path} {item.ItemWidth}x{item.ItemHeight}@({item.InventPosX},{item.InventPosY})");
				var mods = ent.GetComponent<Mods>();
				foreach ( var mod in (mods?.ItemMods ?? Empty<ItemMod>()) ) {
					var affixType = ModType.None;
					int minValue = 0;
					int maxValue = 0;
					int maxLevel = 0;
					string modName = mod.Name;
					string statName = "";
					string tags = "";
					
					foreach(ModType modType in Enum.GetValues(typeof(ModType)).Cast<ModType>()) {
						Tuple<string, ModType> modRecordKey = new Tuple<string, ModType>(mod.Group, modType);
						if( game.Files.Mods.recordsByTier.TryGetValue(modRecordKey, out List<ExileCore.PoEMemory.FilesInMemory.ModsDat.ModRecord> modRecords) ) {
							var tiers = modRecords
								.Where(rec => (searchDomain == ModDomain.Item) || rec.Domain == searchDomain) // matches the right item domain
								.Where(rec => Intersect(baseItemType.Tags, rec.Tags.Select(tag => tag.Key).ToArray()).Any()) // matches the right tags
								.Reverse()
								.ToArray();
							maxLevel = tiers.Length;
							if( mod.Level > maxLevel ) {
								Log($"Unknown mod Level: {mod.Level} of {maxLevel} known levels");
								continue;
							}
							var modRecord = tiers[mod.Level - 1];
							if ( modRecord != null ) {
								// Log($"Mod Record: [{modRecord.AffixType}] \"{modRecord.UserFriendlyName}\" {modRecord.Domain} {modRecord.StatRange[0]} {modRecord.StatNames[0]} Tags: {string.Join(",", modRecord.Tags.Select(tag => tag.Key))}");
								tags = string.Join(", ", modRecord.Tags.Select(tag => tag.Key));
								affixType = modRecord.AffixType;
								modName = modRecord.UserFriendlyName;
								minValue = modRecord.StatRange[0].Min;
								maxValue = modRecord.StatRange[0].Max;
								statName = modRecord.StatNames[0].ToString();
								break;
							}
						}
					}
					Log($" - Mod: ({affixType}, {tags}) (T{maxLevel - (mod.Level - 1)}) + {mod.Value1} to {statName} ({minValue}-{maxValue})");
				}
				Charges charges = ent.GetComponent<Charges>();
				if ( charges != null ) {
					Log($" - Charges: {charges.NumCharges}/{charges.ChargesMax}");
				}
				Quality quality = ent.GetComponent<Quality>();
				if ( quality != null ) {
					Log($" - Quality: {quality.ItemQuality}");
				}
				if ( FlaskManager.IsFlask(ent) ) {
					Log($" - Flask: {ent.RenderName}");
				}
				var sockets = ent.GetComponent<Sockets>();
				if( sockets != null ) {
					Log($" - Sockets: {string.Join("-", sockets.Links.Select(l => l.Length))} {string.Join(" ", sockets.SocketGroup)}");
				}
			}
		}

		internal static void Stats() {
			var stats = GetGame()?.Player.GetComponent<Stats>();
			if ( stats == null ) {
				Log("No Stats");
				return;
			}
			var dict = stats.ParseStats();
			var keys = dict.Keys.ToArray();
			foreach ( GameStat key in keys ) {
				// var value = dict[key];
				// if( value > 0 && value < 100 )
				if( key.ToString().Contains("Crit"))
				Log($"Stats Key: {key} {dict[key]}");
			}
		}
		internal static void NearbyBuffs() {
			var ents = GetGame()?.Entities ?? Empty<Entity>();
			foreach ( var ent in ents ) {
				Vector3 pos = ent.Pos;
				if ( pos == Vector3.Zero ) continue;
				var buffs = ent.GetComponent<Buffs>();
				if ( buffs == null ) continue;
				float zOffset = 0f;
				foreach ( var buff in buffs.BuffsList ) {
					if ( buff == null || buff.Address == 0 || buff.Name == null ) continue;
					if ( buff.Name.Contains("the") ) {
						PersistedText.Add(buff.Name, new Vector3(pos.X, pos.Y, pos.Z + zOffset), 3000, Color.Gray);
						zOffset -= 10.0f;
					}
				}
			}
		}

	}
}
