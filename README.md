![teaser image](game_teaser.jpg)

# HAMMERED

*ETH Zürich Game Programming Lab 2023 Project*

Team: Andrew Dobis, Marie Jaillot, Audrey Leong, Siddharth Menon, Konstantinos Stavratis, and Yuto Takano

## About

Bring your creativity with you and embark on a journey through the world of Hammered, a unique 3D single-player puzzle game based on the concept of Thor's magic hammer. 

Your player wakes up on an island, with hazy memories of yesterday. Next to you is a magic hammer. Maybe this is Thor's! Play as the mortal main character trying to bring the hammer back to Thor, and guide them as their footsteps get hindered by puzzles and challenges that inhibit the islands that lead to Thor. The puzzles are solved using Hammered's core mechanic, which adds a new interesting touch to third-person puzzle games, and is sure to offer you lots of unique gameplay.

Master your ability to use the magical hammer in creative ways to get around obstacles in each puzzle level and aim for the exit. Laugh and embrace the story and narrative provided on this magical archipelago where godly magic is apparently everywhere. Come on, immerse yourself in the world of Hammered!


![Hammered Screenshot Banner](https://github.com/HammeredGame/Hammered/assets/60749079/49943193-aa66-495c-8a3b-5a6f977aeb64)


## Features

_Hammered_ is a 3D game built upon the [MonoGame](https://www.monogame.net/) v3.8.1 framework. As for features, it boasts:

- Custom 3D model and texture assets for all parts of the game
- Beautiful and relaxing music written exclusively for the game
- Extensive use of animated FBX models for smooth character actions, using [Aether.Extras](https://github.com/tainicom/Aether.Extras) 
- Xbox, PlayStation, Switch, and Keyboard input support
- Intuitive and smooth hammer trajectories using A* and continuously adjusted Bezier curves
- Accurate physics support through [bepuphysics v1](https://github.com/bepu/bepuphysics1/)
- Slick modern game UI built through [Myra UI](https://github.com/rds1983/Myra)
- Developer UI for editing games (written with [Imgui.NET](https://github.com/ImGuiNET/ImGui.NET), enabled when built as Debug)

Additionally, _Hammered_ has the following built from scratch:
- Grid-based scene/map discretization system, structure, and algorithms, for three dimensional and optimized A*
- Audio management system
- Game scene/map runtime management system, and custom file format for saving/loading maps during development
- Dialogue and input prompt system
- Forward rendering system with HLSL shaders for PCF shadows, Bloom, and HDR tonemap
- Hardware instantiation for particles and vegetation

## Acknowledgements

We’d like to thank the many friends who playtested our game and discovered the many hilarious
cheats and physics bugs that we initially had: Boyko, Oana, Roxana, Saikiran, Samuel and
Steven. Thank you also to Morten for casting Thor in our final game trailer, it was awesome.

We’d also like to offer [Studio Gobo](https://www.studiogobo.com/) our deepest gratitude for
playtesting our game and giving us valuable feedback.

Our thanks for some SFX goes to ZapSplat, samples by Avery Berman, Kenney’s UI Audio pack, and
Ellr’s Universal UI/Menu Soundpack.

For many of our technical difficulties, the MonoGame User Forum and the MonoGame Discord
Server have been immensely helpful in finding the path to a solution. Thank you.

Finally, we’d like to thank the Game Technology Center for providing us with this wonderful
opportunity.
