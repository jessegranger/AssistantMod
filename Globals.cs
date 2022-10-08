using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Assistant {
	public static partial class Globals {
		static GameController Game;
		static Graphics Gfx;
		static AssistantSettings Settings;
		static bool IsInitialised;
		static State.Machine Machine = new State.Machine();

		public static void Initialise(GameController game, Graphics gfx, AssistantSettings settings) {
			Game = game;
			Gfx = gfx;
			Settings = settings;
			if( PowerOnSelfTest(game) ) {
				Run(TrackMovement);
				OnRelease(Keys.Pause, TogglePause);
				Game.Area.OnAreaChange += Area_OnAreaChange;
				timeInZone.Start();
				IsInitialised = true;
			} else {
				Settings.Enable.Value = false;
			}
			// Uncomment to see detailed state transitions in the logs
			// Machine.EnableLogging((s) => Log(s));
		}

		private static int postCount = 0;
		private static bool PowerOnSelfTest(bool cond, string text) {
			Log($"POST{++postCount}");
			if( ! cond ) Log("FAIL: " + text);
			return cond;
		}
		private static bool PowerOnSelfTest(GameController game) {
			return
				PowerOnSelfTest(game.Area != null, "Area is null") &&
				// PowerOnSelfTest((game.EntityListWrapper?.EntityCache?.Count ?? 0) > 0, "Has no entities") &&
				PowerOnSelfTest((game.IngameState != null), "Has no IngameState") &&
				PowerOnSelfTest((game.IngameState.IngameUi != null), "Has no IngameState.IngameUi") &&
				PowerOnSelfTest((game.IngameState.IngameUi.Map != null), "Has no ui.Map") &&
				PowerOnSelfTest((game.IngameState.IngameUi.Map.SmallMiniMap != null), "Has no ui.Map.SmallMiniMap") &&
				PowerOnSelfTest((game.IngameState.IngameUi.Map.LargeMap != null), "Has no ui.Map.LargeMap") &&
				PowerOnSelfTest(game.IngameState?.IngameUi?.InventoryPanel != null, "Has no inventoryPanel") &&
				PowerOnSelfTest(game.IngameState?.IngameUi?.StashElement != null, "Has no stashElement")
			;
		}

		public const string PATH_STACKEDDECK = "Metadata/Items/DivinationCards/DivinationCardDeck";
		public const string PATH_SCROLL_WISDOM = "Metadata/Items/Currency/CurrencyIdentification";
		public const string PATH_SCROLL_PORTAL = "Metadata/Items/Currency/CurrencyPortal";
		public const string PATH_CHISEL = "Metadata/Items/Currency/CurrencyMapQuality";
		public const string PATH_ALCHEMY = "Metadata/Items/Currency/CurrencyUpgradeToRare";
		public const string PATH_TRANSMUTATION = "Metadata/Items/Currency/CurrencyUpgradeToMagic";
		public const string PATH_ARMOUR_SCRAP = "Metadata/Items/Currency/CurrencyArmourQuality";
		public const string PATH_WHETSTONE = "Metadata/Items/Currency/CurrencyWeaponQuality";
		public const string PATH_ALTERATION = "Metadata/Items/Currency/CurrencyRerollMagic";
		public const string PATH_AUGMENT = "Metadata/Items/Currency/CurrencyAddModToMagic";
		public const string PATH_REGAL = "Metadata/Items/Currency/CurrencyUpgradeMagicToRare";
		public const string PATH_SCOUR = "Metadata/Items/Currency/CurrencyConvertToNormal";
		public const string PATH_FUSING = "Metadata/Items/Currency/CurrencyRerollSocketLinks";
		public const string PATH_REMNANT_OF_CORRUPTION = "Metadata/Items/Currency/CurrencyCorruptMonolith";
		public const string PATH_CLUSTER_SMALL = "Metadata/Items/Jewels/JewelPassiveTreeExpansionSmall";
		public const string PATH_CLUSTER_MEDIUM = "Metadata/Items/Jewels/JewelPassiveTreeExpansionMedium";
		public const string PATH_CLUSTER_LARGE = "Metadata/Items/Jewels/JewelPassiveTreeExpansionLarge";
		public const string PATH_SORCERER_BOOTS = "Metadata/Items/Armours/Boots/BootsInt9";
		public const string PATH_PORTAL = "Metadata/MiscellaneousObjects/MultiplexPortal";
		public const string PATH_STASH = "Metadata/MiscellaneousObjects/Stash";
		public const string PATH_MAP_PREFIX = "Metadata/Items/Maps/";
		public const string PATH_INCUBATOR_PREFIX = "Metadata/Items/Currency/CurrencyIncubation";

		public static Graphics GetGraphics() => Gfx;
		public static GameController GetGame() => Game;
		public static AssistantSettings GetSettings() => Settings;
		public static IngameUIElements GetUI() => Game?.IngameState?.IngameUi;

		public static Entity GetPlayer() => Game?.Player;

		public static RectangleF GetWindow() => Game?.Window?.GetWindowRectangleTimeCache ?? RectangleF.Empty;
		public static float GetLineHeight() => 18f; // TODO: read the font setting from Core

		private static int bottomRightLineCount = 0;
		private static int topLeftLineCount = 0;

		private static float bottomTextPadding = 4f;
		private static float rightTextPadding = 4f;
		private static float topTextPadding = 100f;
		private static float leftTextPadding = 10f;
		internal static Vector2 NextBottomRightLinePos() {
			return GetWindow().BottomRight - new Vector2(
				rightTextPadding,
				bottomTextPadding + (GetLineHeight() * (1 + bottomRightLineCount))
			);
		}
		internal static Vector2 NextTopLeftLinePos() {
			return GetWindow().TopLeft + new Vector2(
				leftTextPadding,
				topTextPadding + (GetLineHeight() * (1 + topLeftLineCount))
			);
		}
		public static void DrawTopLeftText(string line) => DrawTopLeftText(line, Color.White);
		public static void DrawTopLeftText(string line, Color color) => DrawTopLeftText(line, color, Vector2.Zero);
		public static void DrawTopLeftText(string line, Color color, Vector2 offset) {
			Gfx.DrawText(line, NextTopLeftLinePos() + offset, color, FontAlign.Left);
			topLeftLineCount += 1;
		}
		public static void DrawTopLeftText(string line, Color color, uint duration) {
			long start = Time.ElapsedMilliseconds;
			Run(State.From("DrawTopLeftText", (self) => {
				if ( Time.ElapsedMilliseconds - start >= duration ) return null;
				else DrawTopLeftText(line, color);
				return self;
			}));
		}
		public static void DrawBottomRightText(string line) => DrawBottomRightText(line, Color.White);
		public static void DrawBottomRightText(string line, Color color) => DrawBottomRightText(line, color, Vector2.Zero);
		public static void DrawBottomRightText(string line, Color color, Vector2 offset) {
			Gfx.DrawText(line, NextBottomRightLinePos() + offset, color, FontAlign.Right);
			bottomRightLineCount += 1;
		}
		public static void Render() {
			if ( !IsInitialised ) return;
			lineCounts.Clear();
			bottomRightLineCount = 0;
			topLeftLineCount = 0;
			string zoneTime = timeInZone.Elapsed.ToString(@"mm\:ss");
			DrawBottomRightText($"[{(isPaused ? "=" : ">")}] ({zoneTime})", Color.Orange);
		}
		public static void Tick(long dt) {
			if ( !IsInitialised ) return;
			Machine.OnTick();
			UpdateBuffCache();
			updatePressedKeys();
		}
		public static void Run(State s) => Machine.Add(s);
		public static void Run(Func<State, State> s) => Machine.Add(State.From(s));
		public static void Run(string label, Func<State, State> s) => Machine.Add(State.From(label, s));
		public static void Cancel(State s) => Machine.Remove(s);

		private static bool isPaused = true;
		public static void Pause() { isPaused = true; }
		public static void Resume() { isPaused = false; }
		public static void TogglePause() { isPaused = !isPaused; }
		public static bool IsPaused() => isPaused;

		// cache the player buffs for one frame
		private static Dictionary<string, int> buffCache = new Dictionary<string, int>();
		private static void UpdateBuffCache() {
			buffCache.Clear();
			var buffs = Game.Player.GetComponent<Buffs>();
			foreach ( var buff in buffs.BuffsList ) {
				buffCache[buff.Name] = buff.Charges;
			}
		}
		public static bool HasBuff(Entity ent, string buffName) => IsValid(ent) && (ent.GetComponent<Buffs>()?.BuffsList?.Any(b => b.Name != null && b.Name.Equals(buffName)) ?? false);
		public static bool HasBuff(params string[] buffNames) => buffNames.Any(buffCache.ContainsKey);
		public static bool HasAnyBuff(string buffPrefix) => buffCache.Keys.Any(x => x.StartsWith(buffPrefix));
		public static bool TryGetBuffValue(string buffName, out int buffValue) {
			return buffCache.TryGetValue(buffName, out buffValue);
		}
		internal static void RenderBuffs() {
			if ( Settings.ShowEnemyBuffNames ) {
				foreach ( var enemy in NearbyEnemies(200).Where(IsValid) ) {
					foreach(var buff in enemy.GetComponent<Buffs>().BuffsList) {
						DrawTextAtEnt(enemy, buff.Name);
					}
				}
			}
			if ( Settings.ShowBuffNames ) foreach ( var item in buffCache ) {
				DrawTextAtPlayer($"Buff: {item.Key} : {item.Value}");
			}
		}
		internal static void LogBuffs() {
			foreach ( var buff in buffCache.Keys ) {
				Log($"Buff: {buff}");
			}
		}

		public static bool HasMod(Entity ent, string modName) {
			if ( ent == null ) return false;
			var mods = ent.GetComponent<Mods>();
			if ( mods == null ) return false;
			return HasMod(mods.ItemMods, modName);
		}
		public static bool HasMod(List<ItemMod> itemMods, string modName) {
			if ( itemMods == null || modName == null ) return false;
			if ( modName.EndsWith("*") ) {
				string prefix = modName.Substring(0, modName.Length - 1);
				return itemMods.Any(x => x.Name != null && x.Name.StartsWith(prefix));
			} else {
				return itemMods.Any(x => x.Name != null && x.Name.Equals(modName));
			}
		}

		public static bool IsValid(AreaInstance area) => area != null && area.Name != null;
		public static bool IsValid(AreaController area) => area != null && area.CurrentArea != null && area.CurrentArea.Name != null;
		public static bool IsValid(RemoteMemoryObject item) => (item != null && item.Address != 0);
		public static bool IsValid(Element item) => (item != null && item.Address != 0 && item.IsValid && item.ChildCount >= 0 && item.ChildCount < 65535);
		public static bool IsValid(NormalInventoryItem item) => (item != null && item.IsValid && item.Item != null && item.Item.Path != null);
		public static bool IsValid(Entity ent) => (ent != null && ent.Path != null && ent.IsValid);
		public static bool IsValid(ServerInventory.InventSlotItem item) => item != null && IsValid(item.Item);
		public static bool IsValid(LabelOnGround label) => label != null && label.Label != null && label.Label.Text != null && label.ItemOnGround != null;
		public static bool IsValid(GameController game) => game != null && game.Game != null && game.IngameState != null;
		public static bool IsValid(ActorVaalSkill skill) => skill != null && skill.VaalSkillInternalName != null;
		public static bool IsValid(Vector2 vec) => vec != Vector2.Zero && !float.IsNaN(vec.X) && !float.IsInfinity(vec.X) && !float.IsNaN(vec.Y) && !float.IsInfinity(vec.Y);

		public static ItemRarity GetItemRarity(Entity ent) {
			if ( IsValid(ent) ) {
				var mods = ent.GetComponent<Mods>();
				if ( mods != null ) {
					return mods.ItemRarity;
				}
			}
			return ItemRarity.Normal;
		}
		public static Color GetColor(ItemRarity rarity) {
			switch ( rarity ) {
				case ItemRarity.Unique: return Color.Orange;
				case ItemRarity.Rare: return Color.Yellow;
				case ItemRarity.Magic: return Color.Blue;
			}
			return Color.White;
		}

		public static void Log(params string[] words) { DebugWindow.LogMsg(string.Join(" ", words)); }

		public static Vector2 WorldToWindow(Vector3 pos) => Game?.IngameState?.Camera?.WorldToScreen(pos) ?? Vector2.Zero;
		public static Vector2 ScreenRelativeToWindow(Vector2 pos) => ScreenRelativeToWindow(pos.X, pos.Y);
		public static Vector2 ScreenRelativeToWindow(float x, float y) {
			var rect = Game.Window.GetWindowRectangleTimeCache;
			return new Vector2(x * rect.Width, y * rect.Height);
		}
		public static Vector2 WindowToScreenRelative(float x, float y) {
			var rect = Game.Window.GetWindowRectangleTimeCache;
			return new Vector2(x / rect.Width, y / rect.Height);
		}
		public static Vector2 WindowToScreenRelative(Vector2 pos) => WindowToScreenRelative(pos.X, pos.Y);

		private static Dictionary<uint, uint> lineCounts = new Dictionary<uint, uint>();
		public static void DrawTextAtEnt(Entity ent, string text) {
			if ( !IsValid(ent) || text == null ) return;
			var camera = Game.Game.IngameState.Camera;
			var pos = camera.WorldToScreen(ent.Pos);
			lineCounts.TryGetValue(ent.Id, out uint lineCount);
			pos.Y += 14.0f * lineCount;
			Gfx.DrawText(text, pos);
			lineCounts[ent.Id] = lineCount + 1;
		}
		public static void DrawTextAtPlayer(string text) => DrawTextAtEnt(Game.Player, text);
		// [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point lpPoint);

		public static bool IsFullLife(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			if ( HasBuff("petrified_blood") ) {
				maxHP = Math.Min(maxHP, life.MaxHP / 2);
			}
			return life.CurHP >= (life.MaxHP - life.TotalReservedHP);
		}
		public static bool IsFullEnough(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			if ( HasBuff("petrified_blood") ) {
				maxHP = Math.Min(maxHP, life.MaxHP / 2);
			}
			return life.CurHP >= maxHP;
		}
		public static bool IsLowLife(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			return ((float)life.CurHP / maxHP) <= .50f;
		}
		public static bool IsMissingEHP(Entity ent, int amount) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			if ( HasBuff("petrified_blood") ) {
				maxHP = Math.Min(maxHP, life.MaxHP / 2);
			}
			int missing = maxHP - life.CurHP;
			missing += life.MaxES - life.CurES;
			return missing >= amount;
		}
		public static double MissingEHPPercent(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return -1d;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			if ( HasBuff("petrified_blood") ) {
				maxHP = Math.Min(maxHP, life.MaxHP / 2);
			}
			double maxEHP = life.MaxES + maxHP;
			double missing = (maxHP - life.CurHP)
				+ (life.MaxES - life.CurES);
			return (missing / maxEHP);
		}
		public static bool IsMissingLife(Entity ent, int amount) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxHP = life.MaxHP - life.TotalReservedHP;
			if ( HasBuff("petrified_blood") ) {
				maxHP = Math.Min(maxHP, life.MaxHP / 2);
			}
			int missing = maxHP - life.CurHP;
			return missing >= amount;
		}
		public static bool IsLowES(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			return ((float)life.CurES / life.MaxES) <= .50f;
		}
		public static bool IsLowMana(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxMana = life.MaxMana - life.TotalReservedMana;
			return ((float)life.CurMana / maxMana) <= .50f;
		}
		public static bool IsFullMana(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) return false;
			int maxMana = life.MaxMana - life.TotalReservedMana;
			return life.CurMana == maxMana;
		}

		public static void DebugLife(Entity ent) {
			var life = ent?.GetComponent<Life>();
			if ( life == null ) {
				DrawTextAtEnt(ent, "Life is null.");
				return;
			}
			DrawTextAtEnt(ent, $"HP: {life.CurHP} / {life.MaxHP} ({life.TotalReservedHP} reserved {life.ReservedPercentHP}%)");
			DrawTextAtEnt(ent, $"Mana: {life.CurMana} / {life.MaxMana} ({life.TotalReservedMana} reserved {life.ReservedPercentMana}%)");
			DrawTextAtEnt(ent, $"ES: {life.CurES} / {life.MaxES}");

		}

		public static bool IsInMap(AreaController area) {
			var a = area?.CurrentArea;
			if ( a == null || a.IsHideout || a.IsTown ) return false;
			return true;
		}

		public static bool HasEnoughRage(int rage) {
			if ( TryGetBuffValue("rage", out int current) ) {
				return current >= rage;
			}
			return false;
		}

		public static bool HasEnoughMana(Entity ent, int mana) {
			if ( !IsValid(ent) ) return false;
			var life = ent.GetComponent<Life>();
			if ( life == null ) return false;
			return life.CurMana > mana;
		}

		public static bool IsAlive(Entity ent) => IsValid(ent) && ent.IsAlive && !ent.IsDead && (ent.GetComponent<Life>()?.CurHP ?? 0) > 0;

		public static int GetWitherStacks(Entity ent) => IsValid(ent) ? (ent.GetComponent<Buffs>()?.BuffsList.Where(b => b.Name.Equals("withered")).Count() ?? 0) : 0;
		public static int GetPoisonStacks(Entity ent) => IsValid(ent) ? (ent.GetComponent<Buffs>()?.BuffsList.Where(b => b.Name.Equals("poison")).Count() ?? 0) : 0;

		public static bool TryGetGameStat(Entity ent, GameStat stat, out int result) {
			result = 0;
			if ( ent?.GetComponent<Stats>()?.StatDictionary.TryGetValue(stat, out result) ?? false ) {
				return true;
			}
			return false;
		}

		public static bool StashIsOpen() => GetGame()?.IngameState?.IngameUi?.StashElement?.IsVisible ?? false;

		public static bool BackpackIsOpen() => GetGame()?.IngameState?.IngameUi?.InventoryPanel?.IsVisible ?? false;

		public static IEnumerable<T> Empty<T>() { yield break; }
		public static IEnumerable<NormalInventoryItem> BackpackItems() => GetGame()?.IngameState?.IngameUi?.InventoryPanel[InventoryIndex.PlayerInventory]?.VisibleInventoryItems ?? Empty<NormalInventoryItem>();

		public static IEnumerable<NormalInventoryItem> StashItems() => GetGame()?.IngameState?.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems ?? Empty<NormalInventoryItem>();

		public static IEnumerable<LabelOnGround> GroundLabels() => (GetGame()?.IngameState?.IngameUi?.ItemsOnGroundLabels ?? Empty<LabelOnGround>()).Where(x => x.Address != 0 && x.IsVisible);

		public static int StashTab() => GetGame()?.IngameState?.IngameUi?.StashElement?.IndexVisibleStash ?? 0;
		public static State ChangeStashTab(int index, State next = null) {
			if ( !StashIsOpen() ) return next;
			if ( StashTab() == index ) return next;
			return new LeftClickAt(ScreenRelativeToWindow(new Vector2(.40f, .10f + (index * .022f))), 50, 1, next);
		}
		
		public static LabelOnGround GetNearestGroundLabel(string path = null) => GroundLabels()
			.Where(IsValid)
			.Where(label => label.ItemOnGround?.Type != ExileCore.Shared.Enums.EntityType.Npc)
			.Where(label => path == null || (label.ItemOnGround?.Path.Equals(path) ?? false))
			.OrderBy(label => Vector3.DistanceSquared(GetPlayer().Pos, label.ItemOnGround?.Pos ?? Vector3.Zero))
			.FirstOrDefault();

		public static bool ChatIsOpen() {
			var game = GetGame();
			var chat = game.IngameState.IngameUi.ChatBoxRoot;
			return chat != null && chat.IsValid && chat.IsActive;
		}


		public static bool IsClusterJewel(Entity ent) => ent?.Path?.StartsWith("Metadata/Items/Jewels/JewelPassiveTreeExpansion") ?? false;
		private static int GetNumberOfAddedPassivesFromCluster(Entity ent) => ent.GetComponent<Mods>()?.EnchantedMods?.FirstOrDefault()?.Value2 ?? 0;
		private static int GetNumberOfNotables(List<ItemMod> mods) => mods?.Where(mod => mod.Group.Contains("Notable")).Count() ?? 0;
		private static ClusterPassiveType GetTypeOfAddedPassivesFromCluster(Entity ent) {
			int type = ent.GetComponent<Mods>()?.EnchantedMods?.FirstOrDefault()?.Value1 ?? 0;
			return (ClusterPassiveType)type;
		}
		private enum ClusterPassiveType {
			Unknown = 0,
			MaximumLife = 39,
			MaximumES = 40,
			MaximumMana = 41,
			Armour = 42,
			Evasion = 43,
			BlockSpells = 45,
			FireResistance = 46,
			ColdResistance = 47,
			LightningResistance = 48,
			ChaosResistance = 49,
			SuppressSpellDamage = 50,
			CurseEffect = 55,
		}
		public static void RollStashItem() {
			Run(State.From("RollStashItem", (state) => {
				if ( !StashIsOpen() ) {
					Log($"Stash is not open.");
					return null;
				}
				if ( IsPaused() ) {
					Log($"Pause stops the roll.");
					return null;
				}
				var restart = new Delay(250, state);
				var centerItem = StashItems().Where(i => IsValid(i) && !i.Item.HasComponent<Stack>()).FirstOrDefault();
				var centerPos = centerItem.GetClientRect().Center;
				Log($"Target item: {centerItem.Item.Path}");
				var ent = centerItem.Item;
				var mods = ent.GetComponent<Mods>();
				var itemMods = mods.ItemMods;
				switch ( ent.Path ) {
					case PATH_SORCERER_BOOTS:
						var bace = ent.GetComponent<Base>();
						if( bace.isHunter ) {
							switch(mods.ItemRarity) {
								case ItemRarity.Normal:
									return Inventory.PlanUseStashItemOnItem(PATH_TRANSMUTATION, centerItem, 1, restart);
								case ItemRarity.Magic:
									foreach(var mod in itemMods) {
										Log($"RollStashItem: mod {mod.Group} {mod.Level}");
										if( mod.Group == "TailwindOnCriticalStrike" || (mod.Group == "MovementVelocity" && mod.Level == 6) ) {
											PersistedText.Add($"I did it!", GetPlayer().BoundsCenterPos, 3000, Color.Yellow);
											return null;
										}
									}
									if( itemMods.Count < 2 )
										return Inventory.PlanUseStashItemOnItem(PATH_AUGMENT, centerItem, 1, restart);
									return Inventory.PlanUseStashItemOnItem(PATH_ALTERATION, centerItem, 1, restart);
								default:
									PersistedText.Add($"I won't scour such a nice pair of boots.", GetPlayer().BoundsCenterPos, 2000, Color.White);
									break;
							}
							PersistedText.Add($"I should roll for Tailwind, but I don't know how!", GetPlayer().BoundsCenterPos, 2000, Color.White);
						} else if( bace.isRedeemer ) {
							PersistedText.Add($"I should roll for Elusive or Onslaught, but I don't know how!", GetPlayer().BoundsCenterPos, 2000, Color.White);
						} else {
							switch(mods.ItemRarity) {
								case ItemRarity.Normal:
									var qual = ent.GetComponent<Quality>();
									if( qual.ItemQuality < 20 ) {
										uint clicks = (uint)((20 - qual.ItemQuality) / 5);
										return Inventory.PlanUseStashItemOnItem(PATH_ARMOUR_SCRAP, centerItem, clicks, restart);
									}
									return Inventory.PlanUseStashItemOnItem(PATH_TRANSMUTATION, centerItem, 1, restart);
								case ItemRarity.Magic:
									if ( (itemMods.Count - mods.ImplicitMods.Count()) < 2 ) {
										return Inventory.PlanUseStashItemOnItem(PATH_AUGMENT, centerItem, 1, restart);
									}
									foreach ( var mod in itemMods ) {
										Log($"RollStashItem: mod {mod.Group} {mod.Level}");
										// LocalIncreasedEnergyShield Level 6+
										// LocalIncreasedEnergyShieldPercent Level 6+
										if( mod.Level > 6 && (mod.Name.Equals("LocalIncreasedEnergyShield") || mod.Name.Equals("LocalIncreasedEnergyShieldPercent"))) {
											return Inventory.PlanUseStashItemOnItem(PATH_REGAL, centerItem, 1, restart);
										}
									}
									return Inventory.PlanUseStashItemOnItem(PATH_ALTERATION, centerItem, 1, restart);
								case ItemRarity.Rare:
									uint matchCount = 0;
									foreach ( var mod in itemMods ) {
										Log($"RollStashItem: mod {mod.Group} {mod.Level}");
										// LocalIncreasedEnergyShield Level 5+
										// LocalIncreasedEnergyShieldPercent Level 5+
										if( mod.Level >= 5 && (mod.Name.Equals("LocalIncreasedEnergyShield") || mod.Name.Equals("LocalIncreasedEnergyShieldPercent"))) {
											matchCount += 1;
										}
									}
									if ( matchCount == 2 ) {
										Log("I did it!");
										return null;
									} else {
										PersistedText.Add("TODO: Scour.", GetPlayer().Pos, 3000, Color.White);
										return null;
										// return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, centerItem, 1, restart);
									}
								default:
									break;

							}
						}
						break;
					case PATH_CLUSTER_SMALL:
						switch (GetTypeOfAddedPassivesFromCluster(ent)) {
							case ClusterPassiveType.MaximumLife:
								// should roll for Fettle, the only good mod
								if( mods.ItemLevel < 75 ) {
									Log($"Item level too low to roll: Fettle ({mods.ItemLevel} < 75)");
									return null;
								}
								if( HasMod(itemMods, "AfflictionNotableFettle") ) {
									Log($"This item is complete.");
									return null;
								}
								switch ( mods.ItemRarity ) {
									case ItemRarity.Normal: return Inventory.PlanUseStashItemOnItem(PATH_TRANSMUTATION, centerItem, 1, restart);
									case ItemRarity.Magic:
										if( itemMods.Count < 3 && GetNumberOfNotables(itemMods) == 0 ) return Inventory.PlanUseStashItemOnItem(PATH_AUGMENT, centerItem, 1, restart);
										else return Inventory.PlanUseStashItemOnItem(PATH_ALTERATION, centerItem, 1, restart);
									case ItemRarity.Rare: return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, centerItem, 1, restart);
								}
								break;
							case ClusterPassiveType.Armour:
								if( mods.ItemLevel < 68 ) {
									Log($"Item level too low to roll: Enduring Composure ({mods.ItemLevel} < 68)");
									return null;
								}
								// should roll for Enduring Composure
								if( HasMod(itemMods, "AfflictionNotableEnduringComposure") ) {
									Log($"This item is complete.");
									return null;
								}
								switch ( mods.ItemRarity ) {
									case ItemRarity.Normal: return Inventory.PlanUseStashItemOnItem(PATH_TRANSMUTATION, centerItem, 1, restart);
									case ItemRarity.Magic:
										if( itemMods.Count < 3 && GetNumberOfNotables(itemMods) == 0 ) return Inventory.PlanUseStashItemOnItem(PATH_AUGMENT, centerItem, 1, restart);
										else return Inventory.PlanUseStashItemOnItem(PATH_ALTERATION, centerItem, 1, restart);
									case ItemRarity.Rare: return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, centerItem, 1, restart);
								}
								break;
							default:
								Log($"I don't know how to roll a type:{GetTypeOfAddedPassivesFromCluster(ent)} cluster yet.");
								return null;
						}
						break;
					default:
						PersistedText.Add($"I don't know how to roll a {ent.Path} yet.", GetPlayer().BoundsCenterPos, 2000, Color.White);
						return null;
				}
				return null;
			}));
		}

		[DllImport("user32.dll")] public static extern short GetAsyncKeyState(Keys key);
		[DllImport("user32.dll")] private static extern short VkKeyScanA(char ch);
		public static bool IsKeyDown(Keys key) => GetAsyncKeyState(key) < 0;
		public static Keys ToKeys(char c) => (Keys)VkKeyScanA(c);
		public static Keys ToKeys(string s) => (Keys)VkKeyScanA(s[0]);

		public static void PressKey(Keys Key, uint duration, long throttle) {
			Run(new PressKey(Key, duration, throttle, null));
		}
		public static void PressKey(Keys Key, uint duration) {
			Run(new PressKey(Key, duration, null));
		}

		public static string ToString(Keys key) => Enum.GetName(typeof(Keys), key);
		public static string ToString(GameStat stat) => Enum.GetName(typeof(GameStat), stat);

		public static bool AllowInputInChatBox = false;
		public static void ChatCommand(string v) {
			Run(PlanChatCommand(v, null));
		}
		public static State PlanChatCommand(string v, State next = null) {
			State start = State.From(() => { AllowInputInChatBox = true; }); // this has terrible race conditions where the other running states can press keys in the chat
			start
					.Then(new PressKey(Keys.Return, 30))
					.Then(v.Select((c) => new PressKey(ToKeys(c), 10)).ToArray())
					.Then(new PressKey(Keys.Return, 30))
					.Then(() => { AllowInputInChatBox = false; })
					.Then(next);
			return start;
		}

		public static IEnumerable<Entity> NearbyEntities() => GetGame()?.Entities ?? Empty<Entity>();
		public static IEnumerable<Entity> NearbyEnemies() => NearbyEntities().Where(e => IsAlive(e) && e.IsHostile) ?? Empty<Entity>();
		public static IEnumerable<Entity> NearbyEnemies(double range) {
			range *= range; // save a bunch of sqrt inside Distance()
			var player = GetPlayer();
			return IsValid(player) ? NearbyEnemies().Where(e => player.DistanceSquared(e) < range)
				: Empty<Entity>();
		}

		public static double AttacksPerSecond(double more, double less) {
			var stats = GetGame()?.Player.GetComponent<Stats>();
			if ( stats == null ) return 0f;
			var dict = stats.StatDictionary;
			dict.TryGetValue(GameStat.MainHandTotalBaseWeaponAttackDurationMs, out int baseAtkDuration);
			dict.TryGetValue(GameStat.MainHandAttackSpeedPct, out int mainHandAtkSpeedPct);
			dict.TryGetValue(GameStat.VirtualActionSpeedPct, out int actionSpeedPct);
			double atkDuration = baseAtkDuration / (1 + (mainHandAtkSpeedPct / 100f));
			atkDuration /= (1 + (more / 100f)); // blade flurry = 160% base attack speed
			atkDuration *= (1 + (less / 100f)); // melee phys support = 10% less atk spd
			atkDuration /= (1 + (actionSpeedPct / 100f));
			return 1000f / atkDuration;
		}

		public static Func<Vector3> DriftOverTime(Func<Vector3> origin, Vector3 velocity) {
			Stopwatch time = Stopwatch.StartNew();
			return () => origin() + (velocity * time.ElapsedMilliseconds);
		}

		public static Func<Vector3> DriftTowardPlayer(Func<Vector3> origin, uint duration) {
			Stopwatch time = Stopwatch.StartNew();
			return () => {
				float percent = Math.Max(0.0f, Math.Min(1.0f, time.ElapsedMilliseconds / (float)duration));
				Vector3 delta = GetPlayer().BoundsCenterPos - origin();
				return origin() + (delta * percent);
			};
		}

		private static State TrackMovement(State current) {
			if( IsPaused() ) {
				isMoving = false;
				lastPosition = Vector3.Zero;
			} else if ( movementTimer.ElapsedMilliseconds > 66 ) {
				movementTimer.Restart();
				var pos = GetPlayer()?.Pos ?? Vector3.Zero;
				isMoving = lastPosition != Vector3.Zero && pos != Vector3.Zero && 
					lastPosition != pos;
				lastPosition = pos;
			}
			return current;
		}
		private static Stopwatch movementTimer = Stopwatch.StartNew();
		private static Vector3 lastPosition;
		private static bool isMoving = false;
		public static bool IsMoving() => isMoving;
		public static bool IsStationary() => !isMoving;
		public static bool IsIdle(Entity ent) => ent?.GetComponent<Actor>()?.Animation == AnimationE.Idle;
		public static bool IsIdle() => IsIdle(GetPlayer());

		public static bool IsInfluenced(NormalInventoryItem item) => IsInfluenced(item.Item);
		public static bool IsInfluenced(Entity ent) {
			var b = ent.GetComponent<Base>();
			if ( b == null ) return false;
			return b.isCrusader || b.isElder || b.isHunter || b.isRedeemer || b.isShaper || b.isWarlord;
		}

		public static IEnumerable<T> Intersect<T>(T[] a, T[] b) {
			foreach(T x in a) {
				if ( b.Contains(x) ) yield return x;
			}
			yield break;
		}

		public static bool IsFullStack(NormalInventoryItem item) {
			if ( !IsValid(item) ) return false;
			var stack = item.Item.GetComponent<Stack>();
			return stack.Size >= stack.Info.MaxStackSize;
		}

		private static Stopwatch timeInZone = new Stopwatch();
		private static void Area_OnAreaChange(AreaInstance obj) {
			Notify($"Leaving Zone after {GetTimeInZone().ToString(@"mm\:ss")}", Color.Orange);
			timeInZone.Restart();
		}
		public static TimeSpan GetTimeInZone() => timeInZone.Elapsed;

		private static long timeOfLastNotify = 0;
		public static void Notify(string text, Color color, uint duration = 3000, float speed = 1f) {
			Log("Notify: " + text);
			Vector2 pos = NextBottomRightLinePos();
			long timeSinceLastNotify = Time.ElapsedMilliseconds - timeOfLastNotify;
			if( timeSinceLastNotify < GetLineHeight() ) {
				pos.Y += GetLineHeight() - timeSinceLastNotify;
			}
			timeOfLastNotify = Time.ElapsedMilliseconds;
			Stopwatch timer = Stopwatch.StartNew();
			Run((state) => {
				if ( timer.ElapsedMilliseconds > duration ) {
					timer.Stop();
					return null;
				} else {
					Gfx.DrawText(text, pos, color, FontAlign.Right);
					pos.Y -= speed;
					return state;
				}
			});
		}

		public static State Notify(string text, Color color, uint duration, float speed, State next) {
			return State.From("Notify", (state) => {
				Notify(text, color, duration, speed);
				return state.Next;
			}, next);
		}

		public static void DrawFrame(RectangleF rect, Color color, int thickness, uint duration, State next = null) {
			long start = Time.ElapsedMilliseconds;
			Run(State.From("DrawFrame", (self) => {
				if ( Time.ElapsedMilliseconds - start >= duration ) return self.Next;
				GetGraphics().DrawFrame(rect, color, thickness);
				return self;
			}, next));
		}

		public static void DrawText(string text, Vector2 pos, Color color, uint duration, State next = null ) {
			long start = Time.ElapsedMilliseconds;
			Run(State.From("DrawText", (self) => {
				if ( Time.ElapsedMilliseconds - start >= duration ) return self.Next;
				GetGraphics().DrawText(text, pos, color);
				return self;
			}, next));
		}

		/*
		public static void DrawMemory(IMemory M, long startAddress, long endAddress, Vector2 pos, Color color) {
			for ( long i = startAddress; i <= endAddress; i += sizeof(long) ) {
				string lineFront = $"{i:X2}:";
				string lineBack = "";
				for ( long j = i; j < (i + sizeof(long)); j += 1 ) {
					byte b = M.Read<byte>(j);
					lineFront += $" {b:X2}";
					lineBack += $" {Convert.ToChar(b == 0 || b > 127 ? (int)'?' : b)}";
				}
				long value = M.Read<long>(i);
				lineBack += $" <{value:d14}>";
				if ( value != 0 ) {
					Element probe = new Element { Address = value };
					if ( IsValid(probe) ) {
						// lineBack += $" Element: {string.Join("", probe.GetInnerText()?.Take(24))}";
						lineBack += $" Element: {string.Join("", probe.Text?.Take(24))}";
					} else {
						string ascii = M.ReadString(value, 16, true);
						if ( (ascii?.Length ?? 0) > 0 ) {
							lineBack += " ASCII: " + ascii;
						} else {
							string unicode = M.ReadStringU(value, 16, true);
							if ( (unicode?.Length ?? 0) > 0 ) {
								lineBack += " Unicode: " + unicode;
							}
						}
					}
				}
				GetGraphics().DrawText(lineFront + lineBack, pos, color);
				pos.Y += GetLineHeight();
			}
		}

		public static void DrawMemory(IMemory M, long start, long end, Vector2 pos, Color color, uint duration, State next = null) {
			long started = Time.ElapsedMilliseconds;
			Run(State.From("DrawMemory", (self) => {
				if ( Time.ElapsedMilliseconds - started >= duration ) return null;
				DrawMemory(M, start, end, pos, color);
				return self;
			}));
		}
		*/

	}
}
