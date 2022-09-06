using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	internal static class EquipmentManager {
		private static Dictionary<string, float> modGroups = new Dictionary<string, float>();
		private static Dictionary<string, InventoryIndex> hasExactMod = new Dictionary<string, InventoryIndex>();
		private static Stopwatch timer = Stopwatch.StartNew();
		private static InventoryIndex[] equippedInventories = new InventoryIndex[] {
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
			InventoryIndex.Flask
		};
		static EquipmentManager() {
			OnRelease(Keys.I, RefreshInventory);
		}
		public static bool HasEquippedMod(string name) {
			return hasExactMod.ContainsKey(name);
		}
		internal static void RefreshInventory() {
			var game = GetGame();
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) {
				Log("No panel.");
				return;
			}
			if ( !panel.IsVisible ) {
				return;
			}
			modGroups.Clear();
			hasExactMod.Clear();
			timer.Stop();
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
				// Log($"Inventory {equipIndex}: {equipInventory.IsValid} {equipItems.Count}");
				if ( equipItems.Count == 0 ) continue;
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
					hasExactMod[mod.Name] = equipIndex;
					float value = mod.Value1;
					if( modGroups.TryGetValue(mod.Group, out float prev) ) {
						value += prev;
					}
					modGroups[mod.Group] = value;
				}
			}
			// foreach(var group in modGroups.Keys) { Log($"Mod Group: {group} Sum: {modGroups[group]}"); }

		}

		internal static void OnTick() {
			if( timer.ElapsedMilliseconds > 10000 ) {
				RefreshInventory();
				timer.Restart();
			}
		}
	}
}