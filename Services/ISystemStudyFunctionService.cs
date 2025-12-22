namespace ElDesignApp.Services;

using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using ElDesignApp.Models;
using ElDesignApp.Services.Cache;
using ElDesignApp.Services.Global;
using Switch = ElDesignApp.Models.Switch;
using System.Text.Json;

public interface ISystemStudyFunctionService
{
    
    double SimpleDistance(LFNode node, LFNode goal);
    double EuclideanDistance(LFNode node, LFNode goal);

    List<LFNode> LFFindPath(LFNode start, LFNode goal, Func<LFNode, LFNode, double> heuristic);

    List<LFNode> ReconstructPath(Dictionary<LFNode, LFNode> cameFrom, LFNode current);


    List<Bus> AssignVbSwingSourcesAndAutoXY(List<CableBranch> cableBranches, List<BusDuct> busDucts,
        List<Transformer> transformers, List<Switch> switches, List<Bus> buses);

    public Tuple<List<Bus>, List<Branch>> MainLoadFlow(List<CableBranch> cableBranches,
        List<Transformer> transformers, List<BusDuct> busDucts, List<Bus> buses, List<Switch> switches,
        List<Load> loads);

    public Tuple<List<Bus>, List<BusParent>> FunctionBusParentList(List<Bus> buses,
        List<BusParent> busParentList, List<Branch> branches, List<Transformer> transformers);

    public Tuple<List<Bus>, List<BusParent>> BFS(List<Bus> buses, List<BusParent> busParentList, Bus startBus,
        List<bool> visited, List<Branch> branches, List<Transformer> transformers);

    public Tuple<Bus, Branch> DummyBusnBranch(Load load, List<Bus> buses, List<Branch> branches);

    public LFResult DoLoadFlow(int iteration, float precision, List<Bus> busesLf, List<Branch> branchesLf,
        List<Load> loadsLf, string studyType = "", string scBusTag = "");

    public List<Bus> UpdateLFResultBus(List<Bus> busesLf, List<Branch> branchesLf, List<Bus> buses);

    List<Branch> UpdateLFResultBranch(List<Bus> busesLf, List<Branch> branchesLf, List<Branch> branches);

    void DisplayResultLF(List<Bus> buses, List<Branch> branches);

    Tuple<List<Bus>, List<Branch>> DisplayResultSC(List<Bus> busesSc, List<Branch> branchesSc,
        string faultyBusTag);

    public List<Bus> DoShortCircuit(List<Bus> scBuses1, List<Branch> scBranches1, List<Load> scLoads1,
        int inetworkIndex);


}




public class SystemStudyFunctionService : ISystemStudyFunctionService
{
    private readonly IGlobalDataService _globalData;
    private readonly ILayoutFunctionService _layoutFunction;

    // Inject IGlobalDataService into the constructor
    // Inject ILayoutFunctionService into the constructor
    public SystemStudyFunctionService(IGlobalDataService globalData, ILayoutFunctionService layoutFunction)
    {
        _globalData = globalData;
        _layoutFunction = layoutFunction;
    }
    
   
    
    public double SimpleDistance(LFNode node, LFNode goal)
    {
        return 1;
    }

    public double EuclideanDistance(LFNode node, LFNode goal)
    {
        //int dx = goal.X - node.X;
        //int dy = goal.Y - node.Y;
        //return Math.Sqrt(dx * dx + dy * dy);
        return 1;
    }

    public List<LFNode> LFFindPath(LFNode start, LFNode goal, Func<LFNode, LFNode, double> heuristic)
    {
        // A-Star Algorithm
        var openSet = new HashSet<LFNode> { start };
        var cameFrom = new Dictionary<LFNode, LFNode>();

        var gScore = new Dictionary<LFNode, double>
        {
            [start] = 0
        };

        var fScore = new Dictionary<LFNode, double>
        {
            [start] = heuristic(start, goal)
        };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(n => fScore.ContainsKey(n) ? fScore[n] : double.PositiveInfinity).First();

            if (current == goal) return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var edge in current.Edges)
            {
                var neighbor = edge.Target;
                var tentativeGScore = gScore[current] + edge.Weight;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        return null; // Path not found
    }

    public List<LFNode> ReconstructPath(Dictionary<LFNode, LFNode> cameFrom, LFNode current)
    {
        var totalPath = new List<LFNode> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }


    public List<Bus> AssignVbSwingSourcesAndAutoXY(List<CableBranch> cableBranches, List<BusDuct> busDucts,
        List<Transformer> transformers, List<Switch> switches, List<Bus> buses)
    {

        Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff} : - {MethodBase.GetCurrentMethod()?.Name}");
        //
        // this function prepares the nodes list and edges for LF A-Star to establish source - Bus relationship
        // the relationship is required to assign the base voltage and also assign auto X-Y coordinates for the Key SLD
        // input for this function are all branches like CableBranches, BusDucts, Transformers and Buses
        // output is the Buses with updated Base Voltage, Ultimate Swing Source Bus (es) and AutoX,Y value for the Key SLD
        // with base Voltage value available, update of the Admittance values of the branches are done in DoLoadFlow Function
        // Also creates all the additional buses for the branches, if not available
        // also assigns the independent Network Seq. no.

        List<LFNode> LFNodes = new();
        List<Branch> Branches = new();
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
       
        var returnBuses = JsonSerializer.Deserialize<List<Bus>>(JsonSerializer.Serialize(buses,jsonOptions),jsonOptions);

        buses.ForEach(bus => { LFNodes.Add(new LFNode(bus.Tag, bus.SwbdTag)); });

        transformers.ForEach(item =>
        {
            var branch = JsonSerializer.Deserialize<Branch>(JsonSerializer.Serialize(item,jsonOptions),jsonOptions);
            branch.VRatio = item.V1 / item.V2;
            Branches.Add(branch);
        });
        cableBranches.ForEach(item =>
        {
            var branch = JsonSerializer.Deserialize<Branch>(JsonSerializer.Serialize(item,jsonOptions),jsonOptions);
            branch.VRatio = 1;
            Branches.Add(branch);
        });
        busDucts.ForEach(item =>
        {
            var branch = JsonSerializer.Deserialize<Branch>(JsonSerializer.Serialize(item,jsonOptions),jsonOptions);
            branch.VRatio = 1;
            Branches.Add(branch);
        });
        //
        // check if all the buses are there for all the branches, if not add the required buses
        Branches.ForEach(branch =>
        {
            buses = _layoutFunction.BranchBusUpdate(branch.Tag, branch.Category, branch.BfT, branch.BtT, buses);
        });
        //

        Branches.ForEach(branch =>
        {
            var fromNodes = LFNodes.Where(node => node.Name == branch.BfT).ToList();
            List<LFNode> todNodes = LFNodes.Where(node => node.Name == branch.BtT).ToList();

            if (fromNodes.Count == 1 && todNodes.Count == 1)
            {
                fromNodes[0].AddEdge(branch.Tag, todNodes[0], 1, branch.VRatio); // weight  is 1
            }
        });
        //
        // Use a simple heuristic for now for AStar
        var heuristic = SimpleDistance;
        var swingNodes = LFNodes.Where(node => buses.Where(bus => bus.Tag == node.Name).ToList()[0].Category == "Swing")
            .ToList();
        swingNodes.ForEach(swingNode =>
        {
            if (!swingNode.SwingSources.Contains(swingNode.Name)) swingNode.SwingSources.Add(swingNode.Name);
        });

