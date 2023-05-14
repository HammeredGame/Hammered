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
        public record ContinuousUserAction(string Name, string GamepadContinuousStickSide, (Keys up, Keys left, Keys down, Keys right) KeyboardContinuousKeys) : UserAction(Name)
        {
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

            /// <summary>
            /// Returns true on the moment an upward input has been initiated.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool FlickedUp(Input input)
            {
                return input.KeyPress(KeyboardContinuousKeys.Item1) ||
                    input.ButtonPress(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickUp : Buttons.RightThumbstickUp);
            }

            /// <summary>
            /// Returns true on the moment a leftward input has been initiated.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool FlickedLeft(Input input)
            {
                return input.KeyPress(KeyboardContinuousKeys.Item2) ||
                    input.ButtonPress(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickLeft : Buttons.RightThumbstickLeft);
            }

            /// <summary>
            /// Returns true on the moment a downward input has been initiated.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool FlickedDown(Input input)
            {
                return input.KeyPress(KeyboardContinuousKeys.Item3) ||
                    input.ButtonPress(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickDown : Buttons.RightThumbstickDown);
            }

            /// <summary>
            /// Returns true on the moment a rightward input has been initiated.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool FlickedRight(Input input)
            {
                return input.KeyPress(KeyboardContinuousKeys.Item4) ||
                    input.ButtonPress(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickRight : Buttons.RightThumbstickRight);
            }

            /// <summary>
            /// Returns true as long as an upward input is given.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool HeldUp(Input input)
            {
                return input.KeyDown(KeyboardContinuousKeys.Item1) ||
                    input.ButtonHeld(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickUp : Buttons.RightThumbstickUp);
            }

            /// <summary>
            /// Returns true as long as a leftward input is given.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool HeldLeft(Input input)
            {
                return input.KeyDown(KeyboardContinuousKeys.Item2) ||
                    input.ButtonHeld(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickLeft : Buttons.RightThumbstickLeft);
            }

            /// <summary>
            /// Returns true as long as a downward input is given.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool HeldDown(Input input)
            {
                return input.KeyDown(KeyboardContinuousKeys.Item3) ||
                    input.ButtonHeld(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickDown : Buttons.RightThumbstickDown);
            }

            /// <summary>
            /// Returns true as long as a rightward input is given.
            /// </summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public bool HeldRight(Input input)
            {
                return input.KeyDown(KeyboardContinuousKeys.Item4) ||
                    input.ButtonHeld(GamepadContinuousStickSide == "Left" ? Buttons.LeftThumbstickRight : Buttons.RightThumbstickRight);
            }
        }

        /// <summary>
        /// Represents a binary-state action such as controller button or keyboard key.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="GamepadButton"></param>
        /// <param name="KeyboardKey"></param>
        public record DiscreteUserAction(string Name, Buttons GamepadButton, Keys KeyboardKey) : UserAction(Name)
        {
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
        public static readonly ContinuousUserAction CameraMovement = new("Move Camera", "Right", (Keys.F20, Keys.Q, Keys.F20, Keys.E));
        public static readonly DiscreteUserAction Interact = new("Interact", Buttons.A, Keys.F);
        public static readonly DiscreteUserAction SummonHammer = new("Summon Hammer", Buttons.X, Keys.Space);
        public static readonly DiscreteUserAction DropHammer = new("Drop Hammer", Buttons.X, Keys.Space);
        public static readonly DiscreteUserAction Pause = new("Pause", Buttons.Start, Keys.Escape);
        public static readonly DiscreteUserAction Back = new("Back", Buttons.B, Keys.Escape);
        public static readonly DiscreteUserAction Confirm = new("Confirm", Buttons.X, Keys.Space);
        public static readonly DiscreteUserAction Dash = new("Dash", Buttons.LeftTrigger, Keys.LeftShift);
        public static readonly DiscreteUserAction RotateCameraLeft = new("Rotate Camera Left", Buttons.LeftShoulder, Keys.Q);
        public static readonly DiscreteUserAction RotateCameraRight = new("Rotate Camera Right", Buttons.RightShoulder, Keys.E);
        public static readonly DiscreteUserAction MenuItemUp = new("Move Menu Selection Up", Buttons.DPadUp, Keys.Up);
        public static readonly DiscreteUserAction MenuItemDown = new("Move Menu Selection Down", Buttons.DPadDown, Keys.Down);
        public static readonly DiscreteUserAction MenuItemLeft = new("Move Menu Slider Left", Buttons.DPadLeft, Keys.Left);
        public static readonly DiscreteUserAction MenuItemRight = new("Move Menu Slider Right", Buttons.DPadRight, Keys.Right);
    }
}
