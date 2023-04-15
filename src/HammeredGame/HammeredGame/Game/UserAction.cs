using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace HammeredGame.Game
{

    public struct ContinuousUserAction
    {
        public string Name;
        public string GamepadContinuousStickSide;
        public (Keys, Keys, Keys, Keys) KeyboardContinuousKeys;

        public ContinuousUserAction(string name, string stickSide, Keys up, Keys left, Keys down, Keys right)
        {
            Name = name;
            GamepadContinuousStickSide = stickSide;
            KeyboardContinuousKeys = (up, left, down, right);
        }

        public static readonly ContinuousUserAction Movement = new("Move", "Left", Keys.W, Keys.A, Keys.S, Keys.D);

        public static Vector2 GetValue(Input input, ContinuousUserAction action)
        {
            // For keyboard input, simulate a gamepad input but with discrete values in XY scale
            Vector2 result = Vector2.Zero;
            if (input.KeyDown(action.KeyboardContinuousKeys.Item1))
            {
                result.Y += 1;
            }
            if (input.KeyDown(action.KeyboardContinuousKeys.Item2))
            {
                result.X -= 1;
            }
            if (input.KeyDown(action.KeyboardContinuousKeys.Item3))
            {
                result.Y -= 1;
            }
            if (input.KeyDown(action.KeyboardContinuousKeys.Item4))
            {
                result.X += 1;
            }

            // Add gamepad input on top, instead of as a separate branch - we don't know what
            // the condition is for switching between the two input types. Maybe users want to
            // use the keyboard even when the controller is connected, so it's simpler to handle
            // both, and clamp the result.
            result += (Vector2)typeof(GamePadThumbSticks).GetProperty(action.GamepadContinuousStickSide).GetValue(input.GamePadState.ThumbSticks, null);
            result.X = Math.Clamp(result.X, -1f, 1f);
            result.Y = Math.Clamp(result.Y, -1f, 1f);
            return result;
        }

    }
    public struct UserAction
    {
        public string Name;
        public Buttons GamepadButton;
        public Keys KeyboardKey;

        public UserAction(string name, Buttons button, Keys keyboardKey)
        {
            Name = name;
            GamepadButton = button;
            KeyboardKey = keyboardKey;
        }

        public static readonly UserAction Interact = new("Interact", Buttons.A, Keys.Z);
        public static readonly UserAction SummonHammer = new("Summon Hammer", Buttons.X, Keys.Space);
        public static readonly UserAction DropHammer = new("Drop Hammer", Buttons.X, Keys.Space);
        public static readonly UserAction Pause = new("Pause", Buttons.Start, Keys.Escape);
        public static readonly UserAction Back = new("Back", Buttons.B, Keys.Escape);
        public static readonly UserAction Confirm = new("Confirm", Buttons.A, Keys.Enter);
        public static readonly UserAction Dash = new("Dash", Buttons.LeftTrigger, Keys.LeftShift);
        public static readonly UserAction RotateCameraLeft = new("Rotate Camera Left", Buttons.LeftShoulder, Keys.Q);
        public static readonly UserAction RotateCameraRight = new("Rotate Camera Right", Buttons.RightShoulder, Keys.E);

        public static bool Pressed(Input input, UserAction action)
        {
            if (input.KeyPress(action.KeyboardKey) || input.ButtonPress(action.GamepadButton))
            {
                return true;
            }
            return false;
        }

        public static bool Held(Input input, UserAction action)
        {
            if (input.KeyDown(action.KeyboardKey) || input.ButtonHeld(action.GamepadButton))
            {
                return true;
            }
            return false;
        }
    }
}
