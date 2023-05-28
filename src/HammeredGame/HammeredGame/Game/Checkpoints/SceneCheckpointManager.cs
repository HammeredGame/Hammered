using BEPUphysics.Entities.Prefabs;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SoftCircuits.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HammeredGame.Game.Checkpoints
{
    /// <summary>
    /// The SceneCheckpoint Manager is responsible for saving new checkpoints in a scene, and
    /// loading the last available checkpoint. It maintains the last checkpoint in memory, but
    /// also writes it to and loads from the disk for persistence.
    /// </summary>
    public class SceneCheckpointManager
    {
        // The scene that this checkpoint manager is responsible for.
        private readonly Scene scene;

        // The last checkpoint that was saved.
        private Checkpoint checkpoint;

        private readonly GameServices services;

        public SceneCheckpointManager(Scene scene, GameServices services)
        {
            this.scene = scene;
            this.services = services;
        }

        /// <summary>
        /// Save the state of the player, all rocks, all trees, and all keys as a checkpoint, and
        /// write it to disk as well.
        /// </summary>
        /// <param name="name">The unique name of a checkpoint</param>
        public async void SaveCheckpoint(string name)
        {
            // We don't want to save the same checkpoint twice in a row. We do allow saving the same
            // checkpoint if you've saved a different one in between. This is arguably better than
            // the alternative of ignoring the second save attempt, because you can create annoying
            // situations like "attempt puzzle 1 halfway (checkpoint 1) -> go back -> enter puzzle 2
            // (checkpoint 2) and solve it -> come back (checkpoint 1 ignored) and get soft locked
            // in puzzle 1 -> try to restart from last checkpoint, which is before clearing puzzle 2".
            if (checkpoint != null && checkpoint.Name == name) return;

            // Create a new checkpoint reference object
            Checkpoint newCheckpoint = new()
            {
                Name = name
            };

            // Populate the checkpoint with player state and all rock, tree, key states.
            foreach ((string uniqueName, GameObject gameObject) in scene.GameObjects)
            {
                if (gameObject is Player player)
                {
                    newCheckpoint.PlayerState = new PlayerState
                    {
                        Position = player.Position,
                        Rotation = player.Rotation,
                        InputEnabled = player.InputEnabled
                    };
                }
                else if (gameObject is MoveBlock rock)
                {
                    newCheckpoint.RockStates.Add(uniqueName, new RockState
                    {
                        Position = rock.Position,
                        Rotation = rock.Rotation,
                        Scale = rock.Scale,
                        Visible = rock.Visible
                    });
                }
                else if (gameObject is Tree tree)
                {
                    newCheckpoint.TreeStates.Add(uniqueName, new TreeState
                    {
                        Position = tree.Position,
                        Rotation = tree.Rotation,
                        Scale = tree.Scale,
                        Visible = tree.Visible,
                        Fallen = tree.IsTreeFallen()
                    });
                }
                else if (gameObject is Key key)
                {
                    newCheckpoint.KeyStates.Add(uniqueName, new KeyState
                    {
                        Position = key.Position,
                        Rotation = key.Rotation,
                        Visible = key.Visible,
                        Collected = key.IsCollected
                    });
                }
            }
            checkpoint = newCheckpoint;

            // Attempt to write the checkpoint to disk
            try
            {
                string contents = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions()
                {
                    WriteIndented = true,

                    // JSON Serializer by default only serializes properties (those with get;set;
                    // defined). We want to serialize all fields and can't be bothered to add
                    // get;set; to all of them so we tell JSON Serializer to serialize fields as well.
                    IncludeFields = true
                });

                // Write to a file separate for each scene
                await File.WriteAllTextAsync($"checkpoint_{scene.GetType().FullName}.json", contents);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Checkpoint file checkpoint_{scene.GetType().FullName}.json couldn't be saved: {e.Message}");
            }
        }

        /// <summary>
        /// Load the last checkpoint from disk into memory, without applying it to the scene. Use
        /// <see cref="ApplyLastCheckpoint"/> after this to apply it if necessary.
        /// </summary>
        public void LoadContent()
        {
            try
            {
                string contents = File.ReadAllText($"checkpoint_{scene.GetType().FullName}.json");
                checkpoint = JsonSerializer.Deserialize<Checkpoint>(contents, new JsonSerializerOptions()
                {
                    IncludeFields = true
                });
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"Checkpoint file checkpoint_{scene.GetType().FullName}.json couldn't be loaded, assuming no checkpoint.");
            }
        }

        /// <summary>
        /// Whether or not a checkpoint exists for this scene.
        /// </summary>
        public bool CheckpointExists()
        {
            return checkpoint != null;
        }

        /// <summary>
        /// Revert to the last saved checkpoint by applying all saved game state.
        /// </summary>
        public void ApplyLastCheckpoint()
        {
            // If there isn't one in memory, we can't do anything. Even ones that are saved to disk
            // are loaded into memory on startup, so when the variable is null it really means we
            // don't have any checkpoints at all.
            if (checkpoint == null)
            {
                return;
            }

            // Load the checkpoint into the scene by setting states on objects that match the name.
            foreach ((string uniqueName, GameObject gameObject) in scene.GameObjects)
            {
                if (gameObject is Player player)
                {
                    player.Position = checkpoint.PlayerState.Position;
                    player.Rotation = checkpoint.PlayerState.Rotation;
                    player.InputEnabled = checkpoint.PlayerState.InputEnabled;
                }
                else if (gameObject is MoveBlock rock)
                {
                    // Ignore if the checkpoint doesn't have a state for this rock
                    if (!checkpoint.RockStates.ContainsKey(uniqueName)) continue;

                    rock.Position = checkpoint.RockStates[uniqueName].Position;
                    rock.Rotation = checkpoint.RockStates[uniqueName].Rotation;
                    rock.Scale = checkpoint.RockStates[uniqueName].Scale;
                    rock.Visible = checkpoint.RockStates[uniqueName].Visible;
                }
                else if (gameObject is Tree tree)
                {
                    // Ignore if the checkpoint doesn't have a state for this tree
                    if (!checkpoint.TreeStates.ContainsKey(uniqueName)) continue;

                    tree.Position = checkpoint.TreeStates[uniqueName].Position;
                    tree.Rotation = checkpoint.TreeStates[uniqueName].Rotation;
                    tree.Scale = checkpoint.TreeStates[uniqueName].Scale;
                    tree.Visible = checkpoint.TreeStates[uniqueName].Visible;
                    tree.SetTreeFallen(checkpoint.TreeStates[uniqueName].Fallen);
                }
                else if (gameObject is Key key)
                {
                    // Ignore if the checkpoint doesn't have a state for this key
                    if (!checkpoint.KeyStates.ContainsKey(uniqueName)) continue;

                    key.Position = checkpoint.KeyStates[uniqueName].Position;
                    key.Rotation = checkpoint.KeyStates[uniqueName].Rotation;
                    key.Visible = checkpoint.KeyStates[uniqueName].Visible;
                    key.IsCollected = checkpoint.KeyStates[uniqueName].Collected;
                }
            }

            // Exclusively for rocks, there could be saved objects that do not exist in the scene,
            // or vice versa, due to the mechanic of being able to spawn new rocks. We handle this
            // by either deleting objects from the scene or adding new objects at the bottom of the hierarchy.
            //
            // First, if there are rocks in the scene that aren't in the checkpoint, delete them.
            // Since we do destructive operations while looping, we have to use a copy.
            OrderedDictionary<string, GameObject> workingCopy = new();
            workingCopy.AddRange(scene.GameObjects as IEnumerable<KeyValuePair<string, GameObject>>);

            foreach ((string uniqueName, GameObject gameObject) in workingCopy) {
                if (gameObject is MoveBlock rock && !checkpoint.RockStates.ContainsKey(uniqueName))
                {
                    scene.Remove(uniqueName);
                }
            }

            // If there are rocks in the checkpoint that aren't in the scene, add them by creating
            // rocks with hardcoded sizes and bounding boxes. This works for our current scenes but
            // is brittle.
            foreach ((string uniqueName, RockState rockState) in checkpoint.RockStates)
            {
                if (scene.Get<MoveBlock>(uniqueName) == null)
                {
                    // This hardcodes the model and texture and offsets and EVERYTHING!!!
                    //
                    // Ideally we want to restore everything that was saved, but that involves a
                    // more complex data structure involving entity types, sizes, and so on, which
                    // then overlaps a lot with the XML logic which currently isn't possible to abstract.
                    ContentManager cm = services.GetService<ContentManager>();
                    MoveBlock rock = new(services, cm.Load<Model>("Meshes/Rock/rock"), cm.Load<Texture2D>("Meshes/Rock/rock_albedo"), rockState.Position, rockState.Rotation, rockState.Scale, new Box(rockState.Position.ToBepu(), 10, 10, 10, 10000));
                    rock.EntityModelOffset = new(0, -3, 0);
                    rock.Visible = rockState.Visible;
                    scene.GameObjects.Add(uniqueName, rock);
                }
            }
        }

        /// <summary>
        /// Reset all checkpoints for this scene, deleting the checkpoint file from disk as well.
        /// </summary>
        public void ResetAllCheckpoints()
        {
            checkpoint = null;
            File.Delete($"checkpoint_{scene.GetType().FullName}.json");
        }
    }
}