        var nonSwingNodes = LFNodes
            .Where(node => buses.Where(bus => bus.Tag == node.Name).ToList()[0].Category != "Swing").ToList();
        //
        // proceed only when there is atleast one swing node and another non-swing node
        if (swingNodes.Count > 1 && nonSwingNodes.Count > 1)
        {
            // the objective is to find the shortest souce path for each and every non-swing nodes
            // start with any random node and find the shortest path to source and the corresponding source
            //
            // assign base voltage for all swing nodes
            swingNodes.ForEach(node =>
            {
                var bus = buses.Where(bus => bus.Tag == node.Name).ToList()[0];
                node.Vb = bus.VR;
                node.VbAssigned = true;
            });
            //
            // continue untill base voltage are assigned for all non-swing buses
            LFNode goalNonSwingNode = null;
            while (nonSwingNodes.Where(n => n.VbAssigned == false).ToList().Count > 0)
            {
                goalNonSwingNode = nonSwingNodes.Where(n => n.VbAssigned == false).ToList()[0];
  
                // find the shortest path to any swing source
                // based on the path of the shortest route, base voltage to be assigned.
                List<LFNode>? shortestPath = null;
                swingNodes.ForEach(sourceSwingNode =>
                {
                    List<LFNode> path = LFFindPath(sourceSwingNode, goalNonSwingNode, heuristic);
                    if (path == null) return; // go to next source
                    if (shortestPath == null || path.Count < shortestPath.Count) shortestPath = path;
                    if (path != null)
                        // for all the paths, this swing source is also a source, may be an alternate source
                        // assign SwingSources
                        for (var i = 0; i < path.Count; i++)
                        {
                            var node = path[i];
                            if (!node.SwingSources.Contains(path[0].Name)) node.SwingSources.Add(path[0].Name);
                        }
                });
                //
                // if the path not found for this goal node, it apears that this is an isolated bus
                // assign an arbitary base value (say, 0) so that this bus is not checked again
                if (shortestPath == null || shortestPath.Count == 1)
                {
                    // an default value is assigned
                    goalNonSwingNode.Vb = 0f;
                    goalNonSwingNode.VbAssigned = true;
                }
                else
                {
                    // assign the base values for all the nodes in this path as per the voltage ratio of the edges
                    for (var i = 0; i < shortestPath.Count - 1; i++)
                    {
                        var nodeFrom = shortestPath[i];
                        var nodeTo = shortestPath[i + 1];
                        var edge = nodeFrom.Edges.Where(edgeNode =>
                            edgeNode.Source.Name == nodeTo.Name || edgeNode.Target.Name == nodeTo.Name).ToList()[0];
                        nodeTo.Vb = edge.Source.Name == nodeFrom.Name
                            ? nodeFrom.Vb / edge.VRatio
                            : nodeFrom.Vb * edge.VRatio;
                        // assign corresponding Source Swing Node
                        if (!nodeTo.SwingSources.Contains(shortestPath[0].Name))
                            nodeTo.SwingSources.Add(shortestPath[0].Name);
                        nodeTo.VbAssigned = true;
                    }

                    // also assign the 'Y-Coordinate' values for all the nodes in this path
                    // check if any of node in this path already has 'Y' value
                    if (shortestPath[0].Y == null) shortestPath[0].Y = 0;
                    // find the last node in this path which has the Y value, default is the source swing node
                    var lastNodeWithYValue = shortestPath.Where(node => node.Y != null).ToList().Last();
                    // assign Y value for the rest of the nodes in this path
                    var startI =
                        shortestPath.IndexOf(
                            shortestPath.Where(node => node.Name == lastNodeWithYValue.Name).ToList()[0]);
                    for (var i = startI + 1; i < shortestPath.Count; i++) shortestPath[i].Y = shortestPath[i - 1].Y + 1;
                }
            }

            // remove nodes which are not connected (Vb = 0)
            LFNodes.RemoveAll(node => node.Vb == 0f);
            //
            // define Network for all the Bus
            List<List<string>> Networks = new List<List<string>>();
            //
            // arrange all the nodes in the highest order of no. of SourceSwing Buses
            // this is to make sure that the node with the multiple sourcees has the common Network
            LFNodes = LFNodes.OrderByDescending(node => node.SwingSources.Count).ToList();
            //
            LFNodes.ForEach(node =>
            {
                var networkNo = 0;
                // check if any of the source nodes are already part of any Network
                if (Networks.Any(network => node.SwingSources.Any(source => network.Contains(source))))
                {
                    List<string> network = Networks
                        .Where(network => node.SwingSources.Any(source => network.Contains(source))).ToList()[0];
                    networkNo = Networks.IndexOf(network) + 1; // start from 1, not 0                  
                }
                else
                {
                    // add new Network
                    Networks.Add(new List<string>());
                    networkNo = Networks.Count; // start from 1, not 0                 
                }

                node.SwingSources.ForEach(swingSource =>
                {
                    var swingSourceNode = LFNodes.Where(n => n.Name == swingSource).ToList()[0];
                    swingSourceNode.Network = networkNo;
                    if (!Networks[networkNo - 1].Contains(swingSource)) Networks[networkNo - 1].Add(swingSource);
                });
                node.Network = networkNo;
                if (!Networks[networkNo - 1].Contains(node.Name)) Networks[networkNo - 1].Add(node.Name);
            });
            //
            // now update Network for all bus
            //
            // now to sort the nodes/bus as per their KeySLD Y cord, then by switchboard tag
            LFNodes = LFNodes.OrderBy(node => node.Network).ThenBy(node => node.Y).ThenBy(node => node.SwbdTag)
                .ThenBy(node => node.Name).ToList();
            // find the required width of each bus
            // assume that for the KeySLD bus shall have only the lump lodas for all directly connected loads
            // assign X-Cord and Length of each of the Bus
            LFNodes.ForEach(node =>
            {
                var y = node.Y;
                var nodesOfY = LFNodes.Where(n => n.Y == node.Y).ToList();
                node.X = nodesOfY.IndexOf(node) + 1;
                node.L = node.Edges.Count + 1; // 1 for lump loads
            });
        }

