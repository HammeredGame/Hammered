using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using System;

namespace HammeredGame.Game
{
    internal class PauseScreen : Screen
    {
        private Desktop desktop;

        private TimeSpan pauseTime;

        public PauseScreen(TimeSpan totalGameTime) {
            IsPartial = true;
            pauseTime = totalGameTime;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var helloWorld = new Label
            {
                Id = "label",
                Text = "Hello, World!"
            };
            grid.Widgets.Add(helloWorld);

            // ComboBox
            var combo = new ComboBox
            {
                GridColumn = 1,
                GridRow = 0
            };

            combo.Items.Add(new ListItem("Red", Color.Red));
            combo.Items.Add(new ListItem("Green", Color.Green));
            combo.Items.Add(new ListItem("Blue", Color.Blue));
            grid.Widgets.Add(combo);

            // Button
            var button = new TextButton
            {
                GridColumn = 0,
                GridRow = 1,
                Text = "Show"
            };

            button.Click += (s, a) =>
            {
                var messageBox = Dialog.CreateMessageBox("Message", "Some message!");
                messageBox.ShowModal(desktop);
            };

            grid.Widgets.Add(button);

            // Spin button
            var spinButton = new SpinButton
            {
                GridColumn = 1,
                GridRow = 1,
                Width = 100,
                Nullable = true
            };
            grid.Widgets.Add(spinButton);

            // Add it to the desktop
            desktop = new Desktop();
            desktop.Root = grid;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            Input input = GameServices.GetService<Input>();
            if (!otherScreenHasFocus && (input.BACK_PRESS || input.KeyPress(Keys.Escape)))
            {
                ExitScreen();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            desktop.Render();
        }
    }
}
