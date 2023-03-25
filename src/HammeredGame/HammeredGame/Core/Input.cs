using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.CompilerServices;

namespace HammeredGame.Core
{
    /// <summary>
    /// Adapted from AlienScribble Making 3D Games with MonoGame playlist:
    /// https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
    /// <para />
    /// This file contains basic primitives for input handling.
    /// </summary>
    public class Input
    {
        // Deadzone = amount of movement of the controller stick before it will be recognized as
        // valid movement
        public const float DEADZONE = 0.12f;

        public const ButtonState ButtonUp = ButtonState.Released;
        public const ButtonState ButtonDown = ButtonState.Pressed;

        // Keyboard states. We maintain one previous keyboard state to detect the moment of key press.
        public KeyboardState KeyboardState;
        private KeyboardState oldkb;

        // Booleans for held modifier keys.
        public bool SHIFT_DOWN, CTRL_DOWN, ALT_DOWN;

        // Booleans for modifier keys that got pressed down in this tick. (Was not pressed previously).
        // We keep one previous state here as well to detect the moment of press.
        public bool SHIFT_PRESS, CTRL_PRESS, ALT_PRESS;

        private bool old_shift_down, old_ctrl_down, old_alt_down;

        // Mouse states. We maintain one previous mouse state to detect the moment of click.
        public MouseState MouseState;
        private MouseState oldms;

        // Booleans for held mouse clicks.
        public bool LEFT_DOWN, MIDDLE_DOWN, RIGHT_DOWN;

        // Booleans for mouse clicks that got pressed down in this tick. (Was not pressed previously).
        public bool LEFT_CLICK, MIDDLE_CLICK, RIGHT_CLICK;

        // Mouse positions. MouseV and MouseP are convenience wrappers for MOUSE_X and MOUSE_Y.
        public int MOUSE_X, MOUSE_Y;

        public Vector2 MouseV;
        public Point MouseP;

        // Gamepad states and booleans
        public GamePadState GamePadState;
        private GamePadState oldgp;

        // Booleans for held (DOWN) and pressed moment (PRESS)
        public bool A_DOWN, B_DOWN, X_DOWN, Y_DOWN, RB_DOWN, LB_DOWN, START_DOWN, BACK_DOWN, LEFTSTICK_DOWN, RIGHTSTICK_DOWN;

        public bool A_PRESS, B_PRESS, X_PRESS, Y_PRESS, RB_PRESS, LB_PRESS, START_PRESS, BACK_PRESS, LEFTSTICK_PRESS, RIGHSTICK_PRESS;

        // Screen space variables
        private readonly float screenScaleX;

        private readonly float screenScaleY;

        public Input(PresentationParameters pp, RenderTarget2D target)
        {
            // Set screen space variables according to the presentation parameters and render target
            screenScaleX = 1.0f / (pp.BackBufferWidth / (float)target.Width);
            screenScaleY = 1.0f / (pp.BackBufferHeight / (float)target.Height);
        }

        // <----- Quick Input functions for convenience ---->

        /// <summary>
        /// KeyPress - function to check if given key k is pressed (not held down)
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool KeyPress(Keys k)
        {
            return KeyboardState.IsKeyDown(k) && oldkb.IsKeyUp(k);
        }

        /// <summary>
        /// KeyDown - function to check if given key k is held down
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool KeyDown(Keys k)
        {
            return KeyboardState.IsKeyDown(k);
        }

        /// <summary>
        /// ButtonPress - function to check if given gamepad button is pressed
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ButtonPress(Buttons button)
        {
            return GamePadState.IsButtonDown(button) && oldgp.IsButtonUp(button);
        }

        /// <summary>
        /// ButtonPress - function to check if given gamepad button is held down
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ButtonHeld(Buttons button)
        { if (GamePadState.IsButtonDown(button)) return true; return false; }

