using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using SharpDX;
using System.Diagnostics;
// using WindowsInput.Native;
using ExileCore.PoEMemory.Elements.InventoryElements;
using static Assistant.Globals;
using ExileCore.PoEMemory;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace Assistant {
	partial class Assistant : BaseSettingsPlugin<AssistantSettings> {

		public Assistant() {
			Name = "Assistant";
		}
		public bool Paused { get; set; } = true;
		public override bool Initialise() {
			Globals.Initialise(GameController, Graphics, Settings);
			Inventory.Initialise();
			FlaskManager.Initialise();
			SkillManager.Initialise();
			DeathTracker.Initialise();
			Navigation.Initialise();

			BuffManager.Initialise(GameController, Graphics, Settings);
			BuffManager.MaintainVaalBuff(Settings.UseVaalGrace, "vaal_grace", "vaal_aura_dodge");
			BuffManager.MaintainVaalBuff(Settings.UseVaalHaste, "vaal_haste", "vaal_aura_speed");
			BuffManager.MaintainVaalBuff(Settings.UseVaalDiscipline, "vaal_discipline", "vaal_aura_energy_shield", () => IsLowES(GameController.Player));

			BuffManager.MaintainBuff(Settings.UseBloodRage, "BloodRage", "blood_rage", () => IsInMap(GameController.Area) && IsFullEnough(GameController.Player));

			BuffManager.MaintainBuff(Settings.UseSteelskin, "QuickGuard", "quick_guard", () => IsInMap(GameController.Area) && IsMissingLife(GameController.Player, 100));

			BuffManager.MaintainBuff(Settings.UseBerserk, "Berserk", "berserk", () => IsInMap(GameController.Area) && HasEnoughRage(25));

			BuffManager.MaintainBuff(Settings.UseCorruptingFever, "CorruptingFever", "blood_surge", () => IsInMap(GameController.Area) && IsFullEnough(GameController.Player));

			// Withering Step
			BuffManager.MaintainBuff(Settings.UseWitheringStep, "Slither", "slither", () => IsInMap(GameController.Area)
				&& NearbyEnemies(350).Any(e => e.Rarity >= MonsterRarity.Rare && GetWitherStacks(e) < 7)
			);

			// Plague Bearer
			BuffManager.MaintainBuff(Settings.UsePlaguebearer, "CorrosiveShroud", "corrosive_shroud_buff", () => IsInMap(GameController.Area));
			BuffManager.MaintainBuff(Settings.UsePlaguebearer, "CorrosiveShroud", "corrosive_shroud_accumulating_damage", () => HasBuff("corrosive_shroud_at_max_damage"));

			// Use Immortal Call when missing N life
			BuffManager.MaintainBuff(Settings.UseImmortalCall, "ImmortalCall", "mortal_call", () => IsInMap(GameController.Area)
				&& MissingEHPPercent(GetPlayer()) > .10);

			// Molten Shell
			BuffManager.MaintainBuff(Settings.UseMoltenShell, "MoltenShell", "molten_shell_shield",
					() => IsInMap(GameController.Area) && MissingEHPPercent(GameController.Player) > .11 && !HasBuff("vaal_molten_shell"));
			BuffManager.MaintainVaalBuff(Settings.UseVaalMoltenShell, "vaal_molten_shell", "molten_shell_shield",
					() => IsInMap(GameController.Area) && MissingEHPPercent(GameController.Player) > .19);

			// Bone Armour (from Necro Asc.)
			BuffManager.MaintainBuff(Settings.UseBoneArmour, "BoneArmour", "bone_armour", () => IsInMap(GameController.Area));

			// Enduring Cry
			BuffManager.MaintainBuff(Settings.UseEnduringCry, "EnduringCry", "enduring_cry_endurance_charge_benefits",
				() => IsMissingLife(GameController.Player, 1000));

			// Infernal Cry
			Run((state) => {
				if ( !(Settings.UseInfernalCry?.Enabled ?? false) ) return state;
				var hostile = NearbyEnemies(50)
					.FirstOrDefault(e => IsAlive(e)
						&& e.IsHostile
						&& e.IsTargetable
						&& e.Rarity >= MonsterRarity.Rare
						&& !HasBuff(e, "infernal_cry"));
				if( hostile != default ) {
					Log($"Infernal cry re: {hostile.Path} {hostile.IsAlive} {hostile.GetComponent<Life>().CurHP} {hostile.DistancePlayer:F2}");
					SkillManager.TryUseSkill("AbyssalCry", Settings.UseInfernalCry.Value);
					return new Delay(1000, state);
				}
				return new Delay(300, state);
			});
			/*
			// Malevolence (as a Blessing)
			BuffManager.MaintainBuff(Settings.UseMalevolenceBlessing, "DamageOverTimeAura", "player_aura_damage_over_time",
				() => IsInMap(GameController.Area) && IsFullLife(GameController.Player));
			*/
			List<Tuple<ToggleNode, string, string>> blessingData = new List<Tuple<ToggleNode, string, string>>() {
				new Tuple<ToggleNode, string, string>(Settings.UsePurityOfElementsBlessing, "Purity", "player_aura_resists"),
				new Tuple<ToggleNode, string, string>(Settings.UsePurityOfFireBlessing, "FireResistAura", "player_aura_fire_resist"),
				new Tuple<ToggleNode, string, string>(Settings.UsePurityOfIceBlessing, "ColdResistAura", "player_aura_cold_resist"),
				new Tuple<ToggleNode, string, string>(Settings.UsePurityOfLightningBlessing, "LightningResistAura", "player_aura_lightning_resist"),
				new Tuple<ToggleNode, string, string>(Settings.UseDeterminationBlessing, "Determination", "player_aura_armour"),
				new Tuple<ToggleNode, string, string>(Settings.UseDisciplineBlessing, "Discipline", "player_aura_energy_shield"),
				new Tuple<ToggleNode, string, string>(Settings.UseHasteBlessing, "Haste", "player_aura_speed"),
				new Tuple<ToggleNode, string, string>(Settings.UseGraceBlessing, "Grace", "player_aura_evasion"),
				new Tuple<ToggleNode, string, string>(Settings.UseVitalityBlessing, "Vitality", "player_aura_life_regen"),
				new Tuple<ToggleNode, string, string>(Settings.UseClarityBlessing, "Clarity", "player_aura_mana_regen"),
				new Tuple<ToggleNode, string, string>(Settings.UsePrecisionBlessing, "AccuracyAndCritsAura", "player_aura_accuracy_and_crits"),
				new Tuple<ToggleNode, string, string>(Settings.UseHatredBlessing, "Hatred", "player_aura_cold_damage"),
				new Tuple<ToggleNode, string, string>(Settings.UseAngerBlessing, "Anger", "player_aura_fire_damage"),
				new Tuple<ToggleNode, string, string>(Settings.UseWrathBlessing, "Wrath", "player_aura_lightning_damage"),
				new Tuple<ToggleNode, string, string>(Settings.UsePrideBlessing, "PhysicalDamageAura", "player_physical_damage_aura"),
				new Tuple<ToggleNode, string, string>(Settings.UseZealotryBlessing, "SpellDamageAura", "player_aura_spell_damage"),
				new Tuple<ToggleNode, string, string>(Settings.UseMalevolenceBlessing, "DamageOverTimeAura", "player_aura_damage_over_time"),
			};
			Run((state) => {
				var area = GetGame()?.Area;
				if ( IsInMap(area) && (Settings.UseDivineBlessing?.Enabled ?? false) && !HasBuff("grace_period") ) {
					var key = Settings.UseDivineBlessing.Value;
					foreach ( var tup in blessingData ) {
						if ( tup.Item1.Value && (!HasBuff(tup.Item3)) && SkillManager.TryUseSkill(tup.Item2, key) ) {
							break;
						}
					}
				}
				return state;
			});

			// Use flares if darkness stacks get too high:
			BuffManager.ClearBuff(Settings.UseFlares, "delve_degen_buff",
					() => TryGetBuffValue("delve_degen_buff", out int buffValue) && buffValue > 12);

			// BuffManager.MaintainVaalBuff(Settings.UseVaalImpurityOfIce, "cold_impurity", "cold_impurity_buff", VirtualKeyCode.VK_Q);


			PersistedText.Add(GetTimeInZone, (c) => ScreenRelativeToWindow(.319f, .960f), 0, Color.Orange);

			// InputManager.OnRelease(VirtualKeyCode.ESCAPE, () => Paused = true);
			// InputManager.OnRelease(VirtualKeyCode.OEM_MINUS, () => Inventory.StashAll());

			ConfigureHotkey(Settings.StashDeposit, () => Inventory.StashDeposit());
			ConfigureHotkey(Settings.StashRestock, () => Inventory.RestockFromStash());

			OnRelease(Keys.F10, RollStashItem);
			OnRelease(Keys.F6, Test.RunAll);
			OnRelease(Keys.F5, () => ChatCommand("/hideout"));

			Run((state) => {
				NormalInventoryItem item;
				if ( ! BackpackIsOpen() ) return state;
				if ( IsKeyDown(Keys.NumPad6) ) {
					var free = Inventory.GetFreeSlot(1, 1); // TODO: use item actual size
					if ( free == Vector2.Zero ) {
						DrawTextAtPlayer("No free slot.");
						return state;
					}
					item = StashItems().FirstOrDefault();
					if( item != default ) {
						return new CtrlLeftClickAt(item, 30, new Delay(100, state));
					}
				} else if ( IsKeyDown(Keys.NumPad4)) {
					var tradeWindow = GetGame().IngameState.IngameUi.Children.ElementAtOrDefault(70);
					if( tradeWindow != default && tradeWindow.IsVisible ) {
						Element tradeButton = tradeWindow.Children[4];
						Element cardSlot = tradeWindow.Children[5];
						bool isTradeButtonEnabled = tradeButton.BorderColor.G == 46;
						bool isCardSlotFull = cardSlot.Children.Count == 2;
						if( isCardSlotFull && isTradeButtonEnabled ) {
							return new LeftClickAt(tradeButton, 30, 1, new Delay(200, state));
						} else if( isCardSlotFull ) {
							return new CtrlLeftClickAt(cardSlot, 30, new Delay(200, state));
						} else {
							var card = BackpackItems().FirstOrDefault(i => IsValid(i) && i.Item.Path.StartsWith("Metadata/Items/DivinationCards") && IsFullStack(i));
							if( card != default ) {
								return new CtrlLeftClickAt(card, 30, new Delay(200, state));
							}
						}
					} else {
						item = BackpackItems().FirstOrDefault();
						if( item != default ) {
							return new CtrlLeftClickAt(item, 30, new Delay(100, state));
						}
					}
				}
				/*
				 * Notes on the Card Trade UI:
				 * IngameUI Child [70] is the panel.
				 * child [0] = the frame
				 * child [1] = the title elements
				 * child [2] = unknown
				 * child [3] = close button
				 * child [4] = trade button
				 *  -- if BorderColor.G is 14, button is disabled
				 *  -- if BorderColor.G is 46, button is enabled
				 * child [5] = trade window
				 *  -- if length is 1, is empty
				 *  -- if length is 2, is full
				 */
				return state;
			});

			Run((state) => {
				if( IsKeyDown(Keys.NumPad0) ) {
					var items = Empty<NormalInventoryItem>();
					if ( StashIsOpen() ) {
						items = items.Concat(StashItems());
					}
					if( BackpackIsOpen() ) {
						items = items.Concat(BackpackItems());
					}
					foreach(var item in items.Where(IsValid)) {
						var game = GetGame();
						// game.Files.Mods.recordsByTier;
						var score = item.Item.GetComponent<Mods>()?.ItemMods?.Select(mod => mod.Level).Sum() ?? 0;
						var pos = item.GetClientRectCache.TopLeft;
						Graphics.DrawText($"{score}", pos);
					}
				}
				return state;
			});

			// Check for sextants that need a refill
			Run((state) => {
				if ( ! (Settings.ShowSextantWarning?.Value ?? false) ) return state;
				var game = GetGame();
				var watchStones = game?.IngameState?.Data?.ServerData?.GetPlayerInventoryBySlotAndType(InventoryTypeE.AtlasWatchtower, InventorySlotE.AtlasWatchtower);
				if ( watchStones == null ) {
					DrawTextAtPlayer("No watchstones.");
				} else {
					foreach ( var item in watchStones.InventorySlotItems ) {
						var mods = item.Item.GetComponent<Mods>();
						if ( mods.EnchantedMods.Count() == 0 ) {
							DrawTextAtPlayer("Watchstone: needs a sextant.");
						}
					}
				}
				return state;
			});

			HashSet<string> seenDelveLoot = new HashSet<string>();
			Run((state) => {
				if( Settings.ShowDelveLoot ) {
					var area = GetGame().Area.CurrentArea;
					if ( !IsValid(area) ) return state;
					if( area.Name.Equals("Azurite Mine") ) {
						foreach(var ent in NearbyEntities().Where(e => IsValid(e) && !e.IsOpened) ) {
							var path = ent.Path;
							if ( !seenDelveLoot.Contains(path) ) {
								Log($"Seen: {path}");
								seenDelveLoot.Add(path);
							}
							/*
							if ( path.StartsWith("Metadata/Monsters/")
								|| path.StartsWith("Metadata/Characters/")
								|| path.EndsWith("DelveNode")
								|| path.EndsWith("MinerHub")
								|| path.EndsWith("MapViewer")
								|| path.EndsWith("WorldItem")
								|| path.EndsWith("AzuriteShard")
								|| path.Contains("DelveRobot")
								|| path.Contains("DelveLight")
								|| path.Contains("Encounter")
								|| path.Contains("MiscellaneousObjects")
								|| path.Contains("Objects/CityChambers")
								|| path.Contains("Chests/AbyssBonePile")
								|| path.Contains("Chests/Pot")
								|| path.Contains("Chests/Vase")
								|| path.Contains("Chests/FungalBloom")
								|| path.Contains("Chests/Basket")
								|| path.Contains("Chests/Cairn")
								|| path.Contains("Chests/DarkPot")
								|| path.Contains("Chests/Urn")
								|| path.Contains("/Effects/")
								|| path.Contains("/Projectiles/")
								) {
								// TODO: later once we have the lower cases correct, we can skip a lot of this
								// TODO: at that time, 'text' should start = null, and just not render for most cases
								continue;
							}
							*/
							// Metadata/Chests/DelveChests
							// Metadata/Chests/DelveChests/OffPathTrinkets
							// Metadata/Chests/DelveChests/DynamiteTrinkets
							// Metadata/Chests/DelveChests/DynamiteWeapon
							// Metadata/Chests/DelveChests/OffPathWeapon
							// Metadata/Chests/DelveChests/PathWeapon
							string text = null;
							var textColor = Color.White;
							if( ent.Path.EndsWith("Objects/DelveWall") ) {
								if ( !ent.IsTargetable ) continue;
							}
							if ( path.EndsWith("DelveWall") ) text = "Wall";
							else if( path.StartsWith("Metadata/Chests/DelveChests") ) {
								if ( path.Contains("Weapon") ) continue;
								else if ( path.EndsWith("AzuriteShard") ) continue;
								else if ( path.Contains("Generic") ) continue;
								else if ( path.Contains("SuppliesFlares") ) text = "Flares";
								else if ( path.Contains("SuppliesDynamite") ) text = "Dynamite";
								else if ( path.Contains("Fossil") ) text = "Fossil";
								else if ( path.Contains("Trinkets") ) continue;
								else if ( path.Contains("Armour") ) continue;
								else if ( path.Contains("Currency") ) text = "Currency";
								else if ( path.Contains("AzuriteVein") ) text = "Azurite";
								else if ( path.Contains("Resonator") ) text = "Resonators";
							}
							if ( text != null ) {
								var pos = WorldToWindow(ent.BoundsCenterPos);
								var pos2 = WorldToWindow(GetPlayer().BoundsCenterPos);
								var mid = pos2 + (pos - pos2) * .1f;
								Graphics.DrawLine(pos, pos2, 1, Color.Cyan);
								Graphics.DrawText(text, mid, textColor);
							}
							// pos.Y -= .1f;
							// pos.X -= .1f;
							// var pos2 = pos + new Vector2(.2f, .2f);
							// Graphics.DrawFrame(ScreenRelativeToWindow(pos), ScreenRelativeToWindow(pos2), Color.Cyan, 1);
							// Graphics.DrawText(ent.Path, ScreenRelativeToWindow(pos + new Vector2(0f, .2f)));
						}
					}
				}
				return state;
			});

			// InputManager.OnRelease(VirtualKeyCode.F2, () => { if (showRecipe = !showRecipe) currentRecipe.Reset().Add(GameController.Game.IngameState.IngameUi.StashElement); });

			GameController.Area.OnAreaChange += Area_OnAreaChange;
			timeInZone.Start();
			OnRelease(Keys.Pause, () => {
				Paused = !Paused;
				if ( !Paused ) {
					ResetRegen();
				}
			});

			// When you hold space, hold W+E as well
			// InputManager.BindMultiKey(VirtualKeyCode.SPACE, VirtualKeyCode.VK_Q, VirtualKeyCode.VK_W, VirtualKeyCode.VK_E);

			State multiKeyHandler = null;
			Action refreshMultiKey = () => {
				if ( multiKeyHandler != null ) {
					Cancel(multiKeyHandler);
					multiKeyHandler = null;
				}
				if ( Settings.UseMultiKey.Enabled ) {
					multiKeyHandler = PlanMultiKey(
						Settings.UseMultiKey.Value,
						Settings.UseMultiKey1.Enabled ? Settings.UseMultiKey1.Value : Keys.None,
						Settings.UseMultiKey2.Enabled ? Settings.UseMultiKey2.Value : Keys.None,
						Settings.UseMultiKey3.Enabled ? Settings.UseMultiKey3.Value : Keys.None
					);
					Run(multiKeyHandler);
				}
			};
			Settings.UseMultiKey.OnValueChanged += refreshMultiKey;
			Settings.UseMultiKey1.OnValueChanged += refreshMultiKey;
			Settings.UseMultiKey2.OnValueChanged += refreshMultiKey;
			Settings.UseMultiKey3.OnValueChanged += refreshMultiKey;
			refreshMultiKey();

			EnableMovementKeys(Settings.UseArrowKeys);

			Run(PlanLootKey(Settings.ClickNearestLabel));

			OnKeyCombo(",20", () => Run(new KeyDown(Keys.LShiftKey,
				new LeftClick(40, 20, 
				new KeyUp(Keys.LShiftKey)))));

			OnKeyCombo(",4", () => Run(new KeyDown(Keys.LShiftKey,
				new LeftClick(40, 4, 
				new KeyUp(Keys.LShiftKey)))));

			OnKeyCombo(",l22", () => Run(PlanToLinkItem(2, 2)));
			OnKeyCombo(",l31", () => Run(PlanToLinkItem(3, 1)));
			OnKeyCombo(",l4", () => Run(PlanToLinkItem(4, 0)));
			OnKeyCombo(",l5", () => Run(PlanToLinkItem(5, 0)));
			OnKeyCombo(",l6", () => Run(PlanToLinkItem(6, 0)));

			OnKeyCombo(",,", () => {
				Log($"Combo #2 triggered.");
				if ( !BackpackIsOpen() ) return;
				Inventory.RefreshBackpack();
				var item = BackpackItems().Where(i => IsValid(i) && i.Item.Path.StartsWith(PATH_MAP_PREFIX)).FirstOrDefault();
				// var item = Inventory.GetItemUnderCursor();
				if ( !IsValid(item) ) {
					Log($"Target item is invalid.");
					return;
				}
				Vector2 targetItemPosition = item.GetClientRect().Center;
				Log($"Cursor at: {item?.Item?.Path ?? "null"}");

				Run(State.From("Rolling Map", (state) => {
					if ( !BackpackIsOpen() ) {
						Log($"Canceled: backpack is closed.");
						return null;
					}
					if ( !StashIsOpen() ) {
						Log($"Canceled: stash is closed.");
						return null;
					}
					if( StashTab() != 0 ) {
						return ChangeStashTab(0, state);
					}
					Inventory.RefreshBackpack();
					var ent = item.Item;
					if ( ent.GetComponent<Base>().isCorrupted ) {
						Log($"Fail: cannot use a corrupted map.");
						return null;
					}
					State doReset = new Delay(300, State.From(_ => {
						Inventory.RefreshBackpack();
						item = Inventory.GetItemUnderCursor();
						if ( !IsValid(item) ) return null;
						targetItemPosition = item.GetClientRect().Center;
						Log($"Re-targeting to {targetItemPosition}");
						return state;
					}));
					var mods = ent.GetComponent<Mods>();
					if ( mods == null ) {
						Log($"Fail: Target map has null Mods component.");
						return null;
					}
					if ( !mods.Identified ) return Inventory.PlanUseStashItemOnItem(PATH_SCROLL_WISDOM, item, 1, doReset);

					var quality = ent.GetComponent<Quality>()?.ItemQuality ?? 0;
					if( quality < 20 ) {
						Log($"Upgrade: map needs quality {quality}");
						if ( mods.ItemRarity > ItemRarity.Normal ) {
							Log($"Dont waste chisels, scouring first.");
							return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, item, 1, doReset);
						}
						// +5 quality per click, need 20 quality
						uint clicks = (uint)Math.Max(0, Math.Ceiling((double)(20 - quality) / 5));
						return Inventory.PlanUseStashItemOnItem(PATH_CHISEL, item, clicks, doReset);
					}

					switch ( mods.ItemRarity ) {
						case ItemRarity.Normal:
							Log($"Upgrade: map should be rare.");
							return Inventory.PlanUseStashItemOnItem(PATH_ALCHEMY, item, 1, doReset);
						case ItemRarity.Magic:
							Log($"Downgrade: map should be normal.");
							return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, item, 1, doReset);
						case ItemRarity.Rare:
							break;
						default:
							Log($"Fail: Target map has unknown rarity: {mods.ItemRarity}");
							return null;
					}

					int packSize = 0;
					foreach(var mod in mods.ItemMods) {
						if ( // any of the banlisted mods:
							mod.Group.Equals("MapPlayerMaxResists")
							// || mod.Group.Equals("MapMonsterPhysicalReflection")
							|| mod.Name.Equals("MapPlayerNoLifeESRegenMapWorlds") ) {
							Log($"Target map has bad mod: {mod.DisplayName}, scouring.");
							return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, item, 1, doReset);
						}
						if ( !mod.Group.EndsWith("ContainsBoss") ) {
							packSize += mod.Value3;
						}
					}

					if( packSize < 20 ) {
						Log($"Pack size too low ({packSize}%), scouring...");
						return Inventory.PlanUseStashItemOnItem(PATH_SCOUR, item, 1, doReset);
					}

					Log($"Complete (pack size is {packSize}%).");
					return null;
				}));
			});

			Run(PlanTemporalRift());

			Run(new DPSMonitor());

			Run(PlanMonitorXP());

			Run((state) => {
				if( Settings.ShowDistance ) {
					var pos = GetPlayer()?.Pos ?? Vector3.Zero;
					if ( pos == Vector3.Zero ) return state;
					foreach(var ent in NearbyEnemies().Where(IsValid)) {
						DrawTextAtEnt(ent, $"D:{Vector3.Distance(pos, ent.Pos):F0}");
					}
				}
				return state;
			});

			Log("Assistant initialised.");
			/*
			// Attempt to find the "You are dead" pop up
			var child = GameController.IngameState.IngameUi.Root.GetChildFromIndices(1, 107);
			if ( child == null ) {
				Log("Child is null");
			} else {
				Log($"Child: ({child.ChildCount}) -> [{child.IndexInParent}] {(child.IsVisible ? "Visible" : "Hidden")}");
			}
			*/
			return true;
		}
		internal State PlanTemporalRift() {
			int[] recentPast = new int[4];
			long mostRecentSample = 0;
			Stopwatch sampleTimer = Stopwatch.StartNew();
			return State.From((state) => {
				if ( !Settings.UseTemporalRift ) return state;
				var area = GameController.Area;
				if ( !IsInMap(area) ) return state;
				var player = GameController.Player;
				if ( !IsValid(player) ) return state;
				var life = player.GetComponent<Life>();
				if ( life == null ) return state;
				var curEffHp = life.CurHP + life.CurES;
				long now = sampleTimer.ElapsedMilliseconds;
				if ( now - mostRecentSample > 1000 ) {
					mostRecentSample = now;
					for ( int i = 0; i < recentPast.Length - 1; i++ ) {
						recentPast[i] = recentPast[i + 1];
					}
					recentPast[recentPast.Length - 1] = curEffHp;
				}
				int gain = recentPast[0] - curEffHp;
				var maxHp = life.MaxHP - life.TotalReservedHP;
				var maxEffHp = maxHp + life.MaxES;
				if ( gain > (maxEffHp * .4f) ) {
					SkillManager.TryUseSkill("TemporalRift", Settings.UseTemporalRift.Value);
				}
				return state;
			});
		}

		internal State PlanMonitorXP() {
			Stopwatch xpTimer = Stopwatch.StartNew();
			long lastFrameTime = xpTimer.ElapsedMilliseconds;
			MovingAverage xpPerMS = new MovingAverage(1000);
			long xpLastFrame = -1;
			long[] xpToNextLevel = new long[] {
				0, 525, 1760, 3781, 7184, 12186, 19324, 29377, 43181, 61693, 85990, 117506, 157384, 207736, 269997, 346462, 439268, 551295, 685171, 843709, 1030734, 1249629, 1504995, 1800847, 2142652, 2535122, 2984677, 3496798, 4080655, 4742836, 5490247, 6334393, 7283446, 8384398, 9541110, 10874351, 12361842, 14018289, 15859432, 17905634, 20171471, 22679999, 25456123, 28517857, 31897771, 35621447, 39721017, 44225461, 49176560, 54607467, 60565335, 67094245, 74247659, 82075627, 90631041, 99984974, 110197515, 121340161, 133497202, 146749362, 161191120, 176922628, 194049893, 212684946, 232956711, 255001620, 278952403, 304972236, 333233648, 363906163, 397194041, 433312945, 472476370, 514937180, 560961898, 610815862, 664824416, 723298169, 786612664, 855129128, 929261318, 1009443795, 1096169525, 1189918242, 1291270350, 1400795257, 1519130326, 1646943474, 1784977296, 1934009687, 2094900291, 2268549086, 2455921256, 2658074992, 2876116901, 3111280300, 3364828162, 3638186694, 3932818530, 4250334444
			};

			return State.From((resume) => {
			long dt = Math.Max(1, xpTimer.ElapsedMilliseconds - lastFrameTime);
			lastFrameTime += dt;

			if ( !Settings.ShowXPRate.Value ) return resume;
			var game = GetGame();
			var player = game?.Player;
			if ( !IsValid(player) ) return resume;
			var p = player.GetComponent<Player>();
			if ( xpLastFrame == -1 ) xpLastFrame = p.XP;
			else {
				long xpGain = (p.XP - xpLastFrame);
					xpLastFrame = p.XP;
					xpPerMS.Add(xpGain / dt);
				}
				long xpToLevel = xpToNextLevel[p.Level] - p.XP;
				if ( xpPerMS.Value > 0f && Settings.ShowXPRate ) {
					var pctPerMin = (xpPerMS.Value * 1000 * 60 * 60) / xpToNextLevel[p.Level];
					var msToLevel = xpToLevel / xpPerMS.Value;
					var hrToLevel = Math.Min(9999, msToLevel / (1000 * 60 * 60));
					Graphics.DrawText($"{hrToLevel:F2} hrs {pctPerMin * 1000:F2}%/hr", ScreenRelativeToWindow(.68f, .96f), FontAlign.Right);
				}
				return resume;
			});
		}

		internal State PlanMultiKey(Keys mainKey, params Keys[] otherKeys) {
			// press the mainKey, this code presses all the otherKeys
			bool downBefore = false;
			KeyUp[] keyUps = otherKeys.Select(k => new KeyUp(k)).ToArray();
			KeyDown[] keyDowns = otherKeys.Select(k => new KeyDown(k)).ToArray();
			Log($"Creating MultiKey plan: {mainKey} {string.Join(" ", otherKeys)}");
			return State.From((state) => {
				bool downNow = IsKeyDown(mainKey);
				if ( ChatIsOpen() || (downBefore && !downNow) ) {
					foreach ( var up in keyUps ) up.OnTick();
				} else if ( downNow && !downBefore ) {
					foreach ( var down in keyDowns ) down.OnTick();
				}
				downBefore = downNow;
				return state;
			});
		}

		public void EnableMovementKeys(ToggleNode setting) => Run((state) => {
			if ( !(setting?.Value ?? false) ) return state;
			bool left = IsKeyDown(Keys.Left);
			bool right = IsKeyDown(Keys.Right);
			bool up = IsKeyDown(Keys.Up);
			bool down = IsKeyDown(Keys.Down);
			// we cant just add up vector components like normal because of the skewed perspective, so we hack in all 8 corners
			// this lets the diagonals align nicely with the game corridors
			Vector2 motion = (up && left) ? new Vector2(.36f, .27f) :
				(up && right) ? new Vector2(.65f, .26f) :
				(down && left) ? new Vector2(.35f, .63f) :
				(down && right) ? new Vector2(.62f, .61f) :
				(up) ? new Vector2(.5f, .24f) :
				(down) ? new Vector2(.5f, .65f) :
				(left) ? new Vector2(.36f, .45f) :
				(right) ? new Vector2(.65f, .45f) :
				Vector2.Zero;
			if ( motion == Vector2.Zero ) return state;
			var pos = ScreenRelativeToWindow(motion);
			// do the move immediately (in this frame)
			return new MoveMouse(pos.X, pos.Y, state).OnTick();
		});

		private State PlanToLinkItem(int linksInGroupOne, int linksInGroupTwo, State next = null) {
			bool needShift = linksInGroupOne == 6;
			bool didShift = false;
			return State.From("LinkItem", (state) => {
				if( ! StashIsOpen() ) {
					Log($"Stash is closed.");
					return next;
				}
				var item = StashItems().Where(i => IsValid(i) && !i.Item.HasComponent<Stack>()).FirstOrDefault();
				if( ! IsValid(item) ) {
					Log($"LinkItem: invalid target item.");
					return next;
				}
				Log($"Target item: {item.Item.Path}");
				var sockets = item.Item.GetComponent<Sockets>();
				if( sockets == null ) {
					Log($"LinkItem: item has no Sockets component.");
					return next;
				}
				if( sockets.NumberOfSockets < linksInGroupOne || sockets.NumberOfSockets < linksInGroupTwo ) {
					Log($"LinkItem: item cannot support {Math.Max(linksInGroupOne, linksInGroupTwo)} links (has {sockets.NumberOfSockets})");
					return next;
				}
				if( needShift && !didShift ) {
					didShift = true;
					return Inventory.PlanUseStashItem(PATH_FUSING, new KeyDown(Keys.LShiftKey, state));
				}
				bool matchedOne = false;
				bool matchedTwo = linksInGroupTwo == 0;
				foreach(var links in sockets.Links) {
					Log($"Links: {links.Length}");
					if( (!matchedOne) && links.Length == linksInGroupOne ) {
						Log($"Matched group one ({linksInGroupOne})");
						matchedOne = true;
					}
					else if( (!matchedTwo) && links.Length == linksInGroupTwo ) {
						Log($"Matched group two ({linksInGroupTwo}))");
						matchedTwo = true;
					}
				}
				if( matchedOne && matchedTwo ) {
					Log($"Complete.");
					return next;
				}
				Log($"Trying to use an Orb of Fusing...");
				return Inventory.PlanUseStashItemOnItem(PATH_FUSING, item, 1, new Delay(100, state));
			});
		}


		private Stopwatch timeInZone = new Stopwatch();
		private void Area_OnAreaChange(AreaInstance obj) {
			Log(string.Format("Leaving Zone after {0}", timeInZone.Elapsed.ToString(@"mm\:ss")));
			timeInZone.Restart();
		}


		private ChaosRecipe currentRecipe = new ChaosRecipe();
		private bool showRecipe = false;

		private Vector2 DrawLineAt(string text, Vector2 pos) => DrawLineAt(text, pos, Color.White);
		private Vector2 DrawLineAt(string text, Vector2 pos, Color color) {
			Graphics.DrawText(text, pos, color);
			pos.Y += 12f;
			return pos;
		}

		private void InventoryReport() => InventoryReport(null);
		private void InventoryReport(InventoryElement panel) {
			// var pos = ScreenRelativeToWindow(.2f, .5f);
			if ( panel == null ) panel = GetGame().IngameState.IngameUi.InventoryPanel;
			if ( panel == null ) return;
			var playerInventory = panel[InventoryIndex.PlayerInventory];
			if ( playerInventory == null ) return;
			var items = playerInventory.VisibleInventoryItems;
			if ( items == null ) return;
			var query = new ItemQuery();
			// query.MatchCount(1, "AfflictionNotableFettle", "AfflictionNotableToweringThreat", "AfflictionNotableViciousGuard");
			query.MatchCount(1, "AfflictionNotableFettle", "AfflictionNotableViciousGuard");
			// query.MatchAll("AfflictionNotable*");
			foreach ( var item in items ) {
				// pos = DrawLineAt(string.Format("{0}", item.Item.Path), pos);
				// Log(string.Format("{0}", item.Item.Path));
				var ent = item.Item;
				if ( ent == null ) continue;
				var mods = ent.GetComponent<Mods>();
				if ( mods == null ) continue;
				Log(string.Format("{0} {1} {2} iLvl:{3}", item.Item.Path, mods.Identified ? "Identified" : "Unidentified", mods.ItemRarity, mods.ItemLevel));
				if ( mods.ItemMods == null ) continue;
				foreach ( var mod in mods.ItemMods ) {
					Log(string.Format("- {0} '{1}' {2} {3}", mod.Name, mod.DisplayName, mod.Level, String.Join(", ", mod.Values)));

				}
				Log(string.Format("Matches: {0}", query.Matches(item)));

			}
		}



		private string GetTimeInZone() => "(" + timeInZone.Elapsed.ToString(@"mm\:ss") + ")";
		private string GetQuestNotes() {
			var area = GameController.Area.CurrentArea;
			string ret = $"{area.Name} - "; // string.Format("{0}{1} - ", (Paused ? "[Paused] " : ""), area.Name);
			if ( area.Act == 1 ) {
				switch ( area.Name ) {
					case "The Twilight Strand": ret += "Reset your loot filter."; break;
					case "The Coast": ret += "Get the waypoint. Proceed to Mud Flats."; break;
					case "The Mud Flats": ret += "Get three nests. Proceed to Submerged Passage."; break;
					case "The Tidal Island": ret += "Get Level 4, kill Hailrake, logout."; break;
					case "The Submerged Passage": ret += "(after Hailrake) Portal at Depths, exit to Ledge."; break;
					case "The Flooded Depths": ret += "Kill Dweller and log, return to Lower Prison"; break;
					case "The Ledge": ret += "Race to The Climb. Kill nothing."; break;
					case "The Climb": ret += "Get waypoint. Fawn unlocks Navali. Race to Lower Prison."; break;
					case "The Lower Prison": ret += "Return to Depths, then do the Trial here."; break;
					case "The Upper Prison": ret += "Kill Brutus, Proceed to Prisoner's Gate."; break;
					case "Prisoner's Gate": ret += "Gem reward in town. Proceed to Ship Graveyard."; break;
					case "The Ship Graveyard": ret += "Get waypoint! Find Allflame, find Cavern, kill Fairgraves, logout."; break;
					case "The Cavern of Wrath": ret += "Exit to Cavern of Anger"; break;
					case "The Cavern of Anger": ret += "Kill Merveil, proceed to Southern Forest."; break;
				}
			} else if ( area.Act == 2 ) {
				switch ( area.Name ) {
					case "The Southern Forest": ret += "Race NW to Forest Encampment. Kill nothing."; break;
					case "The Riverways": ret += "Get the waypoint on the road, Oak is NW, Alira is SW."; break;
					case "The Western Forest": ret += "Alira (same side as waypoint), Blackguards, Weaver, log out."; break;
					case "The Old Fields": ret += "Place Portal at The Den, proceed to Crossroads."; break;
					case "The Crossroads": ret += "Bridge [for Kraityn], Ruins [for Trial], then Chamber of Sins."; break;
					case "The Fellshrine Ruins": ret += "Follow the road to The Crypt Level 1."; break;
					case "The Crypt Level 1": ret += "Finish the Trial. Do not enter Level 2."; break;
					case "The Chamber of Sins Level 1": ret += "Get waypoint. Enter Level 2."; break;
					case "The Chamber of Sins Level 2": ret += "Finish the Trial. Kill Fidelitas (NW), take Gem, log out."; break;
					case "The Wetlands": ret += "Oak in the center, waypoint and exit behind him."; break;
					case "The Vaal Ruins": ret += "Find and break the Seal. Exit to Northern Forest."; break;
					case "The Northern Forest": ret += "Get waypoint, go NW to the Caverns. Do not enter Dread Thicket."; break;
				}

			} else {
				ret += "No Notes.";
			}
			return ret + " (" + timeInZone.Elapsed.ToString(@"mm\:ss") + ")";
		}
		// sampled over 1000 frames @ 60fps is about 14 seconds
		// sampled over 250 frames @ 60 fps is "recently"
	
		Stopwatch tickTimer = new Stopwatch();
		public override Job Tick() {
			if ( !IsValid(GameController.Player) ) {
				DebugWindow.LogError("Invalid Game (Player), aborting.");
				Log($"Player = {(GameController.Player == null ? "null" : (GameController.Player.Path == null ? "no-path" : (GameController.Player.IsValid ? "is-valid" : "not-valid")))}");
				Settings.Enable.Value = false;
				Paused = true;
				return null;
			}
			try {
				long dt = tickTimer.ElapsedMilliseconds | 1;
				tickTimer.Restart();

				Globals.Tick(dt);
				if ( !IsAlive(GameController.Player) ) {
					return null;
				}

				FlaskManager.OnTick();
				BuffManager.OnTick();
				EventTracker.OnTick();
				DeathTracker.OnTick();
				EquipmentManager.OnTick();
				FocusManager.OnTick();

				if( Settings.DebugGeneralsCry.Value ) DebugGeneralsCry();

				CheckBanner(Settings.UseDefianceBanner, "DefianceBanner", "armour_evasion_banner_buff_aura", "armour_evasion_banner_stage");
				CheckBanner(Settings.UseDreadBanner, "PuresteelBanner", "puresteel_banner_buff_aura", "puresteel_banner_stage");
				CheckBanner(Settings.UseWarBanner, "BloodstainedBanner", "bloodstained_banner_buff_aura", "bloodstained_banner_stage");

			} catch ( Exception e ) {
				Log(e.ToString());
			}

			return null;
		}

		private long deployedCountBefore = 0;
		private bool generalCryWasOnCooldown = false;
		private Dictionary<Entity, GeneralCryStatus> generalCryStatus = new Dictionary<Entity, GeneralCryStatus>();
		private Dictionary<Entity, long> generalCryAttackStart = new Dictionary<Entity, long>();
		private Stopwatch generalCryTimer = Stopwatch.StartNew();
		private enum GeneralCryStatus {
			Born,
			Move,
			Attack,
			Idle,
			Dead
		}
		private void DebugGeneralsCry() {
			var player = GetGame()?.Player;
			if ( !IsValid(player) ) return;
			Actor actor = player.GetComponent<Actor>();
			if ( !IsValid(actor) ) return;
			var warriors = actor.DeployedObjects.Select(o => o.Entity).Where(IsValid).Where(o => o.Path.StartsWith("Metadata/Monsters/DoubleCryWarrior")).ToArray();
			int warriorCount = warriors.Length;
			bool onCooldown = !(SkillManager.TryGetSkill("GeneralsCry", out ActorSkill skill) && !skill.IsOnCooldown);
			if( onCooldown && !generalCryWasOnCooldown ) {
				generalCryTimer.Restart();
				generalCryStatus.Clear();
				// Log($"{generalCryTimer.ElapsedMilliseconds}: General's Cry cast detected by cooldown.");
			}
			long delta = warriorCount - deployedCountBefore;
			if( delta > 0 ) {
				// Log($"{generalCryTimer.ElapsedMilliseconds}: Gained +{delta} warriors.");
			} else if ( delta < 0 ) {
				// Log($"{generalCryTimer.ElapsedMilliseconds}: Lost {delta} warriors.");
			}
			if( warriorCount > 0 ) {
				foreach(var ent in warriors) {
					var entActor = ent.GetComponent<Actor>();
					bool attackingNow = entActor.isAttacking;
					bool movingNow = entActor.isMoving;
					bool deadNow = !(IsValid(ent) && ent.IsAlive);
					if( ! generalCryAttackStart.TryGetValue(ent, out long started) ) started = generalCryTimer.ElapsedMilliseconds;
					long duration = generalCryTimer.ElapsedMilliseconds - started;
					GeneralCryStatus newStatus;
					if( ! generalCryStatus.TryGetValue(ent, out GeneralCryStatus curStatus) ) {
						// Log($"{generalCryTimer.ElapsedMilliseconds}: Gained warrior: {ent.Address}");
						newStatus = GeneralCryStatus.Born;
					} else {
						newStatus = deadNow ? GeneralCryStatus.Dead :
							attackingNow ? GeneralCryStatus.Attack :
							movingNow ? GeneralCryStatus.Move :
							GeneralCryStatus.Idle;
						if ( newStatus == GeneralCryStatus.Attack && curStatus != newStatus ) {
							generalCryAttackStart[ent] = generalCryTimer.ElapsedMilliseconds;
							// var entStats = ent.GetComponent<Stats>();
							// entStats.StatDictionary.TryGetValue(GameStat.AccuracyRating, out int accuracy);
							// Log($"New Warrior Stat (#1 of {entStats.StatsCount}): Accuracy: {accuracy}");
							// foreach(var statEntry in entStats.StatDictionary) {
								// Log($"New Warrior Stat: {Enum.GetName(typeof(GameStat), statEntry.Key)} : {statEntry.Value}");
							// }
						}
						else if ( curStatus == GeneralCryStatus.Attack && newStatus != curStatus ) {
							PersistedText.Add($"{duration} / {generalCryTimer.ElapsedMilliseconds} ms", ent.Pos, 4000, Color.CornflowerBlue);
						}

					}
					generalCryStatus[ent] = newStatus;
				}

				string status = "";
				foreach(var item in generalCryStatus) {
					status += $"{item.Key.Address % 10000}:{item.Value} ";
				}
				Log($"{generalCryTimer.ElapsedMilliseconds}: [ { status }]");
			}
			generalCryWasOnCooldown = onCooldown;
			deployedCountBefore = warriorCount;
		}

		private void CheckBanner(ToggleHotkeyNode setting, string skillName, string auraBuffName, string stageBuffName) {
			if ( !setting.Enabled ) return;
			if ( !IsInMap(GameController.Area) ) return;
			if ( (!HasBuff(auraBuffName))
					|| TryGetBuffValue(stageBuffName, out int stage) && stage == 50 && NearbyEnemies(100).Any(e => e.Rarity > MonsterRarity.Rare) ) {
				SkillManager.TryUseSkill(skillName, setting.Value);
			}
		}

		public override void Render() {
			try {
				var ui = GameController.Game.IngameState.IngameUi;
				var camera = GameController.Game.IngameState.Camera;
				Globals.Render();
				PersistedText.Render(camera, Graphics);
				DeathTracker.Render();

				var stash = GameController.Game.IngameState.IngameUi.StashElement;
				if ( showRecipe && stash != null && stash.IsValid && stash.IsVisible && stash.IndexVisibleStash == 0 )
					currentRecipe.Render(Graphics);

				// RenderLabels();

				if ( Settings.ShowPosition ) DrawTextAtPlayer($"<{GetPlayer().Pos}>");

				RenderBuffs();

				DebugRegen();

				Entity maxEnt = null;
				var maxLife = 0;
				foreach ( var ent in GameController.Entities ) {
					if ( !IsValid(ent) ) continue;
					if ( Settings.DebugCorpses.Value ) {
						if ( ent.IsDead && ent.IsTargetable && ent.DistancePlayer < 50f ) {
							var life = ent.GetComponent<Life>();
							if ( life == null ) continue;
							if ( life.MaxHP > maxLife ) {
								maxEnt = ent;
								maxLife = life.MaxHP;
							}
						}
					}
					if ( Settings.ShowPoisonStacks || Settings.ShowWitherStacks ) {
						if ( ent.Rarity >= MonsterRarity.Rare && (!ent.IsDead) && ent.IsTargetable && ent.IsHostile && ent.DistancePlayer < 70f ) {
							string text = "";
							TryGetGameStat(ent, GameStat.ChaosDamageResistancePct, out int chaosRes);
							if ( Settings.ShowWitherStacks ) {
								text += $"W:{GetWitherStacks(ent)} ";
							}
							if ( Settings.ShowPoisonStacks ) {
								text += $"P:{GetPoisonStacks(ent)} ";
							}
							text += $"C:{(chaosRes > 0 ? "+" : "")}{chaosRes}";
							DrawTextAtEnt(ent, text);
						}
					}
				}
				if ( maxEnt != null ) {
					DrawTextAtEnt(maxEnt, $"{maxLife}");
				}
				if ( Settings.DebugLife.Value ) {
					try {
						var player = GameController.Player;
						var life = player.GetComponent<Life>();
						DrawTextAtPlayer($"CurHP:{life.CurHP} Reserved:{life.TotalReservedHP} Missing: {MissingEHPPercent(player)}  Full: {IsFullEnough(player)}");
						DrawTextAtPlayer($"CurES:{life.CurES} MaxES:{life.MaxES}");
						DrawTextAtPlayer($"CurMana:{life.CurMana} Reserved:{life.TotalReservedMana}");
					} catch ( Exception ) { }
				}

				if ( Settings.ShowFlaskStatus.Value ) {
					FlaskManager.Render();
				}

				if ( Settings.ShowCursorPosition.Value ) {
					var pos = WindowToScreenRelative(Input.MousePosition);
					DrawTextAtPlayer($"X: {pos.X} Y: {pos.Y}");
				}

				if ( Settings.ShowAttackRate.Value ) {
					DrawTextAtPlayer($"Attacks: {AttacksPerSecond(61, 10):F2}/s");
					var dict = GetGame()?.Player.GetComponent<Stats>()?.StatDictionary;
					if( dict != null ) {
						// DrawStatAtPlayer(dict, GameStat.MainHandTotalBaseWeaponAttackDurationMs);
						// DrawStatAtPlayer(dict, GameStat.MainHandAttackSpeedPct);
						// DrawStatAtPlayer(dict, GameStat.VirtualActionSpeedPct);
						// DrawStatAtPlayer(dict, GameStat.SupportMeleePhysicalDamageAttackSpeedPctFinal);
					}
				}

				// DrawTextAtPlayer(string.Format("XP Rate: {0:N}/hr", xpPerMS.Value * (1000 * 60 * 60)));

			} catch ( Exception e ) {
				Log(e.ToString());
			}
		}
		private void DrawStatAtPlayer(Dictionary<GameStat, int> dict, GameStat stat) {
			int value = -1;
			dict.TryGetValue(stat, out value);
			DrawTextAtPlayer($"{stat}: {value}");
		}

		private Stopwatch lifeTimer = new Stopwatch();
		private long lifeTimeBefore = -1;
		private int lifeBefore = -1;
		private int biggestHit = 0;
		private int biggestRegen = 0;
		private MovingAverage lifeRegen = new MovingAverage(20); // very small window
		private void ResetRegen() {
			lifeTimer.Restart();
			lifeTimeBefore = -1;
			lifeBefore = -1;
			biggestHit = 0;
			biggestRegen = 0;
			lifeRegen = new MovingAverage(20); // very small window
		}
		private void DebugRegen() {
			if ( !Settings.DebugRegen.Value ) return;
			if ( !lifeTimer.IsRunning ) lifeTimer.Start();
			if ( lifeTimeBefore == -1 ) lifeTimeBefore = lifeTimer.ElapsedMilliseconds;
			var player = GameController.Player;
			if ( !IsValid(player) ) return;
			var life = player.GetComponent<Life>();
			if ( life == null ) return;
			if ( lifeBefore == -1 ) {
				lifeBefore = life.CurHP + life.CurES;
			} else {
				var elapsed = Math.Max(1, lifeTimer.ElapsedMilliseconds - lifeTimeBefore);
				int lifeNow = life.CurHP + life.CurES;
				int delta = lifeNow - lifeBefore;
				lifeBefore = lifeNow;
				biggestHit = Math.Min(biggestHit, delta);
				var thisRegen = (int)(delta * 1000 / Math.Max(15, elapsed));
				if ( thisRegen > biggestRegen ) {
					Log($"New Max Regen: {thisRegen} gained {delta} ehp in {elapsed} ms");
					biggestRegen = thisRegen;
				}
				lifeRegen.Add(delta);
				lifeTimeBefore = lifeTimer.ElapsedMilliseconds;
				DrawTextAtPlayer($"Regen:[{biggestHit}/{(int)lifeRegen.Value}/+{biggestRegen}]");
			}
		}
	}
}
