using ElDesignApp.Models;
using Switch = ElDesignApp.Models.Switch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace ElDesignApp.Services.SLD
{
    // ============================================================================
    // LAYOUT CONSTANTS
    // ============================================================================

    public static class SLDLayoutConstants
    {
        public const int BranchSlotHeight = 100;      // px per branch in a chain
        public const int NonBranchSlotHeight = 80;     // px per non-branch in a chain
        public const int ElementDrawHeight = 60;       // visual height of any element
        public const int MinTierGap = 120;             // minimum vertical gap between tiers
        public const int SameTierYOffset = 60;         // Y offset above tier for same-tier chains
        public const int ParallelPathXOffset = 80;     // X offset between parallel chains
        public const int ChainPaddingY = 20;           // padding above/below chain in tier gap
        public const int LoadYOffset = 80;             // Y offset below bus for loads
        public const int LoadXSpacing = 60;            // X spacing between loads on same bus
    }

    // ============================================================================
    // ENUMS
    // ============================================================================

    public enum SLDElementCategory { Bus, Branch, NonBranch, Load }

    public enum ChainOrientation { CrossTier, SameTier }

    // ============================================================================
    // DATA CLASSES
    // ============================================================================

    #region Element Wrapper

    /// <summary>
    /// Unified wrapper for any SLD element (bus, branch, non-branch) for layout purposes.
    /// </summary>
    public class SLDElement
    {
        public string Tag { get; set; }
        public string Type { get; set; }            // "Bus", "Cable", "Transformer", "BusDuct", "Switch", "Fuse", "BusBarLink"
        public SLDElementCategory Category { get; set; }
        public string FromElement { get; set; }
        public string ToElement { get; set; }
        public int CordX { get; set; }
        public int CordY { get; set; }
        public int SlotHeight => Category == SLDElementCategory.Branch
            ? SLDLayoutConstants.BranchSlotHeight
            : SLDLayoutConstants.NonBranchSlotHeight;
    }

    #endregion

    #region Chain

    /// <summary>
    /// Represents an ordered chain of elements between two buses.
    /// </summary>
    public class ElementChain
    {
        /// <summary>Bus at the "from" (upstream) end of the chain.</summary>
        public string FromBusTag { get; set; }

        /// <summary>Bus at the "to" (downstream) end of the chain.</summary>
        public string ToBusTag { get; set; }

        /// <summary>Ordered list of element Tags from FromBus side to ToBus side.</summary>
        public List<string> ElementTags { get; set; } = new();

        /// <summary>Does this chain contain at least one branch element?</summary>
        public bool ContainsBranch { get; set; }

        /// <summary>The branch element tag (if any) in this chain.</summary>
        public string BranchTag { get; set; }

        /// <summary>Cross-tier or same-tier chain.</summary>
        public ChainOrientation Orientation { get; set; }

        /// <summary>Total slot height needed for all elements in this chain.</summary>
        public int TotalSlotHeight { get; set; }

        /// <summary>Index when multiple chains share the same bus pair (for parallel offset).</summary>
        public int ParallelIndex { get; set; }

        /// <summary>Total number of parallel chains between the same bus pair.</summary>
        public int ParallelCount { get; set; } = 1;
    }

    #endregion

    #region Trace Result

    /// <summary>
    /// Result of tracing a connection chain from an element to a terminating bus.
    /// </summary>
    public class TraceResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string StartElementTag { get; set; }
        public string Direction { get; set; } // "From" or "To"
        public string TerminatingBusTag { get; set; }

        /// <summary>Ordered elements from the start element outward to the bus (excluding the bus itself).</summary>
        public List<TraceStep> Steps { get; set; } = new();
    }

    public class TraceStep
    {
        public string Tag { get; set; }
        public string Type { get; set; }
        public SLDElementCategory Category { get; set; }
    }

    #endregion

    #region Connection Candidates

    /// <summary>
    /// A valid candidate that can be connected to an element's From or To side.
    /// </summary>
    public class ConnectionCandidate
    {
        public string Tag { get; set; }
        public string Type { get; set; }
        public SLDElementCategory Category { get; set; }

        /// <summary>Which side of the candidate is available: "From", "To", or "Any" (for buses).</summary>
        public string AvailableSide { get; set; }

        /// <summary>Human-readable reason this candidate is valid.</summary>
        public string Reason { get; set; }
    }

    #endregion

    #region Layout Result

    /// <summary>
    /// Full result from the layout engine containing coordinates for all elements.
    /// </summary>
    public class SLDLayoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        /// <summary>Buses with updated CordX, CordY coordinates.</summary>
        public List<Bus> Buses { get; set; } = new();

        /// <summary>All discovered chains between buses.</summary>
        public List<ElementChain> Chains { get; set; } = new();

        /// <summary>Coordinates assigned to chain elements (branch + non-branch): Tag → (CordX, CordY).</summary>
        public Dictionary<string, (int X, int Y)> ElementCoordinates { get; set; } = new();

        /// <summary>Dynamic vertical gaps between tiers: (tierI, tierJ) → gap in px.</summary>
        public Dictionary<(int, int), int> TierGaps { get; set; } = new();

        /// <summary>Cumulative Y-coordinate for each tier index.</summary>
        public Dictionary<int, int> TierY { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    #endregion

    // ============================================================================
    // MAIN SERVICE
    // ============================================================================

    /// <summary>
    /// Service responsible for:
    ///   1. Discovering connection chains between buses
    ///   2. Computing dynamic layout coordinates for all SLD elements
    ///   3. Providing connectivity trace and valid-candidate lookups
    /// </summary>
    public class SLDLayoutService
    {
        // ── Internal lookup maps ────────────────────────────────────────────
        private Dictionary<string, SLDElement> _elementMap = new();
        private HashSet<string> _busTags = new();
        private List<Bus> _buses;
        private List<ElementChain> _chains = new();

        // ════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ════════════════════════════════════════════════════════════════════

        #region BuildLayout

        /// <summary>
        /// Main entry point: discovers chains, computes tier gaps, assigns coordinates.
        /// Call AFTER ConnectionProcessingService.ProcessAllConnections has succeeded.
        /// </summary>
        public SLDLayoutResult BuildLayout(
            List<Bus> buses,
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts,
            List<Switch> switches,
            List<Fuse> fuses,
            List<BusBarLink> busBarLinks,
            int topSpacing,
            int leftSpacing,
            int xGridSpacing,
            Dictionary<string, int> loadConnectionCounts)
        {
            var result = new SLDLayoutResult();
            _buses = buses;

            // Step 1: Build unified element map
            BuildElementMap(buses, cables, transformers, busDucts, switches, fuses, busBarLinks);
            Console.WriteLine($"SLDLayoutService: Registered {_elementMap.Count} elements.");

            // Step 2: Discover all chains
            _chains = DiscoverAllChains();
            result.Chains = _chains;
            Console.WriteLine($"SLDLayoutService: Discovered {_chains.Count} chains.");

            // Step 3: Classify chain orientations using existing SLDY
            ClassifyChainOrientations();

            // Step 4: Compute dynamic tier gaps
            var maxTier = buses.Any() ? buses.Max(b => b.SLDY) : 0;
            var tierGaps = ComputeAllTierGaps(maxTier);
            result.TierGaps = tierGaps;

            // Step 5: Compute tier Y-coordinates
            var tierY = new Dictionary<int, int>();
            tierY[0] = topSpacing;
            for (int t = 1; t <= maxTier; t++)
            {
                var gap = tierGaps.ContainsKey((t - 1, t)) ? tierGaps[(t - 1, t)] : SLDLayoutConstants.MinTierGap;
                tierY[t] = tierY[t - 1] + gap;
            }
            result.TierY = tierY;

            // Step 6: Assign bus coordinates
            foreach (var bus in buses)
            {
                if (tierY.ContainsKey(bus.SLDY))
                {
                    bus.CordY = tierY[bus.SLDY];
                }
                else
                {
                    bus.CordY = topSpacing + bus.SLDY * SLDLayoutConstants.MinTierGap;
                }

                // X-coordinate logic (same as existing)
                var sameTierBuses = buses
                    .Where(b => b.SLDY == bus.SLDY && b.SLDX <= bus.SLDX)
                    .ToList();
                var cordX = 0;
                sameTierBuses.ForEach(item => cordX += item.SLDL);
                bus.CordX = leftSpacing + cordX * xGridSpacing;
                bus.Length = (int)(0.5 * xGridSpacing * (bus.SLDL - 0.5));
            }
            result.Buses = buses;

            // Step 7: Assign chain element coordinates
            var elementCoords = AssignChainElementCoordinates(buses);
            result.ElementCoordinates = elementCoords;

            result.Success = true;
            result.Message = $"Layout computed: {buses.Count} buses, {_chains.Count} chains, " +
                             $"{elementCoords.Count} positioned elements.";

            Console.WriteLine($"SLDLayoutService: {result.Message}");
            return result;
        }

        #endregion

        #region TraceChain (Function A)

        /// <summary>
        /// Traces from a given element's From or To end outward to the terminating bus.
        /// Returns the ordered list of intermediate elements and the bus tag.
        /// </summary>
        /// <param name="elementTag">Tag of the starting element</param>
        /// <param name="direction">"From" to trace via FromElement, "To" to trace via ToElement</param>
        public TraceResult TraceChain(string elementTag, string direction)
        {
            var result = new TraceResult
            {
                StartElementTag = elementTag,
                Direction = direction
            };

            if (!_elementMap.ContainsKey(elementTag))
            {
                result.Success = false;
                result.ErrorMessage = $"Element '{elementTag}' not found.";
                return result;
            }

            var startElem = _elementMap[elementTag];
            string nextTag = direction.Equals("From", StringComparison.OrdinalIgnoreCase)
                ? startElem.FromElement
                : startElem.ToElement;

            var visited = new HashSet<string> { elementTag };
            string previousTag = elementTag;

            while (!string.IsNullOrEmpty(nextTag))
            {
                // Check if it's a bus → done
                if (_busTags.Contains(nextTag))
                {
                    result.TerminatingBusTag = nextTag;
                    result.Success = true;
                    return result;
                }

                // Cycle detection
                if (visited.Contains(nextTag))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Cycle detected at '{nextTag}'.";
                    return result;
                }

                if (!_elementMap.ContainsKey(nextTag))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Element '{nextTag}' referenced but not found.";
                    return result;
                }

                visited.Add(nextTag);
                var current = _elementMap[nextTag];

                result.Steps.Add(new TraceStep
                {
                    Tag = current.Tag,
                    Type = current.Type,
                    Category = current.Category
                });

                // Follow the "other" end
                string followTag;
                if (current.FromElement == previousTag)
                    followTag = current.ToElement;
                else if (current.ToElement == previousTag)
                    followTag = current.FromElement;
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Element '{current.Tag}' does not reference '{previousTag}' on either end.";
                    return result;
                }

                previousTag = nextTag;
                nextTag = followTag;
            }

            // nextTag was null/empty — dead end
            result.Success = false;
            result.ErrorMessage = $"Trace ended without reaching a bus. Last element: '{previousTag}'.";
            return result;
        }

        /// <summary>
        /// Traces the full path from any element through to the source (swing) bus,
        /// going upstream via the "From" direction at each branch.
        /// </summary>
        public List<TraceResult> TraceToSource(string elementTag)
        {
            var results = new List<TraceResult>();
            var visited = new HashSet<string>();
            TraceToSourceRecursive(elementTag, "From", visited, results);
            return results;
        }

        private void TraceToSourceRecursive(string elementTag, string direction,
            HashSet<string> visited, List<TraceResult> results)
        {
            if (visited.Contains(elementTag)) return;
            visited.Add(elementTag);

            var trace = TraceChain(elementTag, direction);
            results.Add(trace);

            if (trace.Success && !string.IsNullOrEmpty(trace.TerminatingBusTag))
            {
                var bus = _buses.FirstOrDefault(b => b.Tag == trace.TerminatingBusTag);
                if (bus != null && !bus.IsSwing)
                {
                    // Find chains going upstream from this bus
                    var upstreamChains = _chains
                        .Where(c => c.ToBusTag == bus.Tag)
                        .ToList();

                    foreach (var chain in upstreamChains)
                    {
                        // The branch in this chain connects to this bus on its To side
                        // so trace from the branch's From side
                        if (!string.IsNullOrEmpty(chain.BranchTag) && !visited.Contains(chain.BranchTag))
                        {
                            TraceToSourceRecursive(chain.BranchTag, "From", visited, results);
                        }
                    }
                }
            }
        }

        #endregion

        #region GetValidCandidates (Function B)

        /// <summary>
        /// Returns the list of elements that can legally be connected to the given element's specified side.
        /// </summary>
        /// <param name="elementTag">Tag of the element being connected</param>
        /// <param name="side">"From" or "To"</param>
        public List<ConnectionCandidate> GetValidCandidates(string elementTag, string side)
        {
            var candidates = new List<ConnectionCandidate>();

            if (!_elementMap.ContainsKey(elementTag))
                return candidates;

            var sourceElem = _elementMap[elementTag];

            // Determine what the source element already connects to on the OTHER side
            string otherSideTag = side.Equals("From", StringComparison.OrdinalIgnoreCase)
                ? sourceElem.ToElement
                : sourceElem.FromElement;

            // Resolve the bus on the other side to prevent same-bus-both-ends
            string otherSideBus = ResolveEndToBus(elementTag, 
                side.Equals("From", StringComparison.OrdinalIgnoreCase) ? "To" : "From");

            foreach (var candidate in _elementMap.Values)
            {
                // Skip self
                if (candidate.Tag == elementTag) continue;

                // === Check by candidate category ===

                if (candidate.Category == SLDElementCategory.Bus)
                {
                    // Buses always accept connections (multi-connection allowed)
                    // But check same-bus-both-ends
                    if (candidate.Tag == otherSideBus)
                        continue; // Would create same-bus-both-ends

                    // RULE R9: Bus cannot connect directly to another Bus
                    // The source element must be a branch or non-branch, not a bus
                    if (sourceElem.Category == SLDElementCategory.Bus)
                        continue; // Bus-to-Bus direct connection is invalid

                    candidates.Add(new ConnectionCandidate
                    {
                        Tag = candidate.Tag,
                        Type = candidate.Type,
                        Category = candidate.Category,
                        AvailableSide = "Any",
                        Reason = "Bus (unlimited connections)"
                    });
                    continue;
                }

                if (candidate.Category == SLDElementCategory.Branch)
                {
                    // Source is also a Branch? → Not allowed (branch-to-branch)
                    if (sourceElem.Category == SLDElementCategory.Branch)
                        continue;

                    // Check if candidate has a free end
                    var freeEnds = GetFreeEnds(candidate);
                    foreach (var freeEnd in freeEnds)
                    {
                        // Verify connecting here won't create branch-to-branch via intermediate chain
                        if (sourceElem.Category == SLDElementCategory.NonBranch)
                        {
                            // Check if connecting this non-branch to a branch is valid
                            // (the non-branch's other side must not also lead to a branch without a bus in between)
                            if (WouldCreateBranchToBranch(elementTag, side, candidate.Tag))
                                continue;
                        }

                        candidates.Add(new ConnectionCandidate
                        {
                            Tag = candidate.Tag,
                            Type = candidate.Type,
                            Category = candidate.Category,
                            AvailableSide = freeEnd,
                            Reason = $"Branch with free {freeEnd} end"
                        });
                    }
                    continue;
                }

                if (candidate.Category == SLDElementCategory.NonBranch)
                {
                    // Check if candidate has a free end
                    var freeEnds = GetFreeEnds(candidate);
                    foreach (var freeEnd in freeEnds)
                    {
                        // If source is Branch, connecting to a non-branch is fine
                        // If source is NonBranch, need to verify no branch-to-branch violation
                        if (sourceElem.Category == SLDElementCategory.Branch)
                        {
                            // Verify the non-branch's other end doesn't lead to another branch
                            string nbOtherEnd = freeEnd == "From" ? candidate.ToElement : candidate.FromElement;
                            if (!string.IsNullOrEmpty(nbOtherEnd) && ResolvesToBranch(nbOtherEnd, candidate.Tag))
                                continue; // Would create branch → NB → branch
                        }

                        candidates.Add(new ConnectionCandidate
                        {
                            Tag = candidate.Tag,
                            Type = candidate.Type,
                            Category = candidate.Category,
                            AvailableSide = freeEnd,
                            Reason = $"Non-branch with free {freeEnd} end"
                        });
                    }
                }
            }

            return candidates;
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  INTERNAL METHODS
        // ════════════════════════════════════════════════════════════════════

        #region Build Element Map

        private void BuildElementMap(
            List<Bus> buses,
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts,
            List<Switch> switches,
            List<Fuse> fuses,
            List<BusBarLink> busBarLinks)
        {
            _elementMap.Clear();
            _busTags.Clear();

            foreach (var bus in buses ?? new List<Bus>())
            {
                _busTags.Add(bus.Tag);
                _elementMap[bus.Tag] = new SLDElement
                {
                    Tag = bus.Tag,
                    Type = "Bus",
                    Category = SLDElementCategory.Bus,
                    CordX = bus.CordX,
                    CordY = bus.CordY
                };
            }

            foreach (var c in cables ?? new List<CableBranch>())
            {
                _elementMap[c.Tag] = new SLDElement
                {
                    Tag = c.Tag, Type = "Cable", Category = SLDElementCategory.Branch,
                    FromElement = c.FromElement, ToElement = c.ToElement
                };
            }

            foreach (var t in transformers ?? new List<Transformer>())
            {
                _elementMap[t.Tag] = new SLDElement
                {
                    Tag = t.Tag, Type = "Transformer", Category = SLDElementCategory.Branch,
                    FromElement = t.FromElement, ToElement = t.ToElement
                };
            }

            foreach (var b in busDucts ?? new List<BusDuct>())
            {
                _elementMap[b.Tag] = new SLDElement
                {
                    Tag = b.Tag, Type = "BusDuct", Category = SLDElementCategory.Branch,
                    FromElement = b.FromElement, ToElement = b.ToElement
                };
            }

            foreach (var s in switches ?? new List<Switch>())
            {
                _elementMap[s.Tag] = new SLDElement
                {
                    Tag = s.Tag, Type = "Switch", Category = SLDElementCategory.NonBranch,
                    FromElement = s.FromElement, ToElement = s.ToElement
                };
            }

            foreach (var f in fuses ?? new List<Fuse>())
            {
                _elementMap[f.Tag] = new SLDElement
                {
                    Tag = f.Tag, Type = "Fuse", Category = SLDElementCategory.NonBranch,
                    FromElement = f.FromElement, ToElement = f.ToElement
                };
            }

            foreach (var l in busBarLinks ?? new List<BusBarLink>())
            {
                _elementMap[l.Tag] = new SLDElement
                {
                    Tag = l.Tag, Type = "BusBarLink", Category = SLDElementCategory.NonBranch,
                    FromElement = l.FromElement, ToElement = l.ToElement
                };
            }
        }

        #endregion

        #region Chain Discovery

        /// <summary>
        /// Discovers all bus-to-bus chains by starting from each branch and tracing both ends,
        /// then finds non-branch-only chains between buses.
        /// </summary>
        public List<ElementChain> DiscoverAllChains()
        {
            var chains = new List<ElementChain>();
            var processedBranches = new HashSet<string>();

            // ── Part 1: Branch-centred chains ──────────────────────────────
            foreach (var elem in _elementMap.Values.Where(e => e.Category == SLDElementCategory.Branch))
            {
                if (processedBranches.Contains(elem.Tag)) continue;
                processedBranches.Add(elem.Tag);

                var chain = BuildChainFromBranch(elem);
                if (chain != null)
                    chains.Add(chain);
            }

            // ── Part 2: Non-branch-only chains (bus ↔ NB ↔ ... ↔ bus) ────
            // Find non-branch elements not already part of any branch chain
            var usedInBranchChain = new HashSet<string>();
            foreach (var c in chains)
                foreach (var tag in c.ElementTags)
                    usedInBranchChain.Add(tag);

            foreach (var elem in _elementMap.Values.Where(e => e.Category == SLDElementCategory.NonBranch))
            {
                if (usedInBranchChain.Contains(elem.Tag)) continue;

                var chain = BuildNonBranchOnlyChain(elem, usedInBranchChain);
                if (chain != null)
                {
                    chains.Add(chain);
                    foreach (var tag in chain.ElementTags)
                        usedInBranchChain.Add(tag);
                }
            }

            // ── Part 3: Assign parallel indices ───────────────────────────
            AssignParallelIndices(chains);

            return chains;
        }

        private ElementChain BuildChainFromBranch(SLDElement branchElem)
        {
            // Trace From side
            var fromSideElements = new List<string>();
            string fromBus = TraceNonBranchesToBus(branchElem.Tag, branchElem.FromElement, fromSideElements);

            // Trace To side
            var toSideElements = new List<string>();
            string toBus = TraceNonBranchesToBus(branchElem.Tag, branchElem.ToElement, toSideElements);

            if (string.IsNullOrEmpty(fromBus) || string.IsNullOrEmpty(toBus))
            {
                Console.WriteLine($"  WARN: Chain from branch '{branchElem.Tag}' has missing bus. " +
                                  $"From='{fromBus}', To='{toBus}'");
            }

            // Build ordered element list: [fromSide reversed] + [branch] + [toSide]
            fromSideElements.Reverse();
            var elements = new List<string>();
            elements.AddRange(fromSideElements);
            elements.Add(branchElem.Tag);
            elements.AddRange(toSideElements);

            var chain = new ElementChain
            {
                FromBusTag = fromBus,
                ToBusTag = toBus,
                ElementTags = elements,
                ContainsBranch = true,
                BranchTag = branchElem.Tag
            };

            chain.TotalSlotHeight = ComputeChainSlotHeight(elements);
            return chain;
        }

        private ElementChain BuildNonBranchOnlyChain(SLDElement startElem, HashSet<string> alreadyUsed)
        {
            // Walk from startElem in both directions to find terminating buses
            var fromElements = new List<string>();
            string fromBus = TraceNonBranchesToBus(startElem.Tag, startElem.FromElement, fromElements);

            var toElements = new List<string>();
            string toBus = TraceNonBranchesToBus(startElem.Tag, startElem.ToElement, toElements);

            // Only create chain if both ends reach a bus
            if (string.IsNullOrEmpty(fromBus) || string.IsNullOrEmpty(toBus))
                return null;

            // Check none of the elements are already used
            if (fromElements.Any(t => alreadyUsed.Contains(t)) ||
                toElements.Any(t => alreadyUsed.Contains(t)))
                return null;

            fromElements.Reverse();
            var elements = new List<string>();
            elements.AddRange(fromElements);
            elements.Add(startElem.Tag);
            elements.AddRange(toElements);

            var chain = new ElementChain
            {
                FromBusTag = fromBus,
                ToBusTag = toBus,
                ElementTags = elements,
                ContainsBranch = false,
                BranchTag = null
            };

            chain.TotalSlotHeight = ComputeChainSlotHeight(elements);
            return chain;
        }

        /// <summary>
        /// From a starting element, follows the chain through non-branch elements
        /// until a bus is found. Collects intermediate non-branch tags.
        /// </summary>
        private string TraceNonBranchesToBus(string callerTag, string currentTag, List<string> intermediates)
        {
            var visited = new HashSet<string> { callerTag };

            while (!string.IsNullOrEmpty(currentTag))
            {
                // Is it a bus?
                if (_busTags.Contains(currentTag))
                    return currentTag;

                // Cycle?
                if (visited.Contains(currentTag))
                {
                    Console.WriteLine($"  WARN: Cycle detected at '{currentTag}' while tracing from '{callerTag}'.");
                    return null;
                }

                if (!_elementMap.ContainsKey(currentTag))
                {
                    Console.WriteLine($"  WARN: Element '{currentTag}' not found while tracing from '{callerTag}'.");
                    return null;
                }

                visited.Add(currentTag);
                var elem = _elementMap[currentTag];

                // If we hit another branch, stop (branch-to-branch is handled separately)
                if (elem.Category == SLDElementCategory.Branch)
                {
                    Console.WriteLine($"  WARN: Branch '{currentTag}' encountered while tracing non-branches from '{callerTag}'.");
                    return null;
                }

                intermediates.Add(currentTag);

                // Follow the other end
                string previousTag = visited.Count == 2 ? callerTag : intermediates.Count >= 2 ? intermediates[^2] : callerTag;
                // More reliable: track previous explicitly
                string nextTag;
                if (elem.FromElement == callerTag || visited.Contains(elem.FromElement))
                    nextTag = elem.ToElement;
                else
                    nextTag = elem.FromElement;

                callerTag = currentTag;
                currentTag = nextTag;
            }

            return null; // Dead end
        }

        private int ComputeChainSlotHeight(List<string> elementTags)
        {
            int total = 0;
            foreach (var tag in elementTags)
            {
                if (_elementMap.TryGetValue(tag, out var elem))
                {
                    total += elem.Category == SLDElementCategory.Branch
                        ? SLDLayoutConstants.BranchSlotHeight
                        : SLDLayoutConstants.NonBranchSlotHeight;
                }
            }
            return total;
        }

        private void ClassifyChainOrientations()
        {
            foreach (var chain in _chains)
            {
                if (string.IsNullOrEmpty(chain.FromBusTag) || string.IsNullOrEmpty(chain.ToBusTag))
                {
                    chain.Orientation = ChainOrientation.CrossTier;
                    continue;
                }

                var fromBus = _buses.FirstOrDefault(b => b.Tag == chain.FromBusTag);
                var toBus = _buses.FirstOrDefault(b => b.Tag == chain.ToBusTag);

                if (fromBus != null && toBus != null && fromBus.SLDY == toBus.SLDY)
                    chain.Orientation = ChainOrientation.SameTier;
                else
                    chain.Orientation = ChainOrientation.CrossTier;
            }
        }

        private void AssignParallelIndices(List<ElementChain> chains)
        {
            // Group chains by their bus pair (order-insensitive)
            var groups = chains
                .Where(c => !string.IsNullOrEmpty(c.FromBusTag) && !string.IsNullOrEmpty(c.ToBusTag))
                .GroupBy(c =>
                {
                    var pair = new[] { c.FromBusTag, c.ToBusTag }.OrderBy(t => t).ToArray();
                    return $"{pair[0]}|{pair[1]}";
                })
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                var list = group.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].ParallelIndex = i;
                    list[i].ParallelCount = list.Count;
                }
            }
        }

        #endregion

        #region Tier Gap Computation

        private Dictionary<(int, int), int> ComputeAllTierGaps(int maxTier)
        {
            var gaps = new Dictionary<(int, int), int>();

            for (int t = 0; t < maxTier; t++)
            {
                var crossTierChains = _chains
                    .Where(c => c.Orientation == ChainOrientation.CrossTier)
                    .Where(c =>
                    {
                        var fromBus = _buses.FirstOrDefault(b => b.Tag == c.FromBusTag);
                        var toBus = _buses.FirstOrDefault(b => b.Tag == c.ToBusTag);
                        if (fromBus == null || toBus == null) return false;

                        int minTier = Math.Min(fromBus.SLDY, toBus.SLDY);
                        int maxT = Math.Max(fromBus.SLDY, toBus.SLDY);

                        // Chain spans this tier gap
                        return minTier <= t && maxT >= t + 1;
                    })
                    .ToList();

                if (!crossTierChains.Any())
                {
                    gaps[(t, t + 1)] = SLDLayoutConstants.MinTierGap;
                    continue;
                }

                // Find the maximum chain height for chains directly between tier t and t+1
                int maxHeight = crossTierChains.Max(c => c.TotalSlotHeight);
                gaps[(t, t + 1)] = Math.Max(SLDLayoutConstants.MinTierGap,
                    maxHeight + 2 * SLDLayoutConstants.ChainPaddingY);
            }

            return gaps;
        }

        #endregion

        #region Coordinate Assignment

        private Dictionary<string, (int X, int Y)> AssignChainElementCoordinates(List<Bus> buses)
        {
            var coords = new Dictionary<string, (int X, int Y)>();

            foreach (var chain in _chains)
            {
                if (string.IsNullOrEmpty(chain.FromBusTag) || string.IsNullOrEmpty(chain.ToBusTag))
                    continue;

                var fromBus = buses.FirstOrDefault(b => b.Tag == chain.FromBusTag);
                var toBus = buses.FirstOrDefault(b => b.Tag == chain.ToBusTag);

                if (fromBus == null || toBus == null) continue;

                if (chain.Orientation == ChainOrientation.SameTier)
                    AssignSameTierCoordinates(chain, fromBus, toBus, coords);
                else
                    AssignCrossTierCoordinates(chain, fromBus, toBus, coords);
            }

            return coords;
        }

        private void AssignCrossTierCoordinates(ElementChain chain, Bus fromBus, Bus toBus,
            Dictionary<string, (int X, int Y)> coords)
        {
            // Ensure fromBus is above toBus
            Bus topBus = fromBus.CordY <= toBus.CordY ? fromBus : toBus;
            Bus bottomBus = fromBus.CordY <= toBus.CordY ? toBus : fromBus;

            int centreX = (topBus.CordX + bottomBus.CordX) / 2;

            // Apply parallel offset
            if (chain.ParallelCount > 1)
            {
                int offset = (chain.ParallelIndex - (chain.ParallelCount - 1) / 2)
                             * SLDLayoutConstants.ParallelPathXOffset;
                centreX += offset;
            }

            int startY = topBus.CordY + SLDLayoutConstants.ChainPaddingY;
            int endY = bottomBus.CordY - SLDLayoutConstants.ChainPaddingY;
            int availableY = endY - startY;

            int totalSlotH = chain.TotalSlotHeight;
            double scale = totalSlotH > 0 ? (double)availableY / totalSlotH : 1.0;
            if (scale < 1.0) scale = 1.0; // Never compress

            // Determine the correct order of elements (top to bottom)
            var orderedTags = fromBus.CordY <= toBus.CordY
                ? chain.ElementTags
                : chain.ElementTags.AsEnumerable().Reverse().ToList();

            double currentY = startY;
            foreach (var tag in orderedTags)
            {
                if (!_elementMap.TryGetValue(tag, out var elem)) continue;

                int slotH = elem.Category == SLDElementCategory.Branch
                    ? SLDLayoutConstants.BranchSlotHeight
                    : SLDLayoutConstants.NonBranchSlotHeight;

                int elemY = (int)(currentY + (slotH * scale) / 2.0);
                coords[tag] = (centreX, elemY);

                currentY += slotH * scale;
            }
        }

        private void AssignSameTierCoordinates(ElementChain chain, Bus fromBus, Bus toBus,
            Dictionary<string, (int X, int Y)> coords)
        {
            int midX = (fromBus.CordX + toBus.CordX) / 2;
            int baseY = fromBus.CordY - SLDLayoutConstants.SameTierYOffset;

            // Apply parallel offset vertically for same-tier
            if (chain.ParallelCount > 1)
            {
                int offset = chain.ParallelIndex * (SLDLayoutConstants.NonBranchSlotHeight + 10);
                baseY -= offset;
            }

            int count = chain.ElementTags.Count;
            int spacing = SLDLayoutConstants.ElementDrawHeight + 20;
            int totalWidth = count * spacing - 20;
            int startX = midX - totalWidth / 2;

            for (int i = 0; i < count; i++)
            {
                var tag = chain.ElementTags[i];
                int elemX = startX + i * spacing + SLDLayoutConstants.ElementDrawHeight / 2;
                coords[tag] = (elemX, baseY);
            }
        }

        #endregion

        #region Candidate Helpers

        /// <summary>
        /// Applies user-saved coordinates from SLDComponent table, overriding auto-layout values.
        /// Call this AFTER BuildLayout() to honour user adjustments.
        /// </summary>
        /// <param name="sldComponents">SLDComponent records for the current SLD</param>
        /// <param name="sldName">SLD name filter (e.g., "key")</param>
        /// <param name="buses">Bus list to update CordX/CordY/Length</param>
        /// <param name="elementCoordinates">Chain element coordinates to update</param>
        public void ApplySavedCoordinates(
            List<SLDComponent> sldComponents,
            string sldName,
            List<Bus> buses,
            Dictionary<string, (int X, int Y)> elementCoordinates)
        {
            if (sldComponents == null || sldComponents.Count == 0) return;

            var relevantComponents = sldComponents
                .Where(c => c.SLD?.Equals(sldName, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            Console.WriteLine($"SLDLayoutService: Applying {relevantComponents.Count} saved coordinates for SLD '{sldName}'.");

            foreach (var comp in relevantComponents)
            {
                if (string.IsNullOrEmpty(comp.PropertyJSON) || string.IsNullOrEmpty(comp.Tag))
                    continue;

                try
                {
                    var type = comp.Type?.ToLowerInvariant() ?? "";

                    switch (type)
                    {
                        case "bus":
                        case "swing":
                        {
                            // Bus format: {"position":{"x":N,"y":N},"length":N}  OR  {"x":N,"y":N}
                            var bus = buses.FirstOrDefault(b => b.Tag == comp.Tag);
                            if (bus == null) break;

                            using var doc = JsonDocument.Parse(comp.PropertyJSON);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("position", out var posElem))
                            {
                                // Format: {"position":{"x":...,"y":...},"length":...}
                                bus.CordX = (int)posElem.GetProperty("x").GetDouble();
                                bus.CordY = (int)posElem.GetProperty("y").GetDouble();
                                if (root.TryGetProperty("length", out var lenElem))
                                    bus.Length = (int)lenElem.GetDouble();
                            }
                            else if (root.TryGetProperty("x", out var xElem))
                            {
                                // Format: {"x":...,"y":...}  (swing type)
                                bus.CordX = (int)xElem.GetDouble();
                                bus.CordY = (int)root.GetProperty("y").GetDouble();
                            }

                            break;
                        }

                        case "cable":
                        case "transformer":
                        case "busduct":
                        case "switch":
                        case "fuse":
                        case "busbarlink":
                        {
                            // Format: {"x":N,"y":N}
                            using var doc = JsonDocument.Parse(comp.PropertyJSON);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("x", out var xElem) &&
                                root.TryGetProperty("y", out var yElem))
                            {
                                int x = (int)xElem.GetDouble();
                                int y = (int)yElem.GetDouble();
                                elementCoordinates[comp.Tag] = (x, y);
                            }

                            break;
                        }

                        case "link":
                            // Link vertices — handled by JS, skip here
                            break;

                        case "swbd":
                            // Switchboard outline — handled by JS, skip here
                            break;

                        default:
                            Console.WriteLine($"  WARN: Unknown SLDComponent type '{comp.Type}' for tag '{comp.Tag}'.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  WARN: Failed to parse PropertyJSON for '{comp.Tag}' ({comp.Type}): {ex.Message}");
                }
            }
        }

        #endregion

        #region Candidate Helpers (Validation)

        /// <summary>
        /// Returns which ends ("From", "To") of the given element are free (null/empty).
        /// For buses, returns empty (buses don't have ends in the same sense).
        /// </summary>
        private List<string> GetFreeEnds(SLDElement elem)
        {
            var freeEnds = new List<string>();
            if (elem.Category == SLDElementCategory.Bus) return freeEnds;

            if (string.IsNullOrEmpty(elem.FromElement))
                freeEnds.Add("From");
            if (string.IsNullOrEmpty(elem.ToElement))
                freeEnds.Add("To");
            return freeEnds;
        }

        /// <summary>
        /// Resolves an element's specified end to the terminating bus tag.
        /// Returns null if it can't be resolved.
        /// </summary>
        private string ResolveEndToBus(string elementTag, string side)
        {
            var trace = TraceChain(elementTag, side);
            return trace.Success ? trace.TerminatingBusTag : null;
        }

        /// <summary>
        /// Checks if connecting sourceTag[side] to targetTag would create a branch-to-branch path.
        /// </summary>
        private bool WouldCreateBranchToBranch(string sourceTag, string side, string targetTag)
        {
            // If source is a non-branch, check if its other side leads to a branch
            if (!_elementMap.TryGetValue(sourceTag, out var source)) return false;
            if (source.Category != SLDElementCategory.NonBranch) return false;

            // Trace the other side of the source
            string otherSide = side.Equals("From", StringComparison.OrdinalIgnoreCase) ? "To" : "From";
            string otherTag = otherSide == "From" ? source.FromElement : source.ToElement;

            if (string.IsNullOrEmpty(otherTag)) return false;

            // Walk the other side to see if it leads to a branch (without hitting a bus first)
            bool otherSideHasBranch = ResolvesToBranch(otherTag, sourceTag);

            // The target is a branch?
            if (!_elementMap.TryGetValue(targetTag, out var target)) return false;
            bool targetIsBranch = target.Category == SLDElementCategory.Branch;

            // Branch-to-branch: both sides lead to branches with no bus in between
            return otherSideHasBranch && targetIsBranch;
        }

        /// <summary>
        /// Checks if following from startTag (coming from callerTag) leads to a branch
        /// before reaching a bus.
        /// </summary>
        private bool ResolvesToBranch(string startTag, string callerTag)
        {
            var visited = new HashSet<string> { callerTag };
            string currentTag = startTag;
            string previousTag = callerTag;

            while (!string.IsNullOrEmpty(currentTag))
            {
                if (_busTags.Contains(currentTag)) return false;
                if (visited.Contains(currentTag)) return false;

                if (!_elementMap.TryGetValue(currentTag, out var elem)) return false;
                if (elem.Category == SLDElementCategory.Branch) return true;

                visited.Add(currentTag);

                // Follow other end
                string nextTag = elem.FromElement == previousTag ? elem.ToElement : elem.FromElement;
                previousTag = currentTag;
                currentTag = nextTag;
            }

            return false;
        }

        #endregion
    }
}