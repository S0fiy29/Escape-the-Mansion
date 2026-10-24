# Development review

A review of the code, content and settings of *Escape the Mansion*, written for the repository. It records what was checked, what was fixed or added, how it was verified, and what is still open. Technical details are in [`SYSTEMS.md`](SYSTEMS.md) and floor data is in [`GAME_DESIGN.md`](GAME_DESIGN.md).

- [Summary](#summary)
- [Scope and method](#scope-and-method)
- [Open items](#open-items)
- [Bugs fixed](#bugs-fixed)
- [Features and improvements added](#features-and-improvements-added)
- [Story and content changes](#story-and-content-changes)
- [Verification](#verification)
- [Recommendations before submission](#recommendations-before-submission)
- [Test checklist for the built game](#test-checklist-for-the-built-game)
- [Repository notes](#repository-notes)

## Summary

The game is complete from the main menu to both endings (22 scenes). The review found and fixed errors that stopped the project from compiling in Unity 6, broken images, crashes in the menus and save code, several puzzle bugs, and inconsistent dialogue. It also added a three-slot save system with a chosen save folder, adaptive hints, working display settings and UI that fits every common laptop screen shape.

**What still needs a decision before submission** is listed in [Open items](#open-items). The most important one is that the combined hint timing has not been play-tested.

## Scope and method

- **Code:** all project scripts under `Assets/Script` (about 60 files). Third-party packs were only checked for compile errors.
- **Scenes:** all 22 scenes, read as data (components, serialized values, button events, dialogue text) and also loaded in the Unity editor.
- **Content:** every dialogue line in build order, the puzzle data of every floor, and the story for continuity.
- **Method:** reading the code, extracting data from the scene files, and running small checks inside the Unity editor (see [Verification](#verification)).
- **Not done:** a full play-through of every floor, and tests on real laptops.

## Open items

Ordered by importance.

### 1. Hint timing needs play-testing

The project used to have two hint mechanisms (a timed lightbulb button with one written hint per floor, and the adaptive `H` key). They are now **one system**: hint 1 is the written text of the floor, hints 2 and 3 are generated, and the existing lightbulb button appears when a hint is ready. See [`SYSTEMS.md`](SYSTEMS.md#4-hints).

What changed for the player:

- Hint 1 now appears at about 35% of the floor's expected time (for example 105 s on lvl1 for an average player), where the old button appeared after the full 300 to 600 s.
- Hints come strictly in order, with a one minute wait between them.
- The old fixed reveal (`TimedActivator`) is turned off at runtime for the Hint Button. Its time is still used as the expected time.

What is not confirmed: the button binding at runtime, and whether 35% / 65% / 100% feels right. Only simulations and edit-mode checks were run. If hints feel too early or late, change the fractions in `HintManager.TierTimes`.

### 2. lvl6 key balance

lvl6 has a **Store-key door but no chest that gives a Store key**. It has two General-key chests but only one General door. Either one General chest should be a Store chest, or the Store door is optional on purpose. See [`GAME_DESIGN.md`](GAME_DESIGN.md#floors-and-puzzles).

More generally, every floor has exactly as many keys as doors for each type, so there are no spare keys. If a player uses a key on an optional door they may not be able to finish the floor. Consider a hint or message about this.

### 3. Code pad

- The pad has no clear button. The entry only resets when it reaches four digits.
- A wrong entry gives no feedback.
- Two codes are shorter than four digits (lvl5: 53, lobby8: 813), so they are easier to guess by pressing digits at random.

### 4. Dialogue scripts not merged

`DialogueTrigger`, `InteractDialogue` and `GameStartDialogue` still carry their own copy of the dialogue logic. They behave the same for the player, but a future fix to `DialogueBase` will not reach them.

### 5. Timers and state not saved

- Enemy positions and progress are not saved.

### 6. Story points

- "Others were here before" is told three times (lvl1 note, lvl2 Alice, lvl2 letter), and "everyone is dead" twice (lvl7 note, lvl8). One of each could be removed.
- The Hint Panel texts for lvl2 and lvl3 are identical, and so are those of lobby5 and lvl8. Check that they fit each floor.

### 7. Smaller items

- About 40 compiler warnings about deprecated Unity calls such as `FindObjectOfType`. They do not affect the game.
- Unity warns that several scenes contain old collider data ("open and re-save to upgrade"). Re-saving each scene removes the warning.
- The pause button is positioned from the screen centre. On very wide screens it sits slightly inside the corner but stays fully visible.
- The camera has a fixed height, so wider screens see more of the map sideways and 4:3 sees less. This is a design choice, but check areas that depend on seeing a long way sideways.
- `LoadPrefs.cs` is unused.
- `com.unity.test-framework` is installed but there are no automated tests.

## Bugs fixed

### Build and assets

| Problem | Fix |
| --- | --- |
| Compile errors in Unity 6 from obsolete calls (`GetInstanceID`, non-generic `TreeView`) | `NavMeshLinkEditor` now uses `EntityId` on Unity 6.2+, and the Lucid Editor tree view uses `TreeView<int>` |
| Main menu pictures were missing: `Mansion.png`, `logo game.png` and `computer_desk.png` were WebP files named `.png`, which Unity cannot import | Converted to real PNG files with the same names and `.meta` files |
| The normal key sprite was 10 times smaller than the other keys, and `door_open` 10 times larger than the other door sprites (different pixels per unit) | Set to match their siblings |

### Crashes and errors

| Problem | Fix |
| --- | --- |
| `NullReferenceException` on every scene load in `MenuController` | Removed a hook that depended on code that was never used. `SceneAutoLoader` now checks for null |
| The save flag stayed on forever if anything threw during a save | The flag is now always released |
| Loading from the pause menu stayed subscribed to scene events when there was no save | Load goes through one path that only subscribes when a scene is really loaded |
| Restoring a chest crashed when its data was missing | Null check added |
| `MusicManager` could use an audio source that did not exist yet | Fixed |
| `MenuController.ResetButton` set the resolution list to an index past the end | Uses the matching entry |
| `MenuController.SetVolume` crashed if there was no `MusicManager` | Null check added |
| `PlayerController` searched the scene for `PlayerAnimation` on every physics step | Looked up once |
| The "Saved" confirmation could stay on screen forever when saving from the pause menu (it used game time) | Uses real time |

### Puzzles and gameplay

| Problem | Fix |
| --- | --- |
| After the first safe was opened, **every later safe was already open** (a static flag that was never reset) | Reset at the start of each scene, and saved |
| Door levers, the exit lever, the exit gate and the safe were **not saved**, so a loaded game could have levers that did nothing on the first pull | All are saved and restored |
| Opening an empty chest showed nothing | Shows "The chest is empty." |
| The player kept walking in place during dialogue | The body and the walking animation stop when the dialogue starts |
| Dialogue speed and controls differed by scene: two scenes had typing speed 0 (instant text), the first `F` press locked input in some scripts so only the button advanced, and `F` while typing skipped whole lines | One shared set of controls, see below |
| A speaker name the script did not know left the screen empty and the player frozen | Any speaker other than Hazil uses the NPC panel |

### Performance

| Problem | Fix |
| --- | --- |
| Loading a save searched the whole scene four times for every saved entry, and logged a warning for each entry that did not match | Every object is looked up once by ID. Warning spam removed |
| Each savable object searched the scene for its manager when it woke up | The manager is found once |

### Settings and screens

| Problem | Fix |
| --- | --- |
| Saved volume, quality and fullscreen choices were never restored (the loader was switched off) | `DisplaySettings` applies them before the first scene loads |
| The first Apply in Graphics set quality to the lowest level and left fullscreen if the player had not touched those controls | Controls start from the settings in effect |
| The quality menu was off by one ("Ultra" set High) | Low, Medium, High and Ultra map to the matching levels |
| The Brightness slider did nothing | Implemented as a screen overlay |
| Wide screens (21:9) cut off hundreds of UI elements in every scene | Every canvas scaler uses Expand. Common laptop shapes were already fine |
| The default window was 1920 x 1080, too big for a 1080p laptop | 1280 x 720, still resizable, still opens as a fullscreen window |
| Lower quality levels had vSync off | vSync is forced on |
| The resolution list could repeat entries (one per refresh rate) | One entry per size |
| The pause button was 80 pixels in six scenes and 100 in the others, and at slightly different positions | All 21 scenes use the same size and position |
| Music: all seven tracks were fully loaded into memory | Imported as Streaming at 70% quality |

## Features and improvements added

- **Three save slots** and a **save folder** the player can choose, **New Game** slot choice, safe writing, damaged-file handling, and import of the old single save. See [`SYSTEMS.md`](SYSTEMS.md#3-saving-and-loading).
- **Adaptive hints** that merge the original Hint Button with generated hints, with a one minute wait and a one-time guide. See [`SYSTEMS.md`](SYSTEMS.md#4-hints).
- **Shared dialogue code** (`DialogueBase`) replacing about 2,000 lines of copy and paste across ten scripts.
- **Chest sound cue** that gets faster and louder as the player nears an unopened chest, to help find hidden chests.
- **Smoother movement:** the diagonal speed matches straight movement, and the body is interpolated.
- **Display settings** that work as described in [`SYSTEMS.md`](SYSTEMS.md#5-display-and-audio-settings).
- This documentation.

## Story and content changes

- The "famous murder case" line in the Office now says it was in the 1970s (it said "back in the year").
- lvl7: "Mrs. Jacob" corrected to "Mr. Jacob".
- lvl8: a sentence that appeared twice was removed, and a line spoken by Hazil that was really Alice's was moved to Alice.
- lobby4: Elana no longer asks "Who are you?" to someone she already met in lvl3.
- lvl5: two lines were added in which Elana says the house is cursed and that people forget things. Later notes refer to this.
- lvl6: Hazil describes Alice's silver hair before Mr. Jacob repeats it.
- lvl8: Cecilia's line about her name now reads "Nobody ever used my name, not even my parents. They all just called me 'Milady.'", so it no longer contradicts her telling Hazil her name.
- Two missing full stops were added (lobby2, lvl2).

## Verification

**What was done.** Every change was compiled in the Unity editor with no errors. Checks were also run inside the editor on the real scenes:

- A save written to a temporary folder, read back, overwritten and deleted. Damaged files and invalid folders were rejected as expected.
- lvl5's puzzle state (two door levers, the exit lever, the safe and the gate) was saved, reset and restored.
- All 22 scenes were loaded to check the canvas scalers, and their UI was laid out at five screen shapes (16:9, 16:10, 3:2, 4:3 and 21:9) to find what gets cut off, before and after the fix. For 16:10, 3:2 and 4:3 nothing was newly cut off compared with 16:9. At 21:9 about 950 elements were newly cut off before the fix and none after.
- The hint timing for fast, average, slow and hint-loving players, the one minute wait over a simulated floor, and the hint text of lvl5 at each stage of its puzzle.
- The combined hint system: hints come in order 1, 2, 3 for two simulated players; the Hint Button, Hint Panel and written text were found on all 11 floors; the longest written hint (256 characters) fits the hint box.
- The hint panel, hint guide and save screen were built and measured to confirm the text fits.
- The quality mapping, the resolution list and the brightness overlay values.
- Every scene that was edited was loaded again to confirm it still opens and shows the new values.

**Limits.**

- None of this replaces playing the game. The adaptive hint timing, the Windows folder window and the settings were not confirmed in a built game.
- There are no automated tests in the project.

## Recommendations before submission

1. Play-test the hint timing ([open item 1](#1-hint-timing-needs-play-testing)).
2. Check the lvl6 Store key ([open item 2](#2-lvl6-key-balance)).
3. Play every floor once from New Game to each ending in a **built** game, with a save and a load in the middle of two floors.
4. Add the required credit text for the music and the art packs (see the README credits table).
5. Add a licence for your own code and the author and supervisor names to the README.
6. Re-save the scenes in Unity to remove the old collider warnings, and clear the other warnings if time allows.

## Test checklist for the built game

Tick these on a built `.exe`. Use the command line option from the README to try screen sizes.

| Test | Result |
| --- | --- |
| Game starts, main menu pictures show | |
| New Game asks for a slot and starts in Home | |
| Play to lvl1: hint guide appears once, `H` works, one minute wait is shown | |
| The lightbulb Hint Button appears when a hint is ready, shows the written text first, and hides during the one minute wait | |
| Save to slot 2, close the game, reopen, Load slot 2: scene, position, keys, chests, doors, levers, gate and safe are restored | |
| Change the save folder with Browse, save, check the file appears there | |
| Settings: change quality and brightness, press Apply, restart: they are kept | |
| First Apply with nothing changed does not leave fullscreen | |
| 1366 x 768, 1920 x 1200, 2256 x 1504 and 2560 x 1080: no UI is cut off, the pause button is visible | |
| Windows display scaling 125% and 150%: the window and fullscreen toggle (`Alt` + `Enter`) behave | |
| Both endings reachable from lvl8 | |
| No errors in `Player.log` (`%USERPROFILE%\AppData\LocalLow\SOFIY_SME\Escape the Mansion\Player.log`) | |

## Repository notes

- The Unity project root is the folder that contains `Assets/`, `Packages/` and `ProjectSettings/`.
- A `.gitignore` is included. **The `Library` folder is over 4 GB and must not be committed.** Keep all `.meta` files, they hold references between assets.
- The `Assets` folder is about 240 MB. The largest single file is about 11 MB, so GitHub's file size limit is not a problem and Git LFS is not needed.
- Folders `Image` and `Sound` that sit next to the Unity project (outside it) hold source art and music. Decide whether they belong in the repository.
