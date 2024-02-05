using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
	partial class Program {
		static bool CheckIfValidAirlockBlock(Program instance, IMyTerminalBlock candidate, string name) {
			return candidate.IsSameConstructAs(instance.Me) && candidate.CustomName.Contains(name) && candidate.CustomName.Contains("Airlock");
		}

		public class Airlock {
			private Program instance;

			public List<IMyDoor> innerDoors = new List<IMyDoor>();
			public List<IMyDoor> outerDoors = new List<IMyDoor>();
			public List<IMyButtonPanel> panels = new List<IMyButtonPanel>();
			public List<IMyAirVent> vents = new List<IMyAirVent>();

			public int cycleAttempts = 0;
			public bool currentState = false; // true = inner doors opened, false = outer
			public bool errorState = false;
			public float lastPressure = 0;
			public string name;

			public Airlock(Program program, string passedName) {
				name = passedName;
				instance = program;

				instance.GridTerminalSystem.GetBlocksOfType(innerDoors, candidate => CheckIfValidAirlockBlock(instance, candidate, name) && candidate.CustomName.Contains(INNER_DOOR_IDENTIFIER));
				instance.GridTerminalSystem.GetBlocksOfType(outerDoors, candidate => CheckIfValidAirlockBlock(instance, candidate, name) && candidate.CustomName.Contains(OUTER_DOOR_IDENTIFIER));
				instance.GridTerminalSystem.GetBlocksOfType(panels, candidate => CheckIfValidAirlockBlock(instance, candidate, name));
				instance.GridTerminalSystem.GetBlocksOfType(vents, candidate => CheckIfValidAirlockBlock(instance, candidate, name));

				for (int id = 0; id < panels.Count; id++) {
					panels[id].SetCustomButtonName(0, "Cycle Airlock");
				}

				if (vents.Count > 0) {
					lastPressure = vents[0].GetOxygenLevel();
					currentState = (lastPressure >= 0.5);
				}
			}

			public void SetErrorState(bool newErrorState) {
				errorState = newErrorState;
				string buttonLabel = errorState ? "Override Airlock" : "Cycle Airlock";

				for (int id = 0; id < panels.Count; id++) {
					panels[id].SetCustomButtonName(0, buttonLabel);
				}
			}
		}
	}
}
