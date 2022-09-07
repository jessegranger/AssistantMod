using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Assistant.Globals;

namespace Assistant {
	static class Inventory {

		// the physical layout of the backpack:
		private static NormalInventoryItem[,] inventoryMap = new NormalInventoryItem[12, 5]; // one Entity appears more than once if it takes up more than one grid
		public static void Initialise() {
			OnRelease(Keys.I, () => {
				try { RefreshBackpack(); } catch ( Exception e ) { Log(e.StackTrace); }
			});
		}
		private class ExpectedItem : NormalInventoryItem {
			public ExpectedItem(int x, int y, int w, int h):base() {
				X = x; Y = y; W = w; H = h;
				Address = 0;
			}
			private int X;
			private int Y;
			private int W;
			private int H;
			public override int InventPosX => X;
			public override int InventPosY => Y;
			public override int ItemWidth => W;
			public override int ItemHeight => H;
		}
		private static void MarkOccupied(NormalInventoryItem[,] map, NormalInventoryItem item) {
			uint x = (uint)Math.Max(0, item.InventPosX);
			uint y = (uint)Math.Max(0, item.InventPosY);
			uint w = (uint)Math.Max(0, item.ItemWidth);
			uint h = (uint)Math.Max(0, item.ItemHeight);
			for(uint i = 0; i < w; i++ ) {
				for(uint j = 0; j < h; j++) {
					map[x + i, y + j] = item;
				}
			}
		}
		public static void MarkExpected(int x, int y, int w, int h) {
			MarkOccupied(inventoryMap, new ExpectedItem(x, y, w, h));
		}
		public static void MarkExpected(Vector2 relPos, int w, int h) {
			var pos = ScreenRelativeToBackpackSlot(relPos);
			MarkExpected((int)pos.X, (int)pos.Y, w, h);
		}
		internal static void RefreshBackpack() {
			var game = GetGame();
			var panel = game?.IngameState?.IngameUi?.InventoryPanel ?? null;
			if ( panel == null ) {
				Log("No panel.");
				return;
			}
			var map = new NormalInventoryItem[12, 5];
			var backpack = panel[InventoryIndex.PlayerInventory];
			foreach(var item in backpack?.VisibleInventoryItems ?? Empty<NormalInventoryItem>() ) {
				MarkOccupied(map, item);
			}
			inventoryMap = map;

			// debug output:
			for ( uint dy = 0; dy < 5; dy++ ) {
				string line = "";
				for ( uint dx = 0; dx < 12; dx++ ) {
					if ( inventoryMap[dx, dy] != null ) line += "x";
					else line += "_";
				}
				Log(line);
			}
		}

		public static NormalInventoryItem FindFirstItem(string path) {
			var game = GetGame();
			if ( game == null ) return null;
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return null;
			if ( !panel.IsVisible ) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return null;
			var items = playerInventory.VisibleInventoryItems;
			if ( items == null ) return null;
			return items.FirstOrDefault(x => x.Item != null && x.Item.IsValid && x.Item.Path != null && x.Item.Path.Equals(path));
		}

		public static NormalInventoryItem FindFirstItemWithoutMods(string path, params string[] modNames) {
			var game = GetGame();
			if ( game == null ) return null;
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return null;
			if ( !panel.IsVisible ) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return null;
			IEnumerable<NormalInventoryItem> items;
			if ( path.EndsWith("*") ) {
				path = path.Substring(0, path.Length - 1);
				items = playerInventory.VisibleInventoryItems.Where(x => x.Item.Path.StartsWith(path));
			} else {
				items = playerInventory.VisibleInventoryItems.Where(x => x.Item.Path.Equals(path));
			}
			return items.FirstOrDefault(x => {
				return CountMatchingMods(x.Item.GetComponent<Mods>().ItemMods, modNames) < modNames.Length;
			});
		}

		public static NormalInventoryItem FindFirstNonMatch(string path, ItemQuery query) {
			var game = GetGame();
			if ( game == null ) return null;
			var panel = game.IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return null;
			if ( !panel.IsVisible ) return null;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return null;
			foreach ( var item in playerInventory.VisibleInventoryItems ) {
				if ( !IsValid(item) ) continue;
				if ( !item.Item.Path.Equals(path) ) continue;
				if ( !query.Matches(item) ) return item;
			}
			return null;
		}