        returnBuses.ForEach(bus =>
        {
            List<LFNode> nodes = LFNodes.Where(n => n.Name == bus.Tag && n.Vb != 0).ToList();
            if (nodes.Count > 0)
            {
                bus.SwingSources = nodes[0].SwingSources;
                bus.Vb = nodes[0].Vb;
                bus.SLDX = (int)nodes[0].X;
                bus.SLDY = (int)nodes[0].Y;
                bus.SLDL = (int)nodes[0].L;
                bus.Network = nodes[0].Network;
            }
            else
            {
                bus.SwingSources = new List<string>();
                bus.Vb = 0;
                bus.SLDX = 0;
                bus.SLDY = 0;
                bus.SLDL = 0;
                bus.Network = 0;
            }
        });
        return returnBuses.Where(bus => bus.Vb != 0).ToList();
    }


    public Tuple<List<Bus>, List<Branch>> MainLoadFlow(List<CableBranch> cableBranches,
        List<Transformer> transformers, List<BusDuct> busDucts, List<Bus> buses, List<Switch> switches,
        List<Load> loads)
    {
        Debug.WriteLine($"{DateTime.Now:hh.mm.ss.ffffff}  - {MethodBase.GetCurrentMethod()?.Name}");

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
       
    
        List<Branch> Branches = new();

        List<string> NonConnectedBuses = new();

        List<BusVisit> BusVisited = new();
        List<BusParent> BusParentList = new();

        List<Load> Loads = new();
        var SLDXY = new List<SLDXY>();
        var SLDXYToUpdate = new List<SLDXY>();

        List<Bus> Buses = new();
        List<Bus> StudyResultLFBus = new();
        List<Branch> StudyResultLFBranch = new();

        Debug.WriteLine("Initialising data......");
        //
        // create the bus with base voltage as per the shortest path from the corresponding source bus to each bus 
        Buses = AssignVbSwingSourcesAndAutoXY(cableBranches, busDucts, transformers, switches, buses);
        NonConnectedBuses.Clear();
        foreach (var bus in Buses)
            if (bus.Vb == 0)
                NonConnectedBuses.Add(bus.Tag);

        Debug.WriteLine(
            $"Total bus count : {Buses.Count}, out of which {NonConnectedBuses.Count} non-connected buses \n {string.Join(",", NonConnectedBuses)}");
        // removing not connected buses for LF study
        Buses.RemoveAll(bus => bus.Vb == 0);


        //
        // Calculating Zb and Zpu for all cable branches and add them to Branches
        foreach (var cable in cableBranches)
        {
            List<Bus> busesFrom = Buses.Where(b => b.Tag == cable.BfT).ToList();
            List<Bus> busesTo = Buses.Where(b => b.Tag == cable.BtT).ToList();
            // check if this branch is connected
            if (busesFrom.Count == 1 && busesTo.Count == 1 && busesFrom[0].Vb != 0 && busesTo[0].Vb != 0)
            {
                cable.Vb = Buses.Where(b => b.Tag == cable.BfT).ToList()[0].Vb;
                cable.Zb = cable.Vb * cable.Vb / (1000000 * _globalData.Sb);
                _layoutFunction.CableRXUpdate(cable);
                //cable.R = cable.Rl * (cable.L / 1000) / cable.Run; // Ohm
                //cable.X = cable.Xl * (cable.L / 1000) / cable.Run; // Ohm
                cable.Ypu = new Complex(cable.Zb, 0) / new Complex(cable.R, cable.X);

                Branches.Add(cable);
            }
        }

        //
        // Calculating Zb and Zpu for all bus ducts
        foreach (var busDuct in busDucts)
        {
            List<Bus> busesFrom = Buses.Where(b => b.Tag == busDuct.BfT).ToList();
            List<Bus> busesTo = Buses.Where(b => b.Tag == busDuct.BtT).ToList();
            if (busesFrom.Count == 1 && busesTo.Count == 1 && busesFrom[0].Vb != null && busesTo[0].Vb != null &&
                busesFrom[0].Vb != 0 && busesTo[0].Vb != 0)

            {
                busDuct.Vb = Buses.Where(b => b.Tag == busDuct.BfT).ToList()[0].Vb;
                busDuct.Zb = (float)(busDuct.Vb * busDuct.Vb) / (1000000 * _globalData.Sb);
                _layoutFunction.BusDuctRXUpdate(busDuct);
                //busDuct.R = busDuct.Rl * (busDuct.L / 1000) / busDuct.Run; // Ohm
                //busDuct.X = busDuct.Xl * (busDuct.L / 1000) / busDuct.Run; // Ohm
                busDuct.Ypu = new Complex(busDuct.Zb, 0) / new Complex(busDuct.R, busDuct.X);

                Branches.Add(busDuct);
            }
        }

        // Calculating Zb and Zpu for all transformer branches
        foreach (var trafo in transformers)
        {
            List<Bus> busesFrom = Buses.Where(b => b.Tag == trafo.BfT).ToList();
            List<Bus> busesTo = Buses.Where(b => b.Tag == trafo.BtT).ToList();
            if (busesFrom.Count == 1 && busesTo.Count == 1 && busesFrom[0].Vb != null && busesTo[0].Vb != null &&
                busesFrom[0].Vb != 0 && busesTo[0].Vb != 0)
            {
                var Sbold = trafo.KVA / 1000; // Sb in MVA
                var Sbnew = _globalData.Sb; // in MVA
                var Vbold = trafo.V1; // in V
                var Vbnew = Buses.Where(b => b.Tag == trafo.BfT).ToList()[0].Vb;
                var Zpuold = trafo.Z / 100; // Z in pc
                var Zpunew = (float)(Zpuold * (Sbnew / Sbold) * Math.Pow(Vbold / Vbnew, 2));
                trafo.Ypu = 1 / (Zpunew * new Complex(1, trafo.XR) / Math.Pow(1 + trafo.XR * trafo.XR, 0.5));

                Branches.Add(trafo);
            }
        }

        //
        //
        //
        foreach (var br in Branches)
        {
            br.Seq = Branches.IndexOf(br);
            var BF = Buses.Where(b => b.Tag == br.BfT).ToList()[0];
            var BT = Buses.Where(b => b.Tag == br.BtT).ToList()[0];
            //System.Diagnostics.Debug.WriteLine($"{br.Seq + 1}. Branch {br.Tag} : From {br.BfT} ({BF.VR}V : Vb= {BF.Vb}) To {br.BtT} ({BT.VR}V : Vb= {BT.Vb})  Y: ({br.Ypu.Real}, {br.Ypu.Imaginary})");
        }

        //
        // Load Data for Load Flow
        Loads.Clear();
        //
        //
        // bypass BFS etc. as they are no longer required
        if (false)
        {
            // check if any Bus has the duplicate tag
            var distinctBus = buses.GroupBy(x => x.Tag).Select(y => y.First());
            var distinctBranches = Branches.GroupBy(x => x.Tag).Select(y => y.First());

            //
            // Breadth First Search
            // Initialised
            foreach (var bus in buses) BusVisited.Add(new BusVisit(bus, false));
            ;
            //
            // sorting by Category (Swing Bus first)
            buses.Sort((x, y) => x.Category.CompareTo(y.Category));
            buses.OrderBy(x => x.Category);
            //
            //
            //List<bool> visited = new() { };
            //Buses.ForEach(bus => { visited.Add(false); });
            //for (int i = 0; i < Buses.Count; i++)
            //{
            //    if (Buses[i].Category != "Swing")
            //    {
            //        continue;
            //    }
            //    //assign BusParent relationship
            //    if (BusParentList.Where(b => b.B == Buses[i].Tag).ToList().Count == 0)
            //    {
            //        BusParentList.Add(new BusParent(Buses[i].T, Buses[i].T, Buses[i].Tag)); // the parent and corresponding swing bus of any swing bus is the bus itselt.
            //    }
            //    // assign base voltage by visiting all buses
            //    // starting from the source bus
            //    var tuple = BFS(Buses, BusParentList, Buses[i], visited, Branches);
            //    Buses = tuple.Item1;
            //    BusParentList = tuple.Item2;
            //}

            var tuple = FunctionBusParentList(buses, BusParentList, Branches, transformers);
            Buses = tuple.Item1;
            BusParentList = tuple.Item2;

            //string ParentBusT;
            //Bus ParentBus;
            //foreach (Bus swingBus in Buses.Where(b => b.Category == "Swing").ToList())
            //{
            //    ParentBusT = swingBus.T;
            //    ParentBus = Buses.Where(b => b.Tag == ParentBusT).ToList()[0];
            //    // Apply Base voltage to this starting Swing Bus
            //    Buses.Where(b => b.Tag == ParentBusT).ToList()[0].Vb = Buses.Where(b => b.Tag == ParentBusT).ToList()[0].V;
            //    //
            //    FunctionBFS(ParentBusT, ParentBusT);
            //    ParentBus.SLDD = 1; // depth level for drawing SLD
            //}
            //check if all bus are visited
            Debug.WriteLine($"Total bus count : {Buses.Count}  before removing non-connected buses");

            foreach (var busVisit in BusVisited)
                if (busVisit.V == false)
                {
                    NonConnectedBuses.Add(busVisit.B.Tag);
                    //removing non-connected bus
                    //Buses.RemoveAll(bus => bus.Tag == busVisit.B.Tag);
                    //removing corresponding cables of non-connected bus
                    var B = Buses.Where(bus => bus.Tag == busVisit.B.Tag).ToList()[0];
                    if (B.Vb == 0) B.Vb = B.VR;
                }
        }

        //
        // public Load(string c, string t, string bt, Complex s, float v) v in kV, s in PU with Sb w/o voltage correction
        // foreach (Load load in loads) { Loads.Add(new Load(load.Category, load.Tag, load.BfT, load.Scpu, load.VR, load.DR)); }
        Loads = JsonSerializer.Deserialize<List<Load>>(JsonSerializer.Serialize(loads, jsonOptions), jsonOptions);
        // Update PU value of S in complex w.r.t Sb
        Loads.ForEach(load => _layoutFunction.LoadUpdatePU(load));
        //
        // Prepare for Load Flow
        Debug.WriteLine("\nStarting Load Flow.......\n");
        //
        // carry out load flow for each of the independent networks / sources

        if (false)
        {
            // let's assume sources are not connected in anyway
            // therefore, total independent networks are same as total sources
            // later find out which sources are connected and accordingly decide actual no of independent networks
            List<int> Networks = new();
            // assigning the network sequence to the source (swing) buses
            // 0: none network
            foreach (var bus in Buses)
                if (bus.Category == "Swing")
                {
                    bus.Network = Networks.Count + 1;
                    Networks.Add(Networks.Count + 1);
                }

            ;
            // assigning the network sequence no to all the other buses based on the sequence no. of their correspondig source bus
            foreach (var bus in Buses)
                if (bus.Category != "Swing")
                {
                    var sourceBusList = BusParentList.Where(b => b.B == bus.Tag).ToList();
                    // if busParentListCount is zero, i.e., bus is not listed in BusParentList, that means this bus is not connected to any network
                    // in such case exclude this bus from LF and SC study
                    if (sourceBusList.Count == 0)
                    {
                        bus.Network = 0; // not connected bus as default int = 0;
                    }
                    else
                    {
                        var sourceBus = sourceBusList[0].S;
                        bus.Network = Buses.Where(b1 => b1.Tag == sourceBus).ToList()[0].Network;
                    }
                }

            ;
            //
            List<List<string>> BusNetworks = new();
            var swingSourceBuses = Buses.Where(bus => bus.Category == "Swing").ToList();
            foreach (var swingSourceBus in swingSourceBuses)
            {
                if (!BusNetworks.Any(network => network.Contains(swingSourceBus.Tag)))
                {
                    BusNetworks.Add(new List<string>());
                    BusNetworks.Last().Add(swingSourceBus.Tag);
                }

                List<Bus> connectedBuses = Buses.Where(bus => bus.SwingSources.Contains(swingSourceBus.Tag)).ToList();
                foreach (var connectedBus in connectedBuses)
                {
                    var allSwingSourceBusTagsToThisConnectedBus = connectedBus.SwingSources;
                    allSwingSourceBusTagsToThisConnectedBus.ForEach(swingSourceBusTag =>
                    {
                        if (BusNetworks.Any(network => network.Contains(swingSourceBusTag)))
                        {
                            // this swingSourceBusTag is part of the already created BusNetwork
                            var index = BusNetworks.IndexOf(BusNetworks
                                .Where(busNetwork => busNetwork.Contains(swingSourceBusTag)).ToList()[0]);
                            if (!BusNetworks[index].Contains(connectedBus.Tag))
                                BusNetworks[index].Add(connectedBus.Tag);
                            ;
                        }
                        else
                        {
                            // this swingSourceBusTag is not part of the already created BusNetwork
                            // craete BusNetwork with this source swing bus
                            BusNetworks.Add(new List<string>());
                            BusNetworks.Last().Add(swingSourceBusTag);
                        }
                    });
                }
            }
        }

        Buses = Buses.OrderBy(node => node.Network).ThenBy(node => node.SLDY).ThenBy(node => node.SwbdTag)
            .ThenBy(node => node.Tag).ToList();

        // create the set of buses for each of the networks
        var totalNetworks = Buses.MaxBy(x => x.Network).Network;
        //totalNetworks = BusNetworks.Count();
        List<List<Bus>> LFBusesSet = new();
        for (var i = 0; i < totalNetworks; i++) LFBusesSet.Add(new List<Bus>());
        foreach (var bus in Buses)
        {
            // drop the non-connected buses from the study
            if (bus.Network == 0) continue;
            LFBusesSet[bus.Network - 1].Add(bus);
        }

        // run loadflow for each of the independent networks
        for (var i = 0; i < totalNetworks; i++)
        {
            List<Bus> LFBuses = [];
            List<Branch> LFBranches = [];
            List<Load> LFLoads = [];

            foreach (var bus in LFBusesSet[i]) LFBuses.Add(bus);
            foreach (var branch in Branches)
                if (LFBuses.Any(b => b.Tag == branch.BfT || b.Tag == branch.BtT))
                    LFBranches.Add(branch);
            foreach (var load in Loads)
                if (LFBuses.Any(b => b.Tag == load.BfT))
                    LFLoads.Add(load);
            //
            var lfResult = DoLoadFlow(_globalData.IterationLF, _globalData.PrecisionLF, LFBuses, LFBranches, LFLoads,
                "LF");

            Buses = UpdateLFResultBus(lfResult.BusResult, LFBranches, Buses);
            Branches = UpdateLFResultBranch(lfResult.BusResult, LFBranches, Branches);

            //resultMessage = resultMessage + $"Load Flow Study for Network {i} completed in {lfResult.MS} miliseconds with {lfResult.IT} iterations.\n";
            // bus result is avialable in lfResult.BusResult whereas Branch result is updated in LFBranches by the DisplayResultLF function
            // store result to save to the database
            lfResult.BusResult.ForEach(lfResultBus =>
            {
                StudyResultLFBus.Add(JsonSerializer.Deserialize<Bus>(JsonSerializer.Serialize(lfResultBus, jsonOptions), jsonOptions));
            });
            LFBranches.ForEach(lfResultBranch =>
            {
                StudyResultLFBranch.Add(
                    JsonSerializer.Deserialize<Branch>(JsonSerializer.Serialize(lfResultBranch, jsonOptions), jsonOptions));
            });
            //
            var result = $"Load Flow Study for Network {i + 1} of {LFBuses.Count} buses, {LFBranches.Count} branches " +
                         $"and {LFLoads.Count} loads completed in {lfResult.MS} miliseconds with {lfResult.IT} iterations";
            Debug.WriteLine(result);
        }

        // display the complete LF result for all networks
        DisplayResultLF(Buses, Branches);
        // saving LF result for DB
        Buses.ForEach(bus => { bus.VoJSON = JsonSerializer.Serialize(bus.Vo, jsonOptions); });
        //
        //
        // Prepare for Short Circuit
        Debug.WriteLine("\nStartig Short Circuit....\n");
        //
        //
        // create independent network SC Study Bus Set
        List<List<Bus>> SCBusesSet = new();
        for (var i = 0; i < totalNetworks; i++) SCBusesSet.Add(new List<Bus>());
        foreach (var bus in Buses)
        {
            // drop the non-connected buses from the study
            if (bus.Network == 0) continue;
            var network = bus.Network;
            SCBusesSet[bus.Network - 1].Add(bus);
        }

        //
        // run the short circuit study for each of the independent networks
        for (var i = 0; i < totalNetworks; i++)
        {
            //if (i != 2) { continue; } // 2: Anapole remove this later
            //
            List<Bus> SCBuses = new List<Bus>();
            var SCBranches = new List<Branch>();
            var SCLoads = new List<Load>();
            //
            // Bus Data for Short Circuit
            foreach (var bus in SCBusesSet[i]) SCBuses.Add(bus);
            //
            // Branch Data for Short Circuit
            foreach (var branch in Branches)
                if (SCBuses.Any(b => b.Tag == branch.BfT || b.Tag == branch.BtT))
                    SCBranches.Add(branch);
            foreach (var load in Loads)
                if (SCBuses.Any(b => b.Tag == load.BfT))
                    SCLoads.Add(load);

            // Create Dummy Bus and corresponding dummy branch for all Swing Buses
            // the new bus becomes the "Swing" bus whereas the original bus becomes the "" unknown bus
            List<string> swingBusList = new List<string>();
            foreach (var bus in SCBuses)
                if (bus.Category == "Swing")
                    swingBusList.Add(bus.Tag);
            foreach (var busT in swingBusList)
            {
                var bus = SCBuses.Where(b => b.Tag == busT).ToList()[0];
                bus.Category = "";

                // Bus Short Circuit in kA , Zsc in Ohm
                var Zsc = bus.VR / (Math.Pow(3, 0.5) * bus.ISC * 1000) *
                          new Complex(1 / bus.XR, Math.Sin(Math.Acos(1 / bus.XR)));
                var Zb = (float)Math.Pow(bus.Vb, 2) / (1000000 * _globalData.Sb);
                var Zscpu = Zsc / Zb;

                var newBus = new Bus
                {
                    Category = "Swing",
                    Tag = bus.Tag + "-Dummy",
                    Vb = bus.VR,
                    SC = bus.SC,
                    ISC = bus.ISC,
                    XR = bus.XR
                };
                
                
                newBus.Cn.Clear();
                newBus.Cn.Add(bus.Tag);
                newBus.Vb = bus.Vb;
                SCBuses.Add(newBus);
                bus.Cn.Add(newBus.Tag);
                var display = $"New bus '{newBus.Tag}' ({newBus.VR}V) is created with category '{newBus.Category}'. " +
                              $"Existing '{bus.Tag}' source bus becomes category '{bus.Category}' and will have connections '{string.Join(",", bus.Cn)}'";
                //System.Diagnostics.Debug.WriteLine(display);

                SCBranches.Add(new Branch("Source", bus.Tag + "-Imp", newBus.Tag, bus.Tag, Zb / Zsc));
                var newBranch = SCBranches.Where(br => br.Tag == bus.Tag + "-Imp").ToList()[0];
                display =
                    $"{swingBusList.IndexOf(busT) + 1}: New created Branch: {newBranch.Tag} is created whihch connects between '{newBranch.BfT}' and "
                    + $"'{newBranch.BtT}' with Y ({Math.Round(newBranch.Ypu.Real, 5)}, {Math.Round(newBranch.Ypu.Imaginary, 5)}) pu";
                //System.Diagnostics.Debug.WriteLine(display);
            }

            //
            // Load Data for Short Circuit
            SCLoads.Clear();
            foreach (var motor in loads.Where(load => load.Category == "Motor").ToList())
                if (SCBuses.Any(b => b.Tag == motor.BfT))
                {
                    var busNbranch = DummyBusnBranch(motor, SCBuses, SCBranches);
                    var newBus = busNbranch.Item1;
                    var newBranch = busNbranch.Item2;
                    SCBuses.Add(newBus);
                    SCBranches.Add(newBranch);
                    SCBuses.Where(bus => bus.Tag == motor.BfT).ToList()[0].Cn.Add(newBus.Tag);
                }

            // No SC contribution for Static Loads (Heaters, Capacitors, etc.)
            // however, would continue to draw power
            if (_globalData.LoadContribution)
                foreach (var heater in loads.Where(load => load.Category == "Heater").ToList())
                    if (SCBuses.Any(b => b.Tag == heater.BfT))
                        SCLoads.Add(new Load(heater.Category, heater.Tag, heater.BfT, heater.Scpu, heater.VR, 1));

            if (_globalData.LoadContribution)
                foreach (var capacitor in loads.Where(load => load.Category == "Capacitor").ToList())
                    if (SCBuses.Any(b => b.Tag == capacitor.BfT))
                        SCLoads.Add(new Load(capacitor.Category, capacitor.Tag, capacitor.BfT, capacitor.Scpu,
                            capacitor.VR, 1));

            foreach (var lumpLoad in loads.Where(load => load.Category == "LumpLoad").ToList())
                if (SCBuses.Any(b => b.Tag == lumpLoad.BfT))
                {
                    //constant Z part
                    if (_globalData.LoadContribution)
                        SCLoads.Add(new Load(lumpLoad.Category, lumpLoad.Tag, lumpLoad.BfT, lumpLoad.Scpu, lumpLoad.VR,
                            1));
                    //constant kVA (motor) part
                    // creating new swing bus for SC contribution
                    //DummyBusnBranch(lumpLoad.Tag + "-FXkVA", lumpLoad.BfT, (1 - lumpLoad.DR) * lumpLoad.S, lumpLoad.Ist, lumpLoad.Pfst, SCBuses, SCBranches);
                    var busNbranch = DummyBusnBranch(lumpLoad, SCBuses, SCBranches);
                    var newBus = busNbranch.Item1;
                    var newBranch = busNbranch.Item2;
                    SCBuses.Add(newBus);
                    SCBranches.Add(newBranch);
                    SCBuses.Where(bus => bus.Tag == lumpLoad.BfT).ToList()[0].Cn.Add(newBus.Tag);
                }
            //

            Debug.WriteLine($"\nStartig Short Circuit for Network {i + 1}....\n");
            // SC Study for all the buses in this network and display the results
            SCBuses = DoShortCircuit(SCBuses, SCBranches, SCLoads, i);
            //
            // update SC results to the Buses
            SCBuses.ForEach(bus =>
            {
                var b = Buses.Where(b => b.Tag == bus.Tag).ToList()[0];
                b.SCkAaMax = bus.SCkAaMax;
                b.Vo = bus.Vo;
                b.SCResult = bus.SCResult;
                b.SCResultJSON = bus.SCResultJSON;
            });
        }

        return new Tuple<List<Bus>, List<Branch>>(Buses, Branches);
    }


    public Tuple<List<Bus>, List<BusParent>> FunctionBusParentList(List<Bus> buses,
        List<BusParent> busParentList, List<Branch> branches, List<Transformer> transformers)
    {
        List<bool> visited = new();
        buses.ForEach(bus => { visited.Add(false); });
        for (var i = 0; i < buses.Count; i++)
        {
            //assign BusParent relationship for only Slack Bus, exit for "Swing" bus
            if (buses[i].Category != "Swing") continue;
            //
            if (busParentList.Where(b => b.B == buses[i].Tag).ToList().Count == 0)
                busParentList.Add(new BusParent(buses[i].Tag, buses[i].Tag,
                    buses[i].Tag)); // the parent and corresponding swing bus of any swing bus is the bus itselt.
            // assign base voltage by visiting all buses
            // starting from the source bus
            var tuple = BFS(buses, busParentList, buses[i], visited, branches, transformers);
            buses = tuple.Item1;
            busParentList = tuple.Item2;
        }

        return new Tuple<List<Bus>, List<BusParent>>(buses, busParentList);
    }


    //
    // breadth first searches
    // to visit all the bus (nodes)
    // to assign Base Voltage
    // starting with source bus (swing)
    public Tuple<List<Bus>, List<BusParent>> BFS(List<Bus> buses, List<BusParent> busParentList, Bus startBus,
        List<bool> visited, List<Branch> branches, List<Transformer> transformers)
    {
        
        var s = buses.IndexOf(startBus);
        Queue<Bus> Q = new();
        Q.Enqueue(buses[s]);
        startBus.SLDY = 0; //Setting the depth level (for drawing purpose) of the source node as 0
        visited[s] = true;
        while (Q.Count > 0)
        {
            var p = Q.Dequeue();
            p.Cn.ForEach(cn =>
            {
                var buscn = buses.Where(bus => bus.Tag == cn).ToList()[0];
                var n = buses.IndexOf(buscn);
                if (visited[n] == false)
                {
                    //Setting the level of each node with an increment in the level of parent node
                    buscn.SLDY = p.SLDY + 1;
                    Q.Enqueue(buscn);
                    visited[n] = true;
                    // assign BusParent relationship for this connected bus
                    var temp = busParentList.Where(b => b.B == p.Tag).ToList();
                    if (busParentList.Where(b => b.B == cn).ToList().Count == 0)
                        busParentList.Add(new BusParent(cn, p.Tag, temp.Count == 1 ? temp[0].S : null));
                    // assign base voltage for this branch
                    var BRtemplist = branches
                        .Where(br => (br.BfT == p.Tag && br.BtT == cn) || (br.BtT == p.Tag && br.BfT == cn)).ToList();
                    if (BRtemplist.Count != 1)
                    {
                        
                    }
                    else
                    {
                        var BR = BRtemplist[0];
                        // if this branch is a cable
                        if (BR.Category == "Cable")
                        {
                            // set Base Voltage for this child bus same as the Base Voltage of thisBus
                            buscn.Vb = p.Vb;
                        }
                        else if (BR.Category == "Transformer")
                        {
                            var TR = transformers.Where(tr => tr.Tag == BR.Tag).ToList()[0];
                            float V1, V2;
                            V1 = TR.BfT == p.Tag ? TR.V1 : TR.V2;
                            V2 = TR.BtT == p.Tag ? TR.V1 : TR.V2;
                            buscn.Vb = p.Vb * V2 / V1;
                        }
                    }
                }
            });
        }

        //return Buses;
        return new Tuple<List<Bus>, List<BusParent>>(buses, busParentList);
    }


    public Tuple<Bus, Branch> DummyBusnBranch(Load load, List<Bus> buses, List<Branch> branches)
    {
        // this function returns a new "dummy" bus and a new branch for this dynamic load
        // the new "dummy" bus becomes "Swing" bus
        // input is the load and Bus & Branch list

        var connectedBus = buses.Where(b => b.Tag == load.BfT).ToList()[0];

        var loadTag = load.Tag;

        var kVA = load.S; // kVA
        var lrc = load.Ist;
        var lrpf = load.Pfst;


        var Zb = connectedBus.Vb * connectedBus.Vb / (1000000 * _globalData.Sb); // Sb in MVA and Vb in V
        var SCkVA = kVA * (lrc > 100 ? lrc / 100 : lrc); // Short Circuit in kVA
        var Zsc = connectedBus.VR * connectedBus.VR / (1000 * SCkVA); // VR in V
        var Zpu = Zsc / Zb * new Complex(lrpf, Math.Sin(Math.Acos(lrpf)));
        var Ypu = 1 / Zpu;
        //            
        var newBus = new Bus
        {
            Category = "Swing",
            Tag = loadTag + "-Dummy",
            Vb = connectedBus.VR,
            SC = connectedBus.SC,
            ISC = connectedBus.ISC,
            XR = connectedBus.XR
        };

        newBus.Cn.Clear();
        newBus.Cn.Add(connectedBus.Tag);
        newBus.Vb = connectedBus.Vb;

        var newBranch = new Branch(load.Category, loadTag + "-Imp", connectedBus.Tag, newBus.Tag, Ypu);
        newBranch.VR = connectedBus.VR;
        newBranch.Vb = connectedBus.Vb;
        //
        var display =
            $"New bus '{newBus.Tag}' ({newBus.VR}V) catogory '{newBus.Category}' for  {load.Category} tag '{load.Tag}' "
            + $"with new branch '{newBranch.Tag}' between '{newBranch.BfT}' and '{newBranch.BtT}' of Y ({newBranch.Ypu})";


        //System.Diagnostics.Debug.WriteLine(display);

        //System.Diagnostics.Debug.WriteLine($"New created Branch: {newBranch.Tag} is connected between {newBranch.BfT} and {newBranch.BtT} "
        //    + $" with Y ({Math.Round(newBranch.Y.Real, 5)}, {Math.Round(newBranch.Y.Imaginary, 5)})");

        return new Tuple<Bus, Branch>(newBus, newBranch);
    }


    public LFResult DoLoadFlow(int iteration, float precision, List<Bus> busesLf, List<Branch> branchesLf,
        List<Load> loadsLf, string studyType = "", string scBusTag = "")
    {
        // start timer
        var LFb4 = DateTime.UtcNow;
        //BusesLF.Sort((x, y) => x.Category.CompareTo(y.Category));
        //// sorting by Category (Swing Bus first)
        //BusesLF.OrderBy(x => x.Category);
        //
        var n = busesLf.Count; // bus                                 //

        // let the initial educated guess for Vit = 1.0 p.u
        // this is for all bus in LF and all but faulty bus in case of SC study
        // check if there is existing value of intial voltage from Database for the last LF or SC study
        
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (studyType == "LF")
        {
            var nullVoJSON = JsonSerializer.Serialize(new Complex(0, 0), jsonOptions);


            foreach (var b1 in busesLf)
            {
                // if there is any existing value of intial voltage from Database for the last LF study
                // then they must be non-zero
                // Deserialize the JSON string into a list of MyDataItem objects

                if (string.IsNullOrEmpty(b1.VoJSON) || b1.VoJSON == nullVoJSON)
                    b1.Vit = new Complex(1, 0);
                else
                    try
                    {
                        b1.Vit = JsonSerializer.Deserialize<Complex>(b1.VoJSON, jsonOptions);
                        //b1.Vit = b1.Vo;
                    }
                    catch (Exception e)
                    {
                        b1.Vit = new Complex(1, 0);
                    }
                // overwriting the existing value of Vit, i.e., disregarding the value from the database
                b1.Vit = new Complex(1, 0);
            }
        }

        if (studyType == "SC")
        {
            var scBus = busesLf.Where(bus => bus.Tag == scBusTag).ToList()[0];

            // if there is any existing value of intial voltage from Database for the last SC study
            // then they must be non-zero
            // Deserialize the JSON string into a list of MyDataItem objects
            var dataItems = JsonSerializer.Deserialize<List<SCBusVal>>(scBus.SCResultJSON ,jsonOptions);
            // Use LINQ to check if any item has a non-zero Real, Imaginary, Magnitude, or Phase
            bool hasNonZeroValue = dataItems.Any(item =>
                item.Vo.Real != 0 ||
                item.Vo.Imaginary != 0 ||
                item.Vo.Magnitude != 0 ||
                item.Vo.Phase != 0
            );
            
            
            if (string.IsNullOrEmpty(scBus.SCResultJSON) || !hasNonZeroValue)
                foreach (var b1 in busesLf)
                    b1.Vit = new Complex(1, 0);
            else
                try
                {
                    //scBus.SCResult = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SCBusVal>>(scBus.SCResultJSON);
                    foreach (var b1 in busesLf)
                    {
                        var results = scBus.SCResult.Where(scResulVal => scResulVal.Tag == b1.Tag).ToList();
                        if (results.Count == 1)
                            b1.Vit = results[0].Vo;
                        else
                            b1.Vit = new Complex(1, 0);
                    }
                }
                catch (Exception e)
                {
                    foreach (var b1 in busesLf) b1.Vit = new Complex(1, 0);
                }

            scBus.Vit = new Complex(0, 0);
        }

        // initialising self addmittances (sum of admittance of connected branches
        foreach (var b1 in busesLf)
        {
            b1.Ybb = new Complex(0, 0);
            foreach (var b2T in b1.Cn)
                try
                {
                    b1.Ybb = b1.Ybb + branchesLf.Where(branch =>
                            (branch.BfT == b1.Tag && branch.BtT == b2T) || (branch.BfT == b2T && branch.BtT == b1.Tag))
                        .ToList()[0].Ypu;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Program aborted for Bus: {b1.Tag} Cn.Count : {b1.Cn.Count}  Error: {e}");
                    throw;
                }
        }

        //
        //
        var stopIteration = false;
        for (var k = 0; k <= iteration; k++)
        {
            // stop any further iteration
            if (stopIteration) break;
            //consider bus 0 is a swing bus whose voltage is fixed at 1.0 pu
            // iterate for the remaining bus
            foreach (var bus in busesLf)
            {
                if (bus.Category == "Swing" || (studyType == "SC" && bus.Tag == scBusTag)) continue;
                //adjusting the VA as per bus voltages
                List<Load> BLoads = new List<Load>();
                // listing all the loads which are connected to this bus
                BLoads = loadsLf.Where(load => load.BfT == bus.Tag).ToList();
                bus.Sit = new Complex(0, 0); // initialised
                foreach (var load in BLoads)
                {
                    // constant power (motor) loads are voltage independent
                    // whereas constant kVA loads (heater) changes proportionate to the bus voltage
                    // P & Q are in kW and kVA respectively whereas Sb is in MVA
                    var lc = 0.001f * new Complex(load.P, -load.Q) / _globalData.Sb;
                    bus.Sit += lc * (1 - load.DR + load.DR * Math.Pow(bus.Vit.Magnitude * bus.Vb / bus.VR, 2));
                }

                //
                var IV = new Complex(bus.Vit.Real, bus.Vit.Imaginary);
                Branch BB;
                Complex Ybb;
                var VY = new Complex(0, 0);
                foreach (var b1T in bus.Cn)
                {
                    var BBList = branchesLf.Where(branch =>
                            (branch.BfT == bus.Tag && branch.BtT == b1T) ||
                            (branch.BtT == bus.Tag && branch.BfT == b1T))
                        .ToList();
                    if (BBList.Count > 0)
                    {
                        BB = BBList[0];
                        Ybb = BB.Ypu;
                        VY = VY + busesLf.Where(b => b.Tag == b1T).ToList()[0].Vit * Ybb;
                    }
                }

                //if (k % 100 == 0) { System.Diagnostics.Debug.WriteLine($"Iteration {k} : VY for {bus.Seq} >  Array : ({VY.Real} {VY.Imaginary}) : Class : ({VY.Real} {VY.Imaginary}) : {VY.ToString()}"); }

                double d = 999; // initialised with large default no.
                var counter = 0;
                do
                {
                    if (bus.Ybb == new Complex(0, 0) || IV == new Complex(0, 0))
                    {
                        var check = 0;
                        break; // break the do while loop as no further iteration is required
                    }

                    bus.Vit = (VY - Complex.Conjugate(bus.Sit / IV)) / bus.Ybb;
                    if (bus.Vit.Magnitude != 0)
                    {
                        d = (bus.Vit - IV).Magnitude / bus.Vit.Magnitude;
                        IV = new Complex(bus.Vit.Real, bus.Vit.Imaginary);
                    }

                    //System.Diagnostics.Debug.WriteLine($"Iteration {k} : Bus : {bus.T}  Vit: {bus.Vit.Magnitude} :   d: {d}  IV: {IV} ", , , , , ));
                    counter = counter + 1;
                } while (d > precision && counter < _globalData.CounterLF && IV.Magnitude < precision);
                //System.Diagnostics.Debug.WriteLine(String.Format("Iteration {0} : Bus : {1}  Vit: {2} :   d: {3}  IV: {4} Counter {5}", k, bus.T, bus.Vit.Magnitude, d, IV, counter));
                //
            }

            // check if further iteration is necessary
            switch (studyType)
            {
                // Load Flow
                case "LF":
                    // this check to be done at say, every 100th iteration
                    if (k % 50 == 0)
                    {
                        stopIteration = true;
                        foreach (var bus in busesLf)
                        {
                            if ((bus.Vit - bus.Vo).Magnitude / bus.Vit.Magnitude > precision &&
                                bus.Vit.Magnitude > 0.000001) stopIteration = false;
                            bus.Vo = bus.Vit; // updatig the operating voltage
                        }
                    }

                    if (stopIteration || k == iteration) iteration = k;
                    break;
                // Short Circuit
                case "SC":
                    // this check to be done at say, every 10th iteration for only the buses which are connected to the faulty bus
                    if (k % 10 == 0)
                    {
                        stopIteration = true;
                        foreach (var busT in busesLf.Where(b => b.Tag == scBusTag).ToList()[0].Cn)
                        {
                            var bus = busesLf.Where(b => b.Tag == busT).ToList()[0];
                            if (bus.Category == "Swing") continue;
                            if ((bus.Vit - bus.Vo).Magnitude / bus.Vit.Magnitude > precision &&
                                bus.Vit.Magnitude > 0.0000001) stopIteration = false;
                            bus.Vo = bus.Vit; // updatig the operating voltage to only the connected bus to the SC Bus
                        }
                    }

                    if (stopIteration || k == iteration)
                    {
                        iteration = k;
                        foreach (var bus in busesLf) bus.Vo = bus.Vit;
                    }

                    break;
                //Other Studies
            }
        }

        // store last LF and SC result in the database to shorten no.of iterations
        if (studyType == "LF")
        {
            // final LF voltage
            //foreach (Bus b1 in BusesLF)
            //{
            //    b1.Vo = b1.Vo;
            //}
        }
        else if (studyType == "SC")
        {
            var scBus = busesLf.Where(bus => bus.Tag == scBusTag).ToList()[0];

            // save bus voltages of all the busses for this faulty bus in the Database to save no. of iteration in the next time of SC study

            List<SCBusVal> scResult = [];
            busesLf.ForEach(b => scResult.Add(new SCBusVal(b.Tag, b.Vo)));

            scBus.SCResult = scResult;
            scBus.SCResultJSON = JsonSerializer.Serialize(scResult, jsonOptions);
            //var a = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SCBusVal>>(scBus.SCResultJSON);
        }


        var interval = DateTime.UtcNow - LFb4; // stop timer and calculate the time interval
        return new LFResult(busesLf, (float)interval.TotalMilliseconds, iteration);
    }


    public List<Bus> UpdateLFResultBus(List<Bus> busesLf, List<Branch> branchesLf, List<Bus> buses)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        // update the Bus with LF results and return the Bus
        for (var i = 0; i < busesLf.Count; i++)
        {
            var b = busesLf[i];
            var busToUpdate = buses.Where(bus => bus.Tag == b.Tag).ToList()[0];
            busToUpdate.Vo = b.Vo;
            busToUpdate.VoJSON = JsonSerializer.Serialize(b.Vo, jsonOptions);
        }

        return buses;
    }

    public List<Branch> UpdateLFResultBranch(List<Bus> busesLf, List<Branch> branchesLf, List<Branch> branches)
    {
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        // update the Branches with LF results and return the Branches
        for (var i = 0; i < branchesLf.Count; i++)
        {
            var br = branchesLf[i];
            var branchToUpdate = branches.Find(b => b.Tag == br.Tag);

            var BF = busesLf.Find(b => b.Tag == br.BfT);
            var BT = busesLf.Find(b => b.Tag == br.BtT);
            var In = (BF.Vo - BT.Vo) * br.Ypu; // pu current direction BF to BT
            var Ibf = 1000000 * _globalData.Sb / (Math.Pow(3, 0.5) * BF.Vb); // A at BfT node
            var Ibt = 1000000 * _globalData.Sb / (Math.Pow(3, 0.5) * BT.Vb); // A at BtT node
            var Sn1 = BF.Vo * Complex.Conjugate(In); // pu
            var Sn2 = BT.Vo * Complex.Conjugate(In); // pu
            var Loss = (BF.Vo - BT.Vo) * Complex.Conjugate(In); // pu
            br.Io = branchToUpdate.Io = Ibf > Ibt ? Ibf * In : Ibt * In; // A
            br.KW = branchToUpdate.KW = 1000 * _globalData.Sb * (float)Sn1.Real; // kW
            br.KVAR = branchToUpdate.KVAR = 1000 * _globalData.Sb * (float)Sn1.Imaginary; //kVAR

            var result = $"{i} : Branch {br.Tag} :: At {BF.Tag} I: {Math.Round(Ibf * In.Magnitude, 3)} A "
                         + $"{Math.Round(In.Phase * 180 / Math.PI, 3)} deg  S: ({Math.Round(br.KW, 3)}, {Math.Round(br.KVAR, 3)}), "
                         + $"At {BT.Tag} I: ({Math.Round(Ibt * In.Magnitude, 3)}, {Math.Round(In.Phase * 180 / Math.PI, 3)}) A  S: "
                         + $"({Math.Round(1000 * _globalData.Sb * Sn2.Real, 3)}, {Math.Round(1000 * _globalData.Sb * Sn2.Imaginary, 3)}) "
                         + $" Loss: ({Math.Round(1000 * _globalData.Sb * Loss.Real, 3)}, {Math.Round(1000 * _globalData.Sb * Loss.Imaginary, 3)})";
            //System.Diagnostics.Debug.WriteLine(result);
        }

        return branches;
    }


    public void DisplayResultLF(List<Bus> buses, List<Branch> branches)
    {
        // arrange Buses network wise
        buses = buses.OrderBy(bus => bus.Network).ThenByDescending(bus => bus.Vb).ToList();
        //results
        Debug.WriteLine("Complete Load Flow Result:");
        for (var i = 0; i < buses.Count; i++)
        {
            var b = buses[i];
            var result =
                $"{i + 1} : Bus: {b.Tag} ({b.VR / 1000}kV): Operating Voltage: {Math.Round(100 * (b.Vb / b.VR) * Complex.Abs(b.Vo), 2)}%  "
                + $"{Math.Round(b.Vo.Phase * 180 / Math.PI, 3)} deg  [{Math.Round(100 * (b.Vb / b.VR) * b.Vo.Real, 2)}, {Math.Round(100 * (b.Vb / b.VR) * b.Vo.Imaginary, 2)}]";
            Debug.WriteLine(result);
        }

        //
        Debug.WriteLine(" ");
        //
        // arrange Branches network wise
        branches = branches.OrderBy(branch => buses.Where(bus => bus.Tag == branch.BfT).ToList()[0].Network).ToList();
        for (var i = 0; i < branches.Count; i++)
        {
            var br = branches[i];
            var BF = buses.Where(b => b.Tag == br.BfT).ToList()[0];
            var BT = buses.Where(b => b.Tag == br.BtT).ToList()[0];
            var In = (BF.Vo - BT.Vo) * br.Ypu;
            var Ibf = 1000 * _globalData.Sb / (Math.Pow(3, 0.5) * BF.Vb); // A
            var Ibt = 1000 * _globalData.Sb / (Math.Pow(3, 0.5) * BT.Vb); // A
            var Sn1 = BF.Vo * Complex.Conjugate(In);
            var Sn2 = BT.Vo * Complex.Conjugate(In);
            var Loss = (BF.Vo - BT.Vo) * Complex.Conjugate(In);
            br.Io = branches.Where(b => b.Tag == br.Tag).ToList()[0].Io = Ibf > Ibt ? Ibf * In : Ibt * In;
            br.KW = branches.Where(b => b.Tag == br.Tag).ToList()[0].KW = 1000 * _globalData.Sb * (float)Sn1.Real;
            br.KVAR = branches.Where(b => b.Tag == br.Tag).ToList()[0].KVAR =
                1000 * _globalData.Sb * (float)Sn1.Imaginary;

            var result = $"{i + 1} : Branch {br.Tag} :: At {BF.Tag} I: {Math.Round(Ibf * In.Magnitude, 3)} A "
                         + $"{Math.Round(In.Phase * 180 / Math.PI, 3)} deg  S: ({Math.Round(br.KW, 3)}, {Math.Round(br.KVAR, 3)}), "
                         + $"At {BT.Tag} I: ({Math.Round(Ibt * In.Magnitude, 3)}, {Math.Round(In.Phase * 180 / Math.PI, 3)}) A  S: "
                         + $"({Math.Round(1000 * _globalData.Sb * Sn2.Real, 3)}, {Math.Round(1000 * _globalData.Sb * Sn2.Imaginary, 3)}) "
                         + $" Loss: ({Math.Round(1000 * _globalData.Sb * Loss.Real, 3)}, {Math.Round(1000 * _globalData.Sb * Loss.Imaginary, 3)})";
            Debug.WriteLine(result);
        }
        ////results
        //System.Diagnostics.Debug.WriteLine(" ");
        //for (int i = 0; i < BusesLF.Count; i++)
        //{
        //    Bus b = BusesLF[i];
        //    Buses.Where(bus => bus.Tag == b.Tag).ToList()[0].Vo = b.Vo;

        //    System.Diagnostics.Debug.WriteLine($"{i} : Bus Tag: {b.Tag} ({b.VR / 1000}kV): Operating Voltage: {Math.Round(100 * (b.Vb / b.VR) * Complex.Abs(b.Vo), 2)}%  "
        //        + $"{Math.Round(b.Vo.Phase * 180 / Math.PI, 3)} deg  [{Math.Round(100 * (b.Vb / b.VR) * b.Vo.Real, 2)}, {Math.Round(100 * (b.Vb / b.VR) * b.Vo.Imaginary, 2)}]");
        //}
        ////
        //System.Diagnostics.Debug.WriteLine(" ");
        ////
        //for (int i = 0; i < BranchesLF.Count; i++)
        //{
        //    Branch br = BranchesLF[i];
        //    Bus BF = BusesLF.Where(b => b.Tag == br.BfT).ToList()[0];
        //    Bus BT = BusesLF.Where(b => b.Tag == br.BtT).ToList()[0];
        //    Complex In = (BF.Vo - BT.Vo) * br.Ypu;
        //    var Ibf = 1000 * GlobalData.Sb / (Math.Pow(3, 0.5) * BF.Vb);   // A
        //    var Ibt = 1000 * GlobalData.Sb / (Math.Pow(3, 0.5) * BT.Vb);   // A
        //    Complex Sn1 = BF.Vo * Complex.Conjugate(In);
        //    Complex Sn2 = BT.Vo * Complex.Conjugate(In);
        //    Complex Loss = (BF.Vo - BT.Vo) * Complex.Conjugate(In);
        //    br.Io = Branches.Where(b => b.Tag == br.Tag).ToList()[0].Io = (Ibf > Ibt) ? Ibf * In : Ibt * In;
        //    br.KW = Branches.Where(b => b.Tag == br.Tag).ToList()[0].KW = 1000 * GlobalData.Sb * (float)Sn1.Real;
        //    br.KVAR = Branches.Where(b => b.Tag == br.Tag).ToList()[0].KVAR = 1000 * GlobalData.Sb * (float)Sn1.Imaginary;

        //    System.Diagnostics.Debug.WriteLine($"{i} : Branch {br.Tag} :: At {BF.Tag} I: {Math.Round(Ibf * In.Magnitude, 3)} A "
        //        + $"{Math.Round(In.Phase * 180 / Math.PI, 3)} deg  S: ({Math.Round(br.KW, 3)}, {Math.Round(br.KVAR, 3)}), "
        //        + $"At {BT.Tag} I: ({Math.Round(Ibt * In.Magnitude, 3)}, {Math.Round(In.Phase * 180 / Math.PI, 3)}) A  S: "
        //        + $"({Math.Round(1000 * GlobalData.Sb * Sn2.Real, 3)}, {Math.Round(1000 * GlobalData.Sb * Sn2.Imaginary, 3)}) "
        //        + $" Loss: ({Math.Round(1000 * GlobalData.Sb * Loss.Real, 3)}, {Math.Round(1000 * GlobalData.Sb * Loss.Imaginary, 3)})");
        //}
    }


    public Tuple<List<Bus>, List<Branch>> DisplayResultSC(List<Bus> busesSc, List<Branch> branchesSc,
        string faultyBusTag)
    {
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        //results
        List<Bus> StudyResultSCBus = new();
        List<Branch> StudyResultSCBranch = new();
        //
        var faultyBus = busesSc.Where(b => b.Tag == faultyBusTag).ToList()[0];
        var Is = new Complex(0, 0);
        var Ib = 1000000 * _globalData.Sb / (Math.Pow(3, 0.5) * faultyBus.Vb); // A
        var dispString = "";
        foreach (var br in branchesSc)
            if (br.BfT == faultyBusTag || br.BtT == faultyBusTag)
            {
                var I = new Complex(0, 0);
                var otherBus = busesSc.Where(b => b.Tag == (br.BfT == faultyBusTag ? br.BtT : br.BfT)).ToList()[0];
                if ((otherBus.Vo - faultyBus.Vo).Magnitude > 0.00001)
                    I = faultyBus.VR / faultyBus.Vb * (otherBus.Vo - faultyBus.Vo) *
                        br.Ypu; // later : no basis to multiply (faultyBus.V/ faultyBus.Vb)

                Is = Is + I;

                //Complex S = faultyBus.Vo * Complex.Conjugate(I);
                dispString +=
                    $" {otherBus.Tag}/{br.Tag} : {Math.Round(Ib * I.Magnitude / 1000, 3)} kA ({Math.Round(Ib * I.Real / 1000, 3)}, "
                    + $"{Math.Round(Ib * I.Imaginary / 1000, 3)})(V: {Math.Round(100 * otherBus.Vb * otherBus.Vo.Magnitude / otherBus.VR, 3)}%) ";
            }

        Debug.WriteLine($"Bus '{faultyBusTag}': {Math.Round(Ib * Is.Magnitude / 1000, 3)}kA "
                        + $"({Math.Round(Ib * Is.Real / 1000, 3)}, {Math.Round(Ib * Is.Imaginary / 1000, 3)}) [ Contribution from{dispString} ]");

        // update actual fault current in Busses Objects
        var tempBusList = busesSc.Where(b => b.Tag == faultyBusTag).ToList();
        if (tempBusList.Count != 0)
            busesSc.Where(b => b.Tag == faultyBusTag).ToList()[0].SCkAaMax =
                (float)Math.Round(Ib * Is.Magnitude / 1000, 2);
        faultyBus.SCkAaMax = (float)Math.Round(Ib * Is.Magnitude / 1000, 2);
        //

        // store result to save to the database
        StudyResultSCBus.Add(JsonSerializer.Deserialize<Bus>(JsonSerializer.Serialize(faultyBus, jsonOptions), jsonOptions));
        // add only results of those connected branches which are contrubiting to this 
        // faulty bus to be made as BusTo (BtT) and the other end buses as BusFrom (BfT)
        branchesSc.ForEach(scResultBranch =>
        {
            if (scResultBranch.BfT == faultyBus.Tag || scResultBranch.BtT == faultyBus.Tag)
            {
                if (scResultBranch.BfT == faultyBus.Tag)
                {
                    var tempswap = scResultBranch.BfT;
                    scResultBranch.BfT = scResultBranch.BtT;
                    scResultBranch.BtT = tempswap;
                }

                StudyResultSCBranch.Add(
                    JsonSerializer.Deserialize<Branch>(JsonSerializer.Serialize(scResultBranch, jsonOptions),jsonOptions));
            }
        });


        return new Tuple<List<Bus>, List<Branch>>(StudyResultSCBus, StudyResultSCBranch);
    }


    public List<Bus> DoShortCircuit(List<Bus> scBuses1, List<Branch> scBranches1, List<Load> scLoads1,
        int inetworkIndex)
    {
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        List<Bus> returnSCBuses = new List<Bus>();
        //
        double timerSC = 0;
        // start timer
        var SCb4 = DateTime.UtcNow;
        // baseSCbusResult to save base result to assign the initial bus voltage for SC studies for subsequent buses
        List<Bus> baseSCbusResult = new();
        List<SCBusVal> firstBusSCResult = new();
        var tempCount = 0;
        //
        // Doing SC study from 2nd bus onwards
        for (var ib = 0; ib < scBuses1.Count; ib++)
            //Parallel.For(0, SCBuses1.Count, ib =>
            //Bus faultyBus = SCBuses1[ib];
            //skip SC study for the dummy created bus
            // doing SC for only one tag due to long time iteration.
            if (scBuses1[ib].Category != "Swing")
            {
                var SCBuses11 = JsonSerializer.Deserialize<List<Bus>>(JsonSerializer.Serialize(scBuses1, jsonOptions), jsonOptions);
                var faultyBus = SCBuses11[ib];
                //System.Diagnostics.Debug.Write($"{ib} : Thread '{Thread.CurrentThread.ManagedThreadId}' strated for the SC calculation for faulty bus '{faultyBus.Tag}'....");
                ////DoTask(SCBuses1, SCBranches1, SCLoads1, ib);
                ////
                if (tempCount != 0)
                    foreach (var bus in SCBuses11)
                        //if (bus.C == "Variable" && bus.T != faultyBus.T) { bus.Vit = new Complex(1, 0); } // the non-faulty and non-swing bus initial voltage set to 1 pu
                        // the non-faulty initial voltage set to 1 pu
                        if (bus.Tag != faultyBus.Tag)
                        {
                            if (bus.Tag == firstBusSCResult[0].Tag)
                            {
                                // for the 1st bus of the 1st SC study Result baseSCbusResult
                                bus.Vit = new Complex(1, 0);
                            }
                            else // for the subsequent bus SC in SCBuses
                            {
                                bus.Vit = firstBusSCResult.Where(b => b.Tag == bus.Tag).ToList()[0].Vo;
                                if (bus.Vit == new Complex(0, 0)) bus.Vit = new Complex(1, 0);
                            }
                        }
                        else
                        {
                            // the faulty bus
                            bus.Vit = new Complex(0, 0);
                        }

                //
                var scResult = DoLoadFlow(_globalData.IterationSC, _globalData.PrecisionSC, SCBuses11, scBranches1,
                    scLoads1, "SC", faultyBus.Tag);
                var result =
                    $"{ib} : Thread {Thread.CurrentThread.ManagedThreadId} ({Math.Round(scResult.MS)}ms {scResult.IT} iterations): ";
                Debug.Write(result);
                var scStudtResult = DisplayResultSC(scResult.BusResult, scBranches1, faultyBus.Tag);
                var faultyBusStudyResult = scStudtResult.Item1.Where(bus => bus.Tag == faultyBus.Tag).ToList()[0];
                returnSCBuses.Add(faultyBusStudyResult);
                timerSC = timerSC + scResult.MS;
                // bus result is avialable in scResult.BusResult whereas the Branch result is updated in SCBranches1 by the DisplayResultSC function
                // storing 1st SC results Vit for utilising in SC studies of subsequent buses
                if (tempCount == 0)
                    firstBusSCResult =
                        JsonSerializer.Deserialize<List<SCBusVal>>(
                            JsonSerializer.Serialize(faultyBusStudyResult.SCResult, jsonOptions), jsonOptions);
                tempCount++;
            }

        ;

        var interval = DateTime.UtcNow - SCb4; // stop timer and calculate the time interval
        Debug.WriteLine(
            $"Short Circuit Study completed for Network {inetworkIndex + 1} in {interval.TotalMilliseconds} ms");
        //
        return returnSCBuses;
    }

    
    
    
    
    
    
    
    
    
}