# SLD (Single Line Diagram) — Connection & Layout Business Logic

> **Version:** 2.0  
> **Date:** 2026-02-15  
> **Purpose:** Defines the rules governing how electrical elements are connected, validated, laid out, and queried in the SLD drawing engine.

---

## 1. Glossary

| Term | Definition |
|------|-----------|
| **Bus** | A horizontal bar representing an electrical bus/node. Has `Tag`, `CordX`, `CordY`, `SLDX`, `SLDY`, `SLDL`, `BoardTag`, `Sec`. A bus can have *multiple* connections on both From and To sides. |
| **Branch** | An element with non-zero impedance that connects two buses: **Cable** (`CableBranch`), **Transformer**, **BusDuct**. Has `Tag`, `FromElement`, `ToElement`, `FromBus`, `ToBus`, `Category`. |
| **Non-Branch** | A zero-/negligible-impedance element: **Switch**, **Fuse**, **BusBarLink**. Has `Tag`, `FromElement`, `ToElement`. |
| **Load** | An end consumer: Motor, Heater, Capacitor, LumpLoad. Has `Tag`, `ConnectedBus`. Loads may have non-branch items in between (defined in the non-branch's `FromElement`/`ToElement`). |
| **Chain** | The ordered sequence of elements between two buses: `Bus → [NonBranch]* → Branch → [NonBranch]* → Bus`. |
| **Tier** | A horizontal row of buses sharing the same `SLDY` value. Source buses are tier 0; downstream buses have increasing tier numbers. |
| **Switchboard** | A group of buses sharing the same `BoardTag`. All buses in one switchboard occupy the **same tier** (same Y-coordinate). |

---

## 2. Element Categories & Properties

### 2.1 Common Properties (All Elements)

Every element has:
- `Tag` — unique identifier across all element types.
- `FromElement` — Tag of the element connected on the "from" (upstream) side.
- `ToElement` — Tag of the element connected on the "to" (downstream) side.

**Exception:** Buses do not have `FromElement`/`ToElement`; they are passive endpoints referenced by other elements. Loads have `ConnectedBus` instead.

### 2.2 Branch Elements

| Type | Class | Inherits | Key Extra Properties |
|------|-------|----------|---------------------|
| Cable | `CableBranch` | `Branch` | `L` (length), `CblDesc` |
| Transformer | `Transformer` | `Branch` | `V1`, `V2`, `Z`, `KVA` |
| Bus Duct | `BusDuct` | `Branch` | `L` (length), `Size` |

All branches additionally have computed `FromBus` and `ToBus` — the resolved bus Tags at each end (possibly through non-branch intermediaries).

### 2.3 Non-Branch Elements

| Type | Class | Inherits | Key Extra Properties |
|------|-------|----------|---------------------|
| Switch | `Switch` | `BaseInfo` | `IsOpen`, `SwitchType`, `VR`, `IR` |
| Fuse | `Fuse` | `BaseInfo` | `RatedCurrent` |
| Bus Bar Link | `BusBarLink` | `BaseInfo` | `RatedCurrent` |

### 2.4 Buses

| Property | Description |
|----------|-------------|
| `CordX`, `CordY` | Pixel coordinates of the bus centre |
| `SLDX` | Horizontal order (left-to-right) within the tier |
| `SLDY` | Vertical tier index (top-to-bottom, source = 0) |
| `SLDL` | Drawing length units (based on load connection count) |
| `BoardTag` | Switchboard grouping tag |
| `Sec` | Section within the switchboard (A, B, …) |
| `Cn` | List of connected bus Tags |
| `Length` | Pixel length of the bus bar drawing |

### 2.5 Loads

| Property | Description |
|----------|-------------|
| `ConnectedBus` | Tag of the bus this load is electrically connected to |
| `LoadType` | Motor, Heater, Capacitor, LumpLoad |

Loads hang downward from their connected bus. A non-branch element (switch/fuse) may sit between the load and the bus — this is defined in the non-branch's `FromElement`/`ToElement` fields, not on the load itself.

---

## 3. Connection Rules

### 3.1 Fundamental Constraints

| # | Rule |
|---|------|
| R1 | Every **branch** must have **exactly one** connection on its `FromElement` end and **exactly one** on its `ToElement` end. |
| R2 | Every **non-branch** must have **exactly one** connection on its `FromElement` end and **exactly one** on its `ToElement` end. |
| R3 | A **bus** may have **multiple** connections (no limit). |
| R4 | **Two branches cannot be directly connected** to each other, nor connected through any chain of non-branch items only (i.e., every branch must ultimately resolve to a bus on each end). |
| R5 | **No element may connect to the same bus on both ends** (no self-loops through the same bus). |
| R6 | **Bidirectional consistency:** If element A's `FromElement` = B's Tag, then B's `FromElement` or `ToElement` must = A's Tag (unless B is a Bus). |
| R7 | If a branch/non-branch end does not resolve to any existing bus, **a new bus is auto-created** to ensure every branch always has two buses. |
| R8 | Switch open/close status (`IsOpen`) is **ignored** for connection/layout purposes — a switch is always treated as a connection. The `IsOpen` flag is only used in load flow calculations. |
| R9 | **Two buses cannot be directly connected.** There must be at least one branch or non-branch element between any two buses. |

### 3.2 Valid Connection Patterns

```
Bus ←→ Branch ←→ Bus                           (simplest)
Bus ←→ NonBranch ←→ Branch ←→ Bus              (non-branch on one side)
Bus ←→ NonBranch ←→ Branch ←→ NonBranch ←→ Bus (non-branch on both sides)
Bus ←→ NB ←→ NB ←→ Branch ←→ NB ←→ Bus        (multiple non-branches in series)
Bus ←→ NonBranch ←→ Bus                         (non-branch directly between buses)
Bus ←→ NB ←→ NB ←→ Bus                          (multiple non-branches, no branch)
```

### 3.3 Invalid Connection Patterns

```
Bus ←→ Bus                       ❌ (buses cannot connect directly; need at least one branch or non-branch)
Branch ←→ Branch                 ❌ (two branches directly connected)
Branch ←→ NB ←→ Branch          ❌ (two branches through non-branches only)
Element ←→ same Element          ❌ (self-loop)
Element ←→ ... ←→ same Bus      ❌ (both ends resolve to same bus)
```

---

## 4. Connection Chain Discovery

### 4.1 Algorithm: Resolve Chain Between Two Buses

For each **branch element**, walk outward from both its `FromElement` and `ToElement` ends:

```
function ResolveChain(branch):
    fromChain = TraceTowardsBus(branch.Tag, branch.FromElement, "From")
    toChain   = TraceTowardsBus(branch.Tag, branch.ToElement, "To")
    
    fullChain = Reverse(fromChain.elements) + [branch] + toChain.elements
    fromBus   = fromChain.terminatingBus
    toBus     = toChain.terminatingBus
    
    return (fromBus, toBus, fullChain)
```

```
function TraceTowardsBus(callerTag, currentTag, direction):
    visited = {callerTag}
    elements = []
    
    while currentTag is not a Bus:
        if currentTag is null or not found → create new bus, return
        if currentTag in visited → error (cycle detected)
        
        visited.add(currentTag)
        element = lookupElement(currentTag)
        
        if element is Branch → error (branch-to-branch)
        
        elements.append(element)
        
        // Follow the "other" end of this non-branch
        nextTag = (element.FromElement == callerTag) 
                    ? element.ToElement 
                    : element.FromElement
        callerTag = currentTag
        currentTag = nextTag
    
    return (terminatingBus: currentTag, elements: elements)
```

### 4.2 Building the Full Chain List

After resolving all branches, we also discover **bus-to-bus non-branch-only chains** (e.g., switch connecting two buses directly). These are found by scanning non-branch elements whose both ends ultimately resolve to buses without passing through any branch.

### 4.3 Same-Tier Chain Detection

A chain connects **same-tier buses** when both the FromBus and ToBus have the same `SLDY` value (typically buses of the same switchboard connected by a bus coupler switch + bus duct).

---

## 5. Layout & Coordinate Assignment

### 5.1 Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `BranchSlotHeight` | 100 px | Vertical space per branch element in a chain |
| `NonBranchSlotHeight` | 80 px | Vertical space per non-branch element in a chain |
| `ElementDrawHeight` | 60 px | Visual drawing height of any element |
| `MinTierGap` | 120 px | Minimum vertical gap between two bus tiers |
| `HorizontalSameTierOffset` | 60 px | Vertical offset above bus tier Y for same-tier chains |
| `ParallelPathXOffset` | 80 px | Horizontal offset between parallel chains |
| `TopSpacing` | from GlobalData | Top margin |
| `LeftSpacing` | from GlobalData | Left margin |
| `XGridSpacing` | from GlobalData | Horizontal grid unit |

### 5.2 Bus Tier Assignment (SLDY)

1. **Source buses** (swing buses or buses with no upstream connection) → `SLDY = 0`.
2. BFS/DFS from sources: for each bus, `SLDY = parent SLDY + 1`.
3. **Switchboard constraint:** All buses with the same `BoardTag` are forced to the **same** `SLDY` (the maximum SLDY among them).

### 5.3 Dynamic Vertical Spacing Between Tiers

Instead of a fixed `yGridSpacing`, the gap between tier *i* and tier *i+1* is computed:

```
function ComputeTierGap(tierI, tierJ):
    // Find all chains connecting buses in tierI to buses in tierJ
    chains = FindChainsBetweenTiers(tierI, tierJ)
    
    if chains is empty:
        return MinTierGap
    
    // Find the longest chain (most elements)
    maxChainHeight = 0
    for each chain in chains:
        height = 0
        for each element in chain:
            if element is Branch: height += BranchSlotHeight
            else:                 height += NonBranchSlotHeight
        maxChainHeight = max(maxChainHeight, height)
    
    return max(MinTierGap, maxChainHeight + 40)  // 40px padding
```

### 5.4 Bus Y-Coordinate Calculation

```
CordY[tier 0] = TopSpacing
CordY[tier i] = CordY[tier i-1] + TierGap(i-1, i)
```

### 5.5 Bus X-Coordinate Calculation

Unchanged from existing logic:
```
CordX = LeftSpacing + (cumulative SLDL of all buses to the left in same tier) × XGridSpacing
Length = 0.5 × XGridSpacing × (SLDL - 0.5)
```

### 5.6 Chain Element Coordinate Assignment (Cross-Tier)

For a chain between `fromBus` (tier *i*) and `toBus` (tier *j*, where *j* > *i*):

```
totalSlots   = sum of slot heights for all elements in chain
startY       = fromBus.CordY + padding
availableY   = toBus.CordY - fromBus.CordY - (2 × padding)
scale        = availableY / totalSlots  (≥ 1.0; elements never compressed)

centreX      = (fromBus.CordX + toBus.CordX) / 2

currentY = startY
for each element in chain (top to bottom):
    slotH = (element is Branch) ? BranchSlotHeight : NonBranchSlotHeight
    element.CordX = centreX
    element.CordY = currentY + (slotH × scale) / 2   // centre of slot
    currentY += slotH × scale
```

### 5.7 Same-Tier Chain Element Coordinates

When both buses are on the same tier (`fromBus.SLDY == toBus.SLDY`):

```
midX = (fromBus.CordX + toBus.CordX) / 2
baseY = fromBus.CordY - HorizontalSameTierOffset

// Lay out elements horizontally
totalWidth = count(elements) × ElementDrawHeight + (count-1) × 20
startX = midX - totalWidth / 2

for each element at index i:
    element.CordX = startX + i × (ElementDrawHeight + 20) + ElementDrawHeight/2
    element.CordY = baseY
```

### 5.8 Parallel Chain Handling

When multiple chains connect the same pair of bus tiers, they are offset horizontally:

```
for chainIndex = 0 to N-1:
    xOffset = (chainIndex - (N-1)/2) × ParallelPathXOffset
    // Apply xOffset to all elements in this chain
```

---

## 6. Connectivity Lookup Functions

### 6.1 Function A — Trace Connection Chain

**Purpose:** Given any element and a direction (From/To), trace the full chain to the terminating bus. Returns all elements in order.

```csharp
/// Returns the ordered chain from the given element's specified end to the terminating bus.
/// Includes all intermediate non-branch elements and the terminating bus.
TraceResult TraceChain(string elementTag, string direction)
```

**Returns:**
- `TerminatingBusTag` — the bus at the end of the chain
- `ChainElements` — ordered list of `(Tag, Type, Category)` from the element outward to the bus
- `Success` / `ErrorMessage`

**Use cases:**
- Display connection path in UI
- Debugging / validation
- Source tracing (trace from any element back to swing source)

### 6.2 Function B — Get Valid Connection Candidates

**Purpose:** Given an element and a side (From/To), return all elements that could legally be connected on that side per the connection rules.

```csharp
/// Returns the list of elements that can legally be connected to the given element's specified end.
List<ConnectionCandidate> GetValidCandidates(string elementTag, string side)
```

**Logic:**

| Element Type | Side | Valid Candidates |
|-------------|------|-----------------|
| **Branch** | From or To | Any Bus, any Non-Branch (that has a free end and doesn't create a branch-to-branch path) |
| **Non-Branch** | From or To | Any Bus, any Branch (that has a free end on the corresponding side), any Non-Branch (that has a free end) |
| **Bus** | (either) | Any Branch or Non-Branch that has a free end |

**Exclusions (always filtered out):**
- The element itself (no self-connection)
- Any element already connected on the requested side
- Any element that would create a branch-to-branch violation
- Any element that would create a same-bus-both-ends violation
- Any element already at maximum connections (branches/non-branches: 1 per end)

**Returns:**
- List of `ConnectionCandidate`: `(Tag, Type, Category, AvailableSide)`

---

## 7. Load Layout

Loads hang **downward** from their connected bus:

1. Loads are placed below their `ConnectedBus` at `CordY = Bus.CordY + LoadOffset`.
2. Multiple loads on the same bus are distributed horizontally along the bus length.
3. If a non-branch element (switch/fuse) sits between a load and its bus (determined by the non-branch's `FromElement`/`ToElement` pointing to the load's `ConnectedBus` on one end and the load's Tag or bus on the other), that non-branch is drawn in the vertical gap between bus and load.

---

## 8. Validation Summary (Pre-Layout)

Before layout is computed, the `ConnectionProcessingService` runs:

| Check | Error Code | Description |
|-------|-----------|-------------|
| Bidirectional consistency | `BIDIRECTIONAL_MISMATCH` | A→B but B does not reference A |
| Missing connections | `MISSING_FROM_ELEMENT` / `MISSING_TO_ELEMENT` | Branch with empty From/To |
| Self-loop | `SELF_LOOP` | Element references itself |
| Same both ends | `SAME_BOTH_ENDS` | FromElement == ToElement |
| Load no bus | `LOAD_NO_BUS` | Load with empty ConnectedBus |
| Load bus missing | `LOAD_BUS_CREATED` | Load references non-existent bus (warning; bus auto-created) |

---

## 9. Data Flow Summary

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Load data from DB (buses, branches, non-branches, loads) │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. ConnectionProcessingService.ProcessAllConnections()      │
│    - Validate bidirectional consistency                      │
│    - Evaluate connections (assign FromBus/ToBus)            │
│    - Create auto-buses where needed                         │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. SLDLayoutService.BuildLayout()                           │
│    - Discover all chains (branch + non-branch sequences)    │
│    - Assign bus tiers (SLDY) with switchboard constraints   │
│    - Compute dynamic tier gaps                              │
│    - Assign bus coordinates (CordX, CordY)                  │
│    - Assign chain element coordinates                       │
│    - Handle same-tier and parallel chains                   │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Serialize to JSON → Pass to JavaScript for rendering     │
│    - Buses, Branches, NonBranches with (CordX, CordY)      │
│    - Chain info for link drawing                            │
└─────────────────────────────────────────────────────────────┘
```

---

## 10. SLDLayoutService API Summary

| Method | Input | Output | Description |
|--------|-------|--------|-------------|
| `BuildLayout(...)` | All element lists, GlobalData settings | `SLDLayoutResult` with all coordinates | Main entry: discovers chains, assigns all coordinates |
| `TraceChain(tag, direction)` | Element tag + "From"/"To" | `TraceResult` (chain elements + bus) | Traces from element to terminating bus |
| `GetValidCandidates(tag, side)` | Element tag + "From"/"To" | `List<ConnectionCandidate>` | Returns legal connection targets |
| `DiscoverChains(...)` | All element lists | `List<ElementChain>` | Finds all bus-to-bus chains |
| `ComputeTierGaps(...)` | Chains + bus tiers | `Dictionary<(int,int), int>` | Calculates dynamic vertical gaps |

---

## 11. User-Saved Coordinates (SLDComponent Table)

Positions are persisted in the `SLDComponent` SQL table (the `SLDXY` table is deprecated).

### 11.1 Schema

| Column | Type | Description |
|--------|------|-------------|
| `UID` | uniqueidentifier | Primary key (default `newid()`) |
| `ProjectId` | nvarchar(100) | Project identifier |
| `Tag` | varchar(100) | Element tag (e.g., `Bus_33kV-102`, `Cable_33kV-205`) |
| `Type` | nvarchar(100) | Element type: `swing`, `bus`, `cable`, `transformer`, `busduct`, `link`, `swbd` |
| `SLD` | nvarchar(200) | Which SLD this belongs to: `"key"` or a switchboard name |
| `PropertyJSON` | nvarchar(max) | JSON string storing position and other visual properties |
| `UpdatedBy` | nvarchar(100) | Last editor |
| `UpdatedOn` | datetime | Last update timestamp |

### 11.2 PropertyJSON Formats by Type

| Type | JSON Format | Example |
|------|-------------|---------|
| `swing` | `{"x":N,"y":N}` | `{"x":1430,"y":130}` |
| `cable` | `{"x":N,"y":N}` | `{"x":1785,"y":1235}` |
| `transformer` | `{"x":N,"y":N}` | `{"x":550,"y":650}` |
| `busduct` | `{"x":N,"y":N}` | `{"x":1180,"y":1235}` |
| `bus` | `{"position":{"x":N,"y":N},"length":N}` | `{"position":{"x":2014,"y":565},"length":112}` |
| `link` | `[{"x":N,"y":N}, ...]` | `[{"x":1345,"y":390},{"x":850,"y":390}]` |
| `swbd` | `{"x":N,"y":N}` | `{"x":421.5,"y":720}` |
| `switch` | `{"x":N,"y":N}` | `{"x":600,"y":400}` |
| `fuse` | `{"x":N,"y":N}` | `{"x":700,"y":500}` |
| `busbarlink` | `{"x":N,"y":N}` | `{"x":800,"y":600}` |

### 11.3 Override Priority

1. **Auto-layout** computes coordinates for all elements.
2. **SLDComponent saved data** overrides auto-layout if a matching entry exists (same `Tag`, `Type`, `SLD`).
3. **User drag on UI** updates SLDComponent entries via `SLDComponentUpdate` JSInvokable callback.

---

## 12. Future Considerations

- **Three-way switches** (`ThreeWaySwitch` class exists): Currently treated as standard 2-terminal non-branch. Future support for T3 connection may require a 3-port layout model.
- **Parallel chains visual optimization**: When many parallel chains exist, consider auto-grouping or collapsing.
- **User-saved coordinates**: The `SLDComponent` table stores user-adjusted positions which override auto-layout. New element types (switch, fuse, busbarlink) should also be persisted to this table when users drag them on the UI.
