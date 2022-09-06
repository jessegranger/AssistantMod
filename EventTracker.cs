using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assistant.Globals;

namespace Assistant {
	public static class EventTracker {
		public static void OnTick() {
			var player = GetGame().Player;
			if ( !IsValid(player) ) return;

			var life = player.GetComponent<Life>();
			bool isDead = false;
			if ( life == null ) return;
			if ( life.CurHP < 1 || player.IsDead || (!player.IsAlive) ) isDead = true;
			if ( wasAliveBefore && isDead ) {
				OnDeath(null, player);
			}
			wasAliveBefore = !isDead;

			double ehpMax = (life.MaxHP - life.ReservedFlatHP) + life.MaxES;
			double ehpCur = life.CurHP + life.CurES;
			double ehpLoss = ehpBefore - ehpCur;
			double ehpPercentLoss = ehpLoss / ehpMax;
			if ( ehpPercentLoss >= .15d ) {
				PersistedText.Add($"Savage Hit {ehpLoss}!", player.Pos, 3000, Color.Red);
				if( OnSavageHit != null ) OnSavageHit(null, player);
			}
			ehpBefore = ehpCur;
		}
		private static bool wasAliveBefore = true;
		private static double ehpBefore = 0.0;
		public static event EventHandler<Entity> OnDeath;
		public static event EventHandler<Entity> OnSavageHit;
		// TODO: public static event EventHandler<int> OnFlaskFull;

	}
}
