using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace HammeredGame.Game
{

    /// <summary>
    /// Represents possible user actions in the game. All actions have a name.
    /// </summary>
    /// <param name="Name"></param>
    public abstract record UserAction(string Name)
    {

        /// <summary>
        /// Represents actions with a continuous XY value, such as 4-key movements on the keyboard
        /// or joysticks.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="GamepadContinuousStickSide"></param>
        /// <param name="KeyboardContinuousKeys"></param>
        public record ContinuousUserAction(string Name, string GamepadContinuousStickSide, (Keys up, Keys left, Keys down, Keys right) KeyboardContinuousKeys) : UserAction(Name) {
            public Vector2 GetValue(Input input)
            {
                // For keyboard input, simulate a gamepad input but with discrete values in XY scale
                Vector2 result = Vector2.Zero;
                if (input.KeyDown(KeyboardContinuousKeys.Item1))
                {
                    result.Y += 1;
                }
                if (input.KeyDown(KeyboardContinuousKeys.Item2))
                {
                    result.X -= 1;
                }
                if (input.KeyDown(KeyboardContinuousKeys.Item3))
                {
                    result.Y -= 1;
                }
                if (input.KeyDown(KeyboardContinuousKeys.Item4))
                {
                    result.X += 1;
                }

                // Add gamepad input on top, instead of as a separate branch - we don't know what
                // the condition is for switching between the two input types. Maybe users want to
                // use the keyboard even when the controller is connected, so it's simpler to handle
                // both, and clamp the result.
                result += (Vector2)typeof(GamePadThumbSticks).GetProperty(GamepadContinuousStickSide).GetValue(input.GamePadState.ThumbSticks, null);
                result.X = Math.Clamp(result.X, -1f, 1f);
                result.Y = Math.Clamp(result.Y, -1f, 1f);
                return result;
            }
        }

        /// <summary>
        /// Represents a binary-state action such as controller button or keyboard key.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="GamepadButton"></param>
        /// <param name="KeyboardKey"></param>
        public record DiscreteUserAction(string Name, Buttons GamepadButton, Keys KeyboardKey) : UserAction(Name) {

            /// <summary>
            /// Returns true on the moment of press, and false afterwards.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool Pressed(Input input)
            {
                return input.KeyPress(KeyboardKey) || input.ButtonPress(GamepadButton);
            }

            /// <summary>
            /// Returns true all the while the button is held down.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool Held(Input input)
            {
                return input.KeyDown(KeyboardKey) || input.ButtonHeld(GamepadButton);
            }
        }

        // Default keys. TODO: allow changing these from config file (and remove the readonly modifier)
        public static readonly ContinuousUserAction Movement = new("Move", "Left", (Keys.W, Keys.A, Keys.S, Keys.D));
        public static readonly DiscreteUserAction Interact = new("Interact", Buttons.A, Keys.Z);
        public static readonly DiscreteUserAction SummonHammer = new("Summon Hammer", Buttons.X, Keys.Space);
        public static readonly DiscreteUserAction DropHammer = new("Drop Hammer", Buttons.X, Keys.Space);
        public static readonly DiscreteUserAction Pause = new("Pause", Buttons.Start, Keys.Escape);
        public static readonly DiscreteUserAction Back = new("Back", Buttons.B, Keys.Escape);
        public static readonly DiscreteUserAction Confirm = new("Confirm", Buttons.A, Keys.Enter);
        public static readonly DiscreteUserAction Dash = new("Dash", Buttons.LeftTrigger, Keys.LeftShift);
        public static readonly DiscreteUserAction RotateCameraLeft = new("Rotate Camera Left", Buttons.LeftShoulder, Keys.Q);
        public static readonly DiscreteUserAction RotateCameraRight = new("Rotate Camera Right", Buttons.RightShoulder, Keys.E);
        public static readonly DiscreteUserAction MenuItemUp = new("Move Menu Selection Up", Buttons.LeftThumbstickUp, Keys.W);
        public static readonly DiscreteUserAction MenuItemDown = new("Move Menu Selection Up", Buttons.LeftThumbstickDown, Keys.S);
    }
}