		public static int CountMatchingMods(List<ItemMod> itemMods, params string[] targetMods) {
			int count = 0;
			foreach ( string mod in targetMods ) {
				if ( HasMod(itemMods, mod) ) count++;
			}
			return count;
		}

		internal static State PlanUseItemOnItem(string path, NormalInventoryItem item, uint clicks = 1, State next = null) {
			if ( !IsValid(item) ) {
				Log("UseItemOnItem: target is invalid");
				return null;
			}
			Log($"UseItemOnItem({path}, {item.Item.Path})");
			Vector2 targetItemPosition = item.GetClientRect().Center;

			// leftClick will be a sequence of states like: SHIFT, CLICK, CLICK, CLICK, SHIFT UP
			// but we build it in reverse (so we can chain the Next pointers easily)
			// start with the tail piece: CLICK, SHIFT UP
			State leftClick = new LeftClickAt(targetItemPosition, inputSpeed, clicks, new KeyUp(Keys.LShiftKey, new Delay(inputSpeed, next)));
			// attach the header: SHIFT DOWN
			leftClick = new KeyDown(Keys.LShiftKey, new Delay(50, leftClick));
			// finish with the plan to use the stash item
			return PlanUseItem(path, leftClick);
		}

		internal static State PlanUseItem(NormalInventoryItem item, State next, State fail) => IsValid(item) ? new RightClickAt(item, inputSpeed, next) : fail;

		internal static State PlanUseItem(string path, State next = null) {
			return PlanUseItem(FindFirstItem(path), next, State.From((s) => {
				Log($"UseItem: Cannot find any {path} to use.");
				return null;
			}));
		}

		public static NormalInventoryItem FindFirstStashItem(string path) => StashItems().Where(IsValid).Where((i) => i.Item.Path.Equals(path)).FirstOrDefault();

		internal static State PlanUseStashItem(string path, State next = null) {
			if ( !StashIsOpen() ) return null;
			var useItem = FindFirstStashItem(path);
			if( IsValid(useItem) ) {
				return new RightClickAt(useItem, inputSpeed, next);
			}
			Log($"UseStashItem: Cannot find any {path} to use.");
			return null;
		}
		internal static State PlanUseStashItemOnItem(string path, NormalInventoryItem item, uint clicks = 1, State next = null) {
			var pos = item.GetClientRect().Center;
			return PlanUseStashItem(path, 
				new KeyDown(Keys.LShiftKey, new Delay(50,
				new LeftClickAt(pos, inputSpeed, clicks,
				new KeyUp(Keys.LShiftKey, new Delay(inputSpeed, next))))));
		}

		public static bool UseItemOnItem(string path, NormalInventoryItem item, uint clicks = 1) {
			State plan = PlanUseItemOnItem(path, item, clicks);
			if ( plan != null ) {
				Run(plan);
				return true;
			}
			return false;
		}

