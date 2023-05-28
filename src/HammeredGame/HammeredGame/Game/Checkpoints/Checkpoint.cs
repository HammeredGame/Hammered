using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HammeredGame.Game.Checkpoints
{
    public struct PlayerState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool InputEnabled;
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
        public PlayerState PlayerState;
        public Dictionary<string, RockState> RockStates = new();
        public Dictionary<string, TreeState> TreeStates = new();
        public Dictionary<string, KeyState> KeyStates = new();
    }
}
