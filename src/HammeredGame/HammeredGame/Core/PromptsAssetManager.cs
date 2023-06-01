using HammeredGame.Game;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Assets;
using Myra.Graphics2D.TextureAtlases;
using Myra.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static HammeredGame.Game.UserAction;

namespace HammeredGame.Core
{
    /// <summary>
    /// The Prompts asset manager maintains the image atlases for input prompts, and automatically
    /// loads in the one appropriate for the currently active input type.
    /// </summary>
    public class PromptsAssetManager
    {
        private readonly Dictionary<InputType, TextureRegionAtlas> controlsAtlas = new();
        private readonly Input input;

        private AssetManager myraAssetManager = new AssetManager(new FileAssetResolver(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));

        public PromptsAssetManager(Input input)
        {
            this.input = input;
        }

        /// <summary>
        /// Should be called on game LoadContent() to load the default keyboard &amp; mouse input
        /// prompt assets.
        /// </summary>
        public void LoadContent()
        {
            LoadAtlas(InputType.KeyboardMouse);
        }

        /// <summary>
        /// Load the input prompt atlas for the desired input type. If the atlas is already loaded,
        /// this will do nothing. Otherwise, this method will query the file system and could be expensive.
        /// </summary>
        /// <param name="type"></param>
        public void LoadAtlas(InputType type)
        {
            if (controlsAtlas.ContainsKey(type))
            {
                return;
            }

            // Myra uses its own asset manager. The default one uses a File stream based
            // implementation that reads from the directory of the currently executing assembly.
            controlsAtlas[type] = myraAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/" + type.ToString() + ".xmat");
        }

        /// <summary>
        /// Should be called on every game Update(), and will try to detect the currently active
        /// input type and load the atlas asynchronously if it has not been loaded.
        /// </summary>
        public void Update()
        {
            // Load texture atlas when input type changed. This is IO heavy so do it asynchronously,
            // and only once for any input type.
            if (!controlsAtlas.ContainsKey(input.CurrentlyActiveInput))
            {
                new Task(() =>
                {
                    InputType activeType = input.CurrentlyActiveInput;
                    controlsAtlas[activeType] = myraAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/" + activeType + ".xmat");
                }).Start();
            }
        }

        /// <summary>
        /// Create an image (that you can set in any Myra UI's Image.Renderable property) for the
        /// controls associated with the specified UserAction, accounting for the currently active
        /// input type.
        /// <para/>
        /// This method only performs a lookup into the internal atlas store, and does not cause an
        /// expensive IO load if the atlas is not found for the active input type. Instead, it will
        /// fall back to returning the keyboard &amp; mouse version.
        /// </summary>
        /// <param name="action">The action to return the image for</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Action was null</exception>
        /// <exception cref="NotSupportedException"></exception>
        public List<TextureRegion> GetImagesForAction(UserAction action)
        {
            if (action is ContinuousUserAction continuousAction)
            {
                var side = continuousAction.GamepadContinuousStickSide;
                var (up, left, down, right) = continuousAction.KeyboardContinuousKeys;

                // Fall back to keyboard and mouse (which we are guaranteed to have loaded in
                // LoadContent()) if the active input atlas hasn't been loaded yet
                InputType inputType = input.CurrentlyActiveInput;
                if (!controlsAtlas.ContainsKey(inputType))
                {
                    inputType = InputType.KeyboardMouse;
                }

                switch (inputType)
                {
                    case InputType.Xbox:
                        // for controller, show either XboxSeriesX_Left_Stick or XboxSeriesX_Right_Stick
                        return new List<TextureRegion>() { controlsAtlas[InputType.Xbox][side] };

                    case InputType.PlayStation:
                        return new();

                    case InputType.Switch:
                        return new();

                    case InputType.KeyboardMouse:
                        // Ignore any keys that we don't have an atlas entry for
                        List<TextureRegion> regions = new();
                        foreach (Keys key in new[] { up, left, down, right}) {
                            if (controlsAtlas[InputType.KeyboardMouse].Regions.ContainsKey(key.ToString()))
                            {
                                regions.Add(controlsAtlas[InputType.KeyboardMouse][key.ToString()]);
                            }
                        }
                        return regions;

                    default:
                        throw new NotSupportedException();
                }
            }
            else if (action is DiscreteUserAction discreteAction)
            {
                var button = discreteAction.GamepadButton;
                var key = discreteAction.KeyboardKey;

                // Fall back to keyboard and mouse (which we are guaranteed to have loaded in
                // LoadContent()) if the active input atlas hasn't been loaded yet
                InputType inputType = input.CurrentlyActiveInput;
                if (!controlsAtlas.ContainsKey(inputType))
                {
                    inputType = InputType.KeyboardMouse;
                }

                switch (inputType)
                {
                    case InputType.Xbox:
                        return new List<TextureRegion>() { controlsAtlas[InputType.Xbox][button.ToString()] };

                    case InputType.PlayStation:
                        return new();

                    case InputType.Switch:
                        return new();

                    case InputType.KeyboardMouse:
                        return new List<TextureRegion>() { controlsAtlas[InputType.KeyboardMouse][key.ToString()] };

                    default:
                        throw new NotSupportedException();
                }
            }

            return new();
        }
    }
}
