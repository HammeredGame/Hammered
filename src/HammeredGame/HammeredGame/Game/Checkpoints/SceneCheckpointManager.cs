using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HammeredGame.Game.Checkpoints
{
    public class SceneCheckpointManager
    {
        private readonly Scene scene;
        private Checkpoint checkpoint;

        public SceneCheckpointManager(Scene scene)
        {
            this.scene = scene;
        }

        public async void SaveCheckpoint(string name)
        {
            // We don't want to save the same checkpoint twice in a row.
            if (checkpoint != null && checkpoint.Name == name) return;

            Checkpoint newCheckpoint = new()
            {
                Name = name
            };

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

            // Save the checkpoint to disk.
            try
            {
                string contents = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    IncludeFields = true
                });
                await File.WriteAllTextAsync($"checkpoint_{scene.GetType().FullName}.json", contents);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Checkpoint file checkpoint.json couldn't be saved: {e.Message}");
            }
        }

        public void LoadContent()
        {
            try
            {
                string contents = File.ReadAllText($"checkpoint_{scene.GetType().FullName}.json");
                checkpoint = JsonSerializer.Deserialize<Checkpoint>(contents, new JsonSerializerOptions()
                {
                    IncludeFields = true
                });

                LoadLastCheckpoint();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"Checkpoint file checkpoint_{scene.GetType().FullName}.json couldn't be loaded, assuming no checkpoint.");
            }
        }

        public bool CheckpointExists()
        {
            return checkpoint != null;
        }

        public void LoadLastCheckpoint()
        {
            // If there isn't one in memory, we can't do anything. Even ones that are saved to disk
            // are loaded into memory on startup, so when the variable is null it really means we
            // don't have any checkpoints at all.
            if (checkpoint == null)
            {
                return;
            }

            // Load the checkpoint into the scene.
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
                    rock.Position = checkpoint.RockStates[uniqueName].Position;
                    rock.Rotation = checkpoint.RockStates[uniqueName].Rotation;
                    rock.Scale = checkpoint.RockStates[uniqueName].Scale;
                    rock.Visible = checkpoint.RockStates[uniqueName].Visible;
                }
                else if (gameObject is Tree tree)
                {
                    tree.Position = checkpoint.TreeStates[uniqueName].Position;
                    tree.Rotation = checkpoint.TreeStates[uniqueName].Rotation;
                    tree.Scale = checkpoint.TreeStates[uniqueName].Scale;
                    tree.Visible = checkpoint.TreeStates[uniqueName].Visible;
                    tree.SetTreeFallen(checkpoint.TreeStates[uniqueName].Fallen);
                }
                else if (gameObject is Key key)
                {
                    key.Position = checkpoint.KeyStates[uniqueName].Position;
                    key.Rotation = checkpoint.KeyStates[uniqueName].Rotation;
                    key.Visible = checkpoint.KeyStates[uniqueName].Visible;
                    key.IsCollected = checkpoint.KeyStates[uniqueName].Collected;
                }
            }
        }

        public void ResetAllCheckpoints()
        {
            checkpoint = null;
            File.Delete($"checkpoint_{scene.GetType().FullName}.json");
        }
    }
}
