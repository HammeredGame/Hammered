using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace HammeredGame.Core
{
    public class AudioManager : Microsoft.Xna.Framework.GameComponent
    {
        #region Fields

        readonly string[] soundNames =
        {
            //combination of self-recored foley and sounds from zapsplat.com
            //bgmusic chord progressions and inspirationg from Avery Berman royalty free music 
            "Audio/stereo_step",
            "Audio/hammer_drop",
            "Audio/lohi_whoosh",
            "Audio/tree_fall",
            "Audio/ding",
            "Audio/door_close1",
            "Audio/door_open1",
            //"Audio/new_laser",
            "Audio/rock_water",
            "Audio/short_roll",
            // no idea where I should be noting this down but credits & licenses:
            // selection_change: CC0
            // Kenney UI Audio pack (https://kenney.nl/assets/ui-audio)
            "Audio/UI/selection_change",
            // selection_confirm: CC BY 4.0 (attribution required, modification indication required)
            // Universal UI/Menu Soundpack (https://ellr.itch.io/universal-ui-soundpack)
            "Audio/UI/selection_confirm",
            "Audio/buzz",
            "Audio/glow",
            "Audio/blip",
            "Audio/retro",
            "Audio/balanced/buzz_b",
            "Audio/balanced/hammer_drop_b",
            "Audio/balanced/new_launch_b",
            "Audio/balanced/tree_crash ",
            "Audio/balanced/fast_whoosh_b",
            "Audio/balanced/catch_b",
            "Audio/balanced/pied_bm",
            "Audio/balanced/tree_crash_bm",
            "Audio/balanced/hammer_fly_m",
            "Audio/balanced/loud_fly",
            "Audio/balanced/quiet_foot",
            "Audio/balanced/soft_step",
            "Audio/balanced/door_close",
            "Audio/balanced/door_open"
        };

        //public AudioListener Listener
        //{
        //    get { return Listener; }
        //    set { Listener = value; }
        //}

        //Listener and Emitter for 3D positioning
        public AudioListener listener = new AudioListener();
        readonly AudioEmitter emitter = new AudioEmitter();

        //store loaded soundeffects
        private Dictionary<string, SoundEffect> sfx = new Dictionary<string, SoundEffect>();

        //keep track of those that are active
        public List<ActiveSound> ActiveSounds = new List<ActiveSound>();

        #endregion

        public AudioManager(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            SoundEffect.DistanceScale = 50;
            SoundEffect.DopplerScale = 0.1f;
            foreach (string soundName in soundNames)
            {
                sfx.Add(soundName, Game.Content.Load<SoundEffect>(soundName));
            }

        }

        //load all of the sound effects
        public override void Initialize()
        {
            //SoundEffect.DistanceScale = 2000;
            //SoundEffect.DopplerScale = 0.1f;



            base.Initialize();
        }

        //unload soundeffects
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    foreach (SoundEffect soundEffect in sfx.Values)
                    {
                        soundEffect.Dispose();
                    }

                    sfx.Clear();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        //cleanup sounds that are no longer active, apply3D to those who are
        public override void Update(GameTime gameTime)
        {
            int i = 0;
            while (i < ActiveSounds.Count)
            {
                ActiveSound active = ActiveSounds[i];

                if (active.Instance.State == SoundState.Stopped)
                {
                    active.Instance.Dispose();
                    ActiveSounds.RemoveAt(i);
                }
                else
                {
                    Apply3D(active);
                    i++;
                }
            }
            base.Update(gameTime);
        }
        //should have an IAudioEmitter in params, need to assign Hammer to emitting instance
        public SoundEffectInstance Play3DSound(string soundName, bool isLooped, AudioEmitter obj_emitter, float volume)
        {
            ActiveSound active = new ActiveSound();

            active.Instance = sfx[soundName].CreateInstance();
            active.Instance.IsLooped = isLooped;

            active.Emitter = obj_emitter;

            Apply3D(active);

            active.Instance.Volume = Math.Clamp(volume, 0.0f, 1.0f);
            active.Instance.Play();

            ActiveSounds.Add(active);

            return active.Instance;

        }

        private void Apply3D(ActiveSound activeSound)
        {
            emitter.Position = activeSound.Emitter.Position;
            //emitter.Forward = activeSound.Emitter.Forward;
            //emitter.Up = activeSound.Emitter.Up;
            //emitter.Velocity = activeSound.Emitter.Velocity;

            activeSound.Instance.Apply3D(listener, emitter);
        }

        /// <summary>
        /// Pause all active sound effects.
        /// </summary>
        public void PauseAll() {
            ActiveSounds.ForEach(s => s.Instance.Pause());
        }

        /// <summary>
        /// Resume all active paused sound effects.
        /// </summary>
        public void ResumeAll()
        {
            ActiveSounds.ForEach(s => s.Instance.Resume());
        }

        /// <summary>
        /// Stop all active sound effects. This will cause the next call to <see
        /// cref="Update(GameTime)"/> to remove those sound effects.
        /// </summary>
        public void StopAll()
        {
            ActiveSounds.ForEach(s => s.Instance.Stop());
        }

        public class ActiveSound
        {
            public SoundEffectInstance Instance;
            public AudioEmitter Emitter;
        }
    }
}
