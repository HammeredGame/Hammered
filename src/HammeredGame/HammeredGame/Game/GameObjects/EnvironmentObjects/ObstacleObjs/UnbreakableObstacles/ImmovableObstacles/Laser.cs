using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using BEPUphysics.PositionUpdating;
using Hammered_Physics.Core;
using HammeredGame.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
    /// <summary>
    /// The <c>Laser</c> class is an immovable obstacle within the game world, blocking the player's
    /// access to other parts of the map.
    /// <para />
    /// In addition to base <c>GameObject</c> properties, the laser also has the following properties defined:
    /// - the current state of the laser -> <code>LaserState laserState</code>
    ///     -- fully blocks both player and hammer -> <code>LaserState.FullBlocking</code>
    ///     -- blocks hammer, but not the player -> <code>LaserState.HammerBlocking</code>
    ///     -- blocks player, but not the hammer -> <code>LaserState.PlayerBlocking</code>
    ///     
    /// - The current scale of the laser -> <code>float laserScale</code>
    ///     -- This gets modified as obstacles collide with the laser
    /// 
    /// - The default length of the laser -> <code>float laserDefaultLength</code>
    ///     -- This is the default start length of the laser (at the start, and the value to return to
    ///        when unobstructed
    /// <para/>
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> ImmovableObstacle
    /// <para />
    /// TODO: The laser's collision handling is not quite robust. (it works alright if we have one obstacle blocking
    /// it, but doesn't work as intended with multiple obstacles blocking it). Possibly need to play around with 
    /// the different collision handlers.
    /// <para/>
    /// 
    /// TODO: Currently, the code only handles the default state of the laser (full blocking). Additionally, it does
    /// not handle any rotating laser functionality.
    /// 
    /// </remarks>

    public class Laser : ImmovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        public enum LaserState
        {
            FullBlocking,
            HammerBlocking,
            PlayerBlocking
        }

        // Default variables (should ideally only be modified at level setup)
        private float laserDefaultLength;
        private float laserDefaultScale;

        // Dynamic variables
        private LaserState laserState;
        private float laserScale;

        public Laser(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                this.Entity.Tag = "ImmovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
                this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, (this.Entity as Box).HalfHeight, 0);
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
                this.ActiveSpace.Add(this.Entity);

                this.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
                this.Entity.CollisionInformation.Events.CollisionEnded += Events_CollisionEnded;
            }

            // Set the default state / variables
            this.laserState = LaserState.FullBlocking;
            this.laserDefaultLength = (this.Entity as Box).Height;
            this.laserDefaultScale = this.Scale;
            this.laserScale = this.Scale;
        }

        private void Events_CollisionEnded(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is ObstacleObject)
                {
                    this.ReturnToDefaultLength();
                }
            }
        }

        private void Events_DetectingInitialCollision(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is ObstacleObject)
                {
                    //pair.Contacts[0].Contact.Position;
                    BEPUutilities.Vector3 pointOfContact = pair.Contacts[0].Contact.Position;
                    float dist = (pointOfContact - MathConverter.Convert(this.Position)).Length();
                    float scale = dist / this.laserDefaultLength;
                    if (scale < this.laserScale)
                        this.SetLaserDynamicScale(scale);
                    
                }
            }
        }

        public void SetLaserState(LaserState state)
        {
            this.laserState = state;
        }

        public void SetLaserAngle(Quaternion angle)
        {
            this.Rotation = angle;
        }

        public void SetLaserDefaultScale(float scale)
        {
            this.laserDefaultScale = scale;
            this.SetLaserDynamicScale(scale);
            this.laserDefaultLength = (this.Entity as Box).Height;
        }

        private void SetLaserDynamicScale(float scale)
        {
            this.laserScale = scale;
            (this.Entity as Box).Height *= scale;
            this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, (this.Entity as Box).HalfHeight, 0);
        }

        private void ReturnToDefaultLength()
        {
            this.laserScale = this.laserDefaultScale;
            (this.Entity as Box).Height = this.laserDefaultLength;
            this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, (this.Entity as Box).HalfHeight, 0);
        }

        public override Matrix GetWorldMatrix()
        {
            Matrix rotationMatrix = Matrix.CreateFromQuaternion(Rotation);
            // For translation, include the model's origin offset so that the collision body
            // position matches with the rendered model
            Matrix translationMatrix = Matrix.CreateTranslation(Position + EntityModelOffset);
            Matrix scaleMatrix = Matrix.CreateScale(this.Scale, this.laserScale, this.Scale);

            // Construct world matrix
            // Be careful! Order matters!
            // The transformations in this framework are applied FROM LEFT TO RIGHT
            // (in contrast with how it is done in mathematical notation).
            ///<example>
            /// Provided the transformation standard affine transformation matrices: Translation (T), Rotation (R) and Scaling (S)
            /// if we wish to apply the transformation: R -> T-> S on a vector
            /// we would express it in mathematical notation as STR,
            /// but as R * T * S in MonoGame (and OpenGL).
            ///</example>

            // World matrix = S -> R -> T
            return scaleMatrix * rotationMatrix * translationMatrix;
        }

    }
}