		internal static State PlanStashAll(State next = null) {
			var settings = GetSettings();
			var doNotStashSet = new HashSet<NormalInventoryItem>();
			var doNotIdentifySet = new HashSet<NormalInventoryItem>();
			var doNotIncubateSet = new HashSet<NormalInventoryItem>();
			var doNotOpenSet = new HashSet<NormalInventoryItem>();
			return State.From("StashAll", (state) => {
				var needs = new Dictionary<string, int>(restockNeeds);
				if ( !StashIsOpen() ) {
					Log("Stash not open.");
					return null;
				}
				if ( !BackpackIsOpen() ) {
					Log("Backpack is not open.");
					return null;
				}
				RefreshBackpack();
				foreach ( var item in BackpackItems() ) {
					if ( doNotStashSet.Contains(item) ) { continue; }
					if ( !IsValid(item) ) { continue; }
					var ent = item.Item;
					var mods = ent.GetComponent<Mods>();
					if ( !doNotIdentifySet.Contains(item) && (!(mods?.Identified ?? true)) ) {
						doNotIdentifySet.Add(item); // never try to identify it twice
						if ( ent.Path.StartsWith(PATH_MAP_PREFIX) && ! settings.IdentifyMaps ) {
							continue;
						}
						return PlanUseItemOnItem(PATH_SCROLL_WISDOM, item, 1, state);
					}
					if ( needs.TryGetValue(ent.Path, out int need) && need > 0 ) {
						needs[ent.Path] -= ent.GetComponent<Stack>()?.Size ?? 1;
						continue;
					}
					if ( ent.Path.Equals(PATH_STACKEDDECK) && settings.OpenStashedDecks ) continue; // a second pass will open these
					if ( ent.Path.StartsWith(PATH_INCUBATOR_PREFIX) && !doNotIncubateSet.Contains(item) ) {
						doNotIncubateSet.Add(item);
						return PlanApplyIncubator(item, state);
					}
					var pos = item.GetClientRect().Center;
					if ( !IsValid(pos) ) { continue; }
					Log($"Stashing {ent.Path} with a ctrl-left-click at: {pos}");
					doNotStashSet.Add(item); // never try to stash it twice
					return new CtrlLeftClickAt(pos.X, pos.Y, inputSpeed, state);
				}
				Func<NormalInventoryItem, State, State> open = (item, nextDeck) => {
					var deckPosition = item.GetClientRect().Center;
					var stackSize = item.Item.GetComponent<Stack>().Size;
					return State.From((nextCard) => {
						if ( !BackpackIsOpen() ) return null;
						if( stackSize > 0 ) {
							Vector2 pos2 = GetFreeSlot(1, 1);
							if ( pos2 == Vector2.Zero ) {
								Log("No more open space found.");
								return null;
							}
							stackSize -= 1;
							MarkExpected(pos2, 1, 1);
							pos2 = ScreenRelativeToWindow(pos2);
							return new RightClickAt(deckPosition, inputSpeed,
								new LeftClickAt(pos2, inputSpeed, 1,
									new Delay(inputSpeed, nextCard)));
						}
						return nextDeck;
					});
				};
				return State.From((nextDeck) => {
					if ( !BackpackIsOpen() ) return null;
					RefreshBackpack();
					foreach ( var item in BackpackItems() ) {
						if ( !IsValid(item) ) { continue; }
						var ent = item.Item;
						if ( ent.Path.Equals(PATH_STACKEDDECK) ) {
							return open(item, new Delay(400, nextDeck));
						}
					}
					return null;
				});

			});
		}
		internal static void StashDeposit(State next = null) {
			Run(PlanTeleportHome(
				PlanOpenStash(
					PlanStashAll(next))));
		}

		private static Dictionary<string, int> restockNeeds = new Dictionary<string, int>() {
			{  PATH_SCROLL_WISDOM, 40 },
			{  PATH_SCROLL_PORTAL, 40 },
			{  PATH_REMNANT_OF_CORRUPTION, 9 },
		};
		private static readonly Keys[] numberKeys = new Keys[] {
			Keys.D0,
			Keys.D1,
			Keys.D2,
			Keys.D3,
			Keys.D4,
			Keys.D5,
			Keys.D6,
			Keys.D7,
			Keys.D8,
			Keys.D9,
		};
		internal static uint inputSpeed = 16;

