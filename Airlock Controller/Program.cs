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
	partial class Program : MyGridProgram {
		// SETTINGS
		const int maxAttempts = 20;
		const bool printDebug = true;

		const string AIR_INTAKE_NAME = "Air Intake";

		const string OUTER_DOOR_IDENTIFIER = "Outer";
		const string INNER_DOOR_IDENTIFIER = "Inner";

		// DO NOT EDIT BELOW HERE
		private List<IMyAirVent> airIntakes = new List<IMyAirVent>();
		private IMyTextSurface debugOut;

		public enum SimpleDoorState {
			Unknown = 0,
			Open = 1,
			Closed = 2
		}

		private Dictionary<string, Airlock> airlocks = new Dictionary<string, Airlock>();
		private Dictionary<string, Airlock> activeAirlocks = new Dictionary<string, Airlock>();

		public void Init() {
			debugOut = Me.GetSurface(0);
			GridTerminalSystem.GetBlocksOfType(airIntakes, airIntake => airIntake.CustomName.StartsWith(AIR_INTAKE_NAME));

			List<IMyAirVent> airlockCandidates = new List<IMyAirVent>();
			GridTerminalSystem.GetBlocksOfType(airlockCandidates, candidate => candidate.IsSameConstructAs(Me) && candidate.CustomName.Contains(" Airlock "));

			for (int count = 0; count < airlockCandidates.Count; count++) {
				string airlockName = airlockCandidates[count].CustomName.Substring(0, airlockCandidates[count].CustomName.IndexOf(" Airlock"));

				if (!airlocks.ContainsKey(airlockName.ToLower())) { // If an airlock with this name doesn't already exist, create one.
					airlocks.Add(airlockName.ToLower(), new Airlock(this, airlockName));
					activeAirlocks.Add(airlockName.ToLower(), airlocks[airlockName.ToLower()]); // Also add to active airlocks so the update loop can verify they're in a legal state.
				}
			}

			Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}

		public Program() {
			Init();
		}

		public SimpleDoorState GetDoorGroupState(List<IMyDoor> doors) {
			SimpleDoorState state = 0;

			for (int count = 0; count < doors.Count; count++) {
				switch (doors[count].Status) {
					case DoorStatus.Open: state = SimpleDoorState.Open; break;
					case DoorStatus.Closed: state = SimpleDoorState.Closed; break;
				}
				if (state == SimpleDoorState.Unknown) { break; }
			}

			return state;
		}

		public float GetExternalPressure() {
			float externalPressure = 0; // Assume the worst if there are no air intakes.

			foreach (IMyAirVent intake in airIntakes) {
				if (!intake.Closed) {
					externalPressure = intake.GetOxygenLevel();
					break;
				} else { airIntakes.Remove(intake); }
			}

			return externalPressure;
		}

		public void CycleDoors(Airlock airlock, List<IMyDoor> doorsToClose, List<IMyDoor> doorsToOpen) {
			for (int count = 0; count < doorsToOpen.Count; count++) {
				doorsToOpen[count].Enabled = true;
				doorsToOpen[count].OpenDoor();
			}

			airlock.cycleAttempts = 0;
			airlock.SetErrorState(false);
		}

		public void Main(string argument, UpdateType updateSource) {
			List<string> keysToDiscard = new List<string>();

			float externalPressure = GetExternalPressure();

			if ((updateSource & UpdateType.Update10) != 0) { // Fast update, update active airlocks.
				foreach (KeyValuePair<string, Airlock> entry in activeAirlocks) {
					float newPressure = entry.Value.vents.Count > 0 ? entry.Value.vents[0].GetOxygenLevel() : 0;

					List<IMyDoor> doorsToClose = entry.Value.currentState ? entry.Value.outerDoors : entry.Value.innerDoors;
					List<IMyDoor> doorsToOpen = entry.Value.currentState ? entry.Value.innerDoors : entry.Value.outerDoors;

					bool allClosed = GetDoorGroupState(doorsToClose) == SimpleDoorState.Closed;

					for (int count = 0; count < doorsToClose.Count; count++) {
						if (doorsToClose[count].Status == DoorStatus.Open || doorsToClose[count].Status == DoorStatus.Opening) {
							doorsToClose[count].CloseDoor();
							doorsToClose[count].Enabled = true;
						} else {
							allClosed = (doorsToClose[count].OpenRatio == 0);
							doorsToClose[count].Enabled = !allClosed;
						}

						//if (allClosed) { break; }
					}

					if (allClosed) {
						for (int count = 0; count < entry.Value.vents.Count; count++) {
							entry.Value.vents[count].Depressurize = !entry.Value.currentState;
						}

						bool pressureOK = entry.Value.currentState ? newPressure >= 0.99f : newPressure <= 0.01 || externalPressure > 0.5 || Math.Abs(newPressure - externalPressure) < 0.01;
						entry.Value.cycleAttempts = (!pressureOK && Math.Abs(newPressure - entry.Value.lastPressure) <= 0.01) ? entry.Value.cycleAttempts + 1 : 0;
						entry.Value.SetErrorState(entry.Value.cycleAttempts >= maxAttempts);

						if (pressureOK) { // Airlock cycling is either complete or stuck, so open the appropriate doors.
							CycleDoors(entry.Value, doorsToClose, doorsToOpen);
							keysToDiscard.Add(entry.Key);
						}
					}

					entry.Value.lastPressure = newPressure;
					AirlockUI.DrawPanels(entry.Value);
				}
			} else if ((updateSource & UpdateType.Update100) != 0) { // Standard update, just refresh the airlock panels.
				foreach (KeyValuePair<string, Airlock> entry in airlocks) {
					entry.Value.lastPressure = entry.Value.vents.Count > 0 ? entry.Value.vents[0].GetOxygenLevel() : 0;
					AirlockUI.DrawPanels(entry.Value);
				}
			} else if (argument.Length > 0) { // Probably triggered by a button press with argument passed.
				string[] args = argument.Split(' ');
				switch (args[0].ToLower()) {
					case "cycle":
						if (args[1] != null && airlocks.ContainsKey(args[1].ToLower())) {
							Airlock currentAirlock = airlocks[args[1].ToLower()];

							if (currentAirlock.errorState) {
								List<IMyDoor> doorsToClose = currentAirlock.currentState ? currentAirlock.outerDoors : currentAirlock.innerDoors;
								List<IMyDoor> doorsToOpen = currentAirlock.currentState ? currentAirlock.innerDoors : currentAirlock.outerDoors;

								CycleDoors(currentAirlock, doorsToClose, doorsToOpen);
								keysToDiscard.Add(args[1].ToLower());
								AirlockUI.DrawPanels(currentAirlock);
							} else if (!activeAirlocks.ContainsKey(args[1].ToLower())) {
								currentAirlock.currentState = !currentAirlock.currentState;
								activeAirlocks.Add(currentAirlock.name.ToLower(), currentAirlock);
							}
						}
						break;
					case "scan": // Re-scan the grid for airlocks.
						Init();
						break;
					default: break; // In case of invalid command, do nothing.
				}
			}

			for (int count = 0; count < keysToDiscard.Count; count++) {
				activeAirlocks.Remove(keysToDiscard[count]);
			}

			if (printDebug) {
				IMyTextSurface debugOut = Me.GetSurface(0);
				RectangleF template = new RectangleF((debugOut.TextureSize - debugOut.SurfaceSize) / 2f, debugOut.SurfaceSize);
				MySpriteDrawFrame canvas = debugOut.DrawFrame();
				float sizeFactor = Math.Min(debugOut.TextureSize.X / 256, debugOut.TextureSize.Y / 256);

				debugOut.ContentType = ContentType.SCRIPT;
				debugOut.Script = "";
				debugOut.ScriptBackgroundColor = Color.Black;
				debugOut.ScriptForegroundColor = Color.White;

				canvas.Add(new MySprite() { // Debug output title
					Type = SpriteType.TEXT,
					Data = "AIRLOCK STATUS",
					Position = new Vector2(template.Size.X / 2, template.Size.Y * 0.05f) + template.Position,
					RotationOrScale = sizeFactor * 0.5f,
					Color = debugOut.ScriptForegroundColor,
					Alignment = TextAlignment.CENTER,
					FontId = "White"
				});

				int counter = 0;
				foreach (KeyValuePair<string, Airlock> entry in airlocks) {
					float flashAlpha = (entry.Value.errorState && (System.DateTime.UtcNow.Millisecond / 500) % 2 == 0) ? 0.05f : 0.01f;
					string pressureStatus = "Error";

					if (entry.Value.errorState) {
						pressureStatus = "ERROR";
					} else {
						if (entry.Value.currentState) {
							pressureStatus = entry.Value.lastPressure >= 0.9 ? "Pressurised" : "Pressurising";
						} else {
							pressureStatus = entry.Value.lastPressure < 0.01 ? "Depressurised" : "Depressurising";
						}
					}

					canvas.Add(new MySprite() { // Airlock name
						Type = SpriteType.TEXT,
						Data = entry.Value.name + " Airlock",
						Position = new Vector2(template.Size.X * 0.05f, template.Size.Y * (0.15f + (0.18f * counter))) + template.Position,
						RotationOrScale = sizeFactor * 0.4f,
						Color = debugOut.ScriptForegroundColor,
						Alignment = TextAlignment.LEFT,
						FontId = "White"
					});

					canvas.Add(new MySprite() { // Current pressurisation status
						Type = SpriteType.TEXT,
						Data = pressureStatus,
						Position = new Vector2(template.Size.X * 0.95f, template.Size.Y * (0.15f + (0.18f * counter))) + template.Position,
						RotationOrScale = sizeFactor * 0.4f,
						Color = debugOut.ScriptForegroundColor,
						Alignment = TextAlignment.RIGHT,
						FontId = "White"
					});

					canvas.Add(new MySprite() { // Percentage bar background
						Type = SpriteType.TEXTURE,
						Data = "SquareSimple",
						Position = new Vector2(template.Size.X / 2, template.Size.Y * (0.27f + (0.18f * counter))) + template.Position,
						Size = new Vector2(template.Size.X * 0.9f, template.Size.Y * 0.07f),
						Color = entry.Value.lastPressure >= 0.5f ? debugOut.ScriptForegroundColor.Alpha(flashAlpha) : Color.Lerp(Color.Red, Color.Yellow, entry.Value.lastPressure * 2).Alpha(flashAlpha),
						Alignment = TextAlignment.CENTER
					});

					canvas.Add(new MySprite() { // Percentage bar
						Type = SpriteType.TEXTURE,
						Data = "SquareSimple",
						Position = new Vector2(template.Size.X * (0.05f + (entry.Value.lastPressure * 0.45f)), template.Size.Y * (0.27f + (0.18f * counter))) + template.Position,
						Size = new Vector2(template.Size.X * 0.9f * entry.Value.lastPressure, template.Size.Y * 0.07f),
						Color = Color.Lerp(Color.Red, Color.Green, entry.Value.lastPressure),
						Alignment = TextAlignment.CENTER
					});

					canvas.Add(new MySprite() { // Percentage text
						Type = SpriteType.TEXT,
						Data = Math.Round(entry.Value.lastPressure * 100).ToString() + "%",
						Position = new Vector2(template.Size.X / 2, template.Size.Y * (0.23f + (0.18f * counter))) + template.Position,
						RotationOrScale = sizeFactor * 0.4f,
						Color = debugOut.ScriptForegroundColor,
						Alignment = TextAlignment.CENTER,
						FontId = "White"
					});

					counter++;
				}

				canvas.Dispose(); // Draw debug output.
			}

			Runtime.UpdateFrequency = activeAirlocks.Count > 0 ? UpdateFrequency.Update10 | UpdateFrequency.Update100 : UpdateFrequency.Update100;
		}
	}
}
