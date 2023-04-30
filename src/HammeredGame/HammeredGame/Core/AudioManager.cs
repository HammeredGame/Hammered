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
            "Audio/stereo_step",
            "Audio/hammer_drop",
            "Audio/lohi_whoosh",
            "Audio/tree_fall",
            "Audio/ding",
            "Audio/door_close",
            "Audio/door_open",
            "Audio/new_laser",
            "Audio/rock_water",
            "Audio/short_roll",
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
            foreach (string soundName in soundNames)
            {
                sfx.Add(soundName, Game.Content.Load<SoundEffect>(soundName));
            }
            SoundEffect.DistanceScale = 2000;
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

            active.Instance.Volume = volume; 
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

        public class ActiveSound
        {
            public SoundEffectInstance Instance;
            public AudioEmitter Emitter; 
        }
    }
}
