using EmptyKeys.UserInterface.Generated.ContractsBlockView_Gamepad_Bindings;
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
		public class CanvasWrapper {
			public MySpriteDrawFrame Canvas;
			private IMyTextSurface surface;

			private RectangleF bounds;
			public RectangleF Bounds { get { return bounds; } }
			private float scale;
			public float Scale { get { return scale; } }

			public CanvasWrapper(IMyTextSurface targetSurface) {
				Canvas = targetSurface.DrawFrame();
				surface = targetSurface;

				bounds = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
				scale = Math.Min(surface.TextureSize.X / 256, surface.TextureSize.Y / 256);
			}
		}
	}
}
