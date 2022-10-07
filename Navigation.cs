using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using static Assistant.Globals;

namespace Assistant {

	// real <X,Y,Z> locations are aligned to a grid, and indexed by a NodeHandle
	public enum NodeHandle : uint {
		Invalid = 0,
	}

	public static partial class Globals {
	}

	public static class Navigation {
		// the Navigation class is responsible for building a mesh of places where we have been
		// the mesh itself is aligned on a grid, so that each potential node can be indexed directly

		// real world ranges seem to be:
		// X: -64k to 64k
		// Y: -64k to 64k
		// Z: -1k to 1k
		private static Vector3 WorldSize = new Vector3(64 * 1024, 64 * 1024, 1 * 1024);

		// the final output will be packed like this:
		// <  rx  ><  ry  >< rz ><    nx    ><    ny    ><    nz    >
		// X is reduced to nx and further reduced to rx
		// Y is reduced to ny and further reduced to ry
		// Z the same...

		private static float gridScale = 100f;
		// X: 128k / 100 needs 11 bits
		private const int worldXBits = 11;
		// Y: 128k / 100 needs 11 bits
		private const int worldYBits = 11;
		// Z: 2k / 100 needs 8 bits
		private const int worldZBits = 8;
		// 11 + 11 + 8 = 30
		private const int worldBits = worldXBits + worldYBits + worldZBits;

		public static NodeHandle GetHandle(Vector3 pos) {
			if ( pos == Vector3.Zero ) return NodeHandle.Invalid;

			Vector3 absPos = pos + WorldSize; // shift the whole thing to absolute value positions

			uint nx = (uint)Math.Round(absPos.X / gridScale); // nx goes from [0..128k] = 17 bits in the packed output
			uint ny = (uint)Math.Round(absPos.Y / gridScale); // ny goes from [0..128k] = 17 bits
			uint nz = (uint)Math.Round(absPos.Z / gridScale); // nz goes from [0..2k] = 11 bits

			uint n = (nx << (worldYBits + worldZBits))
				| (ny << worldZBits)
				| nz;
			return (NodeHandle)n;
		}
		public static Vector3 GetPosition(NodeHandle node) {
			return Vector3.Zero;
		}

		public static NodeHandle PlayerNode { get; private set; }
		private static int CurrentAreaCode;

		public static void Initialise() {
			Run("Navigation", (state) => {
				int areaCode = GetGame().Area.CurrentArea.Area.WorldAreaId;
				if( areaCode != 0 && areaCode != CurrentAreaCode ) {
					CurrentAreaCode = areaCode;
					// TODO: LoadArea();
				}
				PlayerNode = GetHandle(GetPlayer().Pos);
				// DrawTextAtPlayer($"Node: {PlayerNode}");
				return state;
			});
		}

	}
}
