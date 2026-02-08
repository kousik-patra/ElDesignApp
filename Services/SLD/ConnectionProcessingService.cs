using ElDesignApp.Models;
using Switch = ElDesignApp.Models.Switch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ElDesignApp.Services.SLD
{
    // ============================================================================
    // MAIN ENTRY POINT - Use this service
    // ============================================================================

    /// <summary>
    /// Main service that orchestrates Validation → Evaluation flow.
    /// Use this as the single entry point.
    /// </summary>
    public class ConnectionProcessingService
    {
        private readonly ConnectionValidationService _validationService;
        private readonly ConnectionEvaluationService _evaluationService;

        public ConnectionProcessingService()
        {
            _validationService = new ConnectionValidationService();
            _evaluationService = new ConnectionEvaluationService();
        }

        /// <summary>
        /// Main entry point - Validates THEN Evaluates (only if validation passes)
        /// </summary>
        /// <returns>ProcessingResult with Success=true if all OK, or Success=false with errors</returns>
        public ProcessingResult ProcessAllConnections(
            List<Bus> buses,
            List<Load> loads,
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts,
            List<Switch> switches,
            List<Fuse> fuses = null,
            List<BusBarLink> busBarLinks = null)
        {
            var result = new ProcessingResult();

            // ================================================================
            // PHASE 1: VALIDATION (Must pass before evaluation)
            // ================================================================
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("PHASE 1: VALIDATING CONNECTIONS (Bidirectional Consistency Check)");
            Console.WriteLine(new string('=', 70));

            bool isValid = _validationService.ValidateConnections(
                buses, loads, cables, transformers, busDucts, switches, fuses, busBarLinks);

            result.ValidationErrors = new List<ValidationError>(_validationService.Errors);
            result.ValidationWarnings = new List<ValidationWarning>(_validationService.Warnings);
            result.LoadValidationResults = new List<LoadValidationResult>(_validationService.LoadValidationResults);

            // Get buses created for loads (to add to main bus list)
            result.NewBusesCreatedForLoads = new List<Bus>(_validationService.NewBusesCreatedForLoads);
            
            if (!isValid)
            {
                // STOP! Do not proceed with evaluation
                result.Success = false;
                result.Message = $"Validation failed with {_validationService.Errors.Count} error(s). " +
                                 "Please fix the errors and try again.";

                Console.WriteLine("\n❌ PROCESSING STOPPED - Validation errors must be fixed first.\n");
                return result;
            }
            
            // Add new buses created for loads to the main bus list
            if (result.NewBusesCreatedForLoads.Any())
            {
                buses.AddRange(result.NewBusesCreatedForLoads);
                Console.WriteLine($"INFO: Added {result.NewBusesCreatedForLoads.Count} new buses created for loads.");
            }

            Console.WriteLine("\n✅ Validation passed. Proceeding to evaluation...\n");

            // ================================================================
            // PHASE 2: EVALUATION (Only runs if validation passed)
            // ================================================================
            Console.WriteLine(new string('=', 70));
            Console.WriteLine("PHASE 2: EVALUATING CONNECTIONS (Assigning FromBus/ToBus)");
            Console.WriteLine(new string('=', 70));

            // Convert to unified Branch list for evaluation service
            var branches = BuildBranchList(cables, transformers, busDucts);

            var (updatedBuses, updatedBranches) = _evaluationService.EvaluateAllConnections(
                buses, branches, switches, fuses, busBarLinks);

            // Map evaluated FromBus/ToBus back to original lists
            MapBackToOriginalLists(updatedBranches, cables, transformers, busDucts);

            result.Success = true;
            result.Buses = updatedBuses;
            result.Branches = updatedBranches;
            result.Loads = loads;
            result.LoadConnectionCounts = _validationService.LoadConnectionCounts;
            result.EvaluationAlerts = new List<EvaluationAlert>(_evaluationService.Alerts);
            result.Message = "Connection processing completed successfully.";

            Console.WriteLine("\n✅ PROCESSING COMPLETE\n");
            return result;
        }

        private List<Branch> BuildBranchList(
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts)
        {
            var branches = new List<Branch>();

            cables?.ForEach(c => branches.Add(new Branch
            {
                Tag = c.Tag,
                Category = "Cable",
                FromElement = c.FromElement,
                ToElement = c.ToElement,
                FromBus = null,
                ToBus = null,
                VRatio = 1
            }));

            transformers?.ForEach(t => branches.Add(new Branch
            {
                Tag = t.Tag,
                Category = "Transformer",
                FromElement = t.FromElement,
                ToElement = t.ToElement,
                FromBus = null,
                ToBus = null,
                VRatio = t.V1 / t.V2
            }));

            busDucts?.ForEach(b => branches.Add(new Branch
            {
                Tag = b.Tag,
                Category = "BusDuct",
                FromElement = b.FromElement,
                ToElement = b.ToElement,
                FromBus = null,
                ToBus = null,
                VRatio = 1
            }));

            return branches;
        }

        private void MapBackToOriginalLists(
            List<Branch> evaluatedBranches,
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts)
        {
            foreach (var branch in evaluatedBranches)
            {
                var cable = cables?.FirstOrDefault(c => c.Tag == branch.Tag);
                if (cable != null)
                {
                    cable.FromBus = branch.FromBus;
                    cable.ToBus = branch.ToBus;
                    continue;
                }

                var trf = transformers?.FirstOrDefault(t => t.Tag == branch.Tag);
                if (trf != null)
                {
                    trf.FromBus = branch.FromBus;
                    trf.ToBus = branch.ToBus;
                    continue;
                }

                var bd = busDucts?.FirstOrDefault(b => b.Tag == branch.Tag);
                if (bd != null)
                {
                    bd.FromBus = branch.FromBus;
                    bd.ToBus = branch.ToBus;
                }
            }
        }
    }

    
    
    
    // ============================================================================
    // VALIDATION SERVICE - Checks bidirectional consistency
    // ============================================================================

    /// <summary>
    /// Validates bidirectional consistency of connections BEFORE evaluation.
    /// Rule: If element A claims to connect to element B, then B must claim to connect to A.
    /// Exception: Buses are endpoints - they don't claim connections.
    /// </summary>
    public class ConnectionValidationService
    {
        public List<ValidationError> Errors { get; private set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; private set; } = new List<ValidationWarning>();
        public List<LoadValidationResult> LoadValidationResults { get; private set; } = new List<LoadValidationResult>();
        public List<Bus> NewBusesCreatedForLoads { get; private set; } = new List<Bus>();
        public Dictionary<string, int> LoadConnectionCounts { get; private set; } = new Dictionary<string, int>();
        
        private Dictionary<string, ElementInfo> _elementMap = new Dictionary<string, ElementInfo>();
        private HashSet<string> _busTagSet = new HashSet<string>();

        /// <summary>
        /// Validates all connections for bidirectional consistency.
        /// Returns TRUE if validation passes, FALSE if there are errors.
        /// </summary>
        public bool ValidateConnections(
            List<Bus> buses,
            List<Load> loads,
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts,
            List<Switch> switches,
            List<Fuse> fuses = null,
            List<BusBarLink> busBarLinks = null)
        {
            Errors.Clear();
            Warnings.Clear();
            LoadValidationResults.Clear();
            NewBusesCreatedForLoads.Clear();
            LoadConnectionCounts.Clear();
            _elementMap.Clear();
            _busTagSet.Clear();

            // Build bus tag set for quick lookup
            _busTagSet = buses?.Select(b => b.Tag).ToHashSet() ?? new HashSet<string>();

            // Step 1: Register all elements
            RegisterAllElements(buses, cables, transformers, busDucts, switches, fuses, busBarLinks);
            Console.WriteLine($"Registered {_elementMap.Count} elements for validation.");

            // Step 2: Validate bidirectional consistency
            Console.WriteLine("\nValidating bidirectional consistency...");
            ValidateBidirectionalConsistency();

            // Step 3: Validate no missing connections
            ValidateNoMissingConnections();

            // Step 4: Validate no self-loops
            ValidateNoSelfLoops();

            // Step 5: Validate Loads
            Console.WriteLine("\nValidating loads...");
            ValidateLoads(loads, buses);

            // Step 6: Print report
            PrintValidationReport();

            return Errors.Count == 0;
        }
        
        #region Load Validation

        /// <summary>
        /// Validates that all loads connect to valid buses.
        /// If a load references a non-existent bus, creates a new bus.
        /// Also tracks load count per bus for bus length calculation.
        /// </summary>
        private void ValidateLoads(List<Load> loads, List<Bus> buses)
        {
            if (loads == null || loads.Count == 0)
            {
                Console.WriteLine("  No loads to validate.");
                return;
            }

            Console.WriteLine($"  Validating {loads.Count} loads...");

            foreach (var load in loads)
            {
                var result = new LoadValidationResult
                {
                    LoadTag = load.Tag,
                    OriginalConnectedBus = load.ConnectedBus
                };

                // CASE 1: ConnectedBus is not defined
                if (string.IsNullOrEmpty(load.ConnectedBus))
                {
                    Errors.Add(new ValidationError
                    {
                        Code = "LOAD_NO_BUS",
                        SourceElement = load.Tag,
                        SourceType = "Load",
                        Message = $"Load '{load.Tag}' has no ConnectedBus defined."
                    });

                    result.IsValid = false;
                    result.ErrorMessage = "No ConnectedBus defined";
                    LoadValidationResults.Add(result);
                    continue;
                }

                // CASE 2: ConnectedBus does not exist - Create new bus
                if (!_busTagSet.Contains(load.ConnectedBus))
                {
                    string newBusTag = $"{load.Tag}-bus";

                    Warnings.Add(new ValidationWarning
                    {
                        Code = "LOAD_BUS_CREATED",
                        SourceElement = load.Tag,
                        SourceType = "Load",
                        TargetElement = load.ConnectedBus,
                        Message = $"Load '{load.Tag}' references non-existent bus '{load.ConnectedBus}'. " +
                                  $"Creating new bus '{newBusTag}'."
                    });

                    // Create new bus
                    var newBus = new Bus
                    {
                        Tag = newBusTag,
                        VR = 0f,
                        SC = "",
                        Cn = new List<string>(),
                        BoardTag = load.ConnectedBus,  // Use original value as board tag hint
                        IsAutoGenerated = true,
                        GeneratedFrom = load.Tag
                    };

                    NewBusesCreatedForLoads.Add(newBus);
                    _busTagSet.Add(newBusTag);

                    // Update load to point to the new bus
                    result.RequiresNewBus = true;
                    result.NewBusTag = newBusTag;
                    result.IsValid = true;

                    // Update the load's ConnectedBus to the new bus tag
                    load.ConnectedBus = newBusTag;

                    // Track load count for this bus
                    if (!LoadConnectionCounts.ContainsKey(newBusTag))
                        LoadConnectionCounts[newBusTag] = 0;
                    LoadConnectionCounts[newBusTag]++;

                    LoadValidationResults.Add(result);
                    continue;
                }

                // CASE 3: ConnectedBus exists - Valid connection
                result.IsValid = true;
                result.RequiresNewBus = false;

                // Track load count for this bus
                if (!LoadConnectionCounts.ContainsKey(load.ConnectedBus))
                    LoadConnectionCounts[load.ConnectedBus] = 0;
                LoadConnectionCounts[load.ConnectedBus]++;

                LoadValidationResults.Add(result);
            }

            // Print summary
            int validLoads = LoadValidationResults.Count(r => r.IsValid);
            int loadsWithNewBus = LoadValidationResults.Count(r => r.RequiresNewBus);
            int invalidLoads = LoadValidationResults.Count(r => !r.IsValid);

            Console.WriteLine($"  ├─ Valid loads (existing bus): {validLoads - loadsWithNewBus}");
            Console.WriteLine($"  ├─ Loads with new bus created: {loadsWithNewBus}");
            Console.WriteLine($"  └─ Invalid loads (errors): {invalidLoads}");

            // Print load counts per bus
            Console.WriteLine($"\n  Load counts per bus:");
            foreach (var kvp in LoadConnectionCounts.OrderByDescending(k => k.Value))
            {
                Console.WriteLine($"    {kvp.Key}: {kvp.Value} load(s)");
            }
        }

        #endregion

        #region Register Elements

        private void RegisterAllElements(
            List<Bus> buses,
            List<CableBranch> cables,
            List<Transformer> transformers,
            List<BusDuct> busDucts,
            List<Switch> switches,
            List<Fuse> fuses,
            List<BusBarLink> busBarLinks)
        {
            // Buses (endpoints)
            foreach (var bus in buses ?? new List<Bus>())
            {
                _elementMap[bus.Tag] = new ElementInfo
                {
                    Tag = bus.Tag,
                    Type = "Bus",
                    Category = ElementCategory.Bus,
                    FromElement = null,
                    ToElement = null
                };
            }

            // Cables
            foreach (var cable in cables ?? new List<CableBranch>())
            {
                _elementMap[cable.Tag] = new ElementInfo
                {
                    Tag = cable.Tag,
                    Type = "Cable",
                    Category = ElementCategory.Branch,
                    FromElement = cable.FromElement,
                    ToElement = cable.ToElement
                };
            }

            // Transformers
            foreach (var trf in transformers ?? new List<Transformer>())
            {
                _elementMap[trf.Tag] = new ElementInfo
                {
                    Tag = trf.Tag,
                    Type = "Transformer",
                    Category = ElementCategory.Branch,
                    FromElement = trf.FromElement,
                    ToElement = trf.ToElement
                };
            }

            // BusDucts
            foreach (var bd in busDucts ?? new List<BusDuct>())
            {
                _elementMap[bd.Tag] = new ElementInfo
                {
                    Tag = bd.Tag,
                    Type = "BusDuct",
                    Category = ElementCategory.Branch,
                    FromElement = bd.FromElement,
                    ToElement = bd.ToElement
                };
            }

            // Switches
            foreach (var sw in switches ?? new List<Switch>())
            {
                _elementMap[sw.Tag] = new ElementInfo
                {
                    Tag = sw.Tag,
                    Type = "Switch",
                    Category = ElementCategory.NonBranch,
                    FromElement = sw.FromElement,
                    ToElement = sw.ToElement
                };
            }

            // Fuses
            foreach (var fuse in fuses ?? new List<Fuse>())
            {
                _elementMap[fuse.Tag] = new ElementInfo
                {
                    Tag = fuse.Tag,
                    Type = "Fuse",
                    Category = ElementCategory.NonBranch,
                    FromElement = fuse.FromElement,
                    ToElement = fuse.ToElement
                };
            }

            // BusBarLinks
            foreach (var link in busBarLinks ?? new List<BusBarLink>())
            {
                _elementMap[link.Tag] = new ElementInfo
                {
                    Tag = link.Tag,
                    Type = "BusBarLink",
                    Category = ElementCategory.NonBranch,
                    FromElement = link.FromElement,
                    ToElement = link.ToElement
                };
            }
        }

        #endregion

        #region Bidirectional Consistency Check

        private void ValidateBidirectionalConsistency()
        {
            foreach (var element in _elementMap.Values)
            {
                if (element.Category == ElementCategory.Bus)
                    continue;

                // Check FromElement
                if (!string.IsNullOrEmpty(element.FromElement))
                {
                    CheckConnectionClaim(element, element.FromElement, "FromElement");
                }

                // Check ToElement
                if (!string.IsNullOrEmpty(element.ToElement))
                {
                    CheckConnectionClaim(element, element.ToElement, "ToElement");
                }
            }
        }

        private void CheckConnectionClaim(ElementInfo source, string targetTag, string side)
        {
            // Check if target exists
            if (!_elementMap.ContainsKey(targetTag))
            {
                // Target doesn't exist - warning (might be new bus)
                Warnings.Add(new ValidationWarning
                {
                    Code = "UNKNOWN_TARGET",
                    SourceElement = source.Tag,
                    SourceType = source.Type,
                    TargetElement = targetTag,
                    Side = side,
                    Message = $"'{source.Tag}' references {side}='{targetTag}' which does not exist. " +
                              $"A new bus will be created if intentional."
                });
                return;
            }

            var target = _elementMap[targetTag];

            // If target is Bus, that's OK (buses are endpoints)
            if (target.Category == ElementCategory.Bus)
                return;

            // Target must claim back to source
            bool claimsBack = target.FromElement == source.Tag || target.ToElement == source.Tag;

            if (!claimsBack)
            {
                // MISMATCH!
                Errors.Add(new ValidationError
                {
                    Code = "BIDIRECTIONAL_MISMATCH",
                    SourceElement = source.Tag,
                    SourceType = source.Type,
                    SourceSide = side,
                    TargetElement = target.Tag,
                    TargetType = target.Type,
                    TargetFromElement = target.FromElement,
                    TargetToElement = target.ToElement,
                    ExpectedInTarget = source.Tag,
                    Message = $"'{source.Tag}' ({source.Type}) claims {side}='{target.Tag}', " +
                              $"but '{target.Tag}' ({target.Type}) does NOT reference '{source.Tag}' back.\n" +
                              $"       '{target.Tag}' has: FromElement='{target.FromElement ?? "null"}', ToElement='{target.ToElement ?? "null"}'\n" +
                              $"       Expected: FromElement or ToElement should be '{source.Tag}'"
                });
            }
        }

        #endregion

        #region Other Validations

        private void ValidateNoMissingConnections()
        {
            foreach (var element in _elementMap.Values)
            {
                if (element.Category == ElementCategory.Bus)
                    continue;

                if (element.Category == ElementCategory.Branch)
                {
                    if (string.IsNullOrEmpty(element.FromElement))
                    {
                        Errors.Add(new ValidationError
                        {
                            Code = "MISSING_FROM_ELEMENT",
                            SourceElement = element.Tag,
                            SourceType = element.Type,
                            Message = $"Branch '{element.Tag}' has no FromElement defined."
                        });
                    }

                    if (string.IsNullOrEmpty(element.ToElement))
                    {
                        Errors.Add(new ValidationError
                        {
                            Code = "MISSING_TO_ELEMENT",
                            SourceElement = element.Tag,
                            SourceType = element.Type,
                            Message = $"Branch '{element.Tag}' has no ToElement defined."
                        });
                    }
                }

                if (element.Category == ElementCategory.NonBranch)
                {
                    if (string.IsNullOrEmpty(element.FromElement) && string.IsNullOrEmpty(element.ToElement))
                    {
                        Errors.Add(new ValidationError
                        {
                            Code = "NO_CONNECTIONS",
                            SourceElement = element.Tag,
                            SourceType = element.Type,
                            Message = $"'{element.Tag}' has no connections (both FromElement and ToElement empty)."
                        });
                    }
                }
            }
        }

        private void ValidateNoSelfLoops()
        {
            foreach (var element in _elementMap.Values)
            {
                if (element.Category == ElementCategory.Bus)
                    continue;

                if (element.FromElement == element.Tag || element.ToElement == element.Tag)
                {
                    Errors.Add(new ValidationError
                    {
                        Code = "SELF_LOOP",
                        SourceElement = element.Tag,
                        SourceType = element.Type,
                        Message = $"'{element.Tag}' references itself."
                    });
                }

                if (!string.IsNullOrEmpty(element.FromElement) && element.FromElement == element.ToElement)
                {
                    Errors.Add(new ValidationError
                    {
                        Code = "SAME_BOTH_ENDS",
                        SourceElement = element.Tag,
                        SourceType = element.Type,
                        Message = $"'{element.Tag}' has FromElement=ToElement='{element.FromElement}'."
                    });
                }
            }
        }

        #endregion

        #region Print Report

        private void PrintValidationReport()
        {
            Console.WriteLine("\n" + new string('-', 60));

            if (Errors.Count == 0 && Warnings.Count == 0)
            {
                Console.WriteLine("✅ ALL CONNECTIONS ARE VALID");
            }
            else
            {
                if (Errors.Count > 0)
                {
                    Console.WriteLine($"\n❌ ERRORS ({Errors.Count}) - Must fix before proceeding:\n");
                    int i = 1;
                    foreach (var err in Errors)
                    {
                        Console.WriteLine($"  [{i}] [{err.Code}] {err.SourceType} '{err.SourceElement}':");
                        Console.WriteLine($"       {err.Message}");
                        if (err.Code == "BIDIRECTIONAL_MISMATCH")
                        {
                            Console.WriteLine($"\n       FIX: Update '{err.TargetElement}' to reference '{err.ExpectedInTarget}'");
                            Console.WriteLine($"            OR update '{err.SourceElement}' to reference correct element\n");
                        }
                        i++;
                    }
                }

                if (Warnings.Count > 0)
                {
                    Console.WriteLine($"\n⚠️  WARNINGS ({Warnings.Count}):\n");
                    foreach (var warn in Warnings)
                    {
                        Console.WriteLine($"  [{warn.Code}] {warn.Message}");
                    }
                }
            }
            
            // Load validation summary
            if (LoadValidationResults.Any())
            {
                Console.WriteLine($"\n📊 LOAD VALIDATION:");
                Console.WriteLine($"   Total loads: {LoadValidationResults.Count}");
                Console.WriteLine($"   Valid: {LoadValidationResults.Count(r => r.IsValid)}");
                Console.WriteLine($"   New buses created: {NewBusesCreatedForLoads.Count}");
                Console.WriteLine($"   Invalid: {LoadValidationResults.Count(r => !r.IsValid)}");
            }

            Console.WriteLine("\n" + new string('-', 60));
            Console.WriteLine(Errors.Count == 0 ? "RESULT: ✅ VALIDATION PASSED" : "RESULT: ❌ VALIDATION FAILED");
            Console.WriteLine(new string('-', 60));
        }

        #endregion
    }

    // ============================================================================
    // EVALUATION SERVICE - Assigns FromBus/ToBus after validation passes
    // ============================================================================

    /// <summary>
    /// Evaluates connections and assigns FromBus/ToBus values.
    /// Only call this AFTER validation passes.
    /// </summary>
    public class ConnectionEvaluationService
    {
        public List<EvaluationAlert> Alerts { get; private set; } = new List<EvaluationAlert>();

        /// <summary>
        /// Evaluates all connections and assigns FromBus/ToBus values.
        /// </summary>
        public (List<Bus> Buses, List<Branch> Branches) EvaluateAllConnections(
            List<Bus> buses,
            List<Branch> branches,
            List<Switch> switches,
            List<Fuse> fuses,
            List<BusBarLink> busBarLinks)
        {
            Alerts.Clear();

            // Initialize all branch FromBus/ToBus to null
            branches.ForEach(b => { b.FromBus = null; b.ToBus = null; });

            // Step 1: Process non-branch elements first
            foreach (var sw in switches ?? new List<Switch>())
            {
                EvaluateNonBranchConnection(sw.Tag, sw.FromElement, sw.ToElement, "Switch", buses, branches);
            }

            foreach (var fuse in fuses ?? new List<Fuse>())
            {
                EvaluateNonBranchConnection(fuse.Tag, fuse.FromElement, fuse.ToElement, "Fuse", buses, branches);
            }

            foreach (var link in busBarLinks ?? new List<BusBarLink>())
            {
                EvaluateNonBranchConnection(link.Tag, link.FromElement, link.ToElement, "BusBarLink", buses, branches);
            }

            // Step 2: Process branch elements
            foreach (var branch in branches)
            {
                EvaluateBranchConnection(branch, buses, branches);
            }

            // Step 3: Validate all branches have connections
            foreach (var branch in branches)
            {
                if (string.IsNullOrEmpty(branch.FromBus))
                {
                    Alerts.Add(new EvaluationAlert
                    {
                        Severity = AlertSeverity.Error,
                        ElementTag = branch.Tag,
                        Message = $"Branch '{branch.Tag}' has no FromBus after evaluation!"
                    });
                }
                if (string.IsNullOrEmpty(branch.ToBus))
                {
                    Alerts.Add(new EvaluationAlert
                    {
                        Severity = AlertSeverity.Error,
                        ElementTag = branch.Tag,
                        Message = $"Branch '{branch.Tag}' has no ToBus after evaluation!"
                    });
                }
            }

            // Step 4: Update bus connectivity
            UpdateBusConnectivity(buses, branches);

            return (buses, branches);
        }

        #region EvaluateNonBranchConnection

        private void EvaluateNonBranchConnection(
            string elementTag, string fromElement, string toElement, string elementType,
            List<Bus> buses, List<Branch> branches)
        {
            var fromType = ClassifyElement(fromElement, buses, branches);
            var toType = ClassifyElement(toElement, buses, branches);

            Debug.WriteLine($"EvaluateNonBranch: {elementType} '{elementTag}' - From:'{fromElement}'({fromType}), To:'{toElement}'({toType})");

            // Both ends are buses
            if (fromType == ElemType.Bus && toType == ElemType.Bus)
            {
                var fromBus = buses.First(b => b.Tag == fromElement);
                var toBus = buses.First(b => b.Tag == toElement);
                if (!fromBus.Cn.Contains(toElement)) fromBus.Cn.Add(toElement);
                if (!toBus.Cn.Contains(fromElement)) toBus.Cn.Add(fromElement);
                return;
            }

            // Both ends are branches - create intermediate bus
            if (fromType == ElemType.Branch && toType == ElemType.Branch)
            {
                string newBusTag = $"{elementTag}-bus";
                Alerts.Add(new EvaluationAlert
                {
                    Severity = AlertSeverity.Info,
                    ElementTag = elementTag,
                    Message = $"Creating intermediate bus '{newBusTag}' between branches '{fromElement}' and '{toElement}'"
                });

                var newBus = new Bus { Tag = newBusTag, Cn = new List<string>() };
                buses.Add(newBus);

                // Update both branches
                var fromBranch = branches.First(b => b.Tag == fromElement);
                var toBranch = branches.First(b => b.Tag == toElement);
                AssignBusToCorrectEnd(fromBranch, elementTag, newBusTag);
                AssignBusToCorrectEnd(toBranch, elementTag, newBusTag);
                return;
            }

            // One bus, one branch
            if ((fromType == ElemType.Bus && toType == ElemType.Branch) ||
                (fromType == ElemType.Branch && toType == ElemType.Bus))
            {
                string busTag = fromType == ElemType.Bus ? fromElement : toElement;
                string branchTag = fromType == ElemType.Branch ? fromElement : toElement;
                var branch = branches.First(b => b.Tag == branchTag);
                AssignBusToCorrectEnd(branch, elementTag, busTag);
                return;
            }

            // Unknown cases
            if (fromType == ElemType.Unknown && toType == ElemType.Unknown)
            {
                Alerts.Add(new EvaluationAlert
                {
                    Severity = AlertSeverity.Error,
                    ElementTag = elementTag,
                    Message = $"Both ends of '{elementTag}' are unknown elements."
                });
            }
        }

        private void AssignBusToCorrectEnd(Branch branch, string viaElement, string busTag)
        {
            // Determine which end connects via this element
            if (branch.FromElement == viaElement)
            {
                if (branch.FromBus == null) branch.FromBus = busTag;
            }
            else if (branch.ToElement == viaElement)
            {
                if (branch.ToBus == null) branch.ToBus = busTag;
            }
            else
            {
                // Try to assign to null end
                if (branch.FromBus == null) branch.FromBus = busTag;
                else if (branch.ToBus == null) branch.ToBus = busTag;
            }
        }

        #endregion

        #region EvaluateBranchConnection

        private void EvaluateBranchConnection(Branch branch, List<Bus> buses, List<Branch> branches)
        {
            Debug.WriteLine($"EvaluateBranch: '{branch.Tag}' - FromElement:'{branch.FromElement}', ToElement:'{branch.ToElement}'");
            Debug.WriteLine($"  Current: FromBus='{branch.FromBus}', ToBus='{branch.ToBus}'");

            // Evaluate FromBus
            if (branch.FromBus == null)
            {
                branch.FromBus = ResolveElementToBus(branch.Tag, branch.FromElement, "From", buses, branches);
            }

            // Evaluate ToBus
            if (branch.ToBus == null)
            {
                branch.ToBus = ResolveElementToBus(branch.Tag, branch.ToElement, "To", buses, branches);
            }

            Debug.WriteLine($"  Final: FromBus='{branch.FromBus}', ToBus='{branch.ToBus}'");
        }

        private string ResolveElementToBus(string branchTag, string elementTag, string side, List<Bus> buses, List<Branch> branches)
        {
            if (string.IsNullOrEmpty(elementTag))
            {
                Alerts.Add(new EvaluationAlert
                {
                    Severity = AlertSeverity.Error,
                    ElementTag = branchTag,
                    Message = $"Branch '{branchTag}' has null {side}Element."
                });
                return null;
            }

            var elemType = ClassifyElement(elementTag, buses, branches);

            switch (elemType)
            {
                case ElemType.Bus:
                    return elementTag;

                case ElemType.Branch:
                    // Create intermediate bus
                    string newBusTag = $"{branchTag}-{side.ToLower()}-bus";
                    Alerts.Add(new EvaluationAlert
                    {
                        Severity = AlertSeverity.Info,
                        ElementTag = branchTag,
                        Message = $"Creating intermediate bus '{newBusTag}' (branch-to-branch connection)"
                    });

                    var newBus = new Bus { Tag = newBusTag, Cn = new List<string>() };
                    buses.Add(newBus);

                    // Update other branch
                    var otherBranch = branches.First(b => b.Tag == elementTag);
                    if (otherBranch.FromElement == branchTag && otherBranch.FromBus == null)
                        otherBranch.FromBus = newBusTag;
                    else if (otherBranch.ToElement == branchTag && otherBranch.ToBus == null)
                        otherBranch.ToBus = newBusTag;

                    return newBusTag;

                case ElemType.Unknown:
                default:
                    // Create new bus with elementTag as name
                    Alerts.Add(new EvaluationAlert
                    {
                        Severity = AlertSeverity.Info,
                        ElementTag = branchTag,
                        Message = $"Creating new bus '{elementTag}' for {side}Element"
                    });

                    var createdBus = new Bus { Tag = elementTag, Cn = new List<string>() };
                    buses.Add(createdBus);
                    return elementTag;
            }
        }

        #endregion

        #region Helpers

        private enum ElemType { Unknown, Bus, Branch }

        private ElemType ClassifyElement(string tag, List<Bus> buses, List<Branch> branches)
        {
            if (string.IsNullOrEmpty(tag)) return ElemType.Unknown;
            if (buses.Any(b => b.Tag == tag)) return ElemType.Bus;
            if (branches.Any(b => b.Tag == tag)) return ElemType.Branch;
            return ElemType.Unknown;
        }

        private void UpdateBusConnectivity(List<Bus> buses, List<Branch> branches)
        {
            foreach (var branch in branches)
            {
                if (string.IsNullOrEmpty(branch.FromBus) || string.IsNullOrEmpty(branch.ToBus))
                    continue;

                var fromBus = buses.FirstOrDefault(b => b.Tag == branch.FromBus);
                var toBus = buses.FirstOrDefault(b => b.Tag == branch.ToBus);

                if (fromBus != null && toBus != null)
                {
                    if (fromBus.Cn == null) fromBus.Cn = new List<string>();
                    if (toBus.Cn == null) toBus.Cn = new List<string>();

                    if (!fromBus.Cn.Contains(branch.ToBus)) fromBus.Cn.Add(branch.ToBus);
                    if (!toBus.Cn.Contains(branch.FromBus)) toBus.Cn.Add(branch.FromBus);
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // SUPPORTING CLASSES
    // ============================================================================

    #region Enums

    public enum ElementCategory { Bus, Branch, NonBranch }
    public enum AlertSeverity { Info, Warning, Error }

    #endregion

    #region Validation Classes

    public class ElementInfo
    {
        public string Tag { get; set; }
        public string Type { get; set; }
        public ElementCategory Category { get; set; }
        public string FromElement { get; set; }
        public string ToElement { get; set; }
    }

    public class ValidationError
    {
        public string Code { get; set; }
        public string SourceElement { get; set; }
        public string SourceType { get; set; }
        public string SourceSide { get; set; }
        public string TargetElement { get; set; }
        public string TargetType { get; set; }
        public string TargetFromElement { get; set; }
        public string TargetToElement { get; set; }
        public string ExpectedInTarget { get; set; }
        public string Message { get; set; }
    }

    public class ValidationWarning
    {
        public string Code { get; set; }
        public string SourceElement { get; set; }
        public string SourceType { get; set; }
        public string TargetElement { get; set; }
        public string Side { get; set; }
        public string Message { get; set; }
    }
    
    public class LoadValidationResult
    {
        public string LoadTag { get; set; }
        public string OriginalConnectedBus { get; set; }
        public bool IsValid { get; set; }
        public bool RequiresNewBus { get; set; }
        public string NewBusTag { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion

    #region Evaluation Classes

    public class EvaluationAlert
    {
        public AlertSeverity Severity { get; set; }
        public string ElementTag { get; set; }
        public string Message { get; set; }
    }

    #endregion

    #region Result Class

    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Bus> Buses { get; set; }
        public List<Branch> Branches { get; set; }
        public List<Load> Loads { get; set; }
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> ValidationWarnings { get; set; } = new List<ValidationWarning>();
        public List<LoadValidationResult> LoadValidationResults { get; set; } = new List<LoadValidationResult>();
        public List<Bus> NewBusesCreatedForLoads { get; set; } = new List<Bus>();
        public Dictionary<string, int> LoadConnectionCounts { get; set; } = new Dictionary<string, int>();
        public List<EvaluationAlert> EvaluationAlerts { get; set; } = new List<EvaluationAlert>();
    }

    #endregion


}