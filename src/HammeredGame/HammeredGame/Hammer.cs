using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    class Hammer : GameObject
    {
        public Model model;

        public Vector3 _hammerPosition;
        public Quaternion _hammerRotation;
        public float scale;
        public Texture2D tex;

        private float hammerSpeed = 0.1f;
        private Vector3 hammer_vel;
        private Vector3 _lightDirection = new Vector3(3, -2, 5);

        private bool hammerDropped = false;
        private bool hammerEnroute = false;
        private Vector3 dropPos = Vector3.Zero;
        private Vector3 targetPos = Vector3.Zero;

        Input inp;
        Camera activeCamera;
        Player _player;

        public Hammer(Model model, Vector3 pos, float scale, Player p, Input inp, Camera cam, Texture2D t)
        {
            this.model = model;
            this._hammerPosition = pos;
            this.scale = scale;
            this._hammerRotation = Quaternion.Identity;

            _lightDirection.Normalize();
            this.inp = inp;
            this.activeCamera = cam;
            this.tex = t;
            this._player = p;
        }

        public override void Update(GameTime gameTime)
        {
            //Vector3 oldPos = _hammerPosition;

            if (!hammerDropped && !hammerEnroute)
            {
                this._hammerPosition = this._player.GetPosition();
            }

            // Keyboard input
            if (!hammerDropped && inp.KeyDown(Keys.E))
            {
                this.hammerDropped = true;
                this.dropPos = this._hammerPosition;
            }

            if (hammerDropped && inp.KeyDown(Keys.Q))
            {
                //this.targetPos = this._player.GetPosition();
                this.hammerEnroute = true;
            }

            //GamePad Control
            if (inp.gp.IsConnected)
            {
                if (inp.ButtonPress(Buttons.A))
                {
                    this.hammerDropped = true;
                }
                if (inp.ButtonPress(Buttons.B))
                {
                    //this.targetPos = this._player.GetPosition();
                    this.hammerEnroute = true;
                }
            }

            if (this.hammerEnroute && this.hammerDropped)
            {
                this._hammerPosition += this.hammerSpeed * (this._player.GetPosition() - this._hammerPosition);
                if ((this._hammerPosition - this._player.GetPosition()).Length() < 0.5f)
                {
                    this.hammerDropped = false;
                    this.hammerEnroute = false;
                }
            }
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            //if (!hammerDropped) return;

            Vector3 position = GetPosition();
            Quaternion rotation = GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
            Matrix translationMatrix = Matrix.CreateTranslation(position);
            Matrix scaleMatrix = Matrix.CreateScale(scale, 0.5f * scale, scale);

            Matrix world = rotationMatrix * translationMatrix * scaleMatrix;

            DrawModel(model, world, view, projection, tex);
        }

        public Vector3 GetPosition()
        {
            return _hammerPosition;
        }

        public Quaternion GetRotation()
        {
            return _hammerRotation;
        }
    }
}