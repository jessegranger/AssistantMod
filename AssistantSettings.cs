﻿using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace Assistant {
	public class AssistantSettings : ISettings {
		public ToggleNode Enable { get; set; }

		[Menu("Flasks", 5006)] public EmptyNode EmptyFlasks { get; set; } = new EmptyNode();

		[Menu("Use Life Flask on 50% unreserved life", 1, 5006)] public ToggleNode UseLifeFlasks { get; set; }

		[Menu("Use Mana Flask at 40% unreserved mana", 2, 5006)] public ToggleNode UseManaFlasks { get; set; }

		[Menu("Use flasks to cure debuffs", "eg, Bleeding, Frozen", 3, 5006)] public ToggleNode AutoCureDebuffs { get; set; }

		[Menu("Use utility flasks when full", 4, 5006)] public ToggleNode AutoUseFullPotions { get; set; }
		[Menu("Use utility flasks when you hit a rare or unique", "if not already in effect", 4, 5006)] public ToggleNode MaintainFlasksOnRare { get; set; }

		[Menu("Skills", 5007)] public EmptyNode EmptySkills { get; set; } = new EmptyNode();

		[Menu("Force Skill Use (ignore skill bar)", 1, 5007)] public ToggleNode ForceSkillUse { get; set; }
		[Menu("Use Enduring Cry", 1, 5007)] public ToggleHotkeyNode UseEnduringCry { get; set; } = Keys.E;
		[Menu("Use Berserk", 2, 5007)] public ToggleHotkeyNode UseBerserk { get; set; } = Keys.R;
		[Menu("Use Blood Rage", 3, 5007)] public ToggleHotkeyNode UseBloodRage { get; set; } = Keys.R;
		[Menu("Use Bone Armour", 3, 5007)] public ToggleHotkeyNode UseBoneArmour { get; set; } = Keys.T;
		[Menu("Use Corrupting Fever", 3, 5007)] public ToggleHotkeyNode UseCorruptingFever { get; set; } = Keys.T;
		[Menu("Use Defiance Banner", 4, 5007)] public ToggleHotkeyNode UseDefianceBanner { get; set; } = Keys.R;
		[Menu("Use Dread Banner", 4, 5007)] public ToggleHotkeyNode UseDreadBanner { get; set; } = Keys.R;
		[Menu("Use War Banner", 4, 5007)] public ToggleHotkeyNode UseWarBanner { get; set; } = Keys.R;
		[Menu("Use Molten Shell", 6, 5007)] public ToggleHotkeyNode UseMoltenShell { get; set; } = Keys.T;
		[Menu("Use Immortal Call", 7, 5007)] public ToggleHotkeyNode UseImmortalCall { get; set; } = Keys.T;
		[Menu("Use Steelskin", 8, 5007)] public ToggleHotkeyNode UseSteelskin { get; set; } = Keys.T;
		[Menu("Use Temporal Rift", 9, 5007)] public ToggleHotkeyNode UseTemporalRift { get; set; } = Keys.R;
		[Menu("Use Withering Step", 10, 5007)] public ToggleHotkeyNode UseWitheringStep { get; set; } = Keys.W;
		[Menu("Use Plaguebearer", 11, 5007)] public ToggleHotkeyNode UsePlaguebearer { get; set; } = Keys.R;
		[Menu("Use Infernal Cry", 12, 5007)] public ToggleHotkeyNode UseInfernalCry { get; set; } = Keys.Q;
		[Menu("Use Vaal Molten Shell", 13, 5007)] public ToggleHotkeyNode UseVaalMoltenShell { get; set; } = Keys.T;
		[Menu("Use Vaal Grace", 14, 5007)] public ToggleHotkeyNode UseVaalGrace { get; set; } = Keys.R;
		[Menu("Use Vaal Discipline", 15, 5007)] public ToggleHotkeyNode UseVaalDiscipline { get; set; } = Keys.T;
		[Menu("Use Vaal Haste", 16, 5007)] public ToggleHotkeyNode UseVaalHaste { get; set; } = Keys.X;
		[Menu("[Delve] Use Flares in Darkness", 18, 5007)] public ToggleHotkeyNode UseFlares { get; set; } = Keys.D6;
		[Menu("Use Focus", 19, 5007)] public ToggleHotkeyNode UseFocus{ get; set; } = Keys.W;

		[Menu("Blessing", 5010)] public EmptyNode EmptyBlessing { get; set; } = new EmptyNode();
		[Menu("Use Purity of Elements", 1, 5010)] public ToggleNode UsePurityOfElementsBlessing { get; set; }
		[Menu("Use Purity of Fire", 1, 5010)] public ToggleNode UsePurityOfFireBlessing { get; set; }
		[Menu("Use Purity of Ice", 1, 5010)] public ToggleNode UsePurityOfIceBlessing { get; set; }
		[Menu("Use Purity of Lightning", 1, 5010)] public ToggleNode UsePurityOfLightningBlessing { get; set; }
		[Menu("Use Vitality", 1, 5010)] public ToggleNode UseVitalityBlessing { get; set; }
		[Menu("Use Clarity", 1, 5010)] public ToggleNode UseClarityBlessing { get; set; }
		[Menu("Use Precision", 1, 5010)] public ToggleNode UsePrecisionBlessing { get; set; }
		[Menu("Use Determination", 1, 5010)] public ToggleNode UseDeterminationBlessing { get; set; }
		[Menu("Use Discipline", 1, 5010)] public ToggleNode UseDisciplineBlessing { get; set; }
		[Menu("Use Haste", 1, 5010)] public ToggleNode UseHasteBlessing { get; set; }
		[Menu("Use Grace", 1, 5010)] public ToggleNode UseGraceBlessing { get; set; }
		[Menu("Use Anger", 1, 5010)] public ToggleNode UseAngerBlessing { get; set; }
		[Menu("Use Wrath", 1, 5010)] public ToggleNode UseWrathBlessing { get; set; }
		[Menu("Use Pride", 1, 5010)] public ToggleNode UsePrideBlessing { get; set; }
		[Menu("Use Hatred", 1, 5010)] public ToggleNode UseHatredBlessing { get; set; }
		[Menu("Use Zealotry", 1, 5010)] public ToggleNode UseZealotryBlessing { get; set; }
		[Menu("Use Malevolence", 1, 5010)] public ToggleNode UseMalevolenceBlessing { get; set; }
		[Menu("Maintain Divine Blessing", 1, 5010)] public ToggleHotkeyNode UseDivineBlessing { get; set; } = Keys.W;

		[Menu("Looting", 5009)] public EmptyNode EmptyLooting { get; set; } = new EmptyNode();
		[Menu("Identify Maps", 1, 5009)] public ToggleNode IdentifyMaps { get; set; }
		[Menu("Open Stashed Decks", 2, 5009)] public ToggleNode OpenStashedDecks { get; set; }
		[Menu("Quick Loot", 3, 5009)] public ToggleHotkeyNode ClickNearestLabel { get; set; } = Keys.NumPad0;
		[Menu("Stash Deposit", 4, 5009)] public ToggleHotkeyNode StashDeposit { get; set; } = Keys.Oem3;
		[Menu("Stash Restock", 5, 5009)] public ToggleHotkeyNode StashRestock { get; set; } = Keys.Oem6;

		[Menu("Accessibility", 5008)] public EmptyNode EmptyAccessibility { get; set; } = new EmptyNode();
		[Menu("Use Arrow Keys to Move", 0, 5008)] public ToggleNode UseArrowKeys { get; set; }
		[Menu("Use A MultiKey", 1, 5008)] public ToggleHotkeyNode UseMultiKey { get; set; } = Keys.Space;
		[Menu("Send With MultiKey #1", 2, 5008)] public ToggleHotkeyNode UseMultiKey1 { get; set; } = Keys.Q;
		[Menu("Send With MultiKey #2", 3, 5008)] public ToggleHotkeyNode UseMultiKey2 { get; set; } = Keys.W;
		[Menu("Send With MultiKey #3", 4, 5008)] public ToggleHotkeyNode UseMultiKey3 { get; set; } = Keys.E;

		[Menu("Debug", 5005)] public EmptyNode EmptyMain { get; set; } = new EmptyNode();
		[Menu("Show Player Position", 1, 5005)] public ToggleNode ShowPosition { get; set; }
		[Menu("Show Player Buffs", "Internal buff code names", 2, 5005)] public ToggleNode ShowBuffNames { get; set; }
		[Menu("Show Enemy Buffs", 2, 5005)] public ToggleNode ShowEnemyBuffNames { get; set; }
		[Menu("Show Player Life", 3, 5005)] public ToggleNode DebugLife { get; set; }
		[Menu("Show Player Regen", 4, 5005)] public ToggleNode DebugRegen { get; set; }
		[Menu("Show Highest Corpse Life", 5, 5005)] public ToggleNode DebugCorpses { get; set; }
		[Menu("Show Death Frames", 7, 5005)] public ToggleNode ShowDeathFrames { get; set; }
		[Menu("Show Wither Stacks", 8, 5005)] public ToggleNode ShowWitherStacks { get; set; }
		[Menu("Show Poison Stacks", 8, 5005)] public ToggleNode ShowPoisonStacks { get; set; }
		[Menu("Show Flask Status", 9, 5005)] public ToggleNode ShowFlaskStatus { get; set; }
		[Menu("Show Cursor Position", 10, 5005)] public ToggleNode ShowCursorPosition { get; set; }
		[Menu("Debug General's Cry", 11, 5005)] public ToggleNode DebugGeneralsCry { get; set; }
		[Menu("Estimate Attack Rate", 12, 5005)] public ToggleNode ShowAttackRate { get; set; }
		[Menu("Show XP Gains", 13, 5005)] public ToggleNode ShowXPGains { get; set; }
		[Menu("Show XP Rate", 14, 5005)] public ToggleNode ShowXPRate { get; set; }
		[Menu("Show DPS. on Rares", 15, 5005)] public ToggleNode ShowDPS { get; set; }
		[Menu("Debug Distance", 16, 5005)] public ToggleNode ShowDistance { get; set; }
		[Menu("Highlight Delve Loot", 17, 5005)] public ToggleNode ShowDelveLoot { get; set; }
		[Menu("Show Sextant Warning", 17, 5005)] public ToggleNode ShowSextantWarning { get; set; }


	}
}