# Systems documentation

How the game is built. File names are relative to `Assets/Script/`. See the [README](../README.md) for the overview and [`GAME_DESIGN.md`](GAME_DESIGN.md) for floor and story data.

- [1. Scenes and flow](#1-scenes-and-flow)
- [2. Dialogue](#2-dialogue)
- [3. Saving and loading](#3-saving-and-loading)
- [4. Hints](#4-hints)
- [5. Display and audio settings](#5-display-and-audio-settings)
- [6. Audio](#6-audio)
- [7. Player and movement](#7-player-and-movement)
- [8. Puzzle components](#8-puzzle-components)
- [9. Enemies](#9-enemies)
- [10. Authoring notes](#10-authoring-notes)

## 1. Scenes and flow

The game is a straight line through the scenes in the build list:

| Index | Scene | Index | Scene |
| --- | --- | --- | --- |
| 0 | MainMenu | 11 | lvl4 |
| 1 | Home | 12 | lobby5 |
| 2 | Office | 13 | lvl5 |
| 3 | Mansion Front | 14 | lobby6 |
| 4 | lobby1 | 15 | lvl6 |
| 5 | lvl1 | 16 | lobby7 |
| 6 | lobby2 | 17 | lvl7 |
| 7 | lvl2 | 18 | lobby8 |
| 8 | lobby3 | 19 | lvl8 |
| 9 | lvl3 | 20 | GoodEnd |
| 10 | lobby4 | 21 | BadEnd |

**Moving on:** `GateController`, `Enter` and `EnterNPC` all load `buildIndex + 1`. `EnterNPCBE` loads a chosen ending scene. Because of this, **the order of the build list is part of the game logic.** Reordering or inserting scenes changes where each exit leads, which music plays (see [Audio](#6-audio)) and how floors are timed (see [Hints](#4-hints)).

Every game scene has its own `GameController`, which owns the pause panel, saving and loading, and restoring a loaded game. `GameStateManager` and `SceneAutoLoader` are older helpers that are not part of the current save flow.

## 2. Dialogue

**`NPC/DialogueBase.cs`** is the shared logic for dialogue with two panels (one NPC, one player). Ten scripts inherit from it and only describe what is different:

| Script | What it adds |
| --- | --- |
| `NPC`, `NPC2`, `NPC3V2` | Nothing extra. When the last line ends they tell `EnterNPC` the player has talked to the NPC |
| `NPC3` | After the last line it swaps colliders and objects, fades the NPC out and reveals a sprite |
| `NPC4` | A second NPC panel, so two NPCs can speak in one conversation |
| `NPCBE`, `NPCGE`, `NPCTE` (via `EndGameDialogue`) | After the last line, time stops and the end panel shows |
| `InteractDialogue2` | Can also be started from a UI button (`TriggerDialogueFromButton`) |
| `GameStartDialogue2` | Starts by itself when the scene loads. `GameController` calls `DisableDialogue()` when restoring a save |

Behaviour that is the same everywhere:

- Each line is a `DialogueLine` with a `speaker` and a `line`. The speaker `Hazil` uses the player panel. **Any other speaker name uses the NPC panel**, so a new character never leaves the screen empty. The speaker name is only used for choosing the panel. The name the player sees is text in the panel itself.
- `F` starts the dialogue near an NPC, finishes the line that is being typed, then moves to the next line. `Space` skips the typing. The on-screen continue buttons work too.
- A typing speed of 0 or less is replaced by 0.05 seconds per character.
- While a dialogue is open the player cannot move: the dialogue turns off `PlayerController`, which stops the body and the walking animation.

Hooks a subclass can override: `GetSpeakerUI`, `ShowOnlyPanel`, `ClearTexts`, `SetContinueButtons`, `OnDialogueFinished` and `UsesProximity`.

**Not merged yet:** `DialogueTrigger` (starts when the player enters a zone), `InteractDialogue` and `GameStartDialogue` (single panel, line arrays). They have the same controls but their own copy of the logic.

## 3. Saving and loading

Files: `Menu/SaveSystem.cs`, `Menu/SaveSlotsUI.cs`, `Menu/FolderPicker.cs`, `Menu/SaveIds.cs`, `Menu/GameController.cs`.

**Slots.** There are 3 slots, stored as `slot1.json` to `slot3.json`.

- **Default folder:** `%USERPROFILE%\AppData\LocalLow\SOFIY_SME\Escape the Mansion\Saves` (Unity replaces the dot in the company name).
- **Custom folder:** chosen on the save screen. The path is kept in `PlayerPrefs` under `saveFolderPath` and is checked for write access before it is used.
- **Last slot:** `lastSaveSlot` remembers the slot that was used last.
- **Old save:** a single `saveData.json` from the earlier version is copied into slot 1 once, if slot 1 does not exist.

**File format.** A slot is one JSON file written with `JsonUtility`:

```json
{
  "version": 2,
  "savedAt": "2026-10-24 01:20",
  "sceneName": "lvl3",
  "saveData": [ { "uniqueID": "Player", "currentScene": "lvl3", "posX": 1.0, "posY": 0.0, "posZ": 2.0 } ]
}
```

A save is written to a temporary file first and then moved over the old one, so a crash while saving never destroys the previous save. A file that cannot be read is shown as "Damaged save" and can be deleted.

**What is saved.** Each entry in `saveData` is a `SaveData`. The `uniqueID` tells the game what it belongs to:

| `uniqueID` | Contents |
| --- | --- |
| `Player` | Scene name and position. Y and Z are stored swapped and swapped back when loading |
| `PlayerKeys` | How many keys of each of the four types the player holds |
| box, door, chest names | Box positions, door open state, chest open state and trigger collider |
| lever-door names (`DoorController2`) | Open or closed |
| `lever:<hierarchy path>` | A door lever's on or off position |
| `finallever:<hierarchy path>` | Whether the exit lever was used |
| `gate:<hierarchy path>` | Whether the exit gate is open |
| `SafeState` | Whether the safe on the floor is open |
| `GameStartDialogue` | Whether the intro panel is still active |
| names of `SavableObject`s | Active or inactive |
| `HintState` | Floor time, hint progress and the wait before the next hint (see [Hints](#4-hints)) |

Levers, gates and safes have no unique name of their own, so `SaveIds.PathOf` builds an ID from the object's place in the scene hierarchy.

**Flow.**

1. The pause menu's **Save** button calls `GameController.SaveGame()`, which opens `SaveSlotsUI` in Save mode. Choosing a slot calls `SaveToSlot`, which gathers the data (`GatherSaveData`) and writes it with `SaveSystem.SaveGame`.
2. **Load** (pause menu or main menu) opens the slot screen in Load mode. `SaveSystem.LoadSlotIntoGame` stores the data in `MenuController.LoadedSaveData` and loads the saved scene.
3. In the new scene `GameController.Start` sees the stored data and runs `RestoreGameState`. It waits two frames so the scene finishes starting, then looks every object up once by its ID and restores it. Entries that do not belong to an object are skipped quietly.

**Slot screen.** `SaveSlotsUI` builds itself in code, so it works in every scene without setup. It has three modes:

- **Save:** saving over a different slot asks for a second click. The slot the current game already uses (`SaveSystem.ActiveSlot`) does not ask again.
- **Load:** empty or damaged slots cannot be picked. The screen opens even if the folder has no saves, so another folder can be chosen.
- **NewGame:** the slot picked becomes the current game's slot. An occupied slot asks for a second click and is only overwritten when the player saves.

Each slot has a **Delete** button that asks to be clicked twice. The footer shows the save folder with **Browse...** (the native Windows folder window, or Unity's panel in the editor), a typed path box and **Default**. Press `Esc` to close.

## 4. Hints

There is **one hint system**, `Menu/HintManager.cs`. It creates itself when the game starts, so nothing has to be placed in a scene. It combines two things:

- the adaptive timing and the generated hints (sections 4.1 and 4.2), and
- the **lightbulb Hint Button and Hint Panel** that eleven floors already had (lobby5, lobby6, lobby8 and lvl1 to lvl8), described in section 4.3.

### 4.1 Timing and tiers

**Which floors.** Any scene that has a gate, doors, chests, levers or a safe.

**Time.** Time on a floor only counts while the player is free to act: not while paused, in dialogue, a cutscene, or the save screen, and not while the guide is open.

**When hints unlock.** There are three tiers. For each floor:

```text
expected = par x lerp(1, pace, 0.7) x lerp(1, 0.7, helpBias)
tier 1 at 35% of expected (at least 45 s)
tier 2 at 65% of expected (at least 30 s after tier 1)
tier 3 at 100% of expected (at least 30 s after tier 2)
```

- `par` is the floor's `TimedActivator` time (180 s if the floor has none).
- `pace` compares the player's earlier clear times with `par` (clamped to 0.5 to 1.6). A slower player gets hints later.
- `helpBias` (0 to 1) grows when the player has used hints on earlier floors. Such a player gets hints up to 30% sooner.

| Example, par 300 s | Tier 1 | Tier 2 | Tier 3 |
| --- | --- | --- | --- |
| Average player | 105 s | 195 s | 300 s |
| Fast player (pace 0.5) | 68 s | 127 s | 195 s |
| Slow player (pace 1.5) | 142 s | 263 s | 405 s |
| Asks for hints often | 74 s | 137 s | 210 s |

**A floor counts as cleared** when the next scene in the build list loads from a floor the player was playing. Loading a save does not count (`HintManager.SuppressNextClear`).

**Using it.** A badge "Hint ready - press H" appears, or the lightbulb button on floors that have one (section 4.3). Pressing `H` (or clicking the button) shows the **next hint in order**: hint 1, then 2, then 3 (`HintManager.ChooseTier`). Once all unlocked hints have been seen, it repeats the latest. A hint stays on screen for 10 seconds. After a hint is shown the next one is locked for **60 seconds of free time** (`CooldownSeconds`). During that wait `H` shows the last hint again with a countdown, and the badge reads "Next hint in 0:42". Pressing `H` before tier 1 says roughly how long until the first hint.

### 4.2 Hint text

**Hint 1 is the text written for the floor** (from its Hint Panel, see [`GAME_DESIGN.md`](GAME_DESIGN.md#written-hint-texts-hint-1)). Floors without a Hint Panel use a generated hint 1. **Hints 2 and 3 are generated** from the current state of the floor, in this order, and change as the puzzle progresses:

1. The exit gate is open.
2. A key the player holds that fits a locked door.
3. Chests that are still closed.
4. A safe that is still closed. Tier 3 gives the code.
5. Door levers that were not pulled.
6. The lever that opens the exit gate.
7. Locked doors with no matching key.
8. A general reminder to explore and read notes.

Tier 1 is a nudge, tier 2 adds a direction ("nearby, toward the top right") and tier 3 is the most direct.

### 4.3 The original Hint Button

- On the eleven floors, `FindLegacyHint` finds the objects named "Hint Button" and "Hint Panel" when the scene loads and reads the written text from the panel.
- The fixed reveal by `Controller/TimedActivator.cs` is **disabled at runtime** for that button. The component stays in the scene and its time is still used as `par`: lvl1 300 s, lvl2 300 s, lvl3 400 s, lvl4 400 s, lvl5 500 s, lvl6 500 s, lvl7 500 s, lvl8 600 s, lobby5 300 s, lobby6 400 s, lobby8 400 s.
- `HintManager` shows the button only while a hint is ready. It is hidden during the wait, dialogue and pause. The text badge is hidden while the button is shown. The Hint Panel is kept closed, because the hint appears in the shared hint box.
- Clicking the button does the same as pressing `H`.

### 4.4 Guide and stored data

**Hint guide.** At lvl1 a panel explains the system once, the first time the player is free (`hintGuideShown`). `Shift` + `H` opens it again on any floor. On scenes that have nothing to solve, plain `H` opens it.

**Stored data.** `PlayerPrefs`: `hintPace`, `hintHelpBias`, `hintGuideShown` (New Game resets all three). The save slot keeps a `HintState` entry with the floor time, tier viewed, whether a hint was used and the remaining wait (`SaveData.hintCooldown`).

Not play-tested yet: see [`DEVELOPMENT_REVIEW.md`](DEVELOPMENT_REVIEW.md#1-hint-timing-needs-play-testing).

## 5. Display and audio settings

Files: `Menu/DisplaySettings.cs`, `Menu/MenuController.cs`.

- **Applied at start.** Before the first scene loads, `DisplaySettings` applies the saved volume, quality, fullscreen mode, window size and brightness. The menu does not need to be opened.
- **Menu controls start from the real settings.** When the menu opens, each control is set from what is actually in effect, so pressing Apply never changes something the player did not touch.
- **Quality.** The menu shows Low, Medium, High and Ultra, which map to the project's quality levels Low, Medium, High and Ultra (levels 1, 2, 3 and 5). The saved value is the menu index. **vSync is forced on** for every level.
- **Fullscreen and window.** The default is a borderless fullscreen window at the screen's own size. The window is resizable, and the default windowed size is 1280 x 720.
- **Resolution list.** One entry per size (repeated refresh rates are removed). The chosen size is remembered, and only restored if the screen supports it.
- **Brightness.** The slider runs from 0.5 to 1.5. Below 1 a dark layer is drawn over everything. Above 1 a pale haze is drawn. It sits above every canvas, including menus.
- **Reset** returns to a fullscreen window at the screen's own size, Medium quality and brightness 1.
- **UI scaling.** Every canvas scales with a 1920 x 1080 reference and the **Expand** match mode, so the whole design area is always visible, including on ultrawide screens.
- **Stored in** `PlayerPrefs` (on Windows, the registry under `HKEY_CURRENT_USER\Software\`, in a folder named after the company and product): `masterVolume`, `masterQuality`, `masterFullscreen`, `masterBrightness`, `masterResWidth`, `masterResHeight`.

`Menu/LoadPrefs.cs` is an older loader. It is switched off in the main menu scene and is no longer needed.

## 6. Audio

`Menu/MusicManager.cs` is created once and survives scene changes. It picks the track for the current scene **by build index**, from a list set in the main menu scene:

| Scenes | Track |
| --- | --- |
| MainMenu | *Uprising* |
| Home, Office | *Morning Routine [Lofi Study]* |
| Mansion Front, every `lvl` floor | *Unseen Horrors* |
| Every `lobby` | *Echoes of Time v2* |
| GoodEnd | *RiseAbove* |
| BadEnd | *Bewitched Forest* |

The tracks are imported as **Streaming** at 70% quality, so only the playing track is held in memory. Because the list is indexed by build position, adding or moving a scene shifts every track after it.

Chests give a **proximity cue**: a soft chime made in code plays faster and louder as the player walks toward an unopened chest (settings are on `InteractableChest`: radius, interval, volume, optional clip). It stops once the chest is open.

## 7. Player and movement

- **`Player/PlayerController.cs`** reads input every frame, limits the length of the input vector (so diagonals are not faster) and applies it in the physics step. The body uses interpolation and continuous collision. When the component is turned off (for example by a dialogue) it stops the body and resets the animation.
- **`Player/PlayerAnimation.cs`** plays one of eight static or walking animations for the direction of movement.
- **`Player/PlayerManager.cs`** holds the four key counts, updates the key counters and shows short messages ("Press 'F' to interact").

## 8. Puzzle components

| Script | Purpose | Notes |
| --- | --- | --- |
| `Controller/DoorController` | A locked door that needs a key of one type (General, Gate, Store or Secret). A key is used up when the door opens | Needs an `Animator` and an `AudioClip`, or it does nothing |
| `Interact/InteractableChest` + `Menu/InteractableChestData` | Gives one key of the chest's type, or nothing for type `None` (then shows "The chest is empty.") | Both scripts on the same object |
| `Controller/LeverController` + `Controller/DoorManager` + `Controller/DoorController2` | A door lever. It opens some doors and closes others (listed by index). A camera shows the doors while it moves | Needs a non-trigger collider on each door |
| `Interact/Lever` + `Controller/GateController` | The exit lever. Walking into it opens the exit gate. `F` at the open gate loads the next scene | Gate needs an `Animator` with an `Open` parameter |
| `Interact/CodePanel` + `Player/PlayerCodePin` | A code safe. The pad shows the digits typed and opens the safe when the typed text equals the code | The entry clears when it reaches 4 digits. The "opened" state resets at the start of every scene and is saved |
| `Interact/Interactable` | Press `F` in range to run an event, for example open a door | Only works while its linked door is still closed |
| `Controller/TimedActivator` | Switches an object on after a number of seconds. Its time is the expected time (`par`) for hints; its fixed Hint Button reveal is disabled at runtime | |

Keys: General, Gate, Store and Secret keys are counted separately. Keys are used up, so a floor needs exactly as many keys as it has doors for each type.

## 9. Enemies

`AI/NPC_Controller` patrols between waypoint `Node`s on floors lvl5, lvl7 and lvl8. If the player comes within 5 units it switches to chasing, using `AI/AStarManager` to find a path to the nearest waypoint to the player. Touching the player stops time and shows the game over panel.

## 10. Authoring notes

When you add or change a floor:

1. Insert the scene at the correct position in **Build Profiles**. It must come directly before the scene it leads to.
2. Give it a `GameController` and a `SavableManager`. Without a `SavableManager` the game cannot be saved on that floor.
3. Give doors, chests and boxes **unique names** (or set their custom unique ID), because saves use them to find the object again.
4. Make sure every door has its `Animator` and `AudioClip`, and every chest has its `InteractableChestData` with the key type set.
5. Count the keys: for each of the four types, chests that give it should equal the doors that need it.
6. Add a `TimedActivator` (and the Hint Button and Panel) if the floor should have the timed hint. Its time is the expected clear time that the adaptive hints use. `HintManager` finds the Hint Button and Panel by those names.
7. If the floor has enemies, place `Node` waypoints, an `AStarManager`, and an `NPC_Controller` with the player assigned.
8. Update the music list in the main menu scene if the number of scenes changed.
