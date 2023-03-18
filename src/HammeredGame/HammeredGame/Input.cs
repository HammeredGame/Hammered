using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    internal class Input
    {
        public const float DEADZONE = 0.12f;

        public const ButtonState ButtonUp = ButtonState.Released;
        public const ButtonState ButtonDown = ButtonState.Pressed;

        // Keyboard
        public KeyboardState kb, oldkb;
        public bool shift_down, ctrl_down, alt_down;
        public bool shift_press, ctrl_press, alt_press;
        public bool old_shift_down, old_ctrl_down, old_alt_down;

        // Mouse
        public MouseState ms, oldms;
        public bool leftClick, midClick, rightClick;
        public bool leftDown, midDown, rightDown;
        public int mouseX, mouseY;
        public Vector2 mouseV;
        public Point mouseP;

        // Gamepad
        public GamePadState gp, oldgp;
        public bool A_down, B_down, X_down, Y_down, RB_down, LB_down, start_down, back_down, leftStick_down, rightStick_down;
        public bool A_press, B_press, X_press, Y_press, RB_press, LB_press, start_press, back_press, leftStick_press, rightStick_press;

        float screenScaleX, screenScaleY;

        public Input(PresentationParameters pp, RenderTarget2D target)
        {
            screenScaleX = 1.0f / ((float)pp.BackBufferWidth / (float)target.Width);
            screenScaleY = 1.0f / ((float)pp.BackBufferHeight / (float)target.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool KeyPress(Keys k) { if (kb.IsKeyDown(k) && oldkb.IsKeyUp(k)) return true; return false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool KeyDown(Keys k) { if (kb.IsKeyDown(k)) return true; return false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ButtonPress(Buttons button) { if (gp.IsButtonDown(button) && oldgp.IsButtonUp(button)) return true; return false; }

        // Update
        public void Update()
        {
            old_alt_down = alt_down; old_shift_down = shift_down; old_ctrl_down = ctrl_down;
            oldkb = kb; oldms = ms; oldgp = gp;

            // Get the different states
            kb = Keyboard.GetState(); ms = Mouse.GetState(); gp = GamePad.GetState(0);

            // Set Keyboard values
            shift_down = shift_press = ctrl_down = ctrl_press = alt_down = alt_press = false;
            if (kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift)) shift_down = true;
            if (kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl)) ctrl_down = true;
            if (kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt)) alt_down = true;
            if ((shift_down) && (!old_shift_down)) shift_press = true;
            if ((ctrl_down) && (!old_ctrl_down)) ctrl_press = true;
            if ((alt_down) && (!old_alt_down)) alt_press = true;

            // Set Mouse values
            mouseV = new Vector2((float)ms.Position.X * screenScaleX, (float)ms.Position.Y * screenScaleY);
            mouseX = (int)mouseV.X; mouseY = (int)mouseV.Y; mouseP = new Point(mouseX, mouseY);
            leftClick = midClick = rightClick = leftDown = midDown = rightDown = false;
            if (ms.LeftButton == ButtonDown) leftDown = true;
            if (ms.MiddleButton == ButtonDown) midDown = true;
            if (ms.RightButton == ButtonDown) rightDown = true;
            if ((leftDown) && (oldms.LeftButton == ButtonUp)) leftClick = true;
            if ((midDown) && (oldms.MiddleButton == ButtonUp)) midClick = true;
            if ((rightDown) && (oldms.RightButton == ButtonUp)) rightClick = true;

            // Set GamePad values
            A_down = B_down = X_down = Y_down = RB_down = LB_down = start_down = back_down = leftStick_down = rightStick_down = false;
            A_press = B_press = X_press = Y_press = RB_press = LB_press = start_press = back_press = leftStick_press = rightStick_press = false;
            if (gp.Buttons.A == ButtonState.Pressed) { A_down = true; if (gp.Buttons.A == ButtonState.Released) A_press = true; }
            if (gp.Buttons.B == ButtonState.Pressed) { B_down = true; if (gp.Buttons.B == ButtonState.Released) B_press = true; }
            if (gp.Buttons.X == ButtonState.Pressed) { X_down = true; if (gp.Buttons.X == ButtonState.Released) X_press = true; }
            if (gp.Buttons.Y == ButtonState.Pressed) { Y_down = true; if (gp.Buttons.Y == ButtonState.Released) Y_press = true; }
            if (gp.Buttons.RightShoulder == ButtonState.Pressed) { RB_down = true; if (gp.Buttons.RightShoulder == ButtonState.Released) RB_press = true; }
            if (gp.Buttons.LeftShoulder == ButtonState.Pressed) { LB_down = true; if (gp.Buttons.LeftShoulder == ButtonState.Released) LB_press = true; }
            if (gp.Buttons.Back == ButtonState.Pressed) { back_down = true; if (gp.Buttons.Back == ButtonState.Released) back_press = true; }
            if (gp.Buttons.Start == ButtonState.Pressed) { start_down = true; if (gp.Buttons.Start == ButtonState.Released) start_press = true; }
            if (gp.Buttons.LeftStick == ButtonState.Pressed) { leftStick_down = true; if (gp.Buttons.LeftStick == ButtonState.Released) leftStick_press = true; }
            if (gp.Buttons.RightStick == ButtonState.Pressed) { rightStick_down = true; if (gp.Buttons.RightStick == ButtonState.Released) rightStick_press = true; }
        }
    }
}
