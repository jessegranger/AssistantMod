using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assistant.Globals;

namespace Assistant {
	static class FocusManager {
		static FocusManager() {
		}
		internal static void OnTick() {
			var Settings = GetSettings();
			if ( Settings == null ) return;
			if ( !Settings.UseFocus ) return;

			if( EquipmentManager.HasEquippedMod("JunMaster2ImmuneToStatusAilmentsWhileFocused1") ) {
				if( HasBuff("poisoned", "bleeding", "maim", "ignited", "chilled", "frozen", "shocked", "scorched", "brittle", "sapped") ) {
					SkillManager.TryUseSkill("Focus", ToVirtualKey(Settings.UseFocus));
				}
			}
		}
	}
}
