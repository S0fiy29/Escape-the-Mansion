# Escape the Mansion

A 2.5D puzzle and mystery game made in Unity for the CSP650 Final Year Project (UiTM, Semester 6).

You play **Hazil**, an investigator sent to check on a mansion that is supposed to be abandoned. Inside, every floor is a puzzle room with keys, chests, levers, safes and doors, and the people you meet do not tell you the same story. Escape floor by floor to reach one of two endings.

> **Spoilers:** the story, the puzzle solutions and the safe codes are in [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md). This page avoids them.

## Features

- 22 scenes: main menu, intro, 8 puzzle floors, 8 lobbies, and two endings (good and bad).
- Puzzle types: four key colours with locked doors, chests (some empty), door levers, an exit lever and gate, and code safes.
- Dialogue with typed text, a player panel and one or two NPC panels, leading to one of two endings.
- Patrolling enemies on floors 5, 7 and 8 that chase the player along waypoints (A* pathfinding). Touching one ends the run with a game over panel.
- **3 save slots** with a save folder the player can choose, plus New Game slot selection.
- **Adaptive hints:** press `H` (or click the lightbulb button on floors that have one) for up to three hints per floor. Hint 1 is the text written for the floor, hints 2 and 3 are built from what the player has and has not done yet. They unlock sooner or later depending on how the player has been doing, with a one minute wait between hints.
- Display settings that work on laptops: fullscreen window at native size, resizable window, quality, brightness, resolution and volume, all saved between sessions.
- UI that keeps the full 1920x1080 design area visible on 16:9, 16:10, 3:2, 4:3 and ultrawide screens.

## Controls

| Key | Action |
| --- | --- |
| Arrow keys or `W` `A` `S` `D` | Move |
| `F` | Interact: talk, open chests and doors, pull levers, use the exit gate. Also finishes a dialogue line and moves to the next one |
| `Space` | Skip the typing of the current dialogue line |
| `H` | Ask for a hint (puzzle floors only) |
| `Shift` + `H` | Read the hint guide again |
| `Esc` | Close the save and load screen or the hint guide |
| Mouse | On-screen buttons: pause, save, load, continue, settings |

Saving and loading are done from the pause menu. The save screen shows three slots and the folder they are stored in.

## Requirements

| | |
| --- | --- |
| Platform | Windows 10 or 11, 64-bit (standalone build) |
| Unity | **6000.6.6f1** (Unity 6) |
| Render pipeline | Universal Render Pipeline 17.6.0 (2D renderer) |
| Input | Legacy Input Manager |

Main packages: Cinemachine 6.6.0, 2D feature set 2.0.2, AI Navigation 2.0.16, uGUI 2.6.0, TextMesh Pro, Timeline 6.6.0.

## Getting started

1. Install Unity **6000.6.6f1** with the Windows Build Support module.
2. Clone this repository and open the folder that contains `Assets/` and `ProjectSettings/` in Unity Hub (**Add project from disk**).
3. The first import takes a while because Unity rebuilds the `Library` folder, which is not stored in the repository.
4. Open `Assets/Scenes/MainMenu.unity` and press Play.

### Build

`File > Build Profiles > Windows`, then **Build**. All 22 scenes are already in the build list. **The order of scenes matters:** the game moves to the next floor by loading the next scene in the build list, so do not reorder them (see [`docs/SYSTEMS.md`](docs/SYSTEMS.md)).

To test different screen sizes without changing your display, start the built game from a terminal:

```bash
"Escape the Mansion.exe" -screen-width 1366 -screen-height 768 -screen-fullscreen 0
```

## Project layout

```text
Assets/
  Scenes/              The 22 scenes (see the order in docs/SYSTEMS.md)
  Script/
    AI/                Enemy patrol and A* pathfinding
    Camera/            Cinemachine camera switching
    Controller/        Doors, levers, gates, scene transitions, timers
    Interact/          Chests, safes, interactable objects, exit lever
    Menu/              Menus, save system, hints, display settings, music
    NPC/               Dialogue (DialogueBase and its subclasses)
    Player/            Movement, animation, keys, intro dialogues
  Prefabs/             Reusable objects: chests, doors, keys, boxes, levers
  Image/, Animation/   Game art and animation
  Sound/               Music and sound effects
  Cainos/, Gentleland/, Assets_DevilsWorkShop/, NavMeshComponents/   Third-party assets
docs/
  SYSTEMS.md           How each system works
  GAME_DESIGN.md       Floors, puzzles, story and endings (spoilers)
  DEVELOPMENT_REVIEW.md  What was reviewed, fixed and added, and what is still open
```

## Documentation

- [`docs/SYSTEMS.md`](docs/SYSTEMS.md): technical documentation of the dialogue, save, hint, settings, audio, movement and puzzle systems.
- [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md): floor-by-floor puzzle data, story, characters and endings. **Contains spoilers.**
- [`docs/DEVELOPMENT_REVIEW.md`](docs/DEVELOPMENT_REVIEW.md): code and content review, the bugs that were fixed, what was verified, and the remaining issues.

## Known issues and open items

The complete list, with reasons and suggested fixes, is in [`docs/DEVELOPMENT_REVIEW.md`](docs/DEVELOPMENT_REVIEW.md). The ones to know about first:

- **Hint timing is untested in real play.** The hint systems are combined, but the 35% / 65% / 100% timing has only been checked by simulation. Tune it after a play-through.
- lvl6 has a Store-key door but no chest that gives a Store key.
- The safe code pad has no clear button.
- Three dialogue scripts (`DialogueTrigger`, `InteractDialogue`, `GameStartDialogue`) still carry their own copy of the dialogue logic.

## Credits and licences

Check the licence of every asset before publishing or submitting, and keep the attributions that each one requires.

| Asset | Source |
| --- | --- |
| Village props and Lucid Editor | Cainos (`Assets/Cainos`, licence file in `Assets/Cainos/Third Party/Lucid Editor/License.txt`) |
| Steampunk UI | Gentleland (`Assets/Gentleland`, font licences in `ThirdPartyLicenses`) |
| Low poly pixel 2D blocks (isometric tiles) | Devil's Workshop (`Assets/Assets_DevilsWorkShop`, `License.txt` in the pack) |
| NavMesh components | NavMeshPlus (`Assets/NavMeshComponents`) |
| Music: *Echoes of Time v2*, *Unseen Horrors* | Kevin MacLeod |
| Music: *Uprising*, *RiseAbove* | Scott Buckley |
| Music: *Morning Routine [Lofi Study]* | Ghostrifter Official |
| Music: *Bewitched Forest* | Peritune |
| Sound effect: *Door Opening* | Source to be added |

Music tracks like these are usually released under licences that require a credit line, so add the exact credit text each author asks for, in the game and in this file.

Author, supervisor and project licence: *add here*.
