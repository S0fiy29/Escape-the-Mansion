# Game design reference

> **Spoiler warning.** This file contains the full story, the twist, every safe code and the written hints. Do not read it before playing if you want to solve the game yourself. For the technical side see [`SYSTEMS.md`](SYSTEMS.md).

- [Story](#story)
- [Characters](#characters)
- [Story by scene](#story-by-scene)
- [Endings](#endings)
- [Floors and puzzles](#floors-and-puzzles)
- [Safe codes](#safe-codes)
- [Written hint texts](#written-hint-texts-hint-1)
- [Continuity decisions](#continuity-decisions)

## Story

**Premise.** Hazil, an investigator, is asked by his contact Mr. Abraham to quietly look into the old Hayes mansion on the edge of town. People say it is abandoned and haunted, but lights and figures have been seen there. Inside he finds a mansion that does not behave like an abandoned house: doors and gates teleport him between floors, the people he meets disagree about who lives there, and his phone has no signal.

**What is really happening.**

- The mansion is trapped by a curse tied to cursed antiques. The people Hazil meets turn out to be spirits from a **famous gathering of the Hayes family in the 1970s**.
- **Alice and Cecilia are twin sisters.** Cecilia is "Milady", the daughter everyone knows. Alice was the hidden twin, kept in the basement. That is why staff say the Hayes family had three members (Mr. and Mrs. Hayes and Milady), while an old note, and the four chairs in lobby5, show it had four.
- Over the years Alice became angry and possessive. She wants to keep visitors in the mansion as "friends" forever, and she can change what people remember.
- Cecilia hid, was found and protected by the head butler Mr. Jacob, and tries to end the curse. She pretends to be Alice on some floors (lvl6, lobby7, lvl7), which is why "Alice" seems to forget things, change her mood and turn up in places she should not be. Hazil believes it is one girl whose mind is failing.
- A small group, the **Note Keepers** (three people whose identities were hidden), left notes and riddles to protect the family's history. Two were hunted down.
- **Shadows** are creatures that hate light and roam the dark floors.

**Timeline.** The gathering and the murders are in the 1970s. Hazil enters the mansion believing it is the present. When he finally gets a phone signal, Mr. Abraham tells him he has been missing for **eight years**.

## Characters

| Character | Role |
| --- | --- |
| **Hazil** | The player. An investigator who trusts his instincts. Always speaks in the player panel |
| **Mr. Abraham** | His contact. Sends him to the mansion and answers the phone call at the end |
| **Alice** | The hidden twin. Appears at lvl1 saying she has "always been here". Turns out to be the source of the nightmare |
| **Cecilia** | Alice's twin, "Milady". Impersonates Alice on some floors. Wants the curse to end. Is a spirit |
| **Elana** | A maid who joins Hazil from lobby4 to lvl6 while she looks for Milady. Says she has served the family for five years |
| **Mr. Jacob** | The head butler, met in the antique room in lvl6. Denies the mansion is abandoned or haunted, and says he does not know Alice |

On screen, the dialogue box for the disguised Cecilia shows the name **Alice**, so the player learns the truth at the same time as Hazil. Her portrait is the "eye color changed" version, which is a small clue.

## Story by scene

| Scene | What happens |
| --- | --- |
| Home | Hazil wakes at noon with a headache. Mr. Abraham calls and asks him to come to the office |
| Office | Abraham tells him about the lights, sounds and figures at the old mansion and asks him to look quietly. Hazil agrees to go that night |
| Mansion Front | He sees a light in the "abandoned" mansion. The front door is locked, so he looks for another way in |
| lobby1 | He falls into an underground basement |
| lvl1 | He meets **Alice** ("I was waiting for you", "you can't leave without me"). A note says others went missing here. No signal to call Abraham |
| lobby2 | A list of eight antiques. Alice says the basement stores her family's antiques and that she waited during hide and seek. Hazil recalls an article from decades ago. A gate teleports him |
| lvl2 | A floor of many doors and levers. Alice is impressed that he solves it. A letter asks if someone really came before |
| lobby3 | A warning about a tricky floor. Alice says there are others here, "watching", and some are not alive |
| lvl3 | He meets **Elana**, who is looking for "milady". A letter warns that the floors are traps to stop escape. Alice was not there |
| lobby4 | A dark floor. Alice has disappeared after a teleport. Elana joins him to find Alice and Milady |
| lvl4 | A note says a key choice has "no turning back". Boxes of different materials. The floor goes dark |
| lobby5 | Elana says the family has three members and denies that a murder happened. Four antique chairs do not fit that count. A safe is found |
| lvl5 | The Shadows: creatures that hate light. Elana says the head butler warned the staff. She also says the house is cursed and that people who stay forget things or remember them wrong |
| lobby6 | Hazil and Elana head for Mr. Jacob. Two beds in one room |
| lvl6 | Hazil finds "Alice" (really Cecilia) in Milady's music room. Elana brings him to Mr. Jacob, who says Milady has been found, two antiques are missing, and the Shadows appeared years after a big family gathering. Elana leaves to look after Milady |
| lobby7 | Notes about the three Note Keepers, who were hunted, and a family of four. Hazil learns that every door or gate teleports to a random floor |
| lvl7 | "Alice" does not remember Hazil. A note says everyone is dead. He decides to keep it secret |
| lobby8 | A note says there are two girls: one who leads people to escape and one who traps them |
| lvl8 | **Cecilia** reveals she is Alice's twin and Milady. Alice turns hostile when Hazil destroys the cursed antique. Alice reveals she wants to keep him. Hazil realises everyone he met is a spirit |
| GoodEnd / BadEnd | See below |

## Endings

**Good ending.** Hazil escapes outside. Cecilia tells him she is a spirit and that the trapped spirits are now free and her sister is at peace. The graves of Elana and Mr. Jacob are there. He finally gets signal and phones Mr. Abraham, who says Hazil has been missing for eight years and hangs up. Hazil vows to prove he is alive.

**Bad ending.** Hazil is back in the basement with Alice, who admits she lied about wanting to leave. She plans to keep him there forever, and to change the memories of anyone who suspects.

lvl8 is the only floor that uses `EnterNPCBE`, which loads a scene by number. That is how the ending is chosen.

## Floors and puzzles

Counts come from the scene files. "Empty" chests give nothing. A key is used up when a door opens, and a key of one type opens any door of that type.

| Floor | Chests (key given) | Locked doors (key needed) | Levers and gate | Safe | Enemies |
| --- | --- | --- | --- | --- | --- |
| lvl1 | 7: General 2, Gate 1, Store 1, empty 3 | General 2, Gate 1, Store 1 | Exit lever, gate | | |
| lvl2 | | | 9 door levers, 10 lever doors, exit lever, gate | | |
| lvl3 | 6: General 3, Gate 1, Store 1, empty 1 | General 3, Gate 1, Store 1 | 5 door levers, 5 lever doors, gate | | |
| lobby4 | 2: General 1, Store 1 | General 1, Store 1 | | | |
| lvl4 | 6: General 3, Gate 1, Store 2 | General 3, Gate 1, Store 2 | 3 door levers, 3 lever doors, gate | | |
| lobby5 | 2: Secret 1, Store 1 | Secret 1, Store 1 | | Yes | |
| lvl5 | 3: General 2, Gate 1 | General 2, Gate 1 | 2 door levers, 2 lever doors, gate | Yes | Yes |
| lobby6 | 1: Secret 1 | Secret 1 | | Yes | |
| lvl6 | 3: General 2, Secret 1 | General 1, Store 1, Secret 1 | 1 door lever, 1 lever door | Yes | |
| lvl7 | 6: General 2, Gate 1, Store 2, Secret 1 | General 2, Gate 1, Store 2, Secret 1 | 3 door levers, 3 lever doors, gate | Yes | Yes |
| lobby8 | 1: Secret 1 | Secret 1 | | Yes | |
| lvl8 | 3: General 1, Gate 1, Store 1 | General 1, Gate 1, Store 1 | 2 door levers, 2 lever doors, gate | Yes | Yes |

Scenes without puzzles: Home, Office, Mansion Front, lobby1, lobby2, lobby3 and lobby7 are story rooms.

**Key balance.** On every floor the keys from chests exactly match the doors for each type, with one exception to check: **lvl6** has a Store-key door but no chest that gives a Store key (it has two General chests for one General door). Because of the exact balance there are no spare keys, so using a key on an optional door can leave a floor unfinishable. This may be intended ("no turning back"), but players need the save slots and a hint about it.

## Safe codes

Entered on the code pad next to each safe. The pad clears after four digits.

| Floor | Code |
| --- | --- |
| lobby5 | 1974 |
| lvl5 | 53 |
| lobby6 | 4851 |
| lvl6 | 1524 |
| lvl7 | 4316 |
| lobby8 | 813 |
| lvl8 | 2975 |

## Written hint texts (hint 1)

These are the texts written for each floor's Hint Panel. They are now **hint 1** of the hint system (see [`SYSTEMS.md`](SYSTEMS.md#4-hints)); hints 2 and 3 are generated. The "Par time" is the floor's expected time before adaptation: hint 1 appears at about 35% of it for an average player (for example 105 s on lvl1). Before the hint systems were combined, the button appeared at the full time. Punctuation is simplified here.

| Floor | Par time | Hint |
| --- | --- | --- |
| lvl1 | 300 s | Try checking behind the object. You might discover something useful. |
| lvl2 | 300 s | Handle the lever with care. The right choice can guide you to a better path. |
| lvl3 | 400 s | Handle the lever with care. The right choice can guide you to a better path. |
| lvl4 | 400 s | Take some time to explore before opening the door. You might find something useful. |
| lobby5 | 300 s | The dice are crucial to solving the puzzle. Pay attention to their positions and take note of any numbers or patterns. |
| lvl5 | 500 s | To find the key, check behind objects. It might be hidden there. |
| lobby6 | 400 s | The safe vault code is: _ _ 5 _. Try to figure out the missing digits. |
| lvl6 | 500 s | Try checking the floor. It might provide some help. |
| lvl7 | 500 s | The safe vault code is: _ _ _ 6. Try to figure out the missing digits. |
| lobby8 | 400 s | Take a closer look at your surroundings. Sometimes the smallest details lead to important items. |
| lvl8 | 600 s | The dice are crucial to solving the puzzle. Pay attention to their positions and take note of any numbers or patterns. |

Two texts are repeated word for word: lvl2 and lvl3 use the same lever hint, and lobby5 and lvl8 use the same dice hint. Check that they fit each floor.

## Continuity decisions

Decisions about the story that the dialogue now follows:

- Alice and Cecilia are twins, and the family is four people. Staff talk about three because Alice was kept hidden.
- "Milady" is a title used for Cecilia. In lvl8 Cecilia says nobody ever used her name, not even her parents, and everyone just called her Milady.
- Elana sets up the curse and the memory problems in lvl5, so the later notes that mention them make sense.
- Hazil describes Alice's long silver hair to Mr. Jacob in lvl6 before Jacob repeats it.
- The "famous murder case" mentioned in the Office is set in the 1970s.

Still open: the idea that other people were here before Hazil is told several times (lvl1 note, lvl2 Alice, lvl2 letter), and "everyone is dead" is told twice (lvl7 note, lvl8). One of each could be shortened.