        /// <summary>
        /// Update the internal variables for keyboard, gamepad, and mouse current states.
        /// Then update the numerous public booleans for the new state of input.
        /// </summary>
        public void Update()
        {
            old_alt_down = ALT_DOWN; old_shift_down = SHIFT_DOWN; old_ctrl_down = CTRL_DOWN;
            oldkb = KeyboardState; oldms = MouseState; oldgp = GamePadState;

            // Get the different states
            KeyboardState = Keyboard.GetState(); MouseState = Mouse.GetState(); GamePadState = GamePad.GetState(0);

            // Set Keyboard boolean values according to input
            SHIFT_DOWN = SHIFT_PRESS = CTRL_DOWN = CTRL_PRESS = ALT_DOWN = ALT_PRESS = false;
            if (KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift)) SHIFT_DOWN = true;
            if (KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl)) CTRL_DOWN = true;
            if (KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt)) ALT_DOWN = true;
            if (SHIFT_DOWN && !old_shift_down) SHIFT_PRESS = true;
            if (CTRL_DOWN && !old_ctrl_down) CTRL_PRESS = true;
            if (ALT_DOWN && !old_alt_down) ALT_PRESS = true;

            // Set Mouse boolean values according to input
            MouseV = new Vector2(MouseState.Position.X * screenScaleX, MouseState.Position.Y * screenScaleY);
            MOUSE_X = (int)MouseV.X; MOUSE_Y = (int)MouseV.Y; MouseP = new Point(MOUSE_X, MOUSE_Y);
            LEFT_CLICK = MIDDLE_CLICK = RIGHT_CLICK = LEFT_DOWN = MIDDLE_DOWN = RIGHT_DOWN = false;
            if (MouseState.LeftButton == ButtonDown) LEFT_DOWN = true;
            if (MouseState.MiddleButton == ButtonDown) MIDDLE_DOWN = true;
            if (MouseState.RightButton == ButtonDown) RIGHT_DOWN = true;
            if (LEFT_DOWN && oldms.LeftButton == ButtonUp) LEFT_CLICK = true;
            if (MIDDLE_DOWN && oldms.MiddleButton == ButtonUp) MIDDLE_CLICK = true;
            if (RIGHT_DOWN && oldms.RightButton == ButtonUp) RIGHT_CLICK = true;

            // Set GamePad boolean values according to input
            A_DOWN = B_DOWN = X_DOWN = Y_DOWN = RB_DOWN = LB_DOWN = START_DOWN = BACK_DOWN = LEFTSTICK_DOWN = RIGHTSTICK_DOWN = false;
            A_PRESS = B_PRESS = X_PRESS = Y_PRESS = RB_PRESS = LB_PRESS = START_PRESS = BACK_PRESS = LEFTSTICK_PRESS = RIGHSTICK_PRESS = false;
            if (GamePadState.Buttons.A == ButtonState.Pressed) { A_DOWN = true; if (GamePadState.Buttons.A == ButtonState.Released) A_PRESS = true; }
            if (GamePadState.Buttons.B == ButtonState.Pressed) { B_DOWN = true; if (GamePadState.Buttons.B == ButtonState.Released) B_PRESS = true; }
            if (GamePadState.Buttons.X == ButtonState.Pressed) { X_DOWN = true; if (GamePadState.Buttons.X == ButtonState.Released) X_PRESS = true; }
            if (GamePadState.Buttons.Y == ButtonState.Pressed) { Y_DOWN = true; if (GamePadState.Buttons.Y == ButtonState.Released) Y_PRESS = true; }
            if (GamePadState.Buttons.RightShoulder == ButtonState.Pressed) { RB_DOWN = true; if (GamePadState.Buttons.RightShoulder == ButtonState.Released) RB_PRESS = true; }
            if (GamePadState.Buttons.LeftShoulder == ButtonState.Pressed) { LB_DOWN = true; if (GamePadState.Buttons.LeftShoulder == ButtonState.Released) LB_PRESS = true; }
            if (GamePadState.Buttons.Back == ButtonState.Pressed) { BACK_DOWN = true; if (GamePadState.Buttons.Back == ButtonState.Released) BACK_PRESS = true; }
            if (GamePadState.Buttons.Start == ButtonState.Pressed) { START_DOWN = true; if (GamePadState.Buttons.Start == ButtonState.Released) START_PRESS = true; }
            if (GamePadState.Buttons.LeftStick == ButtonState.Pressed) { LEFTSTICK_DOWN = true; if (GamePadState.Buttons.LeftStick == ButtonState.Released) LEFTSTICK_PRESS = true; }
            if (GamePadState.Buttons.RightStick == ButtonState.Pressed) { RIGHTSTICK_DOWN = true; if (GamePadState.Buttons.RightStick == ButtonState.Released) RIGHSTICK_PRESS = true; }
        }
    }
}
