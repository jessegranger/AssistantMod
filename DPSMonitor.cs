using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assistant.Globals;

namespace Assistant {
	class DPSMonitor : State {

		public DPSMonitor(State next = null) : base(next) {
			var game = GetGame();
			game.EntityListWrapper.EntityAdded += OnEntityAdded;
			game.EntityListWrapper.EntityRemoved += OnEntityRemoved;
			game.Area.OnAreaChange += OnAreaChange;
		}

		private long xpAtLastAreaChange = 0;
		private long normalKillsInArea = 0;
		private long magicKillsInArea = 0;
		private long rareKillsInArea = 0;
		private long uniqueKillsInArea = 0;
		private void OnAreaChange(ExileCore.AreaInstance obj) {
			while ( incomingMonsters.TryDequeue(out Sighting ignored) ) { }
			while ( outgoingMonsters.TryDequeue(out uint ignored) ) { }
			while ( dyingMonsters.TryDequeue(out Vector3 ignored) ) { }
			seenMonsters.Clear();
			long xpCurrent = GetGame()?.Player.GetComponent<Player>().XP ?? xpAtLastAreaChange;
			long xpDelta = xpCurrent - xpAtLastAreaChange;
			if ( xpAtLastAreaChange != 0 && xpDelta != 0 && GetSettings().ShowXPReport ) {
				Notify($"Kills: {uniqueKillsInArea} uniq, {rareKillsInArea} rare, {magicKillsInArea} magic, {normalKillsInArea} white, {formatNumber(xpCurrent - xpAtLastAreaChange)} xp", Color.Yellow, 10000, 1);
			}
			xpAtLastAreaChange = xpCurrent;
			normalKillsInArea = 0;
			magicKillsInArea = 0;
			rareKillsInArea = 0;
			uniqueKillsInArea = 0;
		}

		~DPSMonitor() {
			var game = GetGame();
			if ( game == null || game.EntityListWrapper == null) return;
			game.EntityListWrapper.EntityAdded -= OnEntityAdded;
			game.EntityListWrapper.EntityRemoved -= OnEntityRemoved;
		}

		private class Sighting {
			public Entity Ent;
			public long FirstDamageTime;
			public long AddedMS;
			public long AddedHP;
		}
		private Stopwatch seenTimer = Stopwatch.StartNew();
		private Dictionary<uint, Sighting> seenMonsters = new Dictionary<uint, Sighting>();
		private ConcurrentQueue<Sighting> incomingMonsters = new ConcurrentQueue<Sighting>();
		private ConcurrentQueue<uint> outgoingMonsters = new ConcurrentQueue<uint>();
		private ConcurrentQueue<Vector3> dyingMonsters = new ConcurrentQueue<Vector3>();
		private long xpLastFrame = 0;
		// private Dictionary<uint, Entity> seenDoors = new Dictionary<uint, Entity>();
		// private Dictionary<uint, Entity> seenPortals = new Dictionary<uint, Entity>();
		private void OnEntityRemoved(Entity ent) {
			if ( ent == null ) return;
			if ( ent.Path == null ) return;
			outgoingMonsters.Enqueue(ent.Id);
		}
		private void OnEntityDeath(Entity ent) {
			var settings = GetSettings();
			Sighting seen = seenMonsters[ent.Id];
			long damage = seen.AddedHP;
			long ms = 1 + (seenTimer.ElapsedMilliseconds - seen.FirstDamageTime);
			if ( ms > 50 && damage > 1 ) {
				double dps = damage * 1000d / ms;
				string path = ent.Path.Split('/').Last();
				string dpsText = formatNumber(dps);
				string fullText = $"{path} {dpsText}dps = {formatNumber(seen.AddedHP)}hp / {ms}ms ]";
				Log(fullText);
				if ( (settings?.ShowDPS ?? false) && ent.Rarity >= MonsterRarity.Rare ) {
					PersistedText.Add(formatNumber(dps), ent.Pos, 4000, Color.Aqua);
					Notify($"DPS: {dpsText}" , Color.Aqua, 6000, 1f);
				}
			}
			seenMonsters.Remove(ent.Id);
			switch( ent.Rarity ) {
				case MonsterRarity.White: normalKillsInArea += 1; break;
				case MonsterRarity.Magic: magicKillsInArea += 1; break;
				case MonsterRarity.Rare: rareKillsInArea += 1; break;
				case MonsterRarity.Unique: uniqueKillsInArea += 1; break;
			}

			if( GetSettings().ShowXPGains ) {
				dyingMonsters.Enqueue(ent.Pos);
			}
		}
		private string formatNumber(double num) {
			string suffix = "";
			if ( num > 1024 ) {
				suffix = "K";
				num /= 1024;
				if ( num > 1024 ) {
					suffix = "M";
					num /= 1024;
				}
			}
			return num.ToString("N2") + suffix;
		}
		public override State OnTick() {
			var settings = GetSettings();
			while( incomingMonsters.TryDequeue(out Sighting incoming) ) {
				seenMonsters[incoming.Ent.Id] = incoming;
			}
			while( outgoingMonsters.TryDequeue(out uint outgoing) ) {
				seenMonsters.Remove(outgoing);
			}

			long xpCurrent = GetGame()?.Player.GetComponent<Player>().XP ?? xpLastFrame;
			if ( settings.ShowXPGains ) {
				double xpGain = (xpCurrent - xpLastFrame) / (double)dyingMonsters.Count;
				if ( xpGain != 0 ) {
					string xp = (xpGain > 0 ? "+" : "") + $"{xpGain:F0}";
					while ( dyingMonsters.TryDequeue(out Vector3 pos) ) {
						PersistedText.Add(xp, DriftTowardPlayer(() => pos, 1000), 1000, Color.Gold);
					}
				}
			}

			Sighting[] seen = seenMonsters.Values.ToArray();
			int deadCount = 0;
			foreach ( Sighting s in seen ) {
				Life life = s.Ent?.GetComponent<Life>();
				if ( life == null ) continue;
				if ( s.FirstDamageTime == 0 && life.CurHP < s.AddedHP ) {
					s.FirstDamageTime = seenTimer.ElapsedMilliseconds;
				}
				if ( life.CurHP < 1 ) {
					deadCount += 1;
					OnEntityDeath(s.Ent);
				}
			}
			xpLastFrame = xpCurrent;
			return this;
		}
		private void OnEntityAdded(Entity ent) {
			if ( !IsValid(ent) ) return;
			if ( ent.IsHostile && ent.Path.StartsWith("Metadata/Monsters") /* && ent.Rarity >= MonsterRarity.Rare */ ) {
				Life life = ent.GetComponent<Life>();
				if ( life != null && life.CurHP > 0 ) {
					incomingMonsters.Enqueue(new Sighting() { AddedHP = life.CurHP, AddedMS = seenTimer.ElapsedMilliseconds, Ent = ent });
				}
			}
		}

	}
}
