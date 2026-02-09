# Narrath Mod - Comparison with Official Examples and Public Mods

This document compares the Narrath mod implementation with official example mods from FallenOakGames/ShadowsOfForbiddenGodsModding and the CommunityLib framework.

## Sources Compared

1. **Official Minimum Example**: `CSharp_MinimumWorkedExample/` - Basic god mod structure
2. **Official Full Example**: `InsectGod_FullGodModCode/` - Complete Cordyceps Hive Mind god
3. **CommunityLib Framework**: Advanced hooks and utilities for cross-mod compatibility

## Key Findings and Fixes Applied

### ✅ FIXED: Critical Constructor Bug

**Issue Found**: `Hooks_Narrath` was calling `base()` instead of `base(map)`, breaking CommunityLib integration.

**Example Code (InsectGod)**:
```csharp
public class Hooks_Narrath : Hooks
{
    public Hooks_Narrath(Map map) : base(map)  // ✅ Correct
    {
    }
}
```

**Narrath Before**:
```csharp
public Hooks_Narrath(Map map) : base()  // ❌ Wrong - doesn't pass map to base
{
    this.map = map;
}
```

**Narrath After**: Fixed to match the pattern.

---

### ✅ FIXED: Missing God Existence Checks

**Best Practice from InsectGod**: All ModKernel lifecycle methods should check if their god is active.

**Example from InsectGod.ModCore**:
```csharp
public override void beforeMapGen(Map map)
{
    if (map.overmind.god is God_Insect == false) { return; }
    // ... mod-specific code
}

public override void onTurnEnd(Map map)
{
    if (map.overmind.god is God_Insect == false) { return; }
    // ... mod-specific code
}
```

**Narrath Before**: No god checks - would run even if another god was selected.

**Narrath After**: Added checks to:
- `afterMapGen()`
- `afterLoading()` (new method)
- `onTurnStart()`
- `onTurnEnd()`
- `populatingWorldPanicReasons()`

**Why This Matters**: Without these checks, the mod's code runs even when the player selects a different god, wasting performance and potentially causing conflicts.

---

### ✅ FIXED: Missing afterLoading() Hook

**Best Practice from InsectGod**: Re-register hooks and re-initialize state when loading a saved game.

**Example from InsectGod.ModCore**:
```csharp
public override void afterLoading(Map map)
{
    base.afterLoading(map);
    if (map.overmind.god is God_Insect == false) { return; }
    
    // Re-register event runtime fields
    // Re-register tag systems
}
```

**Narrath After**: Added `afterLoading()` to re-register CommunityLib hooks when loading saves.

---

### ⚠️ IDENTIFIED: Non-Existent CommunityLib Hooks

**Issue**: `Hooks_Narrath.cs` attempts to override methods that don't exist in CommunityLib:
- `onGetQuestUtility()` - for modifying hero quest priorities
- `onIsLocationValidForMovement()` - for trapping heroes at Stage 5 Mysteries
- `onGetHeroMoveTarget()` - for Amanuensis compulsion
- `onGetAgentThreat()` - for Kindred Recognition trait

**CommunityLib Actually Provides**:
- `onAgentAI_GetChallengeUtility()` - for agent challenge selection
- `onMoveTaken()` - after movement occurs
- `onPopulatingPathfindingDelegates()` - for pathfinding modification
- Various battle and unit hooks

**Resolution**: 
- Commented out non-existent hook overrides with clear documentation
- Added TODO notes explaining alternative implementation strategies:
  1. Use Harmony patches on quest selection logic
  2. Use `ModKernel.unitAgentAI()` for hero behavior
  3. Handle in `Quest.onComplete()` callbacks
  4. Contribute missing hooks to CommunityLib upstream

**Note**: This is a design issue, not an implementation bug. The hooks were specified in CLAUDE.md but CommunityLib doesn't provide them yet.

---

## Code Structure Comparison

### Namespace Convention

**Official Examples**:
```csharp
namespace ShadowsInsectGod        // Minimum example
namespace ShadowsInsectGod.Code   // Full example
```

**Narrath**: 
```csharp
namespace ShadowsNarrath  // ✅ Follows pattern
```

---

### ModKernel Class Name

**Official Examples**:
```csharp
public class ModCore : ModKernel  // Both examples use "ModCore"
```

**Narrath**:
```csharp
public class Kernel_Narrath : ModKernel  // Uses descriptive name
```

**Assessment**: Both approaches are valid. "ModCore" is more conventional but "Kernel_Narrath" is more descriptive.

---

### God Registration

**Official Pattern** (both examples):
```csharp
public override void onStartGamePresssed(Map map, List<God> gods)
{
    gods.Add(new God_Insect());
}
```

**Narrath**: ✅ Matches exactly (note the triple 's' in "Presssed" - this is correct!)

---

### Power Registration

**Official Pattern**:
```csharp
public override void setup(Map map)
{
    base.setup(map);
    
    powers.Add(new P_Speed(map));
    powerLevelReqs.Add(0);  // Seal requirement
}
```

**Narrath**: ✅ Matches exactly

---

### Seal Breaking

**InsectGod (Time-Based)**:
```csharp
// Uses default time-based seal breaking (no override)
```

**Narrath (Progress-Based)**:
```csharp
public override bool checkSealBreak(int sealIndex)
{
    return mysteryAdvancementCount >= SEAL_THRESHOLDS[sealIndex];
}
```

**Assessment**: Narrath's custom seal-breaking is more complex but necessary for its unique mechanics. This is a design choice, not a code quality issue.

---

### Portrait Loading

