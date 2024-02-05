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
		public static class AirlockUI {
			enum DisplayMode {
				Normal = 0,
				Compact = 1
			}

			public static void DrawAirlockStatus(CanvasWrapper surface, Airlock airlock, RectangleF bounds) {
				DisplayMode displayMode = bounds.Size.Y / bounds.Size.X > 16 / 9 ? DisplayMode.Compact : DisplayMode.Normal;
				

			}

			public static void DrawAirlockStatus(CanvasWrapper surface, Airlock airlock) {
				DrawAirlockStatus(surface, airlock, surface.Bounds);
			}

			public static void DrawPanels(Airlock airlock) {
				List<IMyButtonPanel> panels = airlock.panels;
				float flashAlpha = (airlock.errorState && (System.DateTime.UtcNow.Millisecond / 500) % 2 == 0) ? 0.05f : 0.01f;

				for (int id = 0; id < panels.Count; id++) {
					IMyButtonPanel currentPanel = panels[id];
					IMyTextSurfaceProvider panelSurface = (IMyTextSurfaceProvider)panels[id];
					IMyTextSurface target = null;

					if (panelSurface != null) {
						target = panelSurface.GetSurface(0);
						if (panelSurface.SurfaceCount > 1) {
							// TODO: Add support for setting other surfaces, i.e. for sci-fi button panels.
						}

						if (target != null) {
							target.ContentType = ContentType.SCRIPT;
							target.Script = "";
							target.ScriptBackgroundColor = Color.Black;
							target.ScriptForegroundColor = Color.White;

							CanvasWrapper surface = new CanvasWrapper(target);
							string pressureStatus = "Error";

							if (airlock.errorState) {
								pressureStatus = "ERROR";
							} else {
								if (airlock.currentState) {
									pressureStatus = airlock.lastPressure >= 0.9 ? "Pressurised" : "Pressurising";
								} else {
									pressureStatus = airlock.lastPressure < 0.01 ? "Depressurised" : "Depressurising";
								}
							}

							surface.Canvas.Add(new MySprite() { // Airlock name tag
								Type = SpriteType.TEXT,
								Data = airlock.name + " Airlock",
								Position = new Vector2(surface.Bounds.Size.X / 2, surface.Bounds.Size.Y * 0.1f) + surface.Bounds.Position,
								RotationOrScale = surface.Scale,
								Color = target.ScriptForegroundColor,
								Alignment = TextAlignment.CENTER,
								FontId = "White"
							});

							surface.Canvas.Add(new MySprite() { // Current pressurisation status
								Type = SpriteType.TEXT,
								Data = pressureStatus,
								Position = new Vector2(surface.Bounds.Size.X / 2, surface.Bounds.Size.Y * 0.35f) + surface.Bounds.Position,
								RotationOrScale = surface.Scale,
								Color = target.ScriptForegroundColor,
								Alignment = TextAlignment.CENTER,
								FontId = "White"
							});

							surface.Canvas.Add(new MySprite() { // Horizontal rule
								Type = SpriteType.TEXTURE,
								Data = "SquareSimple",
								Position = new Vector2(surface.Bounds.Size.X / 2, surface.Bounds.Size.Y * 0.55f) + surface.Bounds.Position,
								Size = new Vector2(surface.Bounds.Size.X * 0.8f, 1),
								Color = target.ScriptForegroundColor,
								Alignment = TextAlignment.CENTER
							});

							surface.Canvas.Add(new MySprite() { // Percentage bar background
								Type = SpriteType.TEXTURE,
								Data = "SquareSimple",
								Position = new Vector2(surface.Bounds.Size.X / 2, surface.Bounds.Size.Y * 0.65f) + surface.Bounds.Position,
								Size = new Vector2(surface.Bounds.Size.X * 0.8f, surface.Bounds.Size.Y * 0.2f),
								Color = airlock.lastPressure >= 0.5f ? target.ScriptForegroundColor.Alpha(flashAlpha) : Color.Lerp(Color.Red, Color.Yellow, airlock.lastPressure * 2).Alpha(flashAlpha),
								Alignment = TextAlignment.CENTER
							});

							surface.Canvas.Add(new MySprite() { // Percentage bar
								Type = SpriteType.TEXTURE,
								Data = "SquareSimple",
								Position = new Vector2(surface.Bounds.Size.X / 2, surface.Bounds.Size.Y * 0.65f) + surface.Bounds.Position,
								Size = new Vector2(surface.Bounds.Size.X * 0.8f * airlock.lastPressure, surface.Bounds.Size.Y * 0.2f),
								Color = Color.Lerp(Color.Red, Color.Green, airlock.lastPressure),
								Alignment = TextAlignment.CENTER
							});

							surface.Canvas.Add(new MySprite() { // Percentage text
								Type = SpriteType.TEXT,
								Data = Math.Round(airlock.lastPressure * 100).ToString() + "%",
								Position = new Vector2(surface.Bounds.Size.X / 2, surface.Bounds.Size.Y * 0.55f) + surface.Bounds.Position,
								RotationOrScale = surface.Scale,
								Color = target.ScriptForegroundColor,
								Alignment = TextAlignment.CENTER,
								FontId = "White"
							});

							surface.Canvas.Dispose(); // We're done here. Tell the game to draw the panel.
						}
					}
				}
			}
		}
	}
}
