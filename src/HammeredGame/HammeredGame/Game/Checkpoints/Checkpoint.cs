using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HammeredGame.Game.Checkpoints
{
    public struct PlayerState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool InputEnabled;
    }

    public struct HammerState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Dropped;
    }

    public struct RockState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;
        public bool Visible;
    }

    public struct TreeState
    {
        public Model TreeModel;
        public Texture2D TreeTexture;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;
        public bool Visible;
        public bool Fallen;
    }

    public struct KeyState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Visible;
        public bool Collected;
    }

    public record Checkpoint
    {
        public string Name;
        public List<string> PreviousCheckpointNames = new();
        public PlayerState PlayerState;
        public HammerState HammerState;
        public Dictionary<string, RockState> RockStates = new();
        public Dictionary<string, TreeState> TreeStates = new();
        public Dictionary<string, KeyState> KeyStates = new();
    }
}