**Official Pattern**:
```csharp
public override Sprite getGodPortrait(World world)  // Takes World parameter
{
    return EventManager.getImg("insect.god_portrait.png");
}
```

**Narrath**: ✅ Matches exactly (including the `World world` parameter)

---

### mod_desc.json Format

**Official Example**:
```json
{
    "displayedName": "Insect God",
    "prefix": "insect",
    "description": "...",
    "versionsSupported": ["0.11"],
    "modCredit": "Fallen Oak Games"
}
```

**Narrath**:
```json
{
    "displayedName": "Narrath, That Which Was Half-Spoken",
    "prefix": "ShadowsNarrath",
    "description": "...",
    "versionsSupported": ["0.11", "1.0"],
    "modCredit": "Narrath Mod Team"
}
```

**Assessment**: ✅ Correct format. Note that `prefix` maps to image loading: `EventManager.getImg("prefix.filename.png")`.

---

## Advanced Patterns from InsectGod

### Pattern: Custom Challenges at Locations

**InsectGod** dynamically adds custom challenges to human settlements:
```csharp
public override void onTurnEnd(Map map)
{
    foreach (Location l in map.locations)
    {
        if (l.settlement is SettlementHuman hum)
        {
            bool hasChallenge = false;
            foreach (Challenge c in hum.customChallenges)
            {
                if (c is Ch_InfectRuler) { hasChallenge = true; break; }
            }
            if (!hasChallenge)
            {
                hum.customChallenges.Add(new Ch_InfectRuler(l));
            }
        }
    }
}
```

**Narrath**: Uses a similar pattern in `onTurnStart()` to add `Q_InvestigateMystery` quests. ✅ Good

---

### Pattern: Tutorial/Warning Messages

**InsectGod** provides helpful messages to guide players:
```csharp
public override void afterMapGenAfterHistorical(Map map)
{
    map.world.prefabStore.popMsgHint(
        map.overmind.god.getName() + " is a god focused on...",
        map.overmind.god.getName()
    );
}
```

**Narrath**: ❌ Does not have tutorial messages. **Recommendation**: Add a tutorial message explaining the Mystery mechanic.

---

### Pattern: Cheat Commands for Testing

**InsectGod** includes extensive cheat commands:
```csharp
public override void onCheatEntered(string command)
{
    if (command == "hive") { /* spawn hive */ }
    if (command == "infectPop") { /* infect population */ }
    // ... many more
}
```

**Narrath**: ❌ Does not implement cheat commands. **Recommendation**: Add testing cheats for spawning Mysteries, granting Fragments, etc.

---

### Pattern: Custom Event Runtime Fields

**InsectGod** registers custom event fields:
```csharp
if (EventRuntime.fields.ContainsKey("is_agent_cordyceps") == false)
{
    EventRuntime.fields.Add("is_agent_cordyceps", 
        new TypedField<bool>(c => c.unit is UAEN_Drone || c.unit is UAE_LateStageInfected)
    );
}
```

**Narrath**: ❌ Does not register event fields. **Assessment**: Not critical unless adding custom events.

---

## Recommendations for Future Improvements

### High Priority

1. **✅ DONE**: Fix hooks constructor bug
2. **✅ DONE**: Add god existence checks
3. **✅ DONE**: Add afterLoading() hook
4. **✅ DONE**: Document non-existent hooks issue

### Medium Priority

5. **TODO**: Add tutorial message in `afterMapGenAfterHistorical()` explaining Mysteries
6. **TODO**: Implement hero AI modification via:
   - Harmony patches for quest utility calculation
   - ModKernel.unitAgentAI() override for non-agent heroes
7. **TODO**: Add cheat commands for testing (e.g., "mystery1", "fragment3")

### Low Priority

8. **TODO**: Consider contributing missing hooks upstream to CommunityLib
9. **TODO**: Add developer comments to complex methods
10. **TODO**: Consider adding custom event runtime fields if events are added later

---

## Compatibility Assessment

### With Base Game
- ✅ Uses standard God/Power/Property/Challenge/Quest classes
- ✅ Follows official API patterns
- ✅ Save/load implemented correctly

### With CommunityLib
- ✅ Hooks class structure correct (after fix)
- ✅ Registration pattern correct
- ⚠️ Some designed hooks don't exist in CommunityLib (documented)
- ✅ Pathfinding utilities available if needed later

### With Other Mods
- ✅ No conflicts expected - uses unique class names
- ✅ Namespace isolation (ShadowsNarrath)
- ✅ God check pattern prevents cross-contamination
- ✅ Fragment tracking in separate dictionary (no global state pollution)

---

## Code Quality Summary

**Strengths**:
- Well-structured, follows official patterns closely
- Comprehensive save/load support
- Good use of constants for tuning
- Clear separation of concerns (Powers/, Agents/, Properties/, etc.)
- Defensive null checking in most places

**Improvements Made**:
- Fixed critical hooks constructor bug
- Added god existence checks throughout
- Added afterLoading() hook for save compatibility
- Documented non-existent CommunityLib hooks

**Remaining Gaps** (not bugs, but enhancements):
- No tutorial messages for players
- No cheat commands for testing
- Hero AI modification needs alternative implementation
- Could benefit from more inline documentation

---

## Conclusion

After comparing with official examples and CommunityLib, the Narrath mod follows best practices closely. The critical bugs found (hooks constructor, missing god checks) have been fixed. The non-existent hooks issue is a design constraint, not a code quality problem - the functionality will need to be implemented through alternative means (Harmony patches or ModKernel overrides).

The mod is well-architected and should be compatible with the base game, CommunityLib, and other mods.
