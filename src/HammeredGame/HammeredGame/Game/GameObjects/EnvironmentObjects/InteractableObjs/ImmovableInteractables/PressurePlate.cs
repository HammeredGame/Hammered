﻿using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.PositionUpdating;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables
{
    /// <summary>
    /// The <c>PressurePlate</c> class refers to an unmoving object on the ground, which when pressed by some weight
    /// (currently the character or the hammer) triggers a change of behaviour of its corresponding <paramref>triggerObject</paramref>.
    /// Once the weight is lifted from the <c>PressurePlate</c> instance, the state of the corresponding <paramref>triggerObject</paramref>
    /// reverts to its state before being triggered by the <c>PressurePlate</c> instance.
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="InteractableObject "/> ---> <see cref="ImmovableInteractable "/>
    ///                         ---> <see cref="PressurePlate"/>
    /// </para>
    /// </remarks>
    public class PressurePlate : ImmovableInteractable
    {
        private EnvironmentObject triggerObject;
        private bool playerOn = false, hammerOn = false;
        private bool pressureActivated = false;

        private List<SoundEffect> pressSfx = new List<SoundEffect>();

        public PressurePlate(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                this.Entity.Tag = "ImmovableInteractableBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.ActiveSpace.Add(this.Entity);
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

                //this.entity.CollisionInformation.Events.InitialCollisionDetected += this.Events_InitialCollision;
                this.Entity.CollisionInformation.Events.PairTouching += this.Events_PairTouching;
                this.Entity.CollisionInformation.Events.CollisionEnded += this.Events_CollisionEnded;

                pressSfx = Services.GetService<List<SoundEffect>>();
            }
        }

        public void SetTriggerObject(EnvironmentObject triggerObject)
        {
            this.triggerObject = triggerObject;
        }

        private void Events_PairTouching(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!
                //if (other.Tag is Player)
                //{
                //    System.Diagnostics.Debug.WriteLine("pressureplate and player pair touching");
                //}

                this.SetActivated(true);
            }
        }

        private void Events_InitialCollision(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!
                this.SetActivated(true);
            }
        }

        private void Events_CollisionEnded(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            //System.Diagnostics.Debug.WriteLine(this.entity.CollisionInformation.Pairs.Count);
            if (otherEntityInformation != null)
            {
                if (sender.Pairs.Count <= 1)
                {
                    this.SetActivated(false);
                }
            }
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            //triggerObject.setVisible(true);
            //if (playerOn || hammerOn)
            //{
            //    triggerObject.SetVisible(false);
            //}
            //else
            //{
            //    triggerObject.SetVisible(true);
            //}
            if (this.pressureActivated && triggerObject != null)
            {
                // pressSfx[5].Play();
                if (triggerObject is Door)
                {
                    var door = triggerObject as Door;
                    door.RemoveFromSpace();
                }
                //triggerObject.Visible = false;
                //if (this.ActiveSpace.Entities.Contains(triggerObject.Entity))
                //    this.ActiveSpace.Remove(triggerObject.Entity);
            }
            else if (triggerObject != null)
            {
                // pressSfx[6].Play();
                if (triggerObject is Door)
                {
                    var door = triggerObject as Door;
                    door.AddToSpace();
                }
                //triggerObject.Visible = true;
                //if (!this.ActiveSpace.Entities.Contains(triggerObject.Entity) && triggerObject.Entity != null)
                //    this.ActiveSpace.Add(triggerObject.Entity);
            }
        }

        public bool IsActivated()
        {
            return this.pressureActivated;
        }

        public void SetActivated(bool activate)
        {
            this.pressureActivated = activate;
        }

        //private void ActivateTrigger()
        //{
        //    triggerObject.SetVisible(false);
        //}

        //public override void TouchingPlayer(Player player)
        //{
        //    //this.activateTrigger();
        //    playerOn = true;
        //}

        //public override void NotTouchingPlayer(Player player)
        //{
        //    playerOn = false;
        //}

        //public override void TouchingHammer(Hammer hammer)
        //{
        //    //this.activateTrigger();
        //    hammerOn = true;
        //}

        //public override void NotTouchingHammer(Hammer hammer)
        //{
        //    hammerOn = false;
        //}
    }
}
