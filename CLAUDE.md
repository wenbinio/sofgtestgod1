# CLAUDE.md — Narrath, That Which Was Half-Spoken

## Project Overview

You are implementing a new Elder God mod for **Shadows of Forbidden Gods** (SoFG), a turn-based strategy game where the player controls an Elder God trying to bring about the apocalypse in a fantasy world. The game is built in Unity/C# and has full DLL modding support.

This mod adds **Narrath, That Which Was Half-Spoken** — an Elder God whose core mechanic inverts the game's usual dynamic: instead of creating threats that heroes counter, Narrath creates **Mysteries** that heroes are compelled to **investigate**, and their investigation is itself the vector of apocalypse. The more competent the heroes, the faster they destroy themselves.

Narrath is an incomplete cosmic utterance — a sentence begun at the dawn of reality that was never finished. Those who encounter fragments of it cannot resist trying to complete it, and the pursuit of that completion consumes them.

---

## Technical Environment

### Game Framework
- **Engine:** Unity (C#, .NET Framework 4.x)
- **Modding:** DLL mods compiled against the game's `Assembly-CSharp.dll`
- **Entry Point:** Subclass `ModKernel` — override lifecycle hooks like `beforeMapGen()`, `afterMapGen()`, `onTurnStart()`, `onTurnEnd()`, `populatingWorldPanicReasons()`, etc.
- **Namespace convention:** All mod code in a single namespace (e.g., `ShadowsNarrath`)

### Key References
- **Official modding repo:** https://github.com/FallenOakGames/ShadowsOfForbiddenGodsModding — clone this as the project template. It contains an example mod with the correct .csproj structure, references, and build configuration.
- **DLL Modding Guide (Wiki):** https://shadows-of-forbidden-gods.fandom.com/wiki/DLL_Modding_Guide — Lessons 1–4 walk through setting up Visual Studio, reading base game code via Object Browser, creating Powers, and registering them with ModKernel. **Read all four lessons before writing any code.**
- **CommunityLib:** https://github.com/ilikegoodfood/CommunityLib — Required dependency. Provides Harmony-based hooks, universal agent AI framework (`AIChallenge`, `AITask`), arbitrary pathfinding, and cross-mod interop.

### Dependencies
1. **Assembly-CSharp.dll** — The game's compiled code. Add as a reference. Located in the game's `Managed/` folder. Use the Object Browser in Visual Studio to explore its classes (under `Assets.Code`).
2. **CommunityLib.dll** — Add as a reference. Register hooks by creating a class inheriting `CommunityLib.Hooks` and calling `RegisterHooks()` on the CommunityLib ModKernel during `beforeMapGen()` or when loading a game. **CommunityLib must be loaded before this mod in the load order.**
3. **0Harmony.dll** — Included with CommunityLib. Used for patching base game methods when hooks aren't available.

### Build Output
- Compile to a single DLL (e.g., `Narrath.dll`)
- Place alongside `mod_desc.json`, `preview.png`, and an `Art/` folder containing all image assets
- The game's mod loader reads `mod_desc.json` for metadata and loads the DLL at runtime

### Key Base Game Classes to Study (via Object Browser)
Before writing code, use Visual Studio's Object Browser on `Assembly-CSharp.dll` to study:
- `God` — Base class for Elder Gods. Look at how existing gods define seals, powers, starting agents.
- `Power` — Base class for god abilities. Study `P_EyesInShadow` (She Who Will Feast) as the simplest example.
- `Challenge` — Base class for agent challenges. Study `Ch_Infiltrate` for structure.
- `Property` — Location modifiers. Study how Shadow and Madness are implemented.
- `UA` (Unit Agent) — Agent base class. Study `UA_Supplicant` for starting agents.
- `Person` — Character data. Holds personality traits, stats, relationships.
- `Location` — Settlement data. Holds properties, modifiers, connected locations.
- `Map` — World data. Access to all locations, characters, relationships.
- `ModKernel` — All available lifecycle hooks. Read every method signature.

---

## Design Specification

### God Definition: Narrath

**Seal structure:** 9 seals (standard). **However**, Narrath's seals do not break on a fixed timer. Seals break based on **cumulative Mystery stage advancement** — each time any Mystery on the map advances one stage (by any means), it generates progress toward the next seal. Track this with an integer counter on the God class; define thresholds per seal.

Suggested thresholds (tune during testing):
- Seal 1: 3 total stage advancements
- Seal 2: 8
- Seal 3: 15
- Seal 4: 25
- Seal 5: 40
- Seal 6: 60
- Seal 7: 85
- Seal 8: 115
- Seal 9: 150

This means an aggressive hero force that investigates Mysteries quickly will **accelerate** Narrath's awakening. A passive world that ignores Mysteries keeps Narrath sealed longer but allows Mysteries to fester and spread.

**Starting Power:** 1 (max 10 at full awakening). Each seal break grants +1 max power and +1 current power.
**Agent Recruitment Points:** 2 at start.
**Starting Agent:** The Archivist (custom Supplicant variant).

---

### Core System 1: Mysteries (Location Modifiers)

Mysteries are the backbone of the mod. Implement as **location properties/modifiers** with an internal `stage` integer (1–5).

#### Implementation Approach

Create `Property_Mystery` extending the base property class. Each instance tracks:
- `int stage` (1–5)
- `int investigationProgress` — accumulates from hero quest activity; when it hits a threshold, `stage` increments
- Reference to the parent `God_Narrath` instance (to report stage advancements for seal-breaking)

**Stage thresholds for advancement** (investigation progress required):
- Stage 1 → 2: 50 progress
- Stage 2 → 3: 80 progress
- Stage 3 → 4: 120 progress
- Stage 4 → 5: 200 progress

Heroes investigating a Mystery add progress based on their Lore stat (e.g., `progress += hero.lore * 5` per turn spent investigating). This means high-Lore heroes advance Mysteries faster — which is thematically perfect and creates interesting decisions.

#### Stage Effects

Each stage should apply escalating effects to the location. Implement in `Property_Mystery.turnTick()` or equivalent lifecycle method:

**Stage 1 (The Oddity):**
- Small menace increase at the location (+0.5/turn) to attract hero attention
- Flavor text: strange occurrences, inexplicable inscriptions
- The hero AI should treat this as a low-priority threat — comparable to a minor bandit camp

**Stage 2 (The Pattern):**
- Madness +0.5/turn at the location
- Medium menace to attract stronger heroes
- Investigating hero loses 1 sanity upon the stage advancing
- Adjacent locations gain a subtle "whisper" modifier (visual only, no mechanical effect yet — foreshadowing)

**Stage 3 (The Revelation):**
- Madness +1.0/turn at the location
- The hero who causes this advancement becomes a **Seeker** (Fragment 3 — see below)
- Mystery radiates: spawn Stage 1 Mysteries at up to 2 connected locations that don't already have Mysteries (use `Palimpsest` power logic)
- All heroes/rulers at this location gain Fragment 1

**Stage 4 (The Threshold — Resonance Point):**
- Madness +2.0/turn
- All heroes/rulers who **pass through** this location (not just end turn) gain Fragment 1 automatically
- Location ruler begins erratic behavior: implement as random personality trait flips (1 trait flipped per ~20 turns)
- Population decline: -1% per turn (people become catatonic)
- Contributes to apocalypse score at a low rate

**Stage 5 (The Mouth):**
- Madness +3.0/turn
- Heroes at this location cannot voluntarily leave (override their movement AI — use CommunityLib pathfinding hooks to exclude this location from valid movement destinations for non-agent heroes present here)
- All connected locations gain Fragment 1 exposure per turn for any heroes/rulers present
- Significant apocalypse score contribution (+2/turn, tune as needed)
- Population decline: -3% per turn

#### Mystery-Related Hero Quests

The hero AI needs to treat Mysteries as quests. This is critical — without this, the entire mechanic doesn't function.

**Approach:** Create a custom quest class `Q_InvestigateMystery` that:
- Is generated at any location with `Property_Mystery`
- Has priority scaling with Mystery stage (higher stage = higher priority = stronger heroes sent)
- When a hero undertakes this quest, they spend turns at the location (complexity based on stage: Stage 1 = 30, Stage 2 = 50, Stage 3 = 80, Stage 4 = 100)
- Upon quest completion, the hero adds investigation progress equal to `complexity_completed * (hero.lore / 3)`
- **The quest does NOT remove the Mystery.** It advances it. The hero AI does not know this — it believes it's resolving a threat.
- After completing the quest, the investigating hero gains Fragments based on Mystery stage at time of completion (+1 Fragment per stage, minimum Fragment 1)

Register this quest in `ModKernel.onTurnStart()` by scanning all locations with `Property_Mystery` and ensuring they have an active `Q_InvestigateMystery`.

---

### Core System 2: Fragments (Character Modifiers)

Fragments track how deeply a character has been exposed to Narrath's utterance. Implement as a **per-character integer** tracked on `Person` objects using a `Dictionary<Person, int>` on the `God_Narrath` or `Kernel_Narrath` class. Alternatively, if the game supports custom character properties/modifiers, use that system.

**Fragment count: 0 to 5.**

Process Fragment effects in `onTurnEnd()` by iterating all tracked characters:

**Fragment 0:** No effect (character not tracked).

**Fragment 1:**
- -1 to all stats (Command, Lore, Intrigue, Might — check base game stat names via Object Browser)
- Hero AI: slightly increased priority for `Q_InvestigateMystery` quests (modify quest utility calculation if possible via CommunityLib hooks)

**Fragment 2:**
- -2 to all stats
- **Lateral spread:** Any hero or ruler sharing a location with a Fragment 2+ character gains Fragment exposure. Implement as: per turn, if a Fragment 2+ character shares a location with a Fragment 0 character, the Fragment 0 character has a 15% chance of gaining Fragment 1. For Fragment 3+, increase to 25%.
- Hero strongly prioritizes Mystery investigation over other quests

**Fragment 3 (Seeker):**
- Abandons non-Mystery quests entirely. Override hero quest selection: if a Mystery exists anywhere on the map, the Seeker will pathfind to it. Use CommunityLib's pathfinding to find the nearest Mystery.
- +2 Lore, +2 Command (net effect with the -2 penalty: Lore and Command are unchanged, but Intrigue and Might are at -2). The knowledge is real — Seekers are better at investigation even as they deteriorate.
- Sanity drains by 1/turn passively
- Lateral Fragment spread rate increased: 25% base, +5% per turn spent in same location as a non-Seeker

**Fragment 4 (The Almost-Spoken):**
- Barely functional: -4 to Intrigue and Might (but Lore and Command still boosted)
- **Silencing:** Once per ~30 turns, if this character shares a location with another hero, they can "Silence" them — the target hero loses their next turn (stunned) and gains +1 Fragment. Implement as a flag on the target that skips their action in `onTurnStart()`.
- Lateral spread is now automatic: any character sharing a location gains +1 Fragment per 10 turns of exposure, no chance roll.

**Fragment 5 (Completion):**
- The character is **erased.** Remove them from the game (death, but flavored as disappearance).
- Their location immediately gains a Stage 3 Mystery (or advances an existing Mystery by 1 stage).
- Every character who had this person as a liked/loved person (check `Person.relations` or equivalent) gains +1 Fragment from grief/shock.
- Report the seal-advancement progress to `God_Narrath`.

#### Fragment Persistence
Fragments should persist across saves. Use the modding framework's save/load hooks to serialize the Fragment dictionary. Check `ModKernel.onSave()` and `ModKernel.onLoad()` or equivalent.

#### Chosen One Interaction
The Chosen One is partially resistant to Fragments:
- Immune to Fragment gain from Stage 1 and Stage 2 Mysteries
- Immune to ambient Fragment exposure (the Whisper power and passing through low-stage Mysteries)
- **NOT immune** to Fragment gain from Stage 3+ Mystery investigation, from Seeker lateral spread, or from Fragment 5 completions of characters they knew
- This means the Chosen One can only be corrupted indirectly, through their own allies — exactly the intended dynamic

---

### Powers

Implement each as a class extending `Power`. Register them in the `God_Narrath` constructor, gated by seal number.

#### P_Whisper (Seal 0 — Available from start)
- **Cost:** 1 Power
- **Target:** Any hero on the map (no proximity requirement — Narrath reaches through dreams)
- **Effect:** Target gains Fragment 1. If they already have Fragment 1+, no effect. (Consider: at Seal 5+, this upgrades to granting Fragment 2 if target already has Fragment 1.)
- **Implementation:** Simple — find the target Person, increment their Fragment count in the tracking dictionary.

#### P_Palimpsest (Seal 2)
- **Cost:** 2 Power
- **Target:** Any location with an existing Mystery
- **Effect:** Spawn a Stage 1 Mystery at one connected location (player chooses or auto-selects the connected location with the highest population that doesn't already have a Mystery). The new Mystery is a "copy" of the original's resonance.
- **Implementation:** Find connected locations via `Location.links` or equivalent. Filter for those without `Property_Mystery`. Create and attach a new `Property_Mystery` at stage 1.

#### P_CompelInvestigation (Seal 3)
- **Cost:** 2 Power
- **Target:** Any hero on the map
- **Effect:** The target hero immediately drops their current quest and reprioritizes to the nearest Mystery. Override their quest assignment for the next N turns (3–5 turns). Use CommunityLib hooks to modify hero quest selection.
- **Implementation:** Set a flag on the target that your quest-priority hook reads. For the duration, `Q_InvestigateMystery` has utility = MAX for this hero.

#### P_Redaction (Seal 5)
- **Cost:** 3 Power
- **Target:** Any Seeker (Fragment 3+)
- **Effect:** Remove one personality trait from the target (player chooses or random). The removed trait is transferred to a random ruler connected to the Seeker's current location. The Seeker loses depth; the ruler gains unexpected personality shifts.
- **Implementation:** Access `Person.likes` / `Person.dislikes` or equivalent personality system. Remove one entry from the Seeker. Add it to a random connected ruler.
- **Strategic use:** Transfer a "dislikes combat" trait from a passive Seeker to an aggressive king, making him suddenly pacifist. Or transfer "loves gold" to a frugal ruler, destabilizing their economy.

#### P_Glossolalia (Seal 6)
- **Cost:** 3 Power
- **Target:** Any Resonance Point (Stage 4+ Mystery)
- **Effect:** All Seekers (Fragment 3+) on the map gain +1 Fragment, regardless of distance. The Resonance Point pulses — all adjacent locations gain +10% Madness.
- **Implementation:** Iterate the Fragment dictionary, find all entries with value >= 3, increment by 1. Process any new Fragment 5 completions immediately.

#### P_Unwriting (Seal 7)
- **Cost:** 4 Power
- **Target:** A settlement (location)
- **Effect:** Erase one aspect of the settlement. Present the player with options (or auto-select):
  - **Erase Ruler's Claim:** Causes a succession crisis. Remove the current ruler; the game's AI will generate political instability.
  - **Erase a Trade Route:** Permanently destroy one trade route connected to this settlement.
  - **Erase a Defensive Structure:** Remove the settlement's fortification bonus (if applicable).
- **Implementation:** This interacts with base game systems. Study how the game handles ruler removal, trade route deletion, and fortification via the Object Browser. Use the least invasive approach.

#### P_TheCompletion (Seal 9 — Awakening)
- **Cost:** All remaining Power (minimum 5)
- **Target:** Global (no target selection)
- **Effect:** All Mysteries on the map advance 1 stage. All Seekers gain +1 Fragment. All Fragment 5 completions trigger immediately (generating new Mysteries and Fragment shockwaves). This is a single cast that begins a cascade.
- **Implementation:** Iterate all locations with `Property_Mystery`, call `advanceStage()`. Iterate all tracked characters, increment Fragments. Process completions. The cascade continues naturally in subsequent turns as new Mysteries attract new heroes and new Seekers spread new Fragments.
- **Balance note:** This should be powerful but not auto-win. If the player has only seeded 3–4 Mysteries and 5 Seekers, the cascade fizzles after a few turns. If they've seeded 15+ Mysteries and 20+ Seekers, it's the apocalypse. The power is a multiplier of existing groundwork.

---

### Agents

#### The Archivist (Starting Agent)

Extend `UA` (or the Supplicant base class — check which existing gods use). Register as the starting agent in `God_Narrath`.

**Stats:** High Lore (4), moderate Intrigue (3), low Command (1), low Might (1). The Archivist is a scholar, not a warrior.

**Unique Traits:**
- `Trait_CuratorOfGaps`: Enables the challenge `Ch_PlantMystery`. This is the core agent action.
- `Trait_Annotator`: Enables the challenge `Ch_AnnotateMystery`.
- `Trait_KindredRecognition`: Seekers (Fragment 3+) will not attack or report the Archivist. Implement by hooking into the hero threat-assessment logic — when evaluating whether to attack this agent, if the hero is a Seeker, return threat = 0.

**Challenges:**

`Ch_PlantMystery`:
- **Requirement:** Agent is at an infiltrated location without an existing Mystery
- **Stat:** Lore-based
- **Complexity:** 30
- **Effect:** Attach `Property_Mystery` (Stage 1) to the location
- **Profile/Menace gain:** Low profile (+2), low menace (+3)

`Ch_AnnotateMystery`:
- **Requirement:** Agent is at a location with an existing Mystery
- **Stat:** Intrigue-based
- **Complexity:** 20
- **Effect:** Increase the Mystery's attractiveness to the hero AI. Mechanically: the `Q_InvestigateMystery` quest at this location has its priority boosted by 50% for 30 turns. This ensures stronger heroes are sent to this Mystery specifically.
- **Profile/Menace gain:** Very low (+1/+1)

**AI Behavior (using CommunityLib Universal Agent AI):**
- Primary goal: Plant Mysteries at high-value locations (large settlements, locations near heroes, locations with libraries/arcane sites)
- Secondary goal: Annotate existing Mysteries to steer hero investigation
- Avoidance: Stay away from non-Seeker heroes, the Chosen One (unless they're a Seeker)

#### The Echo (Unlocked at Seal 4)

A non-standard agent. The Echo is a **wandering fragment** — it moves randomly, cannot infiltrate, cannot fight, and cannot perform standard challenges. It is a walking infection vector.

**Stats:** All 1s. The Echo is not stat-dependent.

**Movement:** Random. Each turn, the Echo moves to a random connected location. Use CommunityLib pathfinding to select a random valid neighbor.

**Passive Effects (implement in `onTurnEnd()`):**
- Every location the Echo passes through: all heroes/rulers present gain Fragment 1 exposure (15% chance per turn of proximity)
- If the Echo enters a location with a Mystery: the Mystery advances by `+10 investigation progress` (a small but meaningful bump)
- If the Echo enters a location with a Seeker: the Seeker gains +1 Fragment

**Profile/Menace:** High profile (10 base — the Echo is conspicuous, everyone notices the whispering figure), very low menace (2 — it doesn't register as dangerous). Heroes will try to "rescue" or "investigate" the Echo, exposing themselves to Fragments.

**Combat:** If attacked, the Echo dies in one hit (1 HP). Upon death: the location gains a Stage 1 Mystery, and the killing hero gains Fragment 1. The Echo respawns at the Elder Tomb after 20 turns.

**Spawn:** Spawns automatically when Seal 4 breaks. Only one Echo at a time. If killed, begins a respawn timer.

#### The Amanuensis (Unlocked at Seal 7)

A powerful late-game agent. A Seeker who was frozen at the moment of Fragment 5 completion.

**Stats:** High Lore (5), high Command (4), low Intrigue (1), moderate Might (3).

**Unique Traits:**
- `Trait_TheLastSyllable`: All heroes within 2 hexes are compelled to move toward the Amanuensis. Override their movement AI via CommunityLib hooks: if within range and not already adjacent, set movement target to Amanuensis location. Upon arriving adjacent, gain Fragment 2 immediately.
- `Trait_Unwrite`: Enables `Ch_Unwrite` challenge.
- `Trait_Fragile`: 15 HP (very low for a late-game agent). Upon death: the death location immediately becomes a Stage 4 Mystery (or advances existing Mystery to Stage 4). All characters within 2 hexes gain +2 Fragments. **Killing the Amanuensis is a pyrrhic victory.**

**Challenges:**

`Ch_Unwrite`:
- **Requirement:** Agent is at any location
- **Stat:** Lore-based
- **Complexity:** 60 (slow, expensive)
- **Effect:** Remove one structural feature from the location: a trade route, a fortification, a holy site, or a ruler's claim. Similar to P_Unwriting but agent-driven.
- **Profile/Menace gain:** High (+5/+8) — this is a conspicuous, threatening act

**Spawn:** Spawns automatically when Seal 7 breaks. Only one Amanuensis at a time. Does NOT respawn if killed.

---

### Counterplay (Hero Quests Against Narrath)

Critical for balance and engagement. Heroes need meaningful ways to fight back, but each counter should have a cost.

#### Q_WardOfSilence
- **Trigger:** Any hero at a location with a Mystery
- **Effect:** Suppresses Mystery progression at that location. Investigation progress is halted, Fragment exposure is halved. BUT: the hero must remain stationed at the location — they cannot leave without breaking the ward.
- **Implementation:** Add a modifier to the location that zeroes investigation progress gain. Attach it to a specific hero; if the hero leaves, the modifier is removed.
- **Player counterplay:** While a hero is tied up warding, they can't investigate other threats. Spread Mysteries to force the Alliance to choose which ones to ward.

#### Q_BurnTheWritings
- **Trigger:** Any hero at a location with a Seeker (Fragment 2+)
- **Effect:** Removes the lateral Fragment spread from that Seeker for 50 turns (the writings are destroyed, but the Seeker will produce more eventually). BUT: the hero who burns the writings gains Fragment 1 from exposure.
- **Implementation:** Set a flag on the Seeker that disables their lateral spread timer. The investigating hero's Fragment count increments.

#### Q_SpeakTheCounterWord (Chosen One only)
- **Trigger:** Chosen One at a location with a Mystery
- **Effect:** Reduce the Mystery by 1 stage. If at Stage 1, remove it entirely. This is the most powerful counter but costs the Chosen One significant time (complexity 80).
- **Implementation:** Decrement the Mystery's stage. If stage reaches 0, remove `Property_Mystery`.
- **Limitation:** The Chosen One can only do this at one location at a time. Spreading Mysteries widely makes this insufficient.

#### Ruler Action: Exile the Seeker
- **Trigger:** Ruler in a settlement containing a Seeker hero
- **Effect:** The Seeker is removed from the Alliance and becomes a wandering figure. They leave the settlement and roam randomly (like the Echo, but with Fragment 3+ lateral spread).
- **Risk:** Exiled Seekers spread Fragments in the wilderness to anyone they encounter. Sometimes the cure is worse than the disease.
- **Implementation:** Check if the game supports custom ruler actions via ModKernel hooks. If not, implement as an event that fires with some probability when a Seeker is present in a ruler's settlement for extended periods.

---

### Holy Order Tenet: The Incomplete Hymn

Add a Narrath-specific tenet to Holy Orders.

- **Tenet name:** The Incomplete Hymn
- **Levels:** -2 to 0 (standard tenet range, negative = corrupted)
- **Effect at -1:** Settlements following this faith have a 5% chance per turn of any hero/ruler present gaining Fragment 1. Mysteries planted in these settlements advance 25% faster.
- **Effect at -2:** The above, plus acolytes of this faith actively seek out and spread incomplete hymns. Creates a passive Fragment infrastructure.
- **Implementation:** Follow the pattern of existing god-specific tenets (study Kishi's "exclusive Holy Order tenet" or Iastur's "Maddening Insight" tenet for structure).

---

### World Panic Integration

Override `ModKernel.populatingWorldPanicReasons()`:
- **Add:** "Inexplicable Disappearances" — triggered by Fragment 5 completions. Each completion adds a persistent panic increment.
- **Add:** "Spreading Mysteries" — once 5+ Mysteries exist on the map, add a panic source proportional to the count.
- **Remove (conditionally):** Consider suppressing "Fallen Heroes" panic for heroes who disappear via Fragment 5, since they aren't visibly killed — they just vanish. This makes Narrath stealthier than combat-oriented gods.

---

### Apocalypse Score

Narrath generates score through:
- Stage 4 Mysteries: +0.5/turn each
- Stage 5 Mysteries: +2.0/turn each
- Fragment 5 completions: +5 per completion (one-time burst)
- Settlements with a Stage 5 Mystery that reach 0 population: large one-time score burst

Define these in the god's score calculation method. Study how other gods calculate their apocalypse score via the Object Browser.

---

## File Structure

```
Narrath/
├── Kernel_Narrath.cs              # ModKernel entry point. Registers hooks,
│                                   # manages Fragment dictionary, processes
│                                   # turn-level logic.
│
├── God_Narrath.cs                 # God definition. Seal structure (progress-based),
│                                   # power list, starting agent, score calculation.
│
├── Powers/
│   ├── P_Whisper.cs               # Seal 0. Target hero, grant Fragment 1.
│   ├── P_Palimpsest.cs            # Seal 2. Copy Mystery to connected location.
│   ├── P_CompelInvestigation.cs   # Seal 3. Force hero toward nearest Mystery.
│   ├── P_Redaction.cs             # Seal 5. Remove Seeker trait, transfer to ruler.
│   ├── P_Glossolalia.cs           # Seal 6. All Seekers gain +1 Fragment.
│   ├── P_Unwriting.cs             # Seal 7. Erase settlement feature.
│   └── P_TheCompletion.cs         # Seal 9. Global cascade trigger.
│
├── Properties/
│   ├── Property_Mystery.cs        # Location modifier. Tracks stage (1-5),
│   │                               # investigation progress, per-turn effects.
│   └── Property_WardOfSilence.cs  # Hero-placed counter. Suppresses Mystery progression.
│
├── Agents/
│   ├── UA_Archivist.cs            # Starting agent. Plants and annotates Mysteries.
│   ├── UAAI_Archivist.cs          # Archivist AI (using CommunityLib Universal Agent AI).
│   ├── UA_Echo.cs                 # Seal 4 agent. Random walker, passive Fragment spreader.
│   ├── UAAI_Echo.cs               # Echo AI (random movement, simple).
│   ├── UA_Amanuensis.cs           # Seal 7 agent. Compels heroes, Unwrite challenge.
│   └── UAAI_Amanuensis.cs         # Amanuensis AI.
│
├── Challenges/
│   ├── Ch_PlantMystery.cs         # Archivist: create Mystery at infiltrated location.
│   ├── Ch_AnnotateMystery.cs      # Archivist: boost Mystery priority for hero AI.
│   └── Ch_Unwrite.cs              # Amanuensis: remove settlement feature.
│
├── Quests/
│   ├── Q_InvestigateMystery.cs    # Hero quest (generated at Mystery locations).
│   │                               # Completion advances Mystery, grants Fragments.
│   ├── Q_WardOfSilence.cs         # Hero counter-quest. Suppresses Mystery.
│   ├── Q_BurnTheWritings.cs       # Hero counter-quest. Stops Seeker spread.
│   └── Q_SpeakTheCounterWord.cs   # Chosen One counter-quest. Reduces Mystery stage.
│
├── Traits/
│   ├── Trait_CuratorOfGaps.cs     # Archivist trait: enables Ch_PlantMystery.
│   ├── Trait_Annotator.cs         # Archivist trait: enables Ch_AnnotateMystery.
│   ├── Trait_KindredRecognition.cs# Archivist trait: Seekers ignore this agent.
│   ├── Trait_TheLastSyllable.cs   # Amanuensis trait: compels nearby heroes.
│   ├── Trait_Unwrite.cs           # Amanuensis trait: enables Ch_Unwrite.
│   └── Trait_Fragile.cs           # Amanuensis trait: death spawns Stage 4 Mystery.
│
├── Hooks/
│   └── Hooks_Narrath.cs           # CommunityLib Hooks subclass. Registers all
│                                   # custom hooks for hero AI modification,
│                                   # quest priority overrides, movement restrictions.
│
├── Tenet/
│   └── H_Tenet_IncompleteHymn.cs  # Holy Order tenet. Passive Fragment spread
│                                   # and Mystery acceleration in faithful settlements.
│
├── Art/                            # All PNG assets (see Art Requirements below)
│   ├── god_narrath.png            # God selection portrait
│   ├── agent_archivist.png        # Agent portrait
│   ├── agent_echo.png             # Agent portrait
│   ├── agent_amanuensis.png       # Agent portrait
│   ├── power_whisper.png          # Power icon
│   ├── power_palimpsest.png       # Power icon
│   ├── power_compel.png           # Power icon
│   ├── power_redaction.png        # Power icon
│   ├── power_glossolalia.png      # Power icon
│   ├── power_unwriting.png        # Power icon
│   ├── power_completion.png       # Power icon
│   ├── property_mystery.png       # Location modifier icon
│   └── tenet_hymn.png             # Tenet icon
│
├── mod_desc.json                   # Mod metadata for the game's mod loader
└── preview.png                     # Steam Workshop preview image
```

---

## Implementation Order

Build and test incrementally. Each phase should produce a playable (if incomplete) mod.

### Phase 1: Skeleton (Get it loading)
1. Clone the FallenOakGames example mod repo
2. Set up `Kernel_Narrath.cs` with empty lifecycle hooks
3. Create `God_Narrath.cs` with standard 9-seal structure (use TIME-BASED seals initially for testing; switch to progress-based later)
4. Create a basic `UA_Archivist` that behaves like a default Supplicant
5. Create placeholder `P_Whisper` (target hero, print debug message)
6. Verify the mod loads, the god appears in selection, and a game can start

### Phase 2: Mysteries
1. Implement `Property_Mystery` with stage tracking and per-turn effects
2. Implement `Ch_PlantMystery` for the Archivist
3. Implement `Q_InvestigateMystery` — **this is the hardest part.** You need to create a quest that the hero AI will pick up and pursue. Study how existing quests register themselves with the quest system.
4. Test: plant a Mystery, observe heroes investigating it, observe stage advancement
5. Implement Mystery stage effects (madness, Fragment exposure, Resonance Points)

### Phase 3: Fragments
1. Implement the Fragment tracking dictionary on `Kernel_Narrath`
2. Implement Fragment stat penalties and behavior modifications
3. Implement lateral Fragment spread (Fragment 2+ characters infecting nearby characters)
4. Implement Fragment 5 completion (character erasure, Mystery spawning, grief spread)
5. Implement Seeker behavior (quest reprioritization toward Mysteries)

### Phase 4: Powers
1. Implement P_Whisper, P_Palimpsest, P_CompelInvestigation
2. Implement P_Redaction, P_Glossolalia
3. Implement P_Unwriting, P_TheCompletion
4. Switch seals from time-based to progress-based

### Phase 5: Additional Agents
1. Implement UA_Echo (random walker, passive effects)
2. Implement UA_Amanuensis (hero compulsion, Unwrite, pyrrhic death)

### Phase 6: Counterplay
1. Implement Q_WardOfSilence, Q_BurnTheWritings, Q_SpeakTheCounterWord
2. Implement Exile the Seeker ruler action (or event)
3. Implement the Incomplete Hymn tenet

### Phase 7: Polish
1. World Panic integration
2. Apocalypse score calculation
3. All flavor text and descriptions
4. Art assets (placeholder PNGs initially, then final art)
5. Save/load serialization for Fragment data and Mystery state
6. Balance testing and tuning
7. Cross-mod compatibility testing

---

## Art Requirements

All images should be PNG format. Study existing mod art styles — the game uses a dark, painterly aesthetic reminiscent of old oil paintings. Narrath's visual identity should be **literary and melancholic**: ink stains, half-written manuscripts, empty margins, silence.

- **God portrait:** An abstract representation of incompleteness — perhaps a mouth frozen mid-word, or a page with text that fades into void. Dark palette with deep blues and blacks.
- **Agent portraits:** The Archivist should look scholarly and obsessed. The Echo should appear translucent, half-there. The Amanuensis should look frozen, mouth open, eyes unfocused.
- **Power icons:** Should evoke writing, speech, silence, and erasure. Ink drops, broken quills, empty speech bubbles, redacted text.
- **Mystery icon:** An incomplete glyph or symbol — something that looks like it should mean something but doesn't quite resolve.

---

## Flavor Text Guidelines

Narrath's tone is **melancholic and literary, not visceral or horrific.** The horror is existential — the dread of incompleteness, the tragedy of people consumed by questions they can't stop asking. No body horror, no gore, no cosmic tentacles.

Write descriptions as if they were entries in a scholar's journal, growing increasingly fragmented:

- Stage 1: *"The townsfolk report hearing a sound at the edge of perception. Not a voice — half a voice. The first syllable of something."*
- Stage 3: *"She speaks now only in fragments. Not madness — she is closer to understanding than anyone alive. That is precisely the problem."*
- Fragment 5: *"They finished the sentence. Where they stood, there is nothing — not absence, but the space where meaning should be and isn't. The sentence remains unfinished."*
- The Completion: *"Everyone, everywhere, falls silent. They are listening. They almost hear it. They will never hear it. That is the apocalypse — not destruction, but the eternal, exquisite failure to understand."*

---

## Balance Targets

- **Win condition:** Achievable by turn 300–400 with competent play on difficulty 0
- **Early game (turns 1–100):** Plant 3–5 Mysteries, convert 2–3 heroes to Seekers. Should feel like slow, careful setup.
- **Mid game (turns 100–250):** Mystery network expanding, Seekers spreading Fragments laterally. 10+ Mysteries, 5–10 Seekers. Alliance should be visibly struggling.
- **Late game (turns 250+):** Resonance Points established, The Completion castable. Should feel like an unstoppable cascade if groundwork was laid well.
- **Counterplay effectiveness:** Ward of Silence and Counter-Word should meaningfully slow the player but not halt them. The Chosen One should be able to suppress 2–3 Mysteries but not 10+.
- **Hero investigation speed:** Tune so that a single hero investigating a Mystery takes ~15–25 turns to advance it one stage. This means a Stage 1 → Stage 5 progression takes 60–100 turns of continuous investigation — slow enough that you have time to set up multiple Mysteries, fast enough that heroes feel like they're making progress (and sealing their own doom).

---

## Known Technical Risks

1. **Hero quest registration:** The hero AI's quest selection system may not be trivially extensible. If `Q_InvestigateMystery` cannot be registered as a standard quest, fallback approach: create it as a custom location modifier that generates menace/threat, causing heroes to investigate the location through existing "deal with threat" quest logic. Less elegant but mechanically functional.

2. **Movement restriction (Stage 5):** Preventing heroes from leaving a location may require Harmony patching of the movement system. If CommunityLib hooks don't cover this, implement as a strong attraction instead: heroes at Stage 5 Mysteries have maximum priority for `Q_InvestigateMystery` at that location, effectively trapping them by giving them an irresistible quest rather than mechanically preventing movement.

3. **Fragment serialization:** The game's save system may not natively support custom per-character data. Options: (a) use the game's existing property/modifier system on Person objects if available, (b) serialize the Fragment dictionary as a JSON string in the mod's save data, (c) use CommunityLib's save hooks if they exist.

4. **Cross-mod compatibility:** Test with popular mods — Living Characters, Deep Ones Plus, Orcs Plus, Hero's Journey, Kishi. Narrath should not crash or break these mods. Key risk: any mod that overrides hero quest selection AI could conflict with Seeker behavior. Use CommunityLib hooks rather than direct Harmony patches where possible.

---

## Testing Checklist

- [ ] Mod loads without errors
- [ ] God appears in selection screen with correct portrait and description
- [ ] Game starts with Archivist at Elder Tomb
- [ ] Archivist can plant Mysteries at infiltrated locations
- [ ] Heroes investigate Mysteries (quest generated and pursued)
- [ ] Mystery stages advance through investigation
- [ ] Investigating heroes gain Fragments
- [ ] Fragment stat penalties apply correctly
- [ ] Seekers (Fragment 3) reprioritize to Mystery investigation
- [ ] Fragment lateral spread works (Fragment 2+ infecting nearby characters)
- [ ] Fragment 5 completion erases character and spawns Mystery
- [ ] Seals break based on Mystery advancement (not time)
- [ ] All 7 powers function correctly
- [ ] Echo spawns at Seal 4, moves randomly, spreads Fragments passively
- [ ] Amanuensis spawns at Seal 7, compels heroes, pyrrhic death works
- [ ] Hero counter-quests function (Ward, Burn, Counter-Word)
- [ ] Incomplete Hymn tenet can be adopted and has correct effects
- [ ] World Panic reasons appear correctly
- [ ] Apocalypse score accumulates from Mysteries and completions
- [ ] Game is winnable by turn 400 on difficulty 0
- [ ] Save/load preserves all custom state (Fragments, Mystery stages, seal progress)
- [ ] No crashes when run alongside CommunityLib, Living Characters, and Deep Ones Plus
