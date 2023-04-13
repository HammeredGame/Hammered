# HAMMERED

Welcome to "HAMMERED"! **We recommend playing this game with an Xbox controller**, although keyboard is supported too.

## Controls

| Action                                   | Xbox Controller | Keyboard                            |
|------------------------------------------|-----------------|-------------------------------------|
| Movement                                 | Left Stick      | WASD Keys (and combinations)        |
| Drop hammer at current position          | A button        | E Key                               |
| Summon hammer back                       | B button        | Q Key                               |
| Change camera angle                      | Left Dpad       | 1234 Keys (on alphanumeric section) |
| Reset level (for softlocks or replaying) | Y button        | R Key                               |

Summoning the hammer will make the hammer follows a straight line trajectory from the position the hammer was dropped at towards the player. At the end of the trajectory, the character catches and moves while carrying the hammer.

Camera angles alternate between four four (4) predetermined camera angles.

The "1", "2", "3" and "4" keys for the camera controls on the keyboard are from the alphanumeric keys section of the keyboard, not on the numeric keypad.

Changing the Locale:
1. Navigate to "level4.xml" file. PATH: publish/Content/level4.xml
2. Using regular expressions (regex) replace ([0-9]+)\.([0-9]+) with $1\,$2
3. Go to the top line of "level4.xml" file and change the ' Version="1,0" ' (which was altered because of step 2.) to ' Version="1.0" '
4. The game can now be run normally