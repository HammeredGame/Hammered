﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables
{
    public class Key : CollectibleInteractable
    {
        /* Provisionally 
        */
        private Door _correspondingDoor;
        private bool _keyPickedUp = false;

        public Key(Model model, Vector3 pos, float scale, Texture2D t, Door correspondingDoor) :
            base(model, pos, scale, t)
        {
            _correspondingDoor = correspondingDoor;
        }

        public bool isKeyPickedUp() { return _keyPickedUp; }

        public override void hitByPlayer(Player player)
        {
            //this.activateTrigger();
            _correspondingDoor.setKeyFound(true);
            this.setVisible(false);
            _keyPickedUp=true;
        }

    }
}