		internal static State PlanTeleportHome(State next = null) {
			return State.From("TeleportHome", (state) => {
				var game = GetGame();
				if( !IsValid(game) ) {
					Log("TeleportHome: No valid game controller.");
					return null;
				}
				if( game.IsLoading ) {
					DrawTextAtPlayer("TelportHome: waiting for Loading screen...");
					return state;
				}
				if( ChatIsOpen() ) {
					Log("TeleportHome: Chat is open unexpectedly, aborting.");
					return null;
				}
				if( StashIsOpen() ) {
					Log("TeleportHome: Stash is open (we must be home already).");
					return next;
				}
				if( !IsIdle() ) {
					Log("TeleportHome: waiting for movement to stop.");
					return new Delay(100, state);
				}
				var area = game.Area;
				if ( !IsValid(area) ) {
					DrawTextAtPlayer($"TeleportHome: waiting for valid area...");
					return state; // can be invalid during the loading transition to hideout
				}
				if ( area.CurrentArea.IsHideout || area.CurrentArea.Name.Equals("Azurite Mine") ) {
					Log($"TeleportHome: success!");
					return next; // success!
				}
				if( area.CurrentArea.IsTown ) {
					Log("TeleportHome: using /hideout to get home from town.");
					return new Delay(200, PlanChatCommand("/hideout", new Delay(1000, state)));
				}
				var label = GetNearestGroundLabel(PATH_PORTAL);
				if( IsValid(label) ) {
					Log($"TeleportHome: found portal label, clicking it.");
					return new LeftClickAt(label.Label, 50, 1, new Delay(2000, state));
				}
				if ( !BackpackIsOpen() ) {
					Log($"TeleportHome: opening backpack");
					return new KeyDown(Keys.I, new Delay(500, state));
				}
				Log($"TeleportHome: using a Portal scroll");
				return PlanUseItem(PATH_SCROLL_PORTAL, new Delay(500, state));
			});
		}

		internal static State PlanOpenStash(State next = null) {
			return State.From("OpenStash", (state) => {
				if ( ChatIsOpen() ) {
					Log("OpenStash: Chat is open unexpectedly, aborting.");
					return null;
				}
				if ( StashIsOpen() ) {
					Log("OpenStash: success!");
					return next; // success!
				}
				if ( !IsIdle() ) {
					Log("OpenStash: waiting for motion to stop.");
					return new Delay(500, state); // wait for movement to stop
				}
				var game = GetGame();
				if( !IsValid(game) ) {
					Log("OpenStash: No valid game controller.");
					return null;
				}
				if( game.IsLoading ) {
					DrawTextAtPlayer("OpenStash: waiting for Loading screen...");
					return state;
				}
				var area = game.Area;
				if ( !IsValid(area) ) {
					Log("OpenStash: invalid area");
					return null;
				}
				// no stash outside town or hideout (not true: eg, the mine town)
				// if ( !(area.CurrentArea.IsHideout || area.CurrentArea.IsTown) ) {
					// Log("OpenStash: failed, cannot open a stash outside town or hideout.");
					// return null;
				// }
				var label = GetNearestGroundLabel(PATH_STASH);
				if( IsValid(label) ) {
					Log("OpenStash: found a Stash label, clicking it.");
					return new LeftClickAt(label.Label, 20, 1, new Delay(300, state));
				}
				return State.WaitFor(1000,
					() => GetNearestGroundLabel(PATH_STASH) != null,
					state,
					State.From(() => Log($"OpenStash: no Stash label found")));
			});
		}


		internal static State PlanRestockFromStash(State next = null) {
			return State.From((state) => {
				var game = GetGame();
				if ( !IsValid(game) ) return null;
				if ( game.IsLoading ) return state;
				if ( !StashIsOpen() ) return null;
				if ( !BackpackIsOpen() ) return null;
				RefreshBackpack();
				var needs = new Dictionary<string, int>(restockNeeds);
				var targets = new Dictionary<string, NormalInventoryItem>();
				// first scan what we already have
				foreach ( var item in BackpackItems() ) {
					var ent = item.Item;
					var path = ent.Path;
					if ( needs.ContainsKey(path) ) {
						var stack = ent.GetComponent<Stack>();
						var size = stack?.Size ?? 1;
						var max = stack?.Info?.MaxStackSize ?? 1;
						needs[path] -= size;
						// make note while we scan of where we need to restock partial stacks
						targets[path] = item;
					}
				}
				foreach ( var item in StashItems() ) {
					if ( !IsValid(item) ) continue;
					var ent = item.Item;
					needs.TryGetValue(ent.Path, out int need);
					if ( need > 0 ) {
						Vector2 sourcePos = item.GetClientRect().Center;
						if ( !IsValid(sourcePos) ) continue;
						Vector2 targetPos = Vector2.Zero;
						if ( targets.TryGetValue(ent.Path, out NormalInventoryItem target) ) {
							targetPos = target.GetClientRect().Center;
						} else {
							var slot = GetFreeSlot(1, 1);
							if ( IsValid(slot) ) {
								targetPos = ScreenRelativeToWindow(slot);
							}
						}
						if ( !IsValid(targetPos) ) continue;
						var stackSize = item.Item.GetComponent<Stack>()?.Info?.MaxStackSize ?? 1;
						if ( need < 10 ) {
							// TODO: if need >= 10, it needs to play out a sequence of numberKey[i]'s
							return new ShiftLeftClickAt(sourcePos, inputSpeed,
								new PressKey(numberKeys[need], inputSpeed,
								new PressKey(Keys.Return, inputSpeed,
								new LeftClickAt(targetPos, inputSpeed, 1,
								new Delay(100,
									state)))));
						} else {
							return new CtrlLeftClickAt(sourcePos, inputSpeed, 
								new Delay(100, state));
						}
					}
				}
				return next;
			});
		}

		internal static void RestockFromStash() {
			Run(PlanRestockFromStash());
		}

		private static bool IsOccupied(uint x, uint y, uint w, uint h) {
			uint ex = Math.Min(12, x + w);
			uint ey = Math.Min(5, y + h);
			for ( uint dy = y; dy < ey; dy++ ) {
				for ( uint dx = x; dx < ex; dx++ ) {
					if ( inventoryMap[dx, dy] != null ) return true;
				}
			}
			return false;
		}

		private static Vector2 TopLeftRelativePosition = new Vector2(.625f, .545f);
		private static Vector2 TileRelativeSize = new Vector2(.03f, .049f);
		private static Vector2 TileRelativeCenter(uint tx, uint ty) => new Vector2(TopLeftRelativePosition.X + ((tx + .5f) * TileRelativeSize.X), TopLeftRelativePosition.Y + ((ty + .5f) * TileRelativeSize.Y));
		public static Vector2 ScreenRelativeToBackpackSlot(Vector2 relPos) {
			relPos = (relPos - TopLeftRelativePosition) / TileRelativeSize;
			relPos.X = (int)relPos.X;
			relPos.Y = (int)relPos.Y;
			return relPos;
		}
		internal static Vector2 GetFreeSlot(uint w, uint h) {
			Log($"GetFreeSlot({w},{h})");
			for ( uint dy = 0; dy < 5; dy++ ) {
				for ( uint dx = 0; dx < 12; dx++ ) {
					if ( !IsOccupied(dx, dy, w, h) ) {
						Log($"Found free slot {dx}, {dy}");
						return TileRelativeCenter(dx, dy);
					}
				}
			}
			return Vector2.Zero;
		}
		internal static void HighlightFreeSlot(uint w, uint h) {
			Log($"HighlightFreeSlot({w},{h})");
			RefreshBackpack();
			var pos = GetFreeSlot(w, h);
			if ( pos == Vector2.Zero ) PersistedText.Add("[No Free Slots]", ScreenRelativeToWindow(.5f, .5f), 4000, Color.White);
			else PersistedText.Add("[Free]", ScreenRelativeToWindow(pos.X, pos.Y), 4000, Color.White);
		}

		internal static NormalInventoryItem GetItemUnderCursor() {
			Vector2 pos = ScreenRelativeToBackpackSlot(WindowToScreenRelative(Input.MousePosition));
			Log($"Cursor is over inventory slot: {pos}");
			return inventoryMap[(int)pos.X, (int)pos.Y];
		}

		public static IEnumerable<NormalInventoryItem> EquippedItems() {
			var panel = GetGame()?.IngameState?.IngameUi?.InventoryPanel;
			if ( panel == null || !panel.IsVisible ) yield break;
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
						InventoryIndex.Boots
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
				yield return equipItem;
			}
			yield break;
		}

		internal static State PlanApplyIncubator(NormalInventoryItem incubator, State next = null) {
			var equipItem = EquippedItems().Where(item => IsValid(item) && item.Item.GetComponent<Mods>().IncubatorName == null).FirstOrDefault();
			if( equipItem == null ) {
				Log($"All items have incubators already.");
				return next;
			}
			return new RightClickAt(incubator, inputSpeed, new LeftClickAt(equipItem, inputSpeed, 1, new Delay(200, next)));
		}


	}
}
