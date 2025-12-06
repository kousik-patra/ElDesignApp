using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElDesignApp.Services;


using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ElDesignApp.Models;
using Dapper;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;
using Microsoft.AspNetCore.Components.Forms;
using OfficeOpenXml;
using ElDesignApp.Services;
using Force.DeepCloner;
using Microsoft.Extensions.Logging;


public interface ILayoutFunctionService
{
    
    
    ENU XY2EN(Vector3 xyz, string coordSystem = "LOCAL");
    Vector3 EN2XY(ENU enu);
    float String2Coordinate(string? coordinateString, char? coord);
    List<Vector3> LocationSize(string str);
    Vector3 LocationToVector(string locationString, string coordSystem = "");
    string? VectorToLocation(Vector3 point, string coordSystem = "");


    void SegmentUpdate(Segment segment);

    
    void SleeveUpdate(Sleeve sleeve);
    void SleevePointsUpdate(Sleeve sleeve);



    void BoardUpdate(Board board);
    void BoardFaceUpdate(Board board, string xyne);
    void BoardCentrePointUpdate(Board board);



    void LoadUpdate(Load load);

    void EquipmentUpdate(Equipment equipment);

    void CableRXUpdate(CableBranch cableBranch);

    void BusDuctRXUpdate(BusDuct busDuct);

    List<Bus> BranchBusUpdate(string tag, string category, string bfT, string btT, List<Bus> buses);

    void BranchUpdatePU(Branch branch);

    void TransformerUpdatePU(Transformer transformer);

    void LoadUpdatePU(Load load);
    
    void NodeMarginUpdate(Node node);
    
    
    
List<Segment> AssignFaceForASegmentList(List<Segment> segments);
Vector3 PerpendicularPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd);

double DistancePointToLine1(Vector3 vP, Vector3 vQ, Vector3 vR);

double DistancePointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd);

double DistancePointToLineSquared(Vector3 point, Vector3 lineStart, Vector3 lineEnd);
double AngleBetween(Vector3 a, Vector3 b);

bool Coplaner(Vector3 a, Vector3 b, Vector3 c, Vector3 d);
bool IsParallelOld(Vector3 a, Vector3 b, Vector3 c, Vector3 d);
bool IsParallel(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float tolerance = 1e-5f);
bool IsColinear(Vector3 a, Vector3 b, Vector3 c, Vector3 d);
bool IsColinear1(Vector3 A, Vector3 B, Vector3 C, Vector3 D);
bool IsOverlap(Vector3 A, Vector3 B, Vector3 C, Vector3 D, double gap);
Vector3 IntersectionPoint(Vector3 A, Vector3 B, Vector3 C, Vector3 D);
bool IsFarAway(Segment path1, Segment path2, double d = 0.1);
List<Vector3> IntersectionPoints(Vector3 A, Vector3 B, Vector3 C, Vector3 D);

List<Vector3> IntersectionPointDec(Vector3 A, Vector3 B, Vector3 C, Vector3 D, double gapA,
    double gapB, double gapC, double gapD);

List<NearTag>? TagMatches(string searchTag, List<Board> Boards, List<Load> Loads,
    List<Equipment> Equipments, int i = 3);

Vector3 FindLocationOfTagLevenshtein(string equipmentTag, string equipmentFeeder, List<Board> boards,
    List<Load> loads, List<Equipment> equipments);

string AssignFeeder(Cable thisCable, string boardTag, string feederTag, List<Cable> cables,
    List<Board> boards);

Tuple<List<Segment>, List<Bend>, List<Tee>, List<Cross>, List<Sleeve>, List<Node>> ReadSegNodeEtc(
    List<SegmentResult> segmentResult);

(Vector3, Vector3, float) FindClosestEnds(Vector3 a, Vector3 b, Vector3 c, Vector3 d);
List<Segment> AssignFaceForASegmentBranch(List<Segment> segments);

Task<Tuple<List<Segment>, List<Bend>, List<Tee>, List<Cross>, List<Node>, List<Segment>>>
    GenerateAcc(List<Segment>? wholeSegment);

bool IsSegsFarAway(Segment seg1, Segment seg2, double d = 0.1);

Tuple<List<Node>, List<Segment>> NodeWithJumpSegments(List<Node> nodeP,
    List<Segment> connectedSegmentP, List<Segment> isolatedSegmentP);

Tuple<List<Node>, List<Segment>> NodesInAnotherSegmentWithJump(List<Node> nodeP,
    List<Segment> segmentP);

List<Node> DeadEndNodesWithJump(List<Node>? nodes, float d = 1f);
String DrawLadderJSONPoints(double width, double height, Vector3 end1, Vector3 end2, Vector3 face);

string DrawBendJSONPoints(Vector3 X, Vector3 p1, Vector3 p2, float w1, float w2, Vector3 f1,
    Vector3 f2, float h1, float h2, int step);

string DrawTeeJSONPoints(Vector3 X, Vector3 P1, Vector3 P2, Vector3 P3, float w1, float w2, float w3,
    Vector3 f1, Vector3 f2, Vector3 f3, float h1, float h2, float h3, int step);

string DrawCrossJSONPoints(Vector3 X, Vector3 P1, Vector3 P2, Vector3 P3, Vector3 P4, float w1,
    float w2, float w3, float w4, Vector3 f1, Vector3 f2, Vector3 f3, Vector3 f4, float h1, float h2, float h3,
    float h4, int step);

List<Vector3> GetBendPoints(Vector3 X, Vector3 P1, Vector3 P2, Vector3 P_opposite, float w1, float w2,
    Vector3 f1, Vector3 f2, float h1, float h2, int step);

Vector3 GetPointQuadraticBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2);
string DrawSleeveJSONPoints(List<Vector3> points, double bendingRadius);
string DrawCableJSONPoints(Cable cable, List<Segment> Segment);

Tuple<String, String, float, String, String> DrawCableJSONPointsArranged(Cable cable,
    List<Segment> segments);

Tuple<List<Node>, List<Cable>> UpdateNodeCable(List<Cable> cables, List<Cable> Cable,
    List<Segment> segments, List<Sleeve> Sleeve, List<Node> nodes);

Tuple<bool, int> PointInsideSegment(Vector3 X, Segment seg);

Tuple<List<String>, List<Vector3>, String> AStar(
    Cable cable, double od, double marginFor, Vector3 startV, Vector3 goalV, String routeCriteria,
    List<Node> nodeR, List<Segment> segments, List<Cable> cables, int n, List<Spacing> spacing);

List<Vector3> FindPath(List<Node> nodelist, Vector3 startPoint, Vector3 goalPoint,
    float pathWidth, float jumpDistance);

NodePath? ProcessOpenSet(PriorityQueue<NodePath?, float> openSet, HashSet<String> closedSet,
    Dictionary<string, NodePath?> nodesDict, HashSet<string> otherClosedSet,
    Vector3 otherStartGoal, List<NodePath?> allNodes, float pathWidth);

NodePath? FindPathWithJump(Dictionary<String, NodePath?> forwardNodes,
    Dictionary<string, NodePath?> backwardNodes, List<NodePath?> allNodes, float jumpDistance);

NodePath? FindPathWithBestJump(Dictionary<String, NodePath> forwardNodes,
    Dictionary<String, NodePath?> backwardNodes, List<NodePath?> allNodes, float jumpDistance);

List<Vector3> ReconstructPath(Dictionary<string, NodePath?> forwardNodes,
    Dictionary<string, NodePath?> backwardNodes, NodePath? meetingNode);

float SpacingBeteenCables(Cable cable1, Cable cable2, List<Spacing> spacings);

double SpaceForCable(string cableTag, Node node, List<Cable> cables, List<Spacing> spacings,
    string fieldCriteria = "");

List<Node> NearestDummyNodesOnSegments(Vector3 p, double d, List<Segment> segments, string originTag,
    string routeCriteria, double od, List<Node> nodes);

List<Node> NearestSleeveNodes(Vector3 p, double range, String routeCriteria, double od,
    List<Node> nodes);




    
}



public class LayoutFunctionService : ILayoutFunctionService
{
    private readonly IGlobalDataService _globalData; 
    private readonly IMyFunctionService _myFunction;
    private readonly ILogger<DataRetrievalService> _logger;

    // Inject IGlobalDataService into the constructor
    public LayoutFunctionService(IGlobalDataService globalData, IMyFunctionService myFunction, 
        ILogger<DataRetrievalService> logger)
    {
        _globalData = globalData;
        _myFunction = myFunction;
        _logger = logger;
    }
    
    public readonly JsonSerializerOptions jsonSerializerOptions = new() { IncludeFields = true };

        /// <summary></summary>
    public ENU XY2EN(Vector3 xyz, string coordSystem = "LOCAL")
    {
        ENU enu = new(0f,0f,0f);

        if (_globalData.PlotPlans != null)
        {
            var keyPlotPlan = _globalData.PlotPlans.Find(plan => plan.KeyPlan);
        
            if (keyPlotPlan == null) return enu;

            var x1 = keyPlotPlan?.X1 ?? 0f;
            var x2 = keyPlotPlan?.X2 ?? 0f;
            var y1 = keyPlotPlan?.Y1 ?? 0f;
            var y2 = keyPlotPlan?.Y2 ?? 0f;

            if (coordSystem.ToUpper() == "GLOBAL")
            {
                enu.E = keyPlotPlan?.GlobalE ?? 0f;
                enu.N = keyPlotPlan?.GlobalN ?? 0f;
            }
            else
            {
                enu.E = keyPlotPlan?.LocalE ?? 0f;
                enu.N = keyPlotPlan?.LocalN ?? 0f;
            }

            //check the coordinate orientation        
            if (keyPlotPlan is { XEW: true })
            {
                // X-Axis : East -> West
                enu.E += xyz.X * (x2 > x1 ? 1 : -1);
                enu.N += xyz.Y * (y2 > y1 ? 1 : -1);
            }
            else
            {
                // X-Axis : North -> South
                enu.N += xyz.X * (x2 > x1 ? 1 : -1);
                enu.E += xyz.Y * (y2 > y1 ? 1 : -1);
            }
        }

        enu.U = xyz.Z;
        //
        return enu;
    }

    /// <summary></summary>
    public Vector3 EN2XY(ENU enu)
    {
        Vector3 xyz = new(enu.E, enu.N, enu.U);
        
        var keyPlotPlan = _globalData.PlotPlans?.Find(plan => plan.KeyPlan);
        if (keyPlotPlan == null) return xyz;
        
        var x1 = keyPlotPlan?.X1 ?? 0f;
        var x2 = keyPlotPlan?.X2 ?? 0f;
        var y1 = keyPlotPlan?.Y1 ?? 0f;
        var y2 = keyPlotPlan?.Y2 ?? 0f;

        if (keyPlotPlan is { XEW: true })
        {
            // X-Axis : East -> West
            xyz.X = (enu.E - x1) * (x2 > x1 ? 1 : -1);
            xyz.Y = (enu.N - y1) * (y2 > y1 ? 1 : -1);
        }
        else
        {
            // X-Axis : North -> South
            xyz.X = (enu.N - x1) * (x2 > x1 ? 1 : -1);
            xyz.Y = (enu.E - y1) * (y2 > y1 ? 1 : -1);
        }

        xyz.Z = enu.U;
        //
        return xyz;
    }



    /// <summary></summary>
    public float String2Coordinate(string? coordinateString, char? coord)
    {
        // searches the coordinate value of either E/N/U or W/S/D from a given combined string 
        if (coordinateString == null || coord == null) return 0;

        coord = coord switch
        {
            'E' when coordinateString.Contains('W') => 'W',
            'N' when coordinateString.Contains('S') => 'S',
            'U' when coordinateString.Contains('D') => 'D',
            _ => coord
        };

        float x = 0;
        if (coordinateString.Contains((char)coord))
        {
            var strR = coordinateString.Replace((char)coord, 'A');
            var match1 = Regex.Match(strR, @"[A][:][-]?[0-9]{1,4}[\.]?[0-9]{0,6}");
            var match2 = Regex.Match(strR, @"[A][ ]{0,4}[:]?[ ]{0,4}[0-9]{0,9}[\.]?[0-9]{1,9}[ ]{0,4}[m]{0,2}");
            if (match1.Success)
            {
                x = float.Parse(match1.Captures[0].Value.Replace("A:", ""));
            }
            else if (match2.Success)
            {
                var xstring = match2.Captures[0].Value.Replace("A", "");
                if (xstring.Contains(' ')) xstring = xstring.Replace(" ", "");

                if (xstring.Contains(':')) xstring = xstring.Replace(":", "");

                //
                if (xstring.Contains("mm"))
                    x = float.Parse(xstring.Replace("mm", "")) / 1000;
                else if (xstring.Contains('m'))
                    x = float.Parse(xstring.Replace("m", ""));
                else
                    x = float.Parse(xstring);
            }
            else
            {
                Debug.WriteLine(
                    $"Data : \" {coordinateString} \" in wrong format. Only acceptable format : \" E/N/U : 123456789.123456789 m/mm \".");
            }
        }

        if (coordinateString.Contains('W') || coordinateString.Contains('S') ||
            coordinateString.Contains('D'))
            x = -x;
        return x;
    }


    /// <summary></summary>
    public List<Vector3> LocationSize(string? str)
    {
        //^[E][:][-]?[0-9]{1,4}[\.]?[0-9]{0,3}[ ]{0,4}[N][:][-]?[0-9]{1,4}[\.]?[0-9]{0,3}[ ]{0,4}[U][:][-]?[0-9]{1,4}[\.]?[0-9]{0,3}[ ][B][:][-]?[0-9]{1,4}[\.]?[0-9]{0,3}[ ]{0,4}[D][:][-]?[0-9]{1,4}[\.]?[0-9]{0,3}[ ]{0,4}[H][:][-]?[0-9]{1,4}[\.]?[0-9]{0,3}[\ ]?[m]{0,2}?[\ ]?[a-zA-Z0-9]{0,10}?$
        // "E:3.499 N:0 U:0 B:2 D:1 H:1 mm Global"
        // "E:3.499 N:0 U:0 B:2 D:1 H:1 m"
        Vector3 location = new();
        Vector3 shape = new();
        if (string.IsNullOrEmpty(str)) return [location, shape];
        
        var e = String2Coordinate(str, 'E');
        var n = String2Coordinate(str, 'N');
        var u = String2Coordinate(str, 'U');
        var le = String2Coordinate(str, 'B');
        var ln = String2Coordinate(str, 'D');
        var lu = String2Coordinate(str, 'H');

        location = EN2XY(new ENU(e, n, u));
            
        if(_globalData.SelectedProject is { XEW: true })
            shape = new Vector3(_globalData.SelectedProject.XEW ? le : ln,
                _globalData.SelectedProject.XEW ?  ln : le, lu);


        return [location, shape];
    }


    /// <summary></summary>
    public Vector3 LocationToVector(string locationString, string coordSystem = "")
    {
        // this function accpets the Global/Local/UTM coordinate and convert to Vector3 (XYZ) based on the choosen coordinate system
        
        Vector3 point = new();
        if (_globalData.PlotPlans.Count == 0) return point;

        try
        {
            var e = String2Coordinate(locationString, 'E');
            var n = String2Coordinate(locationString, 'N');
            var u = String2Coordinate(locationString, 'U');

            // if global system of coordinate, then calculate xy differently
            if (coordSystem.ToUpper() == "XYZ")
            {
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true
                };
                point = JsonSerializer.Deserialize<Vector3>(locationString, options);
            }
            else if (coordSystem.ToUpper() == "GLOBAL")
            {
                var globalE = _globalData.PlotPlans?[0].GlobalE ?? 0f;
                var globalN = _globalData.PlotPlans?[0].GlobalN ?? 0f;
                var xyGlobal = EN2XY(new ENU(e - globalE, n - globalN));
                point = new Vector3(xyGlobal.X, xyGlobal.Y, u);
            }
            else
            {
                // assuming "Local" coordinate system
                var xy = EN2XY(new ENU(e, n));
                //CentrePoint = MyFunction.ENUString2XYZ(CentrePointS); not used as ENUString2XYZ is for Global Coordinates (segments)
                point = new Vector3(xy.X, xy.Y, u);
            }
        }
        catch(Exception e)
        {
            Debug.WriteLine(e);
        }
        return point;
    }

    /// <summary></summary>
    public string? VectorToLocation(Vector3 point, string coordSystem = "")
    {
        // this function is opposite to LocationToVector
        // accepts Vector3 and convert to the Global/Local/UTM coordinate based on the chosen coordinate system
        //
        if (_globalData.PlotPlans?.Count == 0) return "";
        if (coordSystem.ToUpper() == "XYZ")
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            return JsonSerializer.Serialize(point, options);
        }

        //
        var enu = XY2EN(point, coordSystem);

        //
        var eS = Math.Round(enu.E, 3).ToString();
        var nS = Math.Round(enu.N, 3).ToString();
        var uS = Math.Round(enu.U, 3).ToString();
        //
        return $"E:{eS}, N:{nS}, U:{uS}";
    }

    
    
    
    public void SegmentUpdate(Segment segment)
    {
        if (_globalData.PlotPlans.Count == 0) return;
        
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        if (segment.UID == Guid.Empty) segment.UID = Guid.NewGuid();
        ;
        segment.End1SegmentConnection ??= [];
        ;
        segment.End2SegmentConnection ??= [];
        ;
        segment.AllowableTypes = segment.AllowableTypesS != null && segment.AllowableTypesS.Contains(',')
            ? segment.AllowableTypesS.Split(',').ToList()
            : [];
        //
        //End1 = MyFunction.ENUString2XYZ(End1S);
        if (segment.End1S != null)
            if (segment.CoordSystem != null)
                segment.End1 = LocationToVector(segment.End1S, segment.CoordSystem);
        //End2 = MyFunction.ENUString2XYZ(End2S);
        if (segment.End2S != null)
            if (segment.CoordSystem != null)
                segment.End2 = LocationToVector(segment.End2S, segment.CoordSystem);
        //
        segment.Length = (segment.End1 - segment.End2).Length();
        segment.End1Array = string.IsNullOrEmpty(segment.End1ArrayS)
            ? []
            : JsonSerializer.Deserialize<List<Vector3>>(segment.End1ArrayS, jsonSerializerOption);
        segment.End2Array = string.IsNullOrEmpty(segment.End2ArrayS)
            ? []
            : JsonSerializer.Deserialize<List<Vector3>>(segment.End2ArrayS, jsonSerializerOption);
        segment.AllowableTypesArray = string.IsNullOrEmpty(segment.AllowableTypesArrayS)
            ? []
            : JsonSerializer.Deserialize<List<List<string>>>(segment.AllowableTypesArrayS, jsonSerializerOption);

        // if ((End1ArrayS != null && End1ArrayS != "") &&
        //     (End2ArrayS != null && End2ArrayS != "") &&
        //     (AllowableTypesArrayS != null && AllowableTypesArrayS != ""))
        // {
        //     End1Array = JsonSerializer.Deserialize<List<Vector3>>(End1ArrayS,jsonSerializerOption);
        //     End2Array = JsonSerializer.Deserialize<List<Vector3>>(End2ArrayS,jsonSerializerOption);
        //     AllowableTypesArray = JsonSerializer.Deserialize<List<List<string>>>(AllowableTypesArrayS,jsonSerializerOption);
        // }
        // else
        // {
        //     End1Array = new() { };
        //     End2Array = new() { };
        //     AllowableTypesArray = new() { };
        // }
        //
        // face update
        // if auto face update is required then the separate FaceUpdate functon to be called

        // Default expected to be "NE" i.e., X is towards "N" and "Y" is toward East
        var xyne = _globalData.PlotPlans?.FirstOrDefault()?.XY ?? "NE";
        
        var e = (float)(segment.FaceS.Contains('E') || segment.FaceS.Contains('e') ? 1 :
            segment.FaceS.Contains('W') || segment.FaceS.Contains('w') ? -1 : 0);
        var n = (float)(segment.FaceS.Contains('N') || segment.FaceS.Contains('n') ? 1 :
            segment.FaceS.Contains('S') || segment.FaceS.Contains('s') ? -1 : 0);
        var u = (float)(segment.FaceS.Contains('U') || segment.FaceS.Contains('u') ? 1 :
            segment.FaceS.Contains('D') || segment.FaceS.Contains('d') ? -1 : 0);
        //
        if (e == 0f && n == 0f && u == 0) n = -1; // default Fcae towards -ve Y axis
        segment.Face = Vector3.Normalize(new Vector3(xyne == "EN" ? e : n, xyne == "EN" ? n : e, u));
        if (Vector3.Cross(segment.End1, segment.End2).Equals(Vector3.Zero))
        {
            segment.Face = new Vector3(1f, 0f, 0f);
        }

    }
    
    
    
    public void SleevePointsUpdate(Sleeve sleeve)
    {
        try
        {
            sleeve.Points = [];
            var pointsArray = sleeve.PointsS.Contains(',') ? sleeve.PointsS.Split(',').ToList() : [];
            pointsArray.ForEach(p => sleeve.Points.Add(LocationToVector(p, sleeve.CoordSystem)));
        }
        catch (Exception e)
        {
            _logger.LogError("{DateTime:hh.mm.ss.ffffff} : SleevePointsUpdate Error for " +
                             "tag {SleeveTag}, Points: '{SleevePointsS}',  Error: {EMessage}", 
                DateTime.Now, sleeve.Tag, sleeve.PointsS, e.Message);
        }
    }
    
    public void SleeveUpdate(Sleeve sleeve)
    {
        if (sleeve.UID == Guid.Empty) sleeve.UID = Guid.NewGuid();
        SleevePointsUpdate(sleeve);
    }
    
   

    public void BoardUpdate(Board board)
    {
        if (_globalData.PlotPlans.Count == 0) return;

        if (board.UID == Guid.Empty) board.UID = Guid.NewGuid();
        ;
        board.Tag = board.Tag.Trim();
        BoardCentrePointUpdate(board);
        
        // Default expected to be "NE" i.e., X is towards "N" and "Y" is toward East
        var xyne = _globalData.PlotPlans?.FirstOrDefault()?.XY ?? "NE";

        if (board.FaceS == null)
            board.FaceS = "?";
        else
            BoardFaceUpdate(board, xyne);
        //switch (xyne)
        //{
        //    case "EN": // EN x axix east/west and y axis north/south
        //        Face = new Vector3((FaceS == "E") ? 1 : (FaceS == "W") ? -1 : 0, (FaceS == "N") ? 1 : (FaceS == "S") ? -1 : 0, 0);
        //        break;
        //    default: // NE x axix north/south and y axis east/west
        //        Face = new Vector3((FaceS == "N") ? 1 : (FaceS == "S") ? -1 : 0, (FaceS == "E") ? 1 : (FaceS == "W") ? -1 : 0, 0);
        //        break;
        //}

        board.PanelTag =board.PanelTagS != null && board.PanelTagS.Contains(',') ? board.PanelTagS.Split(',').ToList() : [];
        
        
        board.PanelWidth = board.PanelWidthS != null && board.PanelWidthS.Contains(',')
            ? board.PanelWidthS.Split(',').ToList().Select(x => double.Parse(x)).ToList()
            : [];
        
        //PanelWidth = JsonConvert.DeserializeObject<List<double>>(PanelWidthS);
        if (board.PanelTag.Count < board.Panels)
            for (var i = board.PanelTag.Count; i < board.Panels; i++)
                board.PanelTag.Add("#" + (i + 1).ToString("D2"));

        board.PanelTagS = string.Join(",", board.PanelTag.ToArray());
        var providedtotalWidth = board.PanelWidth.Take(Math.Min(board.PanelWidth.Count, board.Panels)).Sum(w => w);
        if (board.PanelWidth.Count < board.Panels && providedtotalWidth < board.Width)
        {
            var width = Math.Round((board.Width - providedtotalWidth) / (board.Panels - board.PanelWidth.Count), 3);
            for (var i = board.PanelWidth.Count; i < board.Panels; i++) board.PanelWidth.Add(width);
        }

        board.PanelWidthS = string.Join(",", board.PanelWidth.ToArray());
        //
        var panelDirection = Vector3.Cross(new Vector3(0, 0, 1), board.Face); // left to right
        board.PanelPosition = new List<Vector3>();
        board.PanelPositionS = ""; // for debug
        var height = new Vector3(0, 0, (float)board.Height);
        for (var i = 0; i < board.Panels; i++)
        {
            var l = (i == 0 ? 0 : board.PanelWidth.Take(i).Sum(w => w)) + board.PanelWidth[i] / 2;
            board.PanelPosition.Add(board.CentrePoint + (float)(l - board.Width / 2) * panelDirection + height);
            board.PanelPositionS = board.PanelPositionS + (i != 0 ? ", " : "") + "(" + board.PanelPosition.Last().X.ToString("#.##") +
                                   "," + board.PanelPosition.Last().Y.ToString("#.##") + "," +
                                   board.PanelPosition.Last().Z.ToString("#.##") + ")";
        }
    }
    
    public void BoardFaceUpdate(Board board, string xyne)
    {
        var e = (float)(board.FaceS.Contains('E') || board.FaceS.Contains('e') ? 1 :
            board.FaceS.Contains('W') || board.FaceS.Contains('w') ? -1 : 0);
        var n = (float)(board.FaceS.Contains('N') || board.FaceS.Contains('n') ? 1 :
            board.FaceS.Contains('S') || board.FaceS.Contains('s') ? -1 : 0);
        var u = (float)(board.FaceS.Contains('U') || board.FaceS.Contains('u') ? 1 :
            board.FaceS.Contains('D') || board.FaceS.Contains('d') ? -1 : 0);
        //
        if (e == 0f && n == 0f && u == 0) n = -1; // default Fcae towards -ve Y axis
        board.Face = Vector3.Normalize(new Vector3(xyne == "EN" ? e : n, xyne == "EN" ? n : e, u));
    }
    
    public void BoardCentrePointUpdate(Board board)
    {
        var e = String2Coordinate(board.CentrePointS, 'E');
        var n = String2Coordinate(board.CentrePointS, 'N');
        var u = String2Coordinate(board.CentrePointS, 'U');
        board.CentrePoint = EN2XY(new ENU(e, n, u));
    }
    
    
    
    
    public void LoadUpdate(Load load)
    {
        if (load == null)
        {
            throw new ArgumentNullException(nameof(load));
            return;
        }
        ////var load = Functions.UpdateRating(this);
        //if(VRS == 0f) { VR = 400f; } else { VR = VRS; }
        //if (PAS == 0f) { PA = R; } else { PA = PAS; }

        // assign default blank CSS properties for all its field to be displayed in the Table
        load.CellCSS = [];
        var type = load.GetType();
        List<PropertyInfo> tableProperties = typeof(Load).GetProperties().ToList();
        tableProperties.ForEach(property => load.CellCSS.Add(""));
        //
        // assigning default power factor and  efficiency and Dynamic (Load) Ratio as per load category 
        switch (load.Category)
        {
            case "Motor":
                if (load.Pf == default) load.Pf = 0.8f;
                if (load.Eff == default) load.Eff = 0.9f;
                if (load.DR == default) load.DR = 1f;
                break;
            case "Heater":
                if (load.Pf == default) load.Pf = 1f;
                if (load.Eff == default) load.Eff = 1f;
                if (load.DR == default) load.DR = 0f;
                break;
            case "Capacitor":
                if (load.Pf == default) load.Pf = -0.02f;
                if (load.Eff == default) load.Eff = 100f;
                if (load.DR == default) load.DR = 0f;
                break;
            case "Lump":
            case "Miscellaneous":
            default:
                if (load.Pf == default) load.Pf = 0.85f;
                if (load.Eff == default) load.Eff = 100f;
                // 80% dynamic load and balance 20% static load
                if (load.DR == default) load.DR = 0.8f;
                break;
        }

        //
        // correct the power factor based on the unit type
        //
        if ((load.Unit == "VAR" || load.Unit == "kVAR" || load.Unit == "MVAR") && load.Pf == 1)
            // force the pf to a reasonable value (say 0.1)
            // load with reactive power cannot have unity power factor
            load.Pf = -0.02f;
        //
        if ((load.Unit == "W" || load.Unit == "kW" || load.Unit == "MW") && load.Pf == 0f)
            // force the pf to a reasonable value (say 0.9)
            // load with active power cannot have zero power factor
            load.Pf = 0.8f;
        //
        switch (load.Unit)
        {
            case "A":
                load.SR = (float)(Math.Sqrt(load.Ph) * load.VR * load.R) / 1000;
                break;
            case "VA":
                load.SR = load.R / 1000;
                break;
            case "kVA":
                load.SR = load.R;
                break;
            case "MVA":
                load.SR = 1000 * load.R;
                break;
            case "VAR":
                load.SR = (float)(load.R / 1000 / Math.Sqrt(1 - load.Pf * load.Pf));
                break;
            case "kVAR":
                load.SR = (float)(load.R / Math.Sqrt(1 - load.Pf * load.Pf));
                break;
            case "MVAR":
                load.SR = (float)(load.R * 1000 / Math.Sqrt(1 - load.Pf * load.Pf));
                break;
            case "W":
                load.SR = load.R / 1000 / load.Pf;
                break;
            case "MW":
                load.SR = load.R * 1000 / load.Pf;
                break;
            case "kW":
            default:
                load.SR = load.R / load.Pf;
                break;
        }

        //
        load.IR = (float)(1000 * load.SR / Math.Sqrt(load.Ph) / load.VR);
        load.PR = load.SR * load.Pf;
        //
        if (load.PA == default) load.PA = load.R * load.LF / 100;
        if (load.LF == 100) load.LF = load.PA * 100 / load.R;
        ;
        //
        // calculate operating power factor and operating efficiency  
        switch (load.Category)
        {
            case "Motor":
                // determine operating power factor and operating efficiency as per actual (absorbed) Power, i.e., Load Factor
                // motor data is already ordered in the NavMeno page in descending order
                var motorsData = _globalData.MotorData.Where(motor => motor.RatedkW > load.PR).ToList();
                var pfJSON = "";
                var effJSON = "";
                var ist = 0f;
                var pfst = 0f;
                if (motorsData.Count > 0)
                {
                    pfJSON = motorsData.Last().PowerFactorJSON;
                    effJSON = motorsData.Last().EfficiencyJSON;
                    ist = (float)motorsData.Last().LRC;
                    pfst = (float)motorsData.Last().LRPF;
                }
                else
                {
                    pfJSON = "[[100,75,50,25],[0.92,0.89,0.85,0.8]]";
                    effJSON = "[[100,75,50,25],[94,90,86,82]]";
                    ist = 7.2f;
                    pfst = .4f;
                }
                //JsonSerializerOptions? jsonSerializerOption = new() { IncludeFields = true };
                //var pf = JsonSerializer.Deserialize<double[][]>(string.IsNullOrEmpty(load.PfJSON)? pfJSON: load.PfJSON , jsonSerializerOption);
                
                JsonSerializerOptions jsonSerializerOption = new() { IncludeFields = true };
                
                pfJSON = "[[100,75,50,25],[0.92,0.89,0.85,0.8]]";

                double[][] pf;
                try
                {
                    // Use load.PfJSON if it's not empty and is valid JSON; otherwise, use pfJSON
                    string jsonToDeserialize = string.IsNullOrEmpty(load.PfJSON) || !IsValidJson(load.PfJSON)
                        ? pfJSON
                        : load.PfJSON;

                    pf = JsonSerializer.Deserialize<double[][]>(jsonToDeserialize, jsonSerializerOption)
                         ?? throw new JsonException("Deserialization returned null.");
                }
                catch (JsonException ex)
                {
                    // Log the error (optional) and use default pfJSON
                    Console.WriteLine($"JSON deserialization failed: {ex.Message}");
                    pf = JsonSerializer.Deserialize<double[][]>(pfJSON, jsonSerializerOption)
                         ?? throw new JsonException("Default pfJSON deserialization failed.");
                }
                var polyPf = _myFunction.Polynomial(pf[0], pf[1], 2);
                load.PfO = (float)(polyPf[2] * load.LF * load.LF + polyPf[1] * load.LF + polyPf[0]);
                
                
                effJSON = "[[100,75,50,25],[94,90,86,82]]";
                
                double[][] eff;
                try
                {
                    // Use load.PfJSON if it's not empty and is valid JSON; otherwise, use pfJSON
                    string jsonToDeserialize = string.IsNullOrEmpty(load.EffJSON) || !IsValidJson(load.EffJSON)
                        ? effJSON
                        : load.EffJSON;

                    eff = JsonSerializer.Deserialize<double[][]>(jsonToDeserialize, jsonSerializerOption)
                          ?? throw new JsonException("Deserialization returned null.");
                }
                catch (JsonException ex)
                {
                    // Log the error (optional) and use default pfJSON
                    Console.WriteLine($"JSON deserialization failed: {ex.Message}");
                    eff = JsonSerializer.Deserialize<double[][]>(effJSON, jsonSerializerOption)
                          ?? throw new JsonException("Default effJSON deserialization failed.");
                }
                var polyEff = _myFunction.Polynomial(eff[0], eff[1], 2);
                load.EffO = (float)(polyEff[2] * load.LF * load.LF + polyEff[1] * load.LF + polyEff[0]);
                //
                load.Ist = load.Ist != 0f ? load.Ist : ist;
                load.Pfst = load.Pfst != 0f ? load.Pfst : pfst;
                break;
            default:
                load.PfO = load.Pf;
                load.EffO = load.Eff;
                break;
        }
        
        // Helper method to validate JSON
        bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonDocument.Parse(json); // Attempt to parse JSON
                return true;
            }
            catch
            {
                return false;
            }
        }

        //
        // calculate operating V, I P, Q and S as per the operating load factor (pf and eff)
        load.P = load.PfO * load.SR * (load.LF / 100) / (load.EffO / 100);
        load.Q = (float)Math.Sqrt(1 - load.PfO * load.PfO) * load.SR * (load.LF / 100) / (load.EffO / 100);
        load.S = (float)Math.Sqrt(load.P * load.P + load.Q * load.Q);
        //
        load.V = load.VR;
        load.I = (float)(1000 * load.S / Math.Sqrt(load.Ph) / load.V);
        //
        //
        //var desc = Functions.CableRunCoreAndSize(this.DescM);
        //Rn = (int)desc[0]; C = desc[1]; Sp = desc[2]; Sn = desc[3]; Spe = desc[4];
        //DescM = (RnM > 1 ? RnM + "Rx" : "") + CM + "Cx" + SpM;

        //var descA = Functions.CableRunCoreAndSize(this.DescA);
        //RnA = (int)descA[0]; CA = descA[1]; SpA = descA[2]; SnA = descA[3]; SpeA = descA[4];
        //
        // check later : e/n/u could be either GLOBAL or LOCAL
        var locationshape = LocationSize(load.LocationSize);
        load.Location = locationshape[0];
        load.Size = locationshape[1];
        //
        // calculation of voltage drop and selection of Auto Cable Size
        //
        // cable length update later
        if (load.L == 0) load.L = 300.3f;
        ;
        if (load.L != 0)
        {
            // auto cable sizing and voldage drops as per auto size
            load.MatA = "Cu";

            
            var AutoResult = FindCableSize(load, load.MatA, _globalData.DefaultAvailableCableSizes1C, 
                _globalData.DefaultAvailableCableSizesMC, _globalData.CableData);
            load.RnA = (int)AutoResult[0];
            load.CA = (int)AutoResult[1];
            load.SpA = AutoResult[2];
            load.CblDescA = (load.RnA > 1 ? load.RnA + "Rx" : "") + load.CA + "Cx" + load.SpA;
            var vdA = VoltageDrop(load, "Cu", "A", _globalData.CableData);
            load.VdRA = vdA[0];
            load.VdSA = vdA[1];

            // voltage drop as per given manual size
            if (!string.IsNullOrEmpty(load.CblDescM))
            {
                var descM = CableRunCoreSizeAndMat(load.CblDescM);
                load.RnM = (int)descM[0];
                load.CM = descM[1];
                load.SpM = descM[2];
                load.SnM = descM[3];
                load.SpeM = descM[4];
                load.MatM = descM[5] == 1f ? "Al" : "Cu";
                var vdM = VoltageDrop(load, load.MatM, "M", _globalData.CableData);
                load.VdRM = vdM[0];
                load.VdSM = vdM[1];
            }
        }

        //
        // if selection is auto size then select all the parameters as per AutoSizing , else manual
        if (load.CblAuto || string.IsNullOrEmpty(load.CblDescM))
        {
            load.Rn = load.RnA;
            load.C = load.CA;
            load.Sp = load.SpA;
            load.Sn = load.SnA;
            load.Spe = load.SpeA;
            load.CblDesc = load.CblDescA;
            load.VdR = load.VdRA;
            load.VdS = load.VdSA;
        }
        else
        {
            load.Rn = load.RnM;
            load.C = load.CM;
            load.Sp = load.SpM;
            load.Sn = load.SnM;
            load.Spe = load.SpeM;
            load.CblDesc = load.CblDescM;
            load.VdR = load.VdRM;
            load.VdS = load.VdSM;
        }

        //
        // assign CSS for any specific cases
        // if absorbed power is more than the rating - waring
        var propertyInfoPA = type.GetProperty("PA");
        if (propertyInfoPA == null)
        {
            throw new InvalidOperationException("Property 'PA' not found on type Load.");
        }

        if (load.PA > load.R)
                load.CellCSS[tableProperties.IndexOf(propertyInfoPA)] = "bg-danger";
            else if (load.PA > 0.9f * load.R) load.CellCSS[tableProperties.IndexOf(propertyInfoPA)] = "bg-warning";

        //
        // if the calculated manual running voltage drop is more than allowed limit - warning
        var propertyInfoVdRM = type.GetProperty("VdRM");
        if (propertyInfoVdRM == null)
        {
            throw new InvalidOperationException("Property 'VdRM' not found on type Load.");
        }

        if (load.VdRM > 1.1f * load.AlVdR)
                load.CellCSS[tableProperties.IndexOf(propertyInfoVdRM)] = "bg-danger";
            else if (load.VdRM > load.AlVdR) load.CellCSS[tableProperties.IndexOf(propertyInfoVdRM)] = "bg-warning";
        
        // if calculated manual starting voltage drop is more than allowed limit - warning

        var propertyInfoVdSM = type.GetProperty("VdSM");
        if (propertyInfoVdSM == null)
        {
            throw new InvalidOperationException("Property 'VdSM' not found on type Load.");
        }
        
            if (load.VdSM > 1.1f * load.AlVdS)
                load.CellCSS[tableProperties.IndexOf(propertyInfoVdSM)] = "bg-danger";
            else if (load.VdSM > load.AlVdS) load.CellCSS[tableProperties.IndexOf(propertyInfoVdSM)] = "bg-warning";
        

        //
        load.PhaseIdentifier = _globalData.DefaultPhaseIdentifier; // For Cable Tag no creation
        load.RunSequenceIdentifier = _globalData.DefaultRunSequenceIdentifier; // For Cable Tag no creation
        
    }
    
    
    
    
    
    public void EquipmentUpdate(Equipment equipment)
    {
        if (_globalData.PlotPlans.Count == 0) return;
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        if (equipment.UID == Guid.Empty) equipment.UID = Guid.NewGuid();
        ;
        try
        {
            equipment.Tag = equipment.Tag.Trim();
        }
        catch
            (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        equipment.CentrePoint = LocationToVector(equipment.CentrePointS, equipment.CoordSystem);
        if (string.IsNullOrEmpty(equipment.FaceS))
        {
            equipment.Face = new Vector3(0, -1, 0); // default
        }
        else
        {
            if (equipment.FaceS.Contains('X'))
            {
                // it's in proper Serialised format
                equipment.Face = JsonSerializer.Deserialize<Vector3>(equipment.FaceS, jsonSerializerOption);
            }
            else
            {
                // it's in non serialised format
                // Default expected to be "NE" i.e., X is towards "N" and "Y" is toward East
                var xyne = _globalData.PlotPlans?.FirstOrDefault()?.XY ?? "NE";
                if (equipment.FaceS == null) equipment.FaceS = "?";
                switch (xyne)
                {
                    case "EN": // EN X-axis east/west and Y-axis north/south
                        equipment.Face = new Vector3(equipment.FaceS == "E" ? 1 : equipment.FaceS == "W" ? -1 : 0,
                            equipment.FaceS == "N" ? 1 : equipment.FaceS == "S" ? -1 : 0, 0);
                        break;
                    default: // NE X-axis north/south and Y-axis east/west
                        equipment.Face = new Vector3(equipment.FaceS == "N" ? 1 : equipment.FaceS == "S" ? -1 : 0,
                            equipment.FaceS == "E" ? 1 : equipment.FaceS == "W" ? -1 : 0, 0);
                        break;
                }
            }
        }

        try
        {
            equipment.AliasTags = JsonSerializer.Deserialize<List<string>>(equipment.AliasTagsS, jsonSerializerOption);
        }
        catch (Exception e)
        {
            equipment.AliasTags = new List<string> { equipment.Tag };
            var b = e.ToString(); // some invalid or null entry as AliasTagsS
        }
        
    }
    

    
    
    /// <summary></summary>
    public List<float> CableRunCoreSizeAndMat(string cableDescription)
    {
        //3.5Cx185 // 4Cx95+E // 2Rx3Cx120+Nx70+PEx70
        float Run = 1; // Power Cable Run (no. of cores per phase) 
        float SizePhase = 1; //  Power Cable size of phase conductor 
        float SizeNeutral = 0; //  Power Cable size of Neutral conductor
        float SizePE = 0; //  Power Cable size of PE conductor
        float
            Core = 3; // Cable core nos: 3C (3P+N+E), 3C (3C+PEN/PE/N) 3.5C (3C+1/2PEN/PE/N), 3C (3P), 2C (P+PEN/PE/N), or 1C cable Selected
        float Mat = 0; //  Material 0 for "Cu" , 1 for "Al"
        ; //
        if (!string.IsNullOrEmpty(cableDescription))
        {
            var matchR = Regex.Match(cableDescription, @"^([1-9][R][x])");
            if (matchR.Success) Run = float.Parse(matchR.Captures[0].Value.Replace("Rx", ""));
            var matchC = Regex.Match(cableDescription, @"[0-9][C][x]");
            if (matchC.Success) Core = float.Parse(matchC.Captures[0].Value.Replace("Cx", ""));
            // put the sizes in reverse order as the regex checks and exit once it finds the first match, so  '120' will not be seached once '1' is found, '500' not seached if '50' found
            var matchS = Regex.Match(cableDescription,
                @"[C][x](1000|800|630|500|400|300|240|185|150|120|95|70|50|35|25|16|10|6|4|(2[\.]5)|(1[\.]5)|1|(0[\.]5))");
            if (matchS.Success) SizePhase = float.Parse(matchS.Captures[0].Value.Replace("Cx", ""));
            var matchN = Regex.Match(cableDescription,
                @"[N][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)");
            if (matchN.Success) SizeNeutral = float.Parse(matchN.Captures[0].Value.Replace("Nx", ""));
            var matchPEN = Regex.Match(cableDescription,
                @"[P][E][N][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)");
            if (matchPEN.Success) SizeNeutral = float.Parse(matchPEN.Captures[0].Value.Replace("PENx", ""));
            var matchPE = Regex.Match(cableDescription,
                @"[P][E][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)");
            if (matchPE.Success) SizePE = float.Parse(matchPE.Captures[0].Value.Replace("PEx", ""));
            var matchE = Regex.Match(cableDescription,
                @"[E][x]((0[\.]5)|1|(1[\.]5)|(2[\.]5)|4|6|10|16|25|35|50|70|95|120|150|185|240|300|400|500|630|800|1000)");
            if (matchPE.Success) SizePE = float.Parse(matchPE.Captures[0].Value.Replace("Ex", ""));
            var matchMat = Regex.Match(cableDescription, @"[ ]{0,2}[A-Z]{0,4}[a-z]{0,4}$");
            if (matchMat.Success) Mat = cableDescription.ToLower().Contains("al") ? 1f : 0f;
            if (Core == 3.5)
            {
                SizeNeutral = SizePhase <= 16 ? SizePhase :
                    SizePhase <= 35 ? 16 :
                    SizePhase <= 70 ? SizePhase / 2 :
                    SizePhase == 95 ? 50 :
                    SizePhase == 120 ? 70 :
                    SizePhase == 150 ? 70 :
                    SizePhase == 185 ? 95 :
                    SizePhase <= 300 ? SizePhase / 2 :
                    SizePhase <= 500 ? 240 : SizePhase;
            }
            else if (Core == 4)
            {
                SizeNeutral = SizePhase;
            }
            else if (Core == 5)
            {
                SizeNeutral = SizePhase;
                SizePE = SizePhase;
            }

            ;
        }

        return [Run, Core, SizePhase, SizeNeutral, SizePE, Mat];
    }


    /// <summary></summary>
    private List<float> FindCableSize(Load load, string mat, List<float> _avl1C, List<float> _avlMC, List<CableData>? _cableData)
    {
        var I = load.I;
        var L = load.L;
        var V = load.V;
        var phase = load.Ph;
        var Pf = load.Pf;
        var Ist = load.Ist == 0 ? 6 : load.Ist;
        var Pfst = load.Pfst == 0 ? 0.4 : load.Pfst;
        var C = load.CM;
        var alVdR = load.AlVdR;
        var alVdS = load.AlVdS;
        var DF = load.DF;
        var AM = load.AM == 0 ? 1f : load.AM;
        //
        var avl1C = _avl1C;
        var avlMC = _avlMC;

        var multipleCoreResult = FindCableSize1CorMC((int)C, alVdR, alVdS, DF, mat, avlMC, I, L, V, phase, 4, _cableData);
        // check for single core only when there is no multicore results or the sizes >=95
        if (multipleCoreResult.Count > 0 && multipleCoreResult[1] < 95)
        {
            return [multipleCoreResult[0], C, multipleCoreResult[1]];
        }
        var singleCoreResult = FindCableSize1CorMC(1, alVdR, alVdS, DF, mat, avl1C, I, L, V, phase,10, _cableData);
        //
        // comparing 1C result and multicore result based on the material consumption
        var materialConsumptionSingleCore = (singleCoreResult[0] * phase + C - phase) * singleCoreResult[1];
        var materialConsumptionMultipleCore =
            multipleCoreResult[0] * C * multipleCoreResult[1]; // assumed integrated PEN cable to check later
        //                                                                                                              //
        var thisR = materialConsumptionSingleCore < materialConsumptionMultipleCore
            ? (int)singleCoreResult[0]
            : (int)multipleCoreResult[0];
        var thisC = materialConsumptionSingleCore < materialConsumptionMultipleCore
            ? 1
            : C; // 3 to be corrected later it can be 4 or 5 also
        var thisSp = materialConsumptionSingleCore < materialConsumptionMultipleCore
            ? singleCoreResult[1]
            : multipleCoreResult[1];
        //
        return [thisR, thisC, thisSp];

        //
        List<float> FindCableSize1CorMC(int core, double allowableVdR, double allowableVdS, double cableDeratingFactor,
            string mat, List<float> avl, double I, double L, double V, int phase, int maxCableRun, List<CableData>? cableData)
        {
            double RunningVoltageDropAuto;
            double StartingVoltageDropAuto;
            float cableRunThis = 0;
            List<CableData> cables;
            do
            {
                cableRunThis++;
                cables = cableData.Where(cable =>
                    cable.ConductorMaterial == mat && cable.PhaseNo == core && avl.Contains(cable.PhaseSize) &&
                    cable.AmpicityAir * (cableDeratingFactor/100) > I / cableRunThis).ToList();
            } while (cables.Count() == 0 && cableRunThis<maxCableRun);

            float cableSizeThis = 0;
            try
            {
                cableSizeThis = cableData.Where(Cable =>
                        Cable.ConductorMaterial == mat &&
                        Cable.AmpicityAir * (cableDeratingFactor / 100) > I / cableRunThis)
                    .ToList()[0].PhaseSize;
            }
            catch (Exception e)
            {
                // ignored
                cableSizeThis = 0;

            }

            do
            {
                var tolerance = 0.001f;
                var tempCable = cableData
                    .Where(cable =>
                        cable.ConductorMaterial == mat && Math.Abs(cable.PhaseSize - cableSizeThis) < tolerance)
                    .ToList();
                
                var R = cableData
                    .Where(cable => cable.ConductorMaterial == mat && Math.Abs(cable.PhaseSize - cableSizeThis) < tolerance).ToList()[0].RDC;
                var X = cableData
                    .Where(cable => cable.ConductorMaterial == mat && Math.Abs(cable.PhaseSize - cableSizeThis) < tolerance).ToList()[0].XAC;
                if (R * X < 0.00001)
                {
                    (R, X) = AproximateRX(cableData, mat, cableSizeThis);
                }

                
                RunningVoltageDropAuto =
                    Math.Round(
                        (phase == 1 ? 2 : Math.Pow(3, 0.5)) * (I / cableRunThis) * (L / 1000) *
                        (R * Pf + X * Math.Pow(1 - Math.Pow(Pf, 2), 0.5)) / (0.01 * V), 2);
                StartingVoltageDropAuto =
                    Math.Round(
                        (phase == 1 ? 2 : Math.Pow(3, 0.5)) * (I / cableRunThis) * Ist * (L / 1000) *
                        (R * Pfst + X * Math.Pow(1 - Math.Pow(Pfst, 2), 0.5)) / (0.01 * V), 2);
                // if the last highest cable size is reached but still either running or the starting drop is not within the limit
                // consider one more run and start with the cable size approximately of the equivalent size of one more run (2Rx120 -> 3Rx95) to be checked later if it is working as expected
                if (avl.IndexOf(cableSizeThis) == avl.Count - 1 &&
                    (RunningVoltageDropAuto > allowableVdR || StartingVoltageDropAuto > allowableVdS))
                {
                    cableRunThis++;
                    cableSizeThis = avl[(int)(avl.IndexOf(cableSizeThis) * (cableRunThis - 1) / cableRunThis)];
                }
                else if (avl.IndexOf(cableSizeThis) != avl.Count - 1 &&
                         (RunningVoltageDropAuto > allowableVdR || StartingVoltageDropAuto > allowableVdS))
                {
                    // if either running or startig drop is not within the limit and the last highest cable size is NOT reached
                    // increase one size
                    cableSizeThis = avl[Math.Min(avl.IndexOf(cableSizeThis) + 1, avl.Count - 1)];
                }
            } while (RunningVoltageDropAuto > allowableVdR || StartingVoltageDropAuto > allowableVdS);

            return [cableRunThis, cableSizeThis];
        }
    }


    public (float, float) AproximateRX(List<CableData> cableData, string mat, float phaseSize)
    {
        // return approximate R and X value if the exact value is not available in the CableData
        float r;
        float x;

        if (cableData.Count > 0 && cableData[0].RAC != 0 && cableData[0].XAC != 0)
        {
            // cable catalogue data RAC and XAC are expressed in km and so Rl and Xl
            r = cableData[0].RAC;
            x = cableData[0].XAC;
        }
        else
        {
            r = (mat == "Cu" ? 18.5f : 26.5f) / (float)phaseSize;
            x= -0.03f * (float)Math.Log(phaseSize) + 0.902f;
        }

        return (r, x);
    }

    /// <summary></summary>
    public List<float> VoltageDrop(Load load, string mat, string a, List<CableData>? cableData)
    {
        // string a = "A" for Auto and "M" for Manual
        var I = load.I;
        var L = load.L;
        var V = load.V;
        var phase = load.Ph;
        var Pf = load.Pf;
        var Ist = load.Ist;
        var Pfst = load.Pfst;
        //
        var C = a == "M" ? load.CM : load.CA;
        var Rn = a == "M" ? load.RnM : load.RnA;
        var Sp = a == "M" ? load.SpM : load.SpA;
        var Ph = load.Ph;
        var cablesData = cableData.Where(Cable => Cable.ConductorMaterial == mat && Cable.PhaseSize == Sp)
            .ToList();
        float R;
        float X;

        if (cablesData.Count > 0 && cablesData[0].RAC != 0 && cablesData[0].XAC != 0)
        {
            // cable catalogue data RAC and XAC are expressed in km and so Rl and Xl
            R = cablesData[0].RAC;
            X = cablesData[0].XAC;
        }
        else
        {
            R = (mat == "Cu" ? 18.5f : 26.5f) / (float)Sp;
            X = -0.03f * (float)Math.Log(Sp) + 0.902f;
        }

        //
        // cable catalogue data RAC and XAC are expressed per km whereas L is in m 
        //
        var VdR = Math.Round(
            (phase == 1 ? 2 : Math.Pow(3, 0.5)) * (I / Rn) * (L / 1000) *
            (R * Pf + X * Math.Pow(1 - Math.Pow(Pf, 2), 0.5)) / (0.01 * V), 2);
        var VdS = Math.Round(
            (phase == 1 ? 2 : Math.Pow(3, 0.5)) * (I / Rn) * Ist * (L / 1000) *
            (R * Pfst + X * Math.Pow(1 - Math.Pow(Pfst, 2), 0.5)) / (0.01 * V), 2);
        //
        return [(float)VdR, (float)VdS];
    }
    
    
    public void CableRXUpdate(CableBranch cableBranch)
    {
        var RnCSpSnSpe = CableRunCoreSizeAndMat(cableBranch.CblDesc);
        cableBranch.Run = (int)RnCSpSnSpe[0];
        cableBranch.Core = (int)RnCSpSnSpe[1];
        cableBranch.Sp = RnCSpSnSpe[2];
        cableBranch.Sn = RnCSpSnSpe[3];
        cableBranch.Spe = RnCSpSnSpe[4];

        //var mat = (CblDesc.Contains("Al") || CblDesc.Contains("al")) ? "Al" : "Cu";
        var mat = RnCSpSnSpe[5] == 1f ? "Al" : "Cu";

        var cablesData = _globalData.CableData
            .Where(cableData => cableData.ConductorMaterial == mat && cableData.PhaseNo == cableBranch.Core && cableData.PhaseSize == cableBranch.Sp).ToList();
        if (cablesData.Count > 0 && cablesData[0].RAC != 0 && cablesData[0].XAC != 0)
        {
            // cableBranch catalogue data RAC and XAC are expressed in km and so Rl and Xl
            cableBranch.Rl = cablesData[0].RAC;
            cableBranch.Xl = cablesData[0].XAC;
        }
        else
        {
            cableBranch.Rl = 018.5f / cableBranch.Sp;
            cableBranch.Xl = -0.03f * (float)Math.Log(cableBranch.Sp) + 0.902f;
        }

        //
        // cableBranch catalogue data RAC and XAC are expressed per km whereas L is in m 
        cableBranch.R = cableBranch.Rl * cableBranch.L / cableBranch.Run / 1000;
        cableBranch.X = cableBranch.Xl * cableBranch.L / cableBranch.Run / 1000;
        
    }
    
    public void BusDuctRXUpdate(BusDuct busDuct)
    {
        // busduct data source https://www.bahra-electric.com/Downloads/BahraTBS_Busway_HE_Catalogue.pdf
        //Rated current In [A] 
        double[] IRData = [800, 1000, 1250, 1600, 2000, 2500, 3200, 4000, 5000];
        //Average phase reactance X [mΩ/m] 
        double[] XData = [0.018, 0.018, 0.016, 0.016, 0.011, 0.009, 0.007, 0.006, 0.005];
        // Average phase resistance at thermal conditions R[mΩ/m]
        double[] RData = [0.051, 0.052, 0.042, 0.034, 0.025, 0.021, 0.016, 0.013, 0.011];
        var polX = _myFunction.Polynomial(IRData, XData, 2);
        var polR = _myFunction.Polynomial(IRData, RData, 2);
        //
        busDuct.Rl = (float)(polR[2] * busDuct.IR * busDuct.IR + polR[1] * busDuct.IR + polR[0]);
        busDuct.Xl = (float)(polX[2] * busDuct.IR * busDuct.IR + polX[1] * busDuct.IR + polX[0]);
        busDuct.R = busDuct.Rl * busDuct.L / 1000;
        busDuct.X = busDuct.Xl * busDuct.L / 1000;
        //
    }
    
    
        /// <summary></summary>
    public List<Bus> BranchBusUpdate(string tag, string category, string bfT, string btT, List<Bus> buses)
    {
        // check the connections of this branch
        // at least one end should be connected
        var Bfs = buses.Where(bus => bus.Tag == bfT).ToList();
        var Bts = buses.Where(bus => bus.Tag == btT).ToList();
        if (Bfs.Count != 1 || Bts.Count != 1)
        {
            Debug.WriteLine(
                $"Error !! {category} Tag {tag} not connected to both ends bus, possible reason: bus from '{bfT}' or to '{btT}' or both are not defined yet.");
            // generate new Bus
            Bus bv1 = new();
            Bus bv2 = new();
            // rated voltage
            var vr = 0f;
            // rated SC
            var sc = "";
            // create two buses (nodes) for this transformer or any other type of the branch
            // From side
            if (Bfs.Count != 1)
                bv1 = new Bus("", bfT, vr, sc);
            else
                bv1 = Bfs[0];
            //
            // To side
            if (Bts.Count != 1)
                bv2 = new Bus("", btT, vr, sc);
            else
                bv2 = Bts[0];
            //
            bv1.Cn.Add(bv2.Tag ?? "");
            bv2.Cn.Add(bv1.Tag ?? "");
            //
            // check there is no existing Transformer bus before adding them
            //Buses.RemoveAll(bus => bus.Tag.Contains(Bv1T) || bus.Tag.Contains(Bv2T));
            if (Bfs.Count != 1) buses.Add(bv1);
            if (Bts.Count != 1) buses.Add(bv2);
        }
        else
        {
            // create connections between the from and to busess
            if (!Bfs[0].Cn.Contains(btT)) Bfs[0].Cn.Add(btT);
            if (!Bts[0].Cn.Contains(bfT)) Bts[0].Cn.Add(bfT);
        }

        return buses;
    }

    public void BranchUpdatePU(Branch branch)
    {
        // Sb is in MVA
        branch.Zb = (float)(Math.Pow(10, -6) * branch.Vb * branch.Vb / _globalData.Sb);
        branch.Ib = (float)(branch.Vb / branch.Zb);
        branch.Ypu = 1 / new Complex(branch.R / branch.Zb, branch.X / branch.Zb);
    }  
        
    
    public void TransformerUpdatePU(Transformer transformer)
    {
        //  transformer PU Impedance
        transformer.Category = "Transformer"; // Category
        var z = transformer.Z * transformer.V1 * transformer.V1 / transformer.KVA / 1000;
        if (transformer.XR == 0f) transformer.XR = 10f;
        var pf = Math.Sqrt(1 - 1 / transformer.XR / transformer.XR);
        // Sb is in MVA
        // here Vb shall be corresponding to V1
        transformer.Zb = (float)(Math.Pow(10, -6) * transformer.Vb * transformer.Vb / _globalData.Sb)!;
        transformer.Ib = (float)(transformer.Vb / transformer.Zb)!;
        transformer.Ypu = 1 / new Complex(z * pf / transformer.Zb, z * pf * transformer.XR / transformer.Zb);
    }

    public void LoadUpdatePU(Load load)
    {
        // S in kVA whereas Sb in MVA
        load.Spu = 0.001f * load.S / _globalData.Sb;
        // P & Q in kW and kVAR respectively
        load.Sc = new Complex(load.P, load.Q);
        load.Scpu = 0.001f * load.Sc / _globalData.Sb;
    }
    
    public void NodeMarginUpdate(Node node)
    {
        node.MarginSide1 = _globalData.MarginSide1;
        node.MarginSide2 = _globalData.MarginSide2;
        node.MarginSpare = _globalData.MarginSpare;
        // Margins and spare considered only for ladder , change later
        node.AvailableWidth = node.Width > 0.2 ? 
            (node.Width - node.MarginSide1 - node.MarginSide2) * (1 - node.MarginSpare) : node.Width;
    }

    
    
    
    
    public List<Segment> AssignFaceForASegmentList(List<Segment> segments)
    {
        // this function assigns face for all segments
        List<string> BranchList = [];
        segments.ForEach(seg => { BranchList.Add(seg.CableWayBranch); });
        var DistinchBranchList = BranchList.Distinct().ToList();
        DistinchBranchList.ForEach(branch =>
        {
            List<Segment> list = segments.Where(seg => seg.CableWayBranch == branch).ToList();
            list = AssignFaceForASegmentBranch(list);
        });
        
        // check for all segments
        segments.ForEach(seg =>
        {
            var segNfaceParallel = IsParallel(seg.End1, seg.End2, new Vector3(), seg.Face);
            if (segNfaceParallel ||
                Math.Abs(seg.Face.X) < 0.01f &&
                Math.Abs(seg.Face.Y) < 0.01f &&
                Math.Abs(seg.Face.Z) < 0.01f || float.IsNaN(seg.Face.X) || float.IsNaN(seg.Face.Y) || float.IsNaN(seg.Face.Z))
            {
                // default face is set at Up (Z) direction
                seg.Face = new Vector3(0f, 0f, 1f);
                
                // for vertical trays, the face should not be up, face is set at Y direction
                if (IsParallel(seg.End1, seg.End2, new Vector3(), seg.Face))
                {
                    seg.Face = new Vector3(0f, 1f, 0f);
                }
            }
            
        });
        
        return segments;
    }
    
    
     public Vector3 PerpendicularPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        // Foot of Perpendicular from Point to Line
        // Nearest point F from Point, point on Line lineStart-lineEnd
        // Uses vector projection.
        //https://www.youtube.com/watch?v=TPDgB6136ZE method 2

        var lineDirection = lineEnd - lineStart;

        // Handle degenerate case where lineStart and lineEnd are the same point
        if (lineDirection.LengthSquared() == 0) return lineStart;

        var normalizedLineDirection = Vector3.Normalize(lineDirection);
        return Vector3.Dot(point - lineStart, normalizedLineDirection) * normalizedLineDirection + lineStart;
    }

    public double DistancePointToLine1(Vector3 vP, Vector3 vQ, Vector3 vR)
    {
        // distance, D of point P from line QR
        // is given by PQ x QR/|QR|
        return Vector3.Cross(vQ - vP, vR - vQ).Length() / (vR - vQ).Length();
    }

    public double DistancePointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        // Calculates the distance from a point to a line in 3D space.
        // Formula: |(lineStart - point) x (lineEnd - lineStart)| / |lineEnd - lineStart|

        var lineDirection = lineEnd - lineStart;
        var pointToLineStart = lineStart - point;

        // Handle the degenerate case where lineStart and lineEnd are the same point
        if (lineDirection.LengthSquared() == 0)
            // point and line are the same. distance is 0.
            return pointToLineStart.Length();

        // Calculate the cross-product of pointToLineStart and lineDirection
        var crossProduct = Vector3.Cross(pointToLineStart, lineDirection);

        // Calculate the distance
        return crossProduct.Length() / lineDirection.Length();
    }

    public double DistancePointToLineSquared(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        // Calculates the squared distance from a point to a line in 3D space.
        // Formula: |(lineStart - point) x (lineEnd - lineStart)|^2 / |lineEnd - lineStart|^2

        var lineDirection = lineEnd - lineStart;
        var pointToLineStart = lineStart - point;

        // Handle degenerate case where lineStart and lineEnd are the same point
        if (lineDirection.LengthSquared() == 0) return pointToLineStart.LengthSquared();

        // Calculate the cross product of pointToLineStart and lineDirection
        var crossProduct = Vector3.Cross(pointToLineStart, lineDirection);

        // Calculate the squared distance
        return crossProduct.LengthSquared() / lineDirection.LengthSquared();
    }


    public double AngleBetween(Vector3 a, Vector3 b)
    {
        // to make sure the values are within 1 and -1
        var dot = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
        return Math.Acos(Math.Round(dot, 4));
    }


    /// <summary>Is a b c d Coplaner? </summary>
    public bool Coplaner(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var v = IntersectionPoints(a, b, c, d);
        if (v.Count == 0) return false;
        return Vector3.Distance(v[0], v[1]) < 0.01;
        //below logic does not work for A: {<443.87, 52.61, 1.9>}, B {<443.87, 68.315, 1.9>}, C: {<443.593, 61.21, 5.625>} D: {<443.593, 61.21, 1.85>}
        //https://www.youtube.com/watch?v=fyknpOat01w
        //https://www.cuemath.com/algebra/matrix-equation/
        //Matrix<double> ABC = DenseMatrix.OfArray(new double[,] { { A.X, A.Y, A.Z }, { B.X, B.Y, B.Z }, { C.X, C.Y, C.Z } });
        //MathNetR.Vector<double> K = MathNetR.Vector<double>.Build.DenseOfArray(new double[] { D.X, D.Y, D.Z });
        //MathNetR.Vector<double> I = MathNetR.Vector<double>.Build.DenseOfArray(new double[] { 1, 1, 1 });
        //// A * X = B , so X = A-1 * B where A is ABC, X = xyz, B = K = 1,1,1,
        //var c = (ABC.Inverse() * I) * K;
        //if (Math.Round(c, 2) == 1)
        //{
        //    return true;
        //}
        //else
        //{
        //    return false;
        //}
    }

    /// <summary> Is vector ab and cd are parallel? </summary>
    public bool IsParallelOld(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var v = AngleBetween(b - a, d - c);
        return v < 0.0001 || Math.PI - v < 0.0001;
    }
    
    public bool IsParallel(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float tolerance = 1e-5f)
    {
        Vector3 v1 = b - a;
        Vector3 v2 = d - c;

        // Handle zero-length vectors
        if (v1.LengthSquared() == 0 || v2.LengthSquared() == 0)
        {
            return false; // Or handle as needed
        }

        Vector3 cross = Vector3.Cross(v1, v2);

        return cross.LengthSquared() < tolerance * tolerance;
    }

    /// <summary>Is vector ab and cd are co-linear?  </summary>
    public bool IsColinear(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        // check if they are parallel
        if (!IsParallel(a, b, c, d)) return false;
        // check if distance of C from AB is <0.001
        if (DistancePointToLineSquared(c, a, b) < 0.00001) return true;
        return DistancePointToLineSquared(d, a, b) < 0.00001;
    }


    public bool IsColinear1(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        //var k = JsonConvert.Serialize(new List<string>() { A.ToString(), B.ToString(), C.ToString(), D.ToString() });
        //var ab = B - A;
        //var bc = B - (B == C ? D : C);
        var a = AngleBetween(B - A, B - (B == C ? D : C));
        // Double.IsNaN is to check if the angle is Nan. i.e, could be due to two identical segments, i.e,  A-B C-D
        //var aa = AngleBetween(ab, bc);
        if (a < 0.01 || Math.PI - a < 0.01 || double.IsNaN(a)) return true;

        return false;
    }


    public bool IsOverlap(Vector3 A, Vector3 B, Vector3 C, Vector3 D, double gap)
    {
        // lets not assume A-B and C-D are colinear
        if (!IsColinear(A, B, C, D)) return false;

        if (2 * ((A + B) / 2 - (C + D) / 2).Length() < (A - B).Length() + (C - D).Length() + gap) return true;

        return false;
    }

    public Vector3 IntersectionPoint(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        // assume that A B C D are coplaner and not parallel
        Vector3 P = new();
        float tab = 0;
        //if (Coplaner(A, B, C, D) && !IsParallel(A, B, C, D))

        var U = Vector3.Normalize(B - A);
        var V = Vector3.Normalize(D - C);

        if (U.X * V.Y != U.Y * V.X)
        {
            tab = ((C.X - A.X) * V.Y - (C.Y - A.Y) * V.X) / (U.X * V.Y - U.Y * V.X);
        }
        else if (U.X * V.Z != U.Z * V.X)
        {
            tab = ((C.X - A.X) * V.Z - (C.Z - A.Z) * V.X) / (U.X * V.Z - U.Z * V.X);
        }
        else if (U.Y * V.Z != U.Z * V.Y)
        {
            tab = ((C.Y - A.Y) * V.Z - (C.Z - A.Z) * V.Y) / (U.Y * V.Z - U.Z * V.Y);
        }
        else
        {
            var k = IsParallel(A, B, C, D);
            var k2 = Coplaner(A, B, C, D);
            var a = 0;
        }

        P = A + tab * U;
        //
        return P;
    }

    /// <summary>Path1 and Pathh2 are more than d distance away? </summary>
    public bool IsFarAway(Segment path1, Segment path2, double d = 0.1)
    {
        return Math.Min(path2.End1.X, path2.End2.X) - Math.Max(path1.End1.X, path1.End2.X) > d ||
               Math.Min(path1.End1.X, path1.End2.X) - Math.Max(path2.End1.X, path2.End2.X) > d ||
               Math.Min(path2.End1.Y, path2.End2.Y) - Math.Max(path1.End1.Y, path1.End2.Y) > d ||
               Math.Min(path1.End1.Y, path1.End2.Y) - Math.Max(path2.End1.Y, path2.End2.Y) > d ||
               Math.Min(path2.End1.Z, path2.End2.Z) - Math.Max(path1.End1.Z, path1.End2.Z) > d ||
               Math.Min(path1.End1.Z, path1.End2.Z) - Math.Max(path2.End1.Z, path2.End2.Z) > d;
    }


    public List<Vector3> IntersectionPoints(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        var PQ = new List<Vector3>();
        Vector3 P, Q;

        // collinear, parallel, intersecting skew

        //check if vector AB and CD are parallel
        var AB = B - A;
        var AC = C - A;
        var AD = D - A;
        var CD = D - C;
        var ABXCD = 1000 * Vector3.Cross(AB, CD);
        // check if AB and CD are parallel or not
        if (new Vector3((float)Math.Round(ABXCD.X, 0), (float)Math.Round(ABXCD.Y, 0), (float)Math.Round(ABXCD.Z, 0))
            .Equals(Vector3.Zero))
        {
            // AB and CD are parallel
            // check if they are collinear (A, B and C/D in same line), otherwise there wont be any intersection
            if (Vector3.Normalize(A - B).Equals(Vector3.Normalize(A - C)) ||
                Vector3.Normalize(A - B).Equals(-Vector3.Normalize(A - C)))
            {
                // A, B and C are collinear
                var d = _globalData.distanceMargin; // later define d as the tray width/2 of the other segment 
                var gap = Math.Min(Math.Min((C - A).Length(), (D - B).Length()),
                    Math.Min((A - D).Length(), (C - B).Length()));
                if (gap < 1.5) //change laater
                {
                    P = Q = (C - A).Length() == gap ? (C + A) / 2 :
                        (D - B).Length() == gap ? (D + B) / 2 :
                        (A - D).Length() == gap ? (A + D) / 2 :
                        (C - B).Length() == gap ? (C + B) / 2 : Vector3.Zero;
                    PQ.Add(P);
                    PQ.Add(Q);
                }
            }
            // away parallel lines , not intersecting
        }
        // as AB and CD are not parallel, check further if AB and CD are coplaner lines, i.e., not skew lines, so they will intersect
        else if (Math.Abs(Vector3.Dot(AD, Vector3.Cross(AB, AC))) < 0.000001)
        {
            // co-planer
            // Point AA and BB are the corresponing projection of point C and D on line AB
            var AA = PerpendicularPointOnLine(C, A, B);
            var BB = PerpendicularPointOnLine(D, A, B);
            // Comparing traiange AAPC and BBPD 
            //P = Q = D + (D - C) / ((AA - C).Length() / (BB - D).Length() - 1); discovered worng 03-6-23
            var k = (BB - D).Length();
            var l = (AA - C).Length();
            l = Vector3.Normalize(BB - A) == Vector3.Normalize(AA - C) ? l : -l;
            P = Q = D + (C - D) * k / (k - l);
            // check if both P or Q is not outside the line AB or CD respectively
            if (Math.Abs((A - P).Length() + (B - P).Length() - (A - B).Length()) < 1.5 &&
                Math.Abs((C - Q).Length() + (D - Q).Length() - (C - D).Length()) < 1.5) // 1.5 change later
            {
                PQ.Add(P);
                PQ.Add(Q);
            }

            ;
        }
        else
        {
            // as AB and CD are neither parellel nor co-planer, they must be skew lines
            // skew lines
            var x1 = A.X;
            var x2 = B.X;
            var x3 = C.X;
            var x4 = D.X;
            var y1 = A.Y;
            var y2 = B.Y;
            var y3 = C.Y;
            var y4 = D.Y;
            var z1 = A.Z;
            var z2 = B.Z;
            var z3 = C.Z;
            var z4 = D.Z;
            var dx1 = x2 - x1;
            var dx2 = x4 - x3;
            var dy1 = y2 - y1;
            var dy2 = y4 - y3;
            var dz1 = z2 - z1;
            var dz2 = z4 - z3;
            var dx = x3 - x1;
            var dy = y3 - y1;
            var dz = z3 - z1;
            var c1 = dx * dx1 + dy * dy1 + dz * dz1;
            var c2 = dx * dx2 + dy * dy2 + dz * dz2;
            var c3 = dx1 * dx2 + dy1 * dy2 + dz1 * dz2;
            var c4 = dx1 * dx1 + dy1 * dy1 + dz1 * dz1;
            var c5 = dx2 * dx2 + dy2 * dy2 + dz2 * dz2;
            var k = c4 * c5 - c3 * c3;
            if (Math.Abs(k) < 0.00001)
            {
                // lines are parallel
                var kk = 0;
                //System.Diagnostics.Debug.WriteLine(kk);
            }
            else
            {
                var ss = (c1 * c5 - c2 * c3) / k;
                var tt = (c1 * c3 - c2 * c4) / k;
                P = new Vector3(x1 + dx1 * ss, y1 + dy1 * ss, z1 + dz1 * ss);
                Q = new Vector3(x3 + dx2 * tt, y3 + dy2 * tt, z3 + dz2 * tt);
                //
                // check if both P or Q is not outside the line AB or CD respectively
                if (Math.Abs((A - P).Length() + (B - P).Length() - (A - B).Length()) < 0.01 &&
                    Math.Abs((C - Q).Length() + (D - Q).Length() - (C - D).Length()) < 0.01)
                {
                    PQ.Add(P);
                    PQ.Add(Q);
                }

                ;
            }
        }

        ////https://math.stackexchange.com/questions/1993953/closest-points-between-two-lines
        return PQ;
    }

    public List<Vector3> IntersectionPointDec(Vector3 A, Vector3 B, Vector3 C, Vector3 D, double gapA,
        double gapB, double gapC, double gapD)
    {
        // gapAB/gapCD are the limits to check if the intersection points are within these limits from the edges (input width of tray/2)
        var u = Vector3.Normalize(B - A);
        var v = Vector3.Normalize(D - C);
        var intersections = new List<Vector3>();
        // Line L1 = AB
        // Line L2 = CD
        // Intersections Point on AB is P and on CD is Q
        // P = A + su and Q = C + t v
        // W is PQ which is perpendicular to u and v
        // W = Q-P = C + t v - A - s u
        // W.u = 0
        // (C + t v - A - s u).u = 0
        // C.u + t v.u - A.u - s u.u = 0
        // C.u + t v.u - A.u - s = 0
        // similarly, (Q - P = C + t v - A - s u).u = 0
        // C.v + t - A.v - s u.v = 0
        // C.u + t v.u - A.u - s  = v.u ( C.v + t - A.v - s u.v)
        // C.u + t v.u - A.u - s - C.v v.u - t v.u  + A.v v.u  + s u.v v.u = 0
        // C.u - A.u - s - C.v v.u + A.v v.u  + s u.v v.u = 0
        // s - s u.v v.u = C.u - A.u - C.v v.u + A.v v.u
        // s = (C.u - A.u - C.v u.v + A.v u.v)/(1 - u.v^2)
        // Similarly, u.v (C.u + t v.u - A.u - s) - ( C.v + t - A.v - s u.v) = 0
        // C.u u.v + t v.u u.v - A.u u.v - s u.v - C.v - t + A.v + s u.v = 0
        // t (u.v^2 -1) + C.u u.v - A.u u.v - C.v + A.v = 0
        // t = (A.v - C.v - A.u u.v + C.u u.v)/(1 - u.v^2)
        var uDotv = Vector3.Dot(u, v);
        if (Math.Abs(uDotv) < 0.99)
        {
            // not parallel lines
            var s = (Vector3.Dot(C, u) - Vector3.Dot(A, u) - uDotv * Vector3.Dot(C, v) + uDotv * Vector3.Dot(A, v)) /
                    (1 - uDotv * uDotv);
            var t = (Vector3.Dot(A, v) - Vector3.Dot(C, v) - uDotv * Vector3.Dot(A, u) + uDotv * Vector3.Dot(C, u)) /
                    (1 - uDotv * uDotv);
            var P = A + s * u;
            var Q = C + t * v;
            // check if P and Q are within the AB and CD respectively, i.e., s and t are positive and < distances
            if (s >= -gapA && t >= -gapC && s <= Vector3.Distance(A, B) + gapB && t <= Vector3.Distance(C, D) + gapD)
            {
                // intersection points are inside the lines
                intersections.Add(P);
                intersections.Add(Q);
            }
        }

        return intersections;
    }


    public List<NearTag>? TagMatches(string searchTag, List<Board> Boards, List<Load> Loads,
        List<Equipment> Equipments, int i = 3)
    {
        // this function is to return a list of NearTags for a searchTag based on Levenshtein distance approach to find the closest string match
        // this function to be called even when there could be a perfect match.
        // in case of exact string match, the first item in the list shall be the perfect match
        // search nearest match upto ith character mismatch
        i = searchTag.Length < 4 ? 0 : searchTag.Length < 6 ? 1 : searchTag.Length < 8 ? 2 : i;
        List<NearTag>? NearTags = [];
        //
        // 1st search for a perfect match
        var boardMatches = Boards.Where(board => board.Tag == searchTag).ToList();
        if (boardMatches.Count != 0)
        {
            var item = boardMatches[0];
            NearTags.Add(new NearTag(item.UID, item.Tag, "board", item.CentrePointS, item.CentrePoint));
        }
        else
        {
            var loadMatches = Loads.Where(load => load.Tag == searchTag).ToList();
            if (loadMatches.Count != 0)
            {
                var item = loadMatches[0];
                NearTags.Add(new NearTag(item.UID, item.Tag, "load", item.LocationSize, item.Location));
            }
            else
            {
                var equipmentMatches = Equipments.Where(equip => equip.Tag == searchTag).ToList();
                if (equipmentMatches.Count != 0)
                {
                    var item = equipmentMatches[0];
                    NearTags.Add(new NearTag(item.UID, item.Tag, "equipment", item.CentrePointS, item.CentrePoint));
                }
                else
                {
                    // no exact match found
                    // check nearest matches
                    var s1 = searchTag[..Math.Abs(searchTag.Length - i)];
                    var s2 = searchTag[Math.Min(i, searchTag.Length - 1)..(searchTag.Length - 1)];
                    Boards.Where(item =>
                            item.Tag != null &&
                            (item.Tag.Contains(s1) ||
                             item.Tag.Contains(s2))).ToList()
                        .ForEach(item =>
                        {
                            NearTags.Add(new NearTag(item.UID, item.Tag, "board", item.CentrePointS,
                                item.CentrePoint));
                        });
                    Loads.Where(item =>
                            item.Tag != null &&
                            (item.Tag.Contains(s1) ||
                             item.Tag.Contains(s2))).ToList()
                        .ForEach(item =>
                        {
                            NearTags.Add(new NearTag(item.UID, item.Tag, "load", item.LocationSize, item.Location));
                        });
                    Equipments.Where(item =>
                            item.Tag != null &&
                            (item.Tag.Contains(s1) ||
                             item.Tag.Contains(s2))).ToList()
                        .ForEach(item =>
                        {
                            NearTags.Add(new NearTag(item.UID, item.Tag, "equipment", item.CentrePointS,
                                item.CentrePoint));
                        });
                    //
                    // order NearTags as per the close mtch (Levenshtein distance approach)
                    var orderedNearTags = NearTags.OrderBy(near => Compute(near.Tag, searchTag)).ToList();
                    NearTags = orderedNearTags.Take(10).ToList();
                }
            }
        }

        return NearTags;

        static int Compute(string s, string t)
        {
            var distance = new int[s.Length + 1, t.Length + 1];

            for (var i = 0; i <= s.Length; i++) distance[i, 0] = i;

            for (var j = 0; j <= t.Length; j++) distance[0, j] = j;

            for (var j = 1; j <= t.Length; j++)
            for (var i = 1; i <= s.Length; i++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }

            return distance[s.Length, t.Length];
        }
    }

    public Vector3 FindLocationOfTagLevenshtein(string equipmentTag, string equipmentFeeder, List<Board> boards,
        List<Load> loads, List<Equipment> equipments)
    {
        // this function is rebuilt from FindLocationOfTag function however based on Levenshtein distance approach to find the clossest match
        // this function fetches the existing location in XYZ coordinate system from the Board, LoadMaster and Equipment Database 
        // based on the given Equipment Tag No.
        //
        // update Origin and Destination based on Equipment Data or Board Data or Loadmaster data
        // 1st check the board data
        // then check the load masted data
        // then finally chech the equipment data (dump from 3D model)
        //
        // if the closed match is from the board, then find the coordinate as per the feeder #


        List<NearTag>? NearTags = new();
        boards.Where(board =>
                board.Tag.Contains(equipmentTag[Math.Min(3, equipmentTag.Length)..(equipmentTag.Length - 3)])).ToList()
            .ForEach(item =>
            {
                NearTags.Add(new NearTag(item.UID, item.Tag, "board", item.CentrePointS, item.CentrePoint));
            });
        loads.Where(load =>
                load.Tag.Contains(equipmentTag[Math.Min(3, equipmentTag.Length)..(equipmentTag.Length - 3)])).ToList()
            .ForEach(item =>
            {
                NearTags.Add(new NearTag(item.UID, item.Tag, "load", item.LocationSize, item.Location));
            });
        equipments.Where(equip =>
                equip.Tag.Contains(equipmentTag[Math.Min(3, equipmentTag.Length)..(equipmentTag.Length - 3)])).ToList()
            .ForEach(item =>
            {
                NearTags.Add(new NearTag(item.UID, item.Tag, "load", item.CentrePointS, item.CentrePoint));
            });


        var closestMatch = FindClosestMatch(NearTags, equipmentTag);
        if (closestMatch == null)
        {
            Debug.WriteLine($"Closest match to '{equipmentTag}' not found");
            return new Vector3();
        }

        Debug.WriteLine($"Closest match to '{equipmentTag}' is '{closestMatch.Tag}' of '{closestMatch.Type}'");
        return closestMatch.Location;


        static NearTag? FindClosestMatch(List<NearTag> candidates, string target)
        {
            var minDistance = int.MaxValue;
            NearTag? closestMatch = null;

            foreach (var candidate in candidates)
            {
                var distance = Compute(candidate.Tag, target);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestMatch = candidate;
                }
            }

            return closestMatch;
        }


        static int Compute(string s, string t)
        {
            var distance = new int[s.Length + 1, t.Length + 1];

            for (var i = 0; i <= s.Length; i++) distance[i, 0] = i;

            for (var j = 0; j <= t.Length; j++) distance[0, j] = j;

            for (var j = 1; j <= t.Length; j++)
            for (var i = 1; i <= s.Length; i++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }

            return distance[s.Length, t.Length];
        }
    }

    public string AssignFeeder(Cable thisCable, string boardTag, string feederTag, List<Cable> cables,
        List<Board> boards)
    {
        // this function assigns feeder of boardTag to this cable end to evenly distribute among all cables connected to this Board, LoadMaster and Equipment Database
        // boardTag is where the cable is connected and "feeder" is to be returned
        // feederTag is the already given panel/given tag which to be verified
        // this is randomise and hence to be checked and updated later
        //
        var feeder = "";
        //
        // find all cables connected to this board
        var allConnectedCables = cables.Where(c => boardTag == c.OriginTag || boardTag == c.DestinationTag).ToList();
        if (allConnectedCables.Count > 0)
        {
            var totalcables = allConnectedCables.Count;
            var thisCableIndex = allConnectedCables.IndexOf(thisCable);
            var thisBoards = boards.Where(b => b.Tag == boardTag).ToList();
            // possible that the cable is not connected to a board (other type of equipment like motor transformer etc.)
            // in that case thisBoards.Count == 0, so retain the feederTag as it is
            if (thisBoards.Count == 0) return feederTag;

            // check if the given feederTag is matching with the Board panel tags 
            var thisBoard = thisBoards[0];
            if (!string.IsNullOrEmpty(feederTag) && thisBoard.PanelTag.Contains(feederTag))
                // given panel tag is available in the board panel tag list
                // this is mere confirmation
                return feederTag;

            //either feederTag is empty or not in the board panel tag list
            // assign random panel tag
            //int panelIndex = totalcables % thisBoard.Panels;
            var panelIndex = thisCableIndex % thisBoard.Panels;
            return thisBoard.PanelTag[panelIndex];
        }

        // not possible as there would be at least one cable
        return null;
    }


    public Tuple<List<Segment>, List<Bend>, List<Tee>, List<Cross>, List<Sleeve>, List<Node>> ReadSegNodeEtc(
        List<SegmentResult> segmentResult)
    {
            //     Updated on 23rd February 2025
            //     Read the latest JSON String of the Segment Result from all segmnentresults from DB and
            //     from there read Segment, Bend, Tee, Cross, Sleeve, Node JSONs and
            //     further gets the list of these items from their respective JSON strings
            //     It is expected that the Node has the connected relation
        
        List<Segment> segment = [];
        List<Bend> bend = [];
        List<Tee> tee = [];
        List<Cross> cross = [];
        List<Sleeve> sleeve = [];
        List<Node> node = [];

        if (segmentResult.Count > 0)
        {
            // order as per latest data
            segmentResult = segmentResult.OrderByDescending(o => o.UpdatedDateTime).ToList();
            //
            segment = Json2List(new Segment(), segmentResult[0].StraightSegmentListJSON, 4);
            bend = Json2List(new Bend(), segmentResult[0].BendListJSON, 4);
            tee = Json2List(new Tee(), segmentResult[0].TeeListJSON, 4);
            cross = Json2List(new Cross(), segmentResult[0].CrossListJSON, 4);
            node = Json2List(new Node(), segmentResult[0].NodeListJSON, 3);
            sleeve = Json2List(new Sleeve(), segmentResult[0].SleeveListJSON, 3); // 3

            List<T> Json2List<T>(T x, String json, int index)
            {
                List<T> list = [];
                var itemList = JsonSerializer.Deserialize<List<String>>(json, jsonSerializerOptions);
                itemList?.ForEach(item =>
                {
                    list.Add(JsonSerializer.Deserialize<T>(
                        JsonSerializer.Deserialize<List<String>>(item, jsonSerializerOptions)[index],
                        jsonSerializerOptions));
                });
                //Parallel.For(0, itemList.Count, i =>
                //{
                //    var item = JsonSerializer.Deserialize<List<string>>(itemList[i]);
                //    if (item.Count > 3)
                //    {
                //        List.Add(JsonSerializer.Deserialize<T>(item[index]));
                //    }
                //});
                return list;
            }
        }

        //
        return new Tuple<List<Segment>, List<Bend>, List<Tee>, List<Cross>, List<Sleeve>, List<Node>>(segment, bend,
            tee, cross, sleeve, node);
    }



    public (Vector3, Vector3, float) FindClosestEnds(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        // Calculate distances between all endpoint pairs
        float distanceAC = Vector3.Distance(a, c);
        float distanceAD = Vector3.Distance(a, d);
        float distanceBC = Vector3.Distance(b, c);
        float distanceBD = Vector3.Distance(b, d);

        // Find the minimum distance
        float minDistance = Math.Min(Math.Min(distanceAC, distanceAD), Math.Min(distanceBC, distanceBD));

        // Determine which endpoint pair corresponds to the minimum distance
        if (minDistance == distanceAC)
        {
            return (a, c, minDistance);
        }
        else if (minDistance == distanceAD)
        {
            return (a, d, minDistance);
        }
        else if (minDistance == distanceBC)
        {
            return (b, c, minDistance);
        }
        else
        {
            return (b, d, minDistance);
        }
    }
    
    
    public List<Segment> AssignFaceForASegmentBranch(List<Segment> segments)
    {
        // this function checks the routing of all the given segments of a Branch and assigns the logical face to each of the segments
        // it is assumed that all the segments in this 'segments' are from the same branch, i.e.; they are connected
        // this function should be run before the accessories are generated
        //
        var jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };
        
        //
        segments.OrderBy(seg => seg.Tag).ToList();
        
        // assign face for the 1st segment
        
        if (Math.Abs(segments[0].Face.X) < 0.01f &&
            Math.Abs(segments[0].Face.Y) < 0.01f &&
            Math.Abs(segments[0].Face.Z) < 0.01f)
        {
            segments[0].Face = new Vector3(0f, 0f, 1f);
        }

        if (Math.Abs(Vector3.Dot(segments[0].Face, Vector3.Normalize(segments[0].End1 - segments[0].End1))) > 0.95f)
        {
            segments[0].Face = Vector3.Cross(segments[0].Face, Vector3.Normalize(segments[0].End1 - segments[0].End1));
        }
            
        
        // assign face for the subsequent segments
        for (var i = 1; i < segments.Count; i++)
        {
            
            // common end
            
            (Vector3 closestEndPreviousTray, Vector3 closestEndThisTray, float minDistance) = FindClosestEnds(segments[i-1].End1, segments[i-1].End2, segments[i].End1, segments[i].End2);
            
            var previousTrayDirection = Vector3.Normalize(closestEndPreviousTray == segments[i-1].End1? segments[i-1].End2 - segments[i-1].End1: segments[i-1].End1 - segments[i-1].End2);
            var thisTrayDirection = Vector3.Normalize(closestEndThisTray == segments[i].End1? segments[i].End2 - segments[i].End1: segments[i].End1 - segments[i].End2);
            
            try
            {
                // check if the previous tray direction and thisTray direction are not parallel, if parallel, the same face is retained
                if (Math.Abs(Vector3.Dot(previousTrayDirection, thisTrayDirection)) > 0.95f ||
                    // check if the cross of previousTray and thisTray is not parallel to the previousTray face, if parallel, the same face is retained
                    Math.Abs(Vector3.Dot(segments[i-1].Face, Vector3.Normalize(Vector3.Cross(previousTrayDirection, thisTrayDirection)))) > 0.95f
                    )
                {
                    segments[i].Face = segments[i-1].Face;
                }
                else
                {
                    segments[i].Face = Vector3.Normalize(Vector3.Cross(segments[i-1].Face,Vector3.Cross(previousTrayDirection, thisTrayDirection)));
                }
                
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error for '{segments[i].CableWayBranch}' - {i} while assigning the face.");
            }
            
            // check if everything is OK
            if (Math.Abs(segments[i].Face.X) < 0.01f &&
                Math.Abs(segments[i].Face.Y) < 0.01f &&
                Math.Abs(segments[i].Face.Z) < 0.01f || 
                Vector3.DistanceSquared(segments[i].Face,new Vector3(0f,0f,0f)) <0.9991f || 
                Vector3.DistanceSquared(segments[i].Face,new Vector3(0f,0f,0f)) >1.001f ||
                float.IsNaN(segments[i].Face.X) || float.IsNaN(segments[i].Face.Y) || float.IsNaN(segments[i].Face.Z))
            {
                segments[i].Face = new Vector3(0f, 0f, 1f);
            }
        }
        
        return segments;
    }

    public Task<Tuple<List<Segment>, List<Bend>, List<Tee>, List<Cross>, List<Node>, List<Segment>>>
        GenerateAcc(List<Segment>? wholeSegment)
    {
        var Segments = JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(wholeSegment, jsonSerializerOptions), jsonSerializerOptions);
        //List<Segment> Segments = LayoutFunctions.DeserializeMyClassList(SerializeMyClassList(WholeSegment));
        //List<Segment> Segments = WholeSegment.DeepClone();
        
        
        //
        //List<Segment> Segment = MyFunction.FaceUpdate(List1); faceupdate later
        List<Bend> Bends = [];
        List<Tee> Tees = [];
        List<Cross> Crosses = [];
        List<Vector3> Points = [];
        List<Node> Nodes = [];
        List<Accessory> Accessories = [];
        List<Segment> IsolatedSegments = [];
        //
        List<Seg4Aac> List = [];
        List<NearUID> NearUID = [];
        //
        var watch = Stopwatch.StartNew();
        Debug.WriteLine("creating copy of segment info.....");
        //Segment.RemoveAll(seg => Vector3.Distance(seg.End1, seg.End2) < 0.1);
        Segments.ForEach(item =>
        {
            item.ParentUID = item.UID;
            //
            List.Add(new Seg4Aac());
            
            List[^1].UID = JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(item.UID, jsonSerializerOptions), jsonSerializerOptions);
            //List[^1].UID = item.UID.DeepClone();
            
            List[^1].ParentUID = JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(item.UID, jsonSerializerOptions), jsonSerializerOptions);
            //List[^1].ParentUID = item.UID.DeepClone();
            
            List[^1].Tag = item.Tag;
            List[^1].RecordId = item.RecordId;
            List[^1].End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(item.End1, jsonSerializerOptions), jsonSerializerOptions);
            //List[^1].End1 = item.End1.DeepClone();
            
            List[^1].End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(item.End2, jsonSerializerOptions), jsonSerializerOptions);
            //List[^1].End2 = item.End2.DeepClone();
            
            List[^1].Face = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(item.Face, jsonSerializerOptions), jsonSerializerOptions);
            //List[^1].Face = item.Face.DeepClone();
            
            List[^1].Width = (float)item.Width;
            List[^1].Height = (float)item.Height;
            List[^1].End1End2 = Vector3.Distance(List[^1].End1, List[^1].End2);
        });
        watch.Stop();
        Debug.WriteLine($"..copy info completed in {watch.ElapsedMilliseconds}ms");
        //
        //remove overlap from List
        watch.Restart();
        Debug.WriteLine("start removing overlap.....");
        //
        List<Guid> Removed = new();
        // remove insignificant length segments from the list
        List.RemoveAll(item => item.End1End2 < 0.1);
        //
        for (var i = 0; i < List.Count - 1; i++)
        {
            var seg1 = List[i];
            // skip if this segment is already removed
            if (Removed.Contains(seg1.UID)) continue;
            var gap = 0.1;
            // filter nearby segments for further iteration
            //var ListJ = List.Skip(i + 1).Where(seg2 =>
            //        Math.Min(seg2.End1.X, seg2.End2.X) - Math.Max(seg1.End1.X, seg1.End2.X) < gap &&
            //        Math.Min(seg1.End1.X, seg1.End2.X) - Math.Max(seg2.End1.X, seg2.End2.X) < gap &&
            //        Math.Min(seg2.End1.Y, seg2.End2.Y) - Math.Max(seg1.End1.Y, seg1.End2.Y) < gap &&
            //        Math.Min(seg1.End1.Y, seg1.End2.Y) - Math.Max(seg2.End1.Y, seg2.End2.Y) < gap &&
            //        Math.Min(seg2.End1.Z, seg2.End2.Z) - Math.Max(seg1.End1.Z, seg1.End2.Z) < gap &&
            //        Math.Min(seg1.End1.Z, seg1.End2.Z) - Math.Max(seg2.End1.Z, seg2.End2.Z) < gap
            //    ).ToList();
            ////
            //if (ListJ.Count == 0) continue;
            //for (int j = 0; j < ListJ.Count; j++)
            //..overlap removed in 73136 ms
            for (var j = i + 1; j < List.Count; j++)
            {
                var seg2 = List[j];
                //var seg2 = ListJ[j];
                // skip if this segment is already removed
                if (Removed.Contains(seg2.UID)) continue;
                // skip check if i and j are far off                    
                if (IsFarAway(seg1, seg2)) continue;
                //
                // skip if i and j are not parallel
                if (!IsParallel(seg1.End1, seg1.End2, seg2.End1, seg2.End2)) continue;
                //
                // skip if i and j are not colinear
                if (!Colinear(seg1.End1, seg1.End2, seg2.End1, seg2.End2)) continue;
                //
                // check for overlap reasonably colinear (0.01 angle margin inbuilt) and mid point distance is less than average lengths+margin
                var A = seg1.End1;
                var B = seg1.End2;
                var C = seg2.End1;
                var D = seg2.End2;
                var l1 = Vector3.Distance(A, B);
                var l2 = Vector3.Distance(C, D);
                var M1 = (A + B) / 2;
                var M2 = (C + D) / 2;
                //
                var isOverlapp = Vector3.Distance(M1, M2) <= (l1 + l2) / 2 + 0.01;
                //
                if (isOverlapp)
                {
                    // overlap case
                    // at this moment, remove shorter length which is overlapping
                    // and make one segment as the combined length
                    List<Vector3> Pts = new() { seg1.End1, seg1.End2, seg2.End1, seg2.End2 };
                    Pts = Pts.OrderBy(p => Vector3.Distance(p, new Vector3(-999f, -999f, -99f))).ToList();
                    //var a = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(Pts[0]));
                    var a = Pts[0].DeepClone();
                    //var b = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(Pts[3]));
                    var b = Pts[3].DeepClone();
                    seg1.End1 = new Vector3((float)Math.Round(Pts[0].X, 3), (float)Math.Round(Pts[0].Y, 3),
                        (float)Math.Round(Pts[0].Z, 3));
                    seg1.End2 = new Vector3((float)Math.Round(Pts[3].X, 3), (float)Math.Round(Pts[3].Y, 3),
                        (float)Math.Round(Pts[3].Z, 3));
                    Removed.Add(seg2.UID);
                }
            }
        }

        //
        //// snap all the coordinates to 0.005
        //List.ForEach(seg =>
        //{
        //    seg.End1 = new Vector3(Snap(seg.End1.X), Snap(seg.End1.Y), Snap(seg.End1.Z));
        //    seg.End2 = new Vector3(Snap(seg.End2.X), Snap(seg.End2.Y), Snap(seg.End2.Z));
        //    seg.End1End2 = Vector3.Distance(seg.End2, seg.End1);
        //});
        //float Snap(float nn)
        //{
        //    return (float)(Math.Round(nn * 200) / 200);
        //}
        //
        List.RemoveAll(item => Removed.Contains(item.UID) || item.End1End2 < 0.1);
        //List.ForEach(item => { item.End1End2 = Vector3.Distance(item.End1, item.End2); });
        watch.Stop();
        Debug.WriteLine($".. {Removed.Count} overlap removed in {watch.ElapsedMilliseconds} ms");
        //
        // break at joints
        watch.Restart();
        Debug.WriteLine($"breaking {List.Count} segments at connections....");
        //List.ForEach(seg => seg.ParentUID = seg.UID);
        //
        for (var i = 1; i < List.Count; i++) // loop through the added list 
        for (var j = 0; j < i; j++) // loop through the added list 
        {
            var segi = List[i];
            var segj = List[j];

            if (IsFarAway(segi, segj)) continue;
            if (IsParallel(segi.End1, segi.End2, segj.End1, segj.End2)) continue;

            //check intersection
            var gapAB = 0.004; // <1/2 of 0.01
            var gapCD = 0.004;
            var IntersectionPoints =
                IntersectionPointDec(segi.End1, segi.End2, segj.End1, segj.End2, gapAB, gapAB, gapCD, gapCD);
            // continue if parallel or intersections are outside the lines
            if (IntersectionPoints.Count == 0) continue;
            //
            var P = IntersectionPoints[0]; // on AB
            var Q = IntersectionPoints[1]; // on CD
            //
            // if distant lines, then break not required
            if (Vector3.Distance(P, Q) > 0.01) continue;
            //
            var M = (P + Q) / 2;
            var PQ = new Vector3((float)Math.Round(M.X, 3), (float)Math.Round(M.Y, 3), (float)Math.Round(M.Z, 3));
            //
            // ignore break for insignificant lengths
            if ((Vector3.Distance(segi.End1, PQ) < 0.09 || Vector3.Distance(segi.End2, PQ) < 0.09) &&
                (Vector3.Distance(segj.End1, PQ) < 0.09 || Vector3.Distance(segj.End2, PQ) < 0.09)) continue;
            //
            CheckNBreakOrTrim(segi.UID, PQ, gapAB);
            CheckNBreakOrTrim(segj.UID, PQ, gapCD);
            //
        }

        watch.Stop();
        Debug.WriteLine($"..breaking into {List.Count} segments at connections in {watch.ElapsedMilliseconds} ms");
        //
        // breaks are now created *****************
        //update the Segment List with the added /broken segments
        watch.Restart();
        Debug.WriteLine("updating the original segment list.....");
        List.ForEach(seg =>
        {
            // check if the existing segment or newly created one
            if (seg.UID != seg.ParentUID)
            {
                // new created segment
                var parentSegment = Segments.Where(item => item.UID == seg.ParentUID).ToList()[0];
                //var newSegment = JsonSerializer.Deserialize<Segment>(JsonSerializer.Serialize(parentSegment));
                var newSegment = parentSegment.DeepClone();
                newSegment.UID = seg.UID;
                newSegment.ParentUID = seg.ParentUID;
                //Segments.Add(JsonSerializer.Deserialize<Segment>(JsonSerializer.Serialize(newSegment)));
                Segments.Add(newSegment);
            }

            // updating segment ends and lengths
            var segment = Segments.Where(item => item.UID == seg.UID).ToList()[0];
            //segment.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(seg.End1));
            segment.End1 = seg.End1.DeepClone();
            //segment.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(seg.End2));
            segment.End2 = seg.End2.DeepClone();
            segment.Tag = seg.Tag;
            segment.RecordId = seg.RecordId;
            segment.Length = Vector3.Distance(segment.End1, segment.End2);
        });
        Segments.RemoveAll(item => !List.Any(any => any.UID == item.UID));
        //
        watch.Stop();
        Debug.WriteLine($"..updating the original segment list completed in {watch.ElapsedMilliseconds} ms");
        //
        // accessories -----
        watch.Restart();
        Debug.WriteLine("generating accessories.....");
        //
        // find list of unique ends for the nodes
        Points.Clear();
        var mergegap = 0.05;
        List.ForEach(seg =>
        {
            // add the new point if not existing within a merge gap
            var existingNearbyPoint1 = Points.Where(p => Vector3.Distance(p, seg.End1) < mergegap).ToList();
            if (existingNearbyPoint1.Count != 0)
            {
                // do not add new point, update the segment
                //seg.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(existingNearbyPoint1[0]));
                seg.End1 = existingNearbyPoint1[0].DeepClone();
                //seg.Po2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(existingNearbyPoint1[0]));
                seg.Po2 = existingNearbyPoint1[0].DeepClone();
            }
            else
            {
                Points.Add(seg.End1);
            }

            //
            var existingNearbyPoint2 = Points.Where(p => Vector3.Distance(p, seg.End2) < mergegap).ToList();
            if (existingNearbyPoint2.Count != 0)
            {
                // do not add new point, update the segment
                //seg.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(existingNearbyPoint2[0]));
                seg.End2 = existingNearbyPoint2[0].DeepClone();
                //seg.Po1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(existingNearbyPoint2[0]));
                seg.Po1 = existingNearbyPoint2[0].DeepClone();
            }
            else
            {
                Points.Add(seg.End2);
            }

            //Point.Add(seg.End1);
            //Point.Add(seg.End2);
            seg.DistanceUpdate();
        });
        //
        // possible to have a zero length segment due to above updated ends
        var zeroLengthSegments = List.Where(seg => Vector3.Distance(seg.End1, seg.End2) < mergegap).ToList();
        // remove these zero-length segments as these segments
        List.RemoveAll(seg => Vector3.Distance(seg.End1, seg.End2) < mergegap);
        // create the fresh list of Points as per these non-zero length segments
        Points.Clear();
        List.ForEach(seg =>
        {
            seg.I = List.IndexOf(seg);
            Points.Add(seg.End1);
            Points.Add(seg.End2);
        });
        //
        var UniquePoint = Points.Distinct().ToList();
        // points which are close enough are to be merged

        //UniquePoint.RemoveAll(p => UniquePoint.Any(pp => pp != p && Vector3.Distance(pp, p) < mergegap));
        //
        // Accessory list all connected segments to this point, it lists corresponding ends (End1 or End2 of each connected segments)
        Accessories.Clear();
        UniquePoint.ForEach(p => { Accessories.Add(new Accessory(p)); });
        //
        List.ForEach(segment =>
        {
            // assign the seg and segment connection ends to the corresponding accessory point
            Accessories.Where(a => a.P == segment.End1).ToList().ForEach(p =>
            {
                p.SegGUID.Add(segment.UID);
                p.End.Add(1);
                p.SegI.Add(List.IndexOf(segment));
            });
            Accessories.Where(a => a.P == segment.End2).ToList().ForEach(p =>
            {
                p.SegGUID.Add(segment.UID);
                p.End.Add(2);
                p.SegI.Add(List.IndexOf(segment));
            });
        });
        //
        // for debugging purpose to see the SegGUID and End JSON string
        Accessories.ForEach(a => { a.Update(); });
        watch.Stop();
        // the accessory list is created
        Debug.WriteLine($"...accessory count of {Accessories.Count} determined in {watch.ElapsedMilliseconds} ms");
        //
        // assigning bending edges
        watch.Restart();
        Debug.WriteLine(
            $"assigning bending edges to {Accessories.Where(a => a.End.Count > 1).ToList().Count} connections.....");
        // check intersections for freshly created broken segments
        // if they intersect and are horizontal (faces are similar) then assign their bending edges
        // else in case of vertical intersections, the bending edge is the intersection point itself
        // loop through the accessory list
        //
        Accessories.ForEach(acc =>
        {
            //
            Guid uid1 = new(), uid2 = new(), uid3 = new(), uid4 = new();
            Vector3 Po = new();
            int endOfseg1 = 0, endOfseg2 = 0, endOfseg3 = 0, endOfseg4 = 0;
            //
            //Po = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(acc.P));
            Po = acc.P.DeepClone();
            //
            uid1 = acc.SegGUID[0];
            endOfseg1 = acc.End[0];
            //
            if (acc.End.Count > 1)
            {
                uid2 = acc.SegGUID[1];
                endOfseg2 = acc.End[1];
            }

            if (acc.End.Count > 2)
            {
                uid3 = acc.SegGUID[2];
                endOfseg3 = acc.End[2];
            }

            if (acc.End.Count > 3)
            {
                uid4 = acc.SegGUID[3];
                endOfseg4 = acc.End[3];
            }

            //
            // this is only to assign the bend edge
            switch (acc.SegGUID.Count)
            {
                case 1:
                    // end connection
                    // this joint point has only one segment connected, hence no accessory
                    var seg1 = List.Where(item => item.UID == uid1).ToList()[0];
                    if (endOfseg1 == 1)
                    {
                        //seg1.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(Po));
                        seg1.End1 = Po.DeepClone();
                        //seg1.Po1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(Po));
                        seg1.Po1 = Po.DeepClone();
                    }
                    else
                    {
                        //seg1.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(Po));
                        seg1.End2 = Po.DeepClone();
                        //seg1.Po2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(Po));
                        seg1.Po2 = Po.DeepClone();
                    }

                    break;
                case 2:
                    // bend
                    //
                    AssignEdge(uid1, uid2, Po, endOfseg1, endOfseg2);
                    break;
                case 3:
                    // tee
                    AssignEdge(uid1, uid2, Po, endOfseg1, endOfseg2);
                    AssignEdge(uid2, uid3, Po, endOfseg2, endOfseg3);
                    AssignEdge(uid3, uid1, Po, endOfseg3, endOfseg1);
                    break;
                case >= 4:
                    // cross
                    AssignEdge(uid1, uid2, Po, endOfseg1, endOfseg2);
                    AssignEdge(uid1, uid3, Po, endOfseg1, endOfseg3);
                    AssignEdge(uid1, uid4, Po, endOfseg1, endOfseg4);
                    AssignEdge(uid2, uid3, Po, endOfseg2, endOfseg3);
                    AssignEdge(uid2, uid4, Po, endOfseg2, endOfseg4);
                    AssignEdge(uid3, uid4, Po, endOfseg3, endOfseg4);
                    //
                    // possible that more than 4 connections are at one point, though ideally not desired
                    // however bend edge are not assigned for 5th or more such connections
                    break;
            }

            void AssignEdge(Guid uuid1, Guid uuid2, Vector3 PPo, int eendOfseg1, int eendOfseg2)
            {
                var sseg1 = List.Where(item => item.UID == uuid1).ToList()[0];
                var uu1 = Vector3.Normalize((eendOfseg1 == 1 ? sseg1.End2 : sseg1.End1) - PPo);
                var ww1 = sseg1.Width;
                var sseg2 = List.Where(item => item.UID == uuid2).ToList()[0];
                var uu2 = Vector3.Normalize((eendOfseg2 == 1 ? sseg2.End2 : sseg2.End1) - PPo);
                var ww2 = sseg2.Width;
                //
                var angle = AngleBetween(uu1, uu2);
                var sinA = Math.Sin(angle);
                // almost straight joint between two segments, make sinA =1 only for reducer/adaptor edge calculation or tee/cross opposite sides
                if (Math.Abs(sinA) < 0.1) sinA = 1;
                // horizontal or vertical bend - there is no change
                // assign bend edges
                if (eendOfseg1 == 1)
                {
                    //sseg1.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(PPo));
                    sseg1.End1 = PPo.DeepClone();
                    var d = (float)Math.Min(Vector3.Distance(PPo, sseg1.Po1), ww2 / sinA);
                    sseg1.Po1 = PPo + d * uu1;
                }
                else
                {
                    //sseg1.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(PPo));
                    sseg1.End2 = PPo.DeepClone();
                    var d = (float)Math.Min(Vector3.Distance(PPo, sseg1.Po2), ww2 / sinA);
                    sseg1.Po2 = PPo + d * uu1;
                }

                // same for seg2
                if (eendOfseg2 == 1)
                {
                    //sseg2.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(PPo));
                    sseg2.End1 = PPo.DeepClone();
                    var d = (float)Math.Min(Vector3.Distance(PPo, sseg2.Po1), ww1 / sinA);
                    sseg2.Po1 = PPo + d * uu2;
                }
                else
                {
                    //sseg2.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(PPo));
                    sseg2.End2 = PPo.DeepClone();
                    var d = (float)Math.Min(Vector3.Distance(PPo, sseg2.Po2), ww1 / sinA);
                    sseg2.Po2 = PPo + d * uu2;
                }
            }
            //                
        });
        //
        watch.Stop();
        Debug.WriteLine(
            $"...bending edges are done, to be verified later. completed activity in {watch.ElapsedMilliseconds} ms");
        //
        // verify the edges (especially for small length segments) and update realistic bend edges points
        watch.Restart();
        Debug.WriteLine(
            "verify the edges (especially for small length segments) and update realistic bend edges points......");
        List.ForEach(seg =>
        {
            //
            seg.I = List.IndexOf(seg);
            seg.DistanceUpdate();
            //
            // total 4 cases
            // case 1 : no change : P1 = End1 (earlier End2), P2 = End2 (earlier End1) -> no action
            // case 2: Connection at 1 end: P1 <> End2, P2 = End1 -> check and ensure that P1End2 > 1/2 End1End2
            // case 3: Connection at 2 end: P1 = End2, P2 <> End1 -> check and ensure that P2End1 > 1/2End1End2
            // case 4: Connection at 1 end: P1 <> End2, P2 <> End1 --> check and ensure End1P2-End1P1 > 1/3 End1End2
            //
            //if (Vector3.Distance(seg.End1, seg.Po2) <= 0.001 && Vector3.Distance(seg.End2, seg.Po1) <= 0.001)
            //{
            //    // update Po1 and Po2 if there is no change from the initialised values and the edge has no connection
            //    //Accessory.Where(acc => acc.SegGUID.Contains(seg.UID)).ToList().Any(acc => acc.End[acc.SegGUID.IndexOf(seg.UID)] != 2)
            //    // case of isolated segments
            //    seg.Po2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(seg.End2));
            //    seg.Po1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(seg.End1));
            //}
            if (seg.End1Po1 <= 0.001 && seg.End2Po2 > seg.End1End2 / 2)
                seg.Po2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize((seg.End1 + seg.End2) / 2, jsonSerializerOptions), jsonSerializerOptions);
                //seg.Po2 = ((seg.End1 + seg.End2) / 2).DeepClone();
            if (seg.End2Po2 <= 0.001 && seg.End1Po1 > seg.End1End2 / 2)
                seg.Po1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize((seg.End1 + seg.End2) / 2, jsonSerializerOptions), jsonSerializerOptions);
                //seg.Po1 = ((seg.End1 + seg.End2) / 2).DeepClone();
            if (seg.End1Po1 > 0.001 && seg.End2Po2 > 0.001 && seg.End1Po2 - seg.End1Po1 < seg.End1End2 / 3)
            {
                seg.Po1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize((2 * seg.End1 + seg.End2) / 3, jsonSerializerOptions), jsonSerializerOptions);
                //seg.Po1 = ((2 * seg.End1 + seg.End2) / 3).DeepClone();
                seg.Po2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize((seg.End1 + 2 * seg.End2) / 3, jsonSerializerOptions), jsonSerializerOptions);
                //seg.Po2 = ((seg.End1 + 2 * seg.End2) / 3).DeepClone();
            }
            //
        });
        //
        watch.Stop();
        Debug.WriteLine($"...update of realistic bend edges points completed in {watch.ElapsedMilliseconds} ms");
        //
        // now since all the bends are assigned, hopefully correctly, prepare the segments
        watch.Restart();
        Debug.WriteLine("creating bends, tees and crosses......");
        //            
        Accessories.ForEach(acc =>
        {
            //
            Seg4Aac seg1 = new(), seg2 = new(), seg3 = new(), seg4 = new();
            Guid uid1 = new(), uid2 = new(), uid3 = new(), uid4 = new();
            float w1 = 0, w2 = 0, w3 = 0, w4 = 0;
            Vector3 Po = new(), Po1 = new(), Po2 = new(), Po3 = new(), Po4 = new();
            string n1Tag = "", n2Tag = "", n3Tag = "", n4Tag = "";
            int endOfseg1 = 0, endOfseg2 = 0, endOfseg3 = 0, endOfseg4 = 0;
            //
            // possible that more than 4 connections are at one point, though ideally not desired
            List<Seg4Aac> seg5OrMore = new();
            List<Guid> uid5OrMore = new();
            List<float> w5OrMore = new();
            List<Vector3> Po5OrMore = new();
            List<string> n5OrMoreTag = new();
            List<Vector3> u5OrMore = new();
            List<int> endOfseg5OrMore = new();
            //
            Po = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(acc.P, jsonSerializerOptions), jsonSerializerOptions);
            //Po = acc.P.DeepClone();
            //
            uid1 = acc.SegGUID[0];
            seg1 = List.Where(item => item.UID == acc.SegGUID[0]).ToList()[0];
            endOfseg1 = acc.End[0];
            w1 = seg1.Width;
            Po1 = endOfseg1 == 1 ? seg1.Po1 : seg1.Po2;
            n1Tag = "N" + Nodes.Count.ToString("D6");
            //
            if (acc.End.Count > 1)
            {
                uid2 = acc.SegGUID[1];
                seg2 = List.Where(item => item.UID == acc.SegGUID[1]).ToList()[0];
                endOfseg2 = acc.End[1];
                w2 = seg2.Width;
                Po2 = endOfseg2 == 1 ? seg2.Po1 : seg2.Po2;
                n2Tag = "N" + (Nodes.Count + 1).ToString("D6");
            }

            if (acc.End.Count > 2)
            {
                uid3 = acc.SegGUID[2];
                seg3 = List.Where(item => item.UID == acc.SegGUID[2]).ToList()[0];
                endOfseg3 = acc.End[2];
                w3 = seg3.Width;
                Po3 = endOfseg3 == 1 ? seg3.Po1 : seg3.Po2;
                n3Tag = "N" + (Nodes.Count + 2).ToString("D6");
            }

            if (acc.End.Count > 3)
            {
                uid4 = acc.SegGUID[3];
                seg4 = List.Where(item => item.UID == acc.SegGUID[3]).ToList()[0];
                endOfseg4 = acc.End[3];
                w4 = seg4.Width;
                Po4 = endOfseg4 == 1 ? seg4.Po1 : seg4.Po2;
                n4Tag = "N" + (Nodes.Count + 3).ToString("D6");
            }

            // possible that more than 4 connections are at one point, though ideally not desired
            if (acc.End.Count > 4)
                // define parameters for each of such additional connections in list
                for (var ii = 4; ii < acc.SegGUID.Count; ii++)
                {
                    uid5OrMore.Add(acc.SegGUID[ii]);
                    seg5OrMore.Add(List.Where(item => item.UID == acc.SegGUID[ii]).ToList()[0]);
                    endOfseg5OrMore.Add(acc.End[ii]);
                    w5OrMore.Add(seg5OrMore.Last().Width);
                    Po5OrMore.Add(endOfseg5OrMore.Last() == 1 ? seg5OrMore.Last().Po1 : seg5OrMore.Last().Po2);
                    n5OrMoreTag.Add("N" + (Nodes.Count + ii).ToString("D6"));
                }

            //
            // this is only to assign the bend edge
            switch (acc.SegGUID.Count)
            {
                case 1:
                    // end connection
                    // this joint point has only one segment connected, hence no accessory
                    break;
                case 2:
                    // bend
                    //
                    var bend = new Bend("B" + Bends.Count.ToString("D6"), Po, Po1, Po2, w1, w2,
                        seg1.Face, seg2.Face, seg1.Height, seg2.Height, _globalData.CurveStep, n1Tag, n2Tag);
                    Bends.Add(JsonSerializer.Deserialize<Bend>(JsonSerializer.Serialize(bend,jsonSerializerOptions), jsonSerializerOptions));
                    //Bends.Add(bend.DeepClone());
                    break;
                case 3:
                    // tee
                    // order of points not required as Tee can be in any sequence betwee 3 points
                    //
                    var tee = new Tee("T" + Tees.Count.ToString("D6"), Po, Po1, Po2, Po3,
                        w1, w2, w3, seg1.Face, seg2.Face, seg3.Face,
                        seg1.Height, seg2.Height, seg3.Height,
                        _globalData.CurveStep, n1Tag, n2Tag, n3Tag);
                    Tees.Add(JsonSerializer.Deserialize<Tee>(JsonSerializer.Serialize(tee, jsonSerializerOptions), jsonSerializerOptions));
                    //Tees.Add(tee.DeepClone());
                    break;
                case >= 4:
                    // cross
                    // check and order the points
                    // as cross is considering section 1-2 crossing 3-4
                    var u1 = Vector3.Normalize((endOfseg1 == 1 ? seg1.End2 : seg1.End1) - Po);
                    var u2 = Vector3.Normalize((endOfseg2 == 1 ? seg2.End2 : seg2.End1) - Po);
                    var u3 = Vector3.Normalize((endOfseg3 == 1 ? seg3.End2 : seg3.End1) - Po);
                    var u4 = Vector3.Normalize((endOfseg4 == 1 ? seg4.End2 : seg4.End1) - Po);
                    //
                    var angle12 = AngleBetween(u1, u2);
                    var angle13 = AngleBetween(u1, u3);
                    var angle14 = AngleBetween(u1, u4);
                    //
                    Cross cross = new();
                    if (angle12 == new List<double> { angle12, angle13, angle14 }.Max())
                        // 1-2 & 3-4 cross
                        cross = new Cross("C" + Crosses.Count.ToString("D6"), Po, Po1, Po2, Po3, Po4,
                            w1, w2, w3, w4, seg1.Face, seg2.Face, seg3.Face, seg4.Face,
                            seg1.Height, seg2.Height, seg3.Height, seg4.Height,
                            _globalData.CurveStep, n1Tag, n2Tag, n3Tag, n4Tag);
                    else if (angle13 == new List<double> { angle12, angle13, angle14 }.Max())
                        // 1-3 & 2-4 cross
                        cross = new Cross("C" + Crosses.Count.ToString("D6"), Po, Po1, Po3, Po2, Po4,
                            w1, w3, w2, w4, seg1.Face, seg3.Face, seg2.Face, seg4.Face,
                            seg1.Height, seg3.Height, seg2.Height, seg4.Height,
                            _globalData.CurveStep, n1Tag, n3Tag, n2Tag, n4Tag);
                    else if (angle14 == new List<double> { angle12, angle13, angle14 }.Max())
                        // 1-4 & 2-3 cross
                        cross = new Cross("C" + Crosses.Count.ToString("D6"), Po, Po1, Po4, Po3, Po2,
                            w1, w4, w3, w2, seg1.Face, seg4.Face, seg3.Face, seg2.Face,
                            seg1.Height, seg4.Height, seg3.Height, seg2.Height,
                            _globalData.CurveStep, n1Tag, n4Tag, n3Tag, n2Tag);
                    //
                    Crosses.Add(JsonSerializer.Deserialize<Cross>(JsonSerializer.Serialize(cross, jsonSerializerOptions), jsonSerializerOptions));
                    //Crosses.Add(cross.DeepClone());
                    //
                    // possible that more than 4 connections are at one point, though ideally not desired
                    if (acc.End.Count > 4)
                    {
                        // no further geometry to be created apart from the cross which is already created as per 1st four connected segments
                    }

                    break;
            }

            //
            // add node information and update this end point
            //
            if (acc.SegGUID.Count > 1)
            {
                if (Nodes.Any(n => Vector3.Distance(n.Point, Po1) < 0.002))
                {
                    var N = Nodes.Where(n => Vector3.Distance(n.Point, Po1) < 0.002).ToList()[0];
                }

                Nodes.Insert(Nodes.Count,
                    new Node(n1Tag, Po1, w1, seg1.Face, "Segment", uid1, endOfseg1, new List<string> { n2Tag }));
                Nodes.Insert(Nodes.Count,
                    new Node(n2Tag, Po2, w2, seg2.Face, "Segment", uid2, endOfseg2, new List<string> { n1Tag }));
            }

            if (acc.SegGUID.Count > 2)
            {
                Nodes[^2].ConnectedNodesTag.Add(n3Tag);
                Nodes[^1].ConnectedNodesTag.Add(n3Tag);

                Nodes.Insert(Nodes.Count,
                    new Node(n3Tag, Po3, w3, seg3.Face, "Segment", uid3, endOfseg3, new List<string> { n1Tag, n2Tag }));
            }

            if (acc.SegGUID.Count > 3)
            {
                Nodes[^3].ConnectedNodesTag.Add(n4Tag);
                Nodes[^2].ConnectedNodesTag.Add(n4Tag);
                Nodes[^1].ConnectedNodesTag.Add(n4Tag);
                Nodes.Insert(Nodes.Count,
                    new Node(n4Tag, Po4, w4, seg4.Face, "Segment", uid4, endOfseg4,
                        new List<string> { n1Tag, n2Tag, n3Tag }));
                // possible that more than 4 connections are at one point, though ideally not desired
                if (acc.End.Count > 4)
                    // in this case, add nodes
                    for (var ii = 4; ii < acc.SegGUID.Count; ii++)
                    {
                        var niiTag = n5OrMoreTag[ii - 4];
                        var poii = Po5OrMore[ii - 4];
                        var wii = w5OrMore[ii - 4];
                        var segii = seg5OrMore[ii - 4];
                        var uidii = uid5OrMore[ii - 4];
                        var endOfsegii = endOfseg5OrMore[ii - 4];
                        var niiXTag = n5OrMoreTag.Where(tg => tg != niiTag).ToList();
                        var connectionTags = new List<string> { n1Tag, n2Tag, n3Tag, n4Tag };
                        if (niiXTag.Count > 0) connectionTags.Concat(niiXTag);

                        //
                        Nodes[^4].ConnectedNodesTag.Add(niiTag);
                        Nodes[^3].ConnectedNodesTag.Add(niiTag);
                        Nodes[^2].ConnectedNodesTag.Add(niiTag);
                        Nodes[^1].ConnectedNodesTag.Add(niiTag);
                        //
                        Nodes.Insert(Nodes.Count,
                            new Node(niiTag, poii, wii, segii.Face, "Segment", uidii, endOfsegii, connectionTags));
                    }
            }

            // 
            // updating segments with the node information of this connection end
            if (acc.SegGUID.Count > 1)
            {
                UpdateSegmentForThisEnd(uid1, endOfseg1, Po1, n1Tag);
                UpdateSegmentForThisEnd(uid2, endOfseg2, Po2, n2Tag);
            }

            if (acc.SegGUID.Count > 2) UpdateSegmentForThisEnd(uid3, endOfseg3, Po3, n3Tag);
            if (acc.SegGUID.Count > 3) UpdateSegmentForThisEnd(uid4, endOfseg4, Po4, n4Tag);
            //
            // possible that more than 4 connections are at one point, though ideally not desired
            if (acc.SegGUID.Count > 4)
                for (var ii = 4; ii < acc.SegGUID.Count; ii++)
                {
                    var niiTag = n5OrMoreTag[ii - 4];
                    var poii = Po5OrMore[ii - 4];
                    var uidii = uid5OrMore[ii - 4];
                    var endOfsegii = endOfseg5OrMore[ii - 4];
                    //
                    UpdateSegmentForThisEnd(uidii, endOfsegii, poii, niiTag);
                }
        });
        //
        watch.Stop();
        Debug.WriteLine($"... bends, tees and crosses are created in {watch.ElapsedMilliseconds} ms");
        //
        // node connection for both end of the segments
        watch.Restart();
        Debug.WriteLine(
            "node connection for both end of the segments and determining isolated segments (without any connections)");
        //
        List<Guid> IsolatedSegs = new();
        Segments.ForEach(seg =>
        {
            // no. of ends already assigned with a node
            var nns = Nodes.Where(n => n.SegmentUID == seg.UID).ToList();
            // nns Count can be zero only for isolated segments
            // if nns Count == 2 then no action required to add new node
            // only if nns Count ==1 then new node to be added
            if (nns.Count == 0)
            {
                // these are the cases where are isolated segments not connected to any other segments
                // no nodes should be generated for these segments
                IsolatedSegs.Add(seg.UID);
            }
            else if (nns.Count == 1)
            {
                var doneEnd = nns[0].SegmentEnd;
                var notYetDoneEnd = nns[0].SegmentEnd == 1 ? 2 : 1;
                var nTag = "N" + Nodes.Count.ToString("D6");
                nns[0].ConnectedNodesTag.Add(nTag);
                var listSeg = List.Where(s => s.UID == seg.UID).ToList()[0];
                var nPoint = notYetDoneEnd == 1 ? seg.End1 : seg.End2;

                Nodes.Insert(Nodes.Count,
                    new Node(nTag, nPoint, seg.Width, seg.Face, "Segment", seg.UID, notYetDoneEnd,
                        new List<string> { nns[0].Tag }));
                if (notYetDoneEnd == 1)
                    seg.Node1 = nTag;
                else
                    seg.Node2 = nTag;
            }
            else if (nns.Count == 2)
            {
                var n1s = Nodes.Where(n => n.Tag == seg.Node1).ToList();
                var n2s = Nodes.Where(n => n.Tag == seg.Node2).ToList();
                var n1 = n1s[0];
                var n2 = n2s[0];
                if (!n1.ConnectedNodesTag.Contains(n2.Tag)) n1.ConnectedNodesTag.Add(n2.Tag);
                if (!n2.ConnectedNodesTag.Contains(n1.Tag)) n2.ConnectedNodesTag.Add(n1.Tag);
            }
            //
        });
        //
        // remove isolated segments from the Segment List
        IsolatedSegments = Segments.Where(seg => IsolatedSegs.Contains(seg.UID)).ToList();
        Segments.RemoveAll(seg => IsolatedSegs.Contains(seg.UID));
        Segments.ForEach(seg => { seg.Isolated = false; });
        //
        // provide nodes to isolated segments so that the jump nodes can be created for isolated segments
        IsolatedSegments.ForEach(seg =>
        {
            seg.Node1 = "N" + Nodes.Count.ToString("D6");
            seg.Node2 = "N" + (Nodes.Count + 1).ToString("D6");
            Nodes.Insert(Nodes.Count,
                new Node(seg.Node1, seg.End1, seg.Width, seg.Face, "Segment", seg.UID, 1,
                    new List<string> { seg.Node2 }));
            Nodes.Insert(Nodes.Count,
                new Node(seg.Node2, seg.End2, seg.Width, seg.Face, "Segment", seg.UID, 2,
                    new List<string> { seg.Node1 }));
            seg.Isolated = true;
        });
        //
        Debug.WriteLine(
            $"...accessories completed : removed {IsolatedSegs.Count} isolated segments, {Segments.Count} segments,  {Bends.Count} bends, {Tees.Count} tees, {Crosses.Count} crosses and {Nodes.Count} nodes.");
        //
        var resultTuple =  new Tuple<List<Segment>, List<Bend>, List<Tee>, List<Cross>, List<Node>, List<Segment>>(Segments, Bends,
            Tees, Crosses, Nodes, IsolatedSegments);
        return Task.FromResult(resultTuple);

        //
        //
        //
        void UpdateSegmentForThisEnd(Guid uid, int node, Vector3 p, string tag)
        {
            var segs = Segments.Where(seg => seg.UID == uid).ToList();

            var seg = segs[0];

            if (node == 1)
            {
                seg.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(p,jsonSerializerOptions), jsonSerializerOptions);
                //seg.End1 = p.DeepClone();
                seg.Node1 = tag;
            }
            else if (node == 2)
            {
                seg.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(p,jsonSerializerOptions), jsonSerializerOptions);
                //seg.End2 = p.DeepClone();
                seg.Node2 = tag;
            }

            ;
        }

        //
        bool Colinear(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            // check if distance of C from AB is <0.001
            if (Vector3.Distance(A, B) > Vector3.Distance(C, D))
            {
                if (DistancePointToLineSquared(C, A, B) > 0.00001) return false;
                if (DistancePointToLineSquared(D, A, B) > 0.00001) return false;
            }
            else
            {
                if (DistancePointToLineSquared(A, C, D) > 0.00001) return false;
                if (DistancePointToLineSquared(B, C, D) > 0.00001) return false;
            }

            return true;
        }

        //
        void CheckNBreakOrTrim(Guid iuid, Vector3 I, double gap)
        {
            // break cases to be evaluated
            // case0: Intersection point near to both the edges : not a credible case
            // case1: Intersection point near edge1 : no break
            // case2: Intersection point near edge2 : no break
            // case3: Intersection point away and in-between edges : break
            //
            var seg = List.Where(l => l.UID == iuid).ToList()[0];
            //
            var P1 = Vector3.Distance(seg.End1, I);
            var P2 = Vector3.Distance(seg.End2, I);
            if (P1 <= gap && P2 <= gap)
            {
                // case 0: Intersection point near to both the edges : no credible case
                // in this case, this segment should be removed from the list as the segment length seems too short
            }
            else if (P1 <= gap && P2 > gap)
            {
                // case 1: Intersection point near edge1
                seg.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(I, jsonSerializerOptions), jsonSerializerOptions);
                //seg.End1 = I.DeepClone();
            }
            else if (P1 > gap && P2 <= gap)
            {
                // case 2: Intersection point near edge2
                seg.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(I, jsonSerializerOptions), jsonSerializerOptions);
                //seg.End2 = I.DeepClone();
            }
            else if (P1 > gap && P2 > gap)
            {
                // case 3: Intersection point away and in-between edges
                // case of breaking segment
                // add new segment to the List
                AddNew(seg, I, seg.End2);
                // assign end2
                seg.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(I, jsonSerializerOptions), jsonSerializerOptions);
                //seg.End2 = I.DeepClone();
            }
        }

        //
        void AddNew(Seg4Aac seg, Vector3 E1, Vector3 E2)
        {
            // create the new segment temp as per parent segment seg with ends E1 and E2
            var temp = JsonSerializer.Deserialize<Seg4Aac>(JsonSerializer.Serialize(seg, jsonSerializerOptions), jsonSerializerOptions);
            //var temp = seg.DeepClone();
            // assign ends
            temp.End1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(E1, jsonSerializerOptions), jsonSerializerOptions);
            //temp.End1 = E1.DeepClone();
            temp.End2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(E2, jsonSerializerOptions), jsonSerializerOptions);
            //temp.End2 = E2.DeepClone();
            // update temp
            temp.UID = Guid.NewGuid();
            temp.ParentUID = seg.ParentUID;
            var a = (List.Where(l => l.ParentUID == temp.ParentUID).ToList().Count + 1).ToString("D2");
            temp.Tag = seg.Tag + "-" + a;
            temp.RecordId = seg.RecordId + "-" + a;
            temp.End1End2 = Vector3.Distance(temp.End2, temp.End1);
            List.Insert(List.Count, JsonSerializer.Deserialize<Seg4Aac>(JsonSerializer.Serialize(temp, jsonSerializerOptions), jsonSerializerOptions));
            //List.Insert(List.Count, temp.DeepClone());
        }

        //
        static bool IsFarAway(Seg4Aac path1, Seg4Aac path2, double d = 0.1)
        {
            if (
                Math.Min(path2.End1.X, path2.End2.X) - Math.Max(path1.End1.X, path1.End2.X) < d &&
                Math.Min(path1.End1.X, path1.End2.X) - Math.Max(path2.End1.X, path2.End2.X) < d &&
                Math.Min(path2.End1.Y, path2.End2.Y) - Math.Max(path1.End1.Y, path1.End2.Y) < d &&
                Math.Min(path1.End1.Y, path1.End2.Y) - Math.Max(path2.End1.Y, path2.End2.Y) < d &&
                Math.Min(path2.End1.Z, path2.End2.Z) - Math.Max(path1.End1.Z, path1.End2.Z) < d &&
                Math.Min(path1.End1.Z, path1.End2.Z) - Math.Max(path2.End1.Z, path2.End2.Z) < d
            )
                return false;

            return true;
        }
        //
        //
    }


    public bool IsSegsFarAway(Segment seg1, Segment seg2, double d = 0.1)
    {
        Vector3 maxSeg1 = Vector3.Max(seg1.End1, seg1.End2);
        Vector3 minSeg1 = Vector3.Min(seg1.End1, seg1.End2);
        Vector3 maxSeg2 = Vector3.Max(seg2.End1, seg2.End2);
        Vector3 minSeg2 = Vector3.Min(seg2.End1, seg2.End2);

        return !(minSeg2.X - maxSeg1.X < d && minSeg1.X - maxSeg2.X < d &&
                 minSeg2.Y - maxSeg1.Y < d && minSeg1.Y - maxSeg2.Y < d &&
                 minSeg2.Z - maxSeg1.Z < d && minSeg1.Z - maxSeg2.Z < d);
    }
    
    public Tuple<List<Node>, List<Segment>> NodeWithJumpSegments(List<Node> nodeP,
        List<Segment> connectedSegmentP, List<Segment> isolatedSegmentP)
    {
        // this function has disregard cable laid status and spacing criteria
        // jump cable between nearby crossing segments where there is no nodes, i.e., crossing segments midway
        // add connection of nodes based on the cable route criteria and dx, dy, dz and d between nodes
        // dx = 0.5, dy = 0.5, dz = ?, d = 10 assumed can be changed later
        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        var NodeR = JsonSerializer.Deserialize<List<Node>>(JsonSerializer.Serialize(nodeP, jsonSerializerOption),
            jsonSerializerOption);
        var ConnectedSegmentR =
            JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(connectedSegmentP, jsonSerializerOption),
                jsonSerializerOption);
        var IsolatedSegmentR =
            JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(isolatedSegmentP, jsonSerializerOption),
                jsonSerializerOption);
        var JoinedSegmentR = ConnectedSegmentR.Concat(IsolatedSegmentR).ToList();

        if (JoinedSegmentR.Count > 1)
        {
            var nodeCount = NodeR.Count;
            for (var i = 0; i < JoinedSegmentR.Count - 1; i++)
            {
                // filter only rearby segments to carry out further checks
                //var nearbySegments = JoinedSegmentR.Where(seg => !IsSegsFarAway(JoinedSegmentR[i], seg, .6f)).ToList();
                List<int> filteredIndices =  JoinedSegmentR.Skip(i+1)
                    .Select((seg, index) => new { seg, index }) // Create anonymous objects with seg and index
                    .Where(item => !IsSegsFarAway(JoinedSegmentR[i], item.seg, 0.6f)) // Filter based on IsSegsFarAway
                    .Select(item => item.index) // Select the indices
                    .ToList();
                
                if(i%100 == 0) Debug.WriteLine($"Node with jump for segment # {i}, node created {NodeR.Count()}");
                for (var k = 0; k < filteredIndices.Count; k++)
                {
                    var j = filteredIndices[k] + i + 1;
                    var S1 = JoinedSegmentR[i];
                    var S2 = JoinedSegmentR[j];
                    //
                    //if (IsFarAway(S1, S2, 0.6)) continue;
                    if (IsParallel(S1.End1, S1.End2, S2.End1, S2.End2)) continue;
                    if (Coplaner(S1.End1, S1.End2, S2.End1, S2.End2)) continue;
                    //
                    {
                        // gap1 and gap2 are by default 0.01 just to takes into consideration of all the calculation errors,
                        // however, for an open-ended node (i.e., have only another end connection) must check with larger gap for jump nodes
                        var n11 = NodeR.Where(n => n.Tag == S1.Node1).ToList()[0];
                        var n12 = NodeR.Where(n => n.Tag == S1.Node2).ToList()[0];
                        var n21 = NodeR.Where(n => n.Tag == S2.Node1).ToList()[0];
                        var n22 = NodeR.Where(n => n.Tag == S2.Node2).ToList()[0];
                        double g11 = 0.01, g12 = 0.01, g21 = 0.01, g22 = 0.01;

                        bool OpenEnded(string nTag)
                        {
                            var nNode = NodeR.Where(n => n.Tag == nTag).ToList()[0];
                            var connectingNodes = NodeR.Where(n => nNode.ConnectedNodesTag.Contains(n.Tag)).ToList();
                            var sameSeg = connectingNodes.All(cNode => cNode.SegmentUID == nNode.SegmentUID);
                            return sameSeg;
                        }

                        if (OpenEnded(n11.Tag)) g11 = .6;
                        if (OpenEnded(n12.Tag)) g12 = .6;
                        if (OpenEnded(n21.Tag)) g21 = .6;
                        if (OpenEnded(n22.Tag)) g22 = .6;
                        // assume S1 and S2 are skew lines
                        //List<Vector3> PQ = IntersectionPoints(S1.End1, S1.End2, S2.End1, S2.End2);
                        var PQ = IntersectionPointDec(S1.End1, S1.End2, S2.End1, S2.End2, g11, g12, g21, g22);
                        if (PQ.Count != 0)
                        {
                            // skewed lines
                            var P = PQ[0];
                            var Q = PQ[1];
                            var d = (P - Q).Length();
                            if (d > 0.1 && d < 0.6)
                            {
                                // checking and creating jump nodes, if required
                                var jn1Tag = CheckOrCreateNewJumpNode(P, S1);
                                var jn2Tag = CheckOrCreateNewJumpNode(Q, S2);
                                NodeR.Where(n => n.Tag == jn1Tag).ToList()[0].ConnectedNodesTag.Add(jn2Tag);
                                NodeR.Where(n => n.Tag == jn2Tag).ToList()[0].ConnectedNodesTag.Add(jn1Tag);
                                //
                            }
                        }
                    }
                }
            }
        }

        //
        string CheckOrCreateNewJumpNode(Vector3 O, Segment seg)
        {
            Node JN;
            // check point O is coinciding with seg Ends or near the openended segment end
            // s is the factor of width. point O is away from edges
            var s = Vector3.Dot(O - seg.End1, seg.End2 - seg.End1) / (Vector3.Distance(seg.End1, seg.End2) * seg.Width);
            // if s factor w.r.t length is <0.01 then it's reasonably near to End1, if s >0.9 then It's reasonably near to End2
            if (Vector3.Distance(O, seg.End1) < seg.Width)
            {
                JN = NodeR.Where(n => n.Tag == seg.Node1).ToList()[0];
            }
            else if (Vector3.Distance(O, seg.End2) < seg.Width)
            {
                JN = NodeR.Where(n => n.Tag == seg.Node2).ToList()[0];
            }
            else
            {
                // does not near the edges
                // initialize Jump Node and jump point list is not already exists
                if (seg.Node9?.Any() ?? false)
                {
                    seg.Node9 = [];
                    seg.End9 = [];
                }

                // check if O coincides with any of the existing jump points 
                if (seg.End9?.Where(jEnd => Vector3.Distance(jEnd, O) < 0.01).ToList().Count > 0)
                {
                    // coincides with already existing jump points
                    // no need to create new jump point
                    var jumpPoint = seg.End9.Where(jEnd => Vector3.Distance(jEnd, O) < 0.01).ToList()[0];
                    var jnTag = seg.Node9[seg.End9.IndexOf(jumpPoint)];
                    JN = NodeR.Where(n => n.Tag == jnTag).ToList()[0];
                }
                else
                {
                    // does not coincide with any of the already existing jump points
                    // create new jump point
                    var jnTag = "N" + NodeR.Count.ToString("D6");
                    //var jnPoint = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(O));
                    var jnPoint = new Vector3(O.X, O.Y, O.Z);
                    JN = new Node(jnTag, jnPoint, seg.Width, seg.Face, "Segment", seg.UID, 9,
                        [seg.Node1, seg.Node2]);
                    // add pre-existing jump nodes in this segment as connected nodes to this new jump node
                    if (seg.Node9?.Count > 0)
                        seg.Node9.ForEach(jNode =>
                        {
                            JN.ConnectedNodesTag.Add(jNode);
                            NodeR.Where(n => n.Tag == jNode).ToList()[0].ConnectedNodesTag.Add(jnTag);
                        });
                    seg.Node9.Add(jnTag);
                    seg.End9.Add(jnPoint);
                    //
                    NodeR.Add(JsonSerializer.Deserialize<Node>(JsonSerializer.Serialize(JN, jsonSerializerOption),
                        jsonSerializerOption));
                    NodeR.Where(n => n.Tag == seg.Node1).ToList()[0].ConnectedNodesTag.Add(jnTag);
                    NodeR.Where(n => n.Tag == seg.Node2).ToList()[0].ConnectedNodesTag.Add(jnTag);
                }
            }

            return JN.Tag;
        }

        //
        //
        JoinedSegmentR.ForEach(seg =>
        {
            seg.Node9S = JsonSerializer.Serialize(seg.Node9, jsonSerializerOption);
            seg.End9S = JsonSerializer.Serialize(seg.End9, jsonSerializerOption);
        });
        //
        return new Tuple<List<Node>, List<Segment>>(NodeR, JoinedSegmentR);
    }

    public Tuple<List<Node>, List<Segment>> NodesInAnotherSegmentWithJump(List<Node> nodeP,
        List<Segment> segmentP)
    {
        // this function creates connection for cases where a node of a segment falls into another segment
        // this case is possible if due to some reasons there is no tee/cross created
        // it first created a dummy node in the segment
        // and then create connections
        //

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        var NodeR = JsonSerializer.Deserialize<List<Node>>(JsonSerializer.Serialize(nodeP, jsonSerializerOption),
            jsonSerializerOption);
        var SegmentR =
            JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(segmentP, jsonSerializerOption),
                jsonSerializerOption);
        // create segment boudary limits, i.e., eight corners
        SegmentR.ForEach(seg =>
        {
            var L = seg.End2 - seg.End1;
            var W = (float)seg.Width;
            var H = (float)seg.Height;
            var l = Vector3.Normalize(L);
            var h = Vector3.Normalize(seg.Face);
            var w = Vector3.Cross(l, h);
            //
            var p1 = seg.End1 - W / 2 * w;
            var p2 = seg.End1 + W / 2 * w;
            var p3 = p1 + H * h;
            var p4 = p2 + H * h;
            var p5 = p1 + L;
            var p6 = p2 + L;
            var p7 = p3 + L;
            var p8 = p4 + L;
            List<float> XL = [p1.X, p2.X, p3.X, p4.X, p5.X, p6.X, p7.X, p8.X];
            List<float> YL = [p1.Y, p2.Y, p3.Y, p4.Y, p5.Y, p6.Y, p7.Y, p8.Y];
            List<float> ZL = [p1.Z, p2.Z, p3.Z, p4.Z, p5.Z, p6.Z, p7.Z, p8.Z];
            var insedeNodes = NodeR.Where(node =>
                XL.Max() >= node.Point.X && XL.Min() <= node.Point.X &&
                YL.Max() >= node.Point.Y && YL.Min() <= node.Point.Y &&
                ZL.Max() >= node.Point.Z && ZL.Min() <= node.Point.Z &&
                node.SegmentUID != seg.UID &&
                PointInsideSegment(node.Point, seg).Item1
            ).ToList();
            //
            if (insedeNodes.Count > 0)
                insedeNodes.ForEach(insidNode =>
                {
                    // for each such node, create dummy node in segment seg and create connection with this dummy node
                    // before creating dummy node, check if this node is near to any end of the segment
                    // checking and creating jump nodes, if required
                    var O = insidNode.Point;
                    //
                    //
                    Node JN;
                    // check point O is coinciding with seg Ends or near the openended segment end
                    var s = Vector3.Dot(O - seg.End1, seg.End2 - seg.End1) /
                            Vector3.DistanceSquared(seg.End1, seg.End2);
                    // if s factor w.r.t length is <0.01 then its reasonably near to End1, if s >0.9 then its reasonably near to End2
                    if (s < 0.1)
                    {
                        JN = NodeR.Where(n => n.Tag == seg.Node1).ToList()[0];
                    }
                    else if (s > 0.9)
                    {
                        JN = NodeR.Where(n => n.Tag == seg.Node2).ToList()[0];
                    }
                    else
                    {
                        // does not near the edges
                        // initialize Jump Node and jump point list is not already exists
                        if (seg.Node9?.Any() ?? false)
                        {
                            seg.Node9 = new List<string>();
                            seg.End9 = new List<Vector3>();
                        }

                        // check if O coincides with any of the existing jump points 
                        if (seg.End9?.Where(jEnd => Vector3.Distance(jEnd, O) < 0.01).ToList().Count > 0)
                        {
                            // coincides with already existing jump points
                            // no need to create new jump point
                            var jumpPoint = seg.End9.Where(jEnd => Vector3.Distance(jEnd, O) < 0.01).ToList()[0];
                            var jnTag = seg.Node9[seg.End9.IndexOf(jumpPoint)];
                            JN = NodeR.Where(n => n.Tag == jnTag).ToList()[0];
                        }
                        else
                        {
                            // does not coincide with any of the already existing jump points
                            // create new jump point
                            var jnTag = "N" + NodeR.Count.ToString("D6");
                            //var jnPoint = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(O));
                            var jnPoint = new Vector3(O.X, O.Y, O.Z);
                            JN = new Node(jnTag, jnPoint, seg.Width, seg.Face, "Segment", seg.UID, 9,
                                new List<string> { seg.Node1, seg.Node2, insidNode.Tag });
                            // add pre-existing jump nodes in this segment as connected nodes to this new jump node
                            if (seg.Node9?.Count > 0)
                                seg.Node9.ForEach(jNode =>
                                {
                                    JN.ConnectedNodesTag.Add(jNode);
                                    NodeR.Where(n => n.Tag == jNode).ToList()[0].ConnectedNodesTag.Add(jnTag);
                                });
                            seg.Node9.Add(jnTag);
                            seg.End9.Add(jnPoint);
                            seg.Node9S = JsonSerializer.Serialize(seg.Node9, jsonSerializerOption);
                            seg.End9S = JsonSerializer.Serialize(seg.End9, jsonSerializerOption);
                            //
                            NodeR.Add(JsonSerializer.Deserialize<Node>(
                                JsonSerializer.Serialize(JN, jsonSerializerOption), jsonSerializerOption));
                            NodeR.Where(n => n.Tag == seg.Node1).ToList()[0].ConnectedNodesTag.Add(jnTag);
                            NodeR.Where(n => n.Tag == seg.Node2).ToList()[0].ConnectedNodesTag.Add(jnTag);
                            insedeNodes[0].ConnectedNodesTag.Add(jnTag);
                        }
                    }
                });
        });

        return new Tuple<List<Node>, List<Segment>>(NodeR, SegmentR);
    }

    public List<Node> DeadEndNodesWithJump(List<Node>? nodes, float d = 1f)
    {
        // this function is separate from the NodeWithJumpSegments
        // this function checks available nearby nodes for dead end nodes (where node has only one connection)
        // and provides connections
        // distance d = 1m

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        nodes = JsonSerializer.Deserialize<List<Node>>(JsonSerializer.Serialize(nodes, jsonSerializerOption),
            jsonSerializerOption);
        var deadEndNodes = nodes.Where(node => node.ConnectedNodesTag.Count == 1).ToList();
        if (deadEndNodes.Count == 0) return nodes;
        deadEndNodes.ForEach(node =>
        {
            var nearbyNodes = nodes.Where(n =>
                Math.Abs(n.Point.X - node.Point.X) < d &&
                Math.Abs(n.Point.Y - node.Point.Y) < d &&
                Math.Abs(n.Point.Z - node.Point.Z) < d &&
                n.Tag != node.Tag &&
                !n.ConnectedNodesTag.Contains(node.Tag) &&
                !node.ConnectedNodesTag.Contains(n.Tag) &&
                Vector3.Distance(n.Point, node.Point) < d
            ).ToList();
            // assign connections
            if (nearbyNodes.Count > 1)
                nearbyNodes.ForEach(nearNode =>
                {
                    // add connections
                    nearNode.ConnectedNodesTag.Add(node.Tag);
                    node.ConnectedNodesTag.Add(nearNode.Tag);
                });
        });
        return nodes;
    }


    public String DrawLadderJSONPoints(double width, double height, Vector3 end1, Vector3 end2, Vector3 face)
    {
        
        List<Vector3> points = [];
        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };
        if (end1 == end2 || Math.Abs(Vector3.Dot(Vector3.Normalize(end2 - end1), Vector3.Normalize(face)))>0.95f)
        {
            return JsonSerializer.Serialize(points, jsonSerializerOption);
        }
        else
        {


            //Vector3 p1, p2, p3, p4, p5, p6, p7, p8, p1p2, pw;
            face = Vector3.Multiply((float)height, Vector3.Normalize(face));
            var p1p2 = Vector3.Subtract(end2, end1);
            var pw = Vector3.Multiply((float)(width / 2), Vector3.Normalize(Vector3.Cross(face, p1p2)));
            var p2 = Vector3.Add(end1, pw);
            var p3 = Vector3.Subtract(end1, pw);
            var p1 = Vector3.Add(p2, face);
            var p4 = Vector3.Add(p3, face);
            var p5 = Vector3.Add(p1, p1p2);
            var p6 = Vector3.Add(p2, p1p2);
            var p7 = Vector3.Add(p3, p1p2);
            var p8 = Vector3.Add(p4, p1p2);
            points = [p1, p2, p3, p4, p5, p6, p7, p8];

            // Convert Unity Vector3 to SerializableVector3
            //var serializableVector3List = points.Select(vector => SerializableVector3.FromVector3(vector)).ToList();

            // Serialize to JSON
            //var jsonString = JsonConvert.SerializeObject(serializableVector3List, Formatting.Indented) ?? throw new ArgumentNullException("JsonConvert.SerializeObject(serializableList, Formatting.Indented)");

            //return JsonSerializer.Serialize(points).ToString();

            //return jsonString;

            return JsonSerializer.Serialize(points, jsonSerializerOption);
        }
    }

    public string DrawBendJSONPoints(Vector3 X, Vector3 p1, Vector3 p2, float w1, float w2, Vector3 f1,
        Vector3 f2, float h1, float h2, int step)
    {
        // this function will return json list of vertices that defines the plane of bend 
        List<List<Vector3>> PointList = new();
        // curve of a bend with starting axis point P1, end axis point P2, intersection of axis X
        // width w1 and w2 respectively, height h1 and h2 respectively
        // bending radious is not required
        // Point A is starting bend point, i.e., P1 -w1/2
        // Point B is starting bend point, i.e., P1 +w1/2
        // Similarly C and D are the end point corresponding to P2 : P2-w2/2, P2+w2/2
        //
        // check if X coincides with either P1 or P2, then make X = 1/2(P1+P2). otherwise, the further codes will not work (March 2024)

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };


        if (Vector3.Distance(X, p1) < 0.001 || Vector3.Distance(X, p2) < 0.001) X = p1 + p2 / 2;
        var W1 = Vector3.Normalize(Vector3.Cross(p1 - X, f1));
        var W2 = Vector3.Normalize(Vector3.Cross(p2 - X, f2));
        var bendType = "";
        if (Vector3.Normalize(f1) != Vector3.Normalize(f2) && Vector3.Normalize(f1) != -Vector3.Normalize(f2))
        {
            // vertical bend
            bendType = "V";
            W2 = W1;
        }
        else
        {
            // horizontal bend
            if (Vector3.Dot(W1, p2 - X) < 0) W1 = -W1;
            if (Vector3.Dot(W2, p1 - X) < 0) W2 = -W2;
        }

        //
        var A = p1 - w1 / 2 * W1;
        var B = p1 + w1 / 2 * W1;
        var C = p2 - w2 / 2 * W2;
        var D = p2 + w2 / 2 * W2;
        //
        // point AT & BT are the start point of t step, similarly CT & DT are the end point of t step
        // AT = A at t = 0; BT = B at t = step-1
        // ATH is the corresponding top point on the bend, similarly BTH, CTH & DTH
        // three rectangles (six planes) to be drawn for each step t
        // rectangle # 1:  AT-ATH-BTH-BT, #2:  AT-CT-DT-BT, #3: CT-CTH-DTH-DT
        Vector3 AT = new(A.X, A.Y, A.Z); // starting points of the iteration
        Vector3 BT = new(B.X, B.Y, B.Z);
        var ATH = A + f1 * h1; // starting points of the iteration
        var BTH = B + f1 * h1; // starting points of the iteration
        //
        var angle = Math.PI - AngleBetween(p1 - X, p2 - X);
        if (Math.Abs(angle) < 0.05)
        {
            // P1, X and P2 are almost in a straight line, so this bend is as good as straight ladder
            var A1 = A + f1 * h1;
            var B1 = B + f1 * h1;
            var C1 = C + f2 * h2;
            var D1 = D + f2 * h2;
            var points = new List<Vector3> { A, C1, A1, A, C1, C, A, D, C, A, D, B, B, D1, B1, B, D1, D };
            PointList.Add(
                JsonSerializer.Deserialize<List<Vector3>>(JsonSerializer.Serialize(points, jsonSerializerOption),
                    jsonSerializerOption));
            return JsonSerializer.Serialize(PointList);
        }

        //Vector3 XXAC = bendType == "V" ? X - ((w1 + w2) / 4) * W1 : X - (w1 / 2) * W1 - (w2 / 2) * W2;
        var XXAC = bendType == "V"
            ? X - (w1 + w2) / 4 * W1
            : X - (Vector3.Normalize(p2 - X) * (w1 / 2) + Vector3.Normalize(p1 - X) * (w2 / 2)) /
            (float)Math.Sin(angle);
        //Vector3 XXBD = bendType == "V" ? X + ((w1 + w2) / 4) * W1 : X + (w1 / 2) * W1 + (w2 / 2) * W2;
        var XXBD = bendType == "V"
            ? X + (w1 + w2) / 4 * W1
            : X + (Vector3.Normalize(p2 - X) * (w1 / 2) + Vector3.Normalize(p1 - X) * (w2 / 2)) /
            (float)Math.Sin(angle);

        Vector3 XXACH = new(), XXBDH = new(), AH = new(), BH = new(), CH = new(), DH = new();
        Vector3 CT = new(), DT = new(), ht = new(), CTH = new(), DTH = new();
        //if (bendType == "V")
        {
            // for vertical bend
            XXACH = XXAC + f1 * h1 + f2 * h2;
            XXBDH = XXBD + f1 * h1 + f2 * h2;
            AH = A + f1 * h1;
            BH = B + f1 * h1;
            CH = C + f2 * h2;
            DH = D + f2 * h2;
        }

        // for horizontal bend
        for (var i = 0; i < step; i++)
        {
            var t = (float)(i + 1) / step;
            //
            CT = GetPointQuadraticBezier(t, A, XXAC, C);
            DT = GetPointQuadraticBezier(t, B, XXBD, D);
            //
            if (bendType == "V")
            {
                CTH = GetPointQuadraticBezier(t, AH, XXACH, CH);
                DTH = GetPointQuadraticBezier(t, BH, XXBDH, DH);
            }
            else
            {
                // for horizontal bend
                ht = (1 - t) * h1 * f1 + t * h2 * f2;
                CTH = CT + ht;
                DTH = DT + ht;
            }

            //
            var points = new List<Vector3>
                { AT, CTH, ATH, AT, CTH, CT, AT, DT, CT, AT, DT, BT, BT, DTH, BTH, BT, DTH, DT };
            PointList.Add(
                JsonSerializer.Deserialize<List<Vector3>>(JsonSerializer.Serialize(points, jsonSerializerOption),
                    jsonSerializerOption));
            //
            // setting value for the next iteration
            //AT = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(CT));
            AT = new Vector3(CT.X, CT.Y, CT.Z);
            //BT = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(DT));
            BT = new Vector3(DT.X, DT.Y, DT.Z);
            //ATH = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(CTH));
            ATH = new Vector3(CTH.X, CTH.Y, CTH.Z);
            //BTH = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(DTH));
            BTH = new Vector3(DTH.X, DTH.Y, DTH.Z);
        }
        return JsonSerializer.Serialize(PointList, jsonSerializerOption);
    }

    //
    public string DrawTeeJSONPoints(Vector3 X, Vector3 P1, Vector3 P2, Vector3 P3, float w1, float w2, float w3,
        Vector3 f1, Vector3 f2, Vector3 f3, float h1, float h2, float h3, int step)
    {
        // this function will return json list of vertices that defines the plane of bend 
        // this includes bend part of 1-2, 2-3 and 3-1 (one of them could be straight line)
        // this also include three triangles towards P1, P2 and P3

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };


        List<List<Vector3>> PointList = new();
        //
        // for bend part 1 (1-2)  (X, P1, P2, w1, w2, f1, f2, h1, h2)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P1, P2, P3, w1, w2, f1, f2, h1, h2, step), jsonSerializerOption),
            jsonSerializerOption));
        // for bend part 2 (2-3) (X, P2, P3, w2, w3, f2, f3, h2, h3)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P2, P3, P1, w2, w3, f2, f3, h2, h3, step), jsonSerializerOption),
            jsonSerializerOption));
        // for bend part 3 (3-1) (X, P3, P1, w3, w1, f3, f1, h3, h1)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P3, P1, P2, w3, w1, f3, f1, h3, h1, step), jsonSerializerOption),
            jsonSerializerOption));
        //
        // for triangle part 1 (X, P1-w/2, P1+w/2)
        var W1 = Vector3.Normalize(Vector3.Cross(P1 - X, f1));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P1 - w1 / 2 * W1, P1 + w1 / 2 * W1 }, jsonSerializerOption),
            jsonSerializerOption));
        // for triangle part 2 (X, P2-w/2, P2+w/2)
        var W2 = Vector3.Normalize(Vector3.Cross(P2 - X, f2));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P2 - w2 / 2 * W2, P2 + w2 / 2 * W2 }, jsonSerializerOption),
            jsonSerializerOption));
        // for triangle part 3 (X, P3-w/2, P3+w/2)
        var W3 = Vector3.Normalize(Vector3.Cross(P3 - X, f3));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P3 - w3 / 2 * W3, P3 + w3 / 2 * W3 }, jsonSerializerOption),
            jsonSerializerOption));
        //
        return JsonSerializer.Serialize(PointList, jsonSerializerOption);
    }


    public string DrawCrossJSONPoints(Vector3 X, Vector3 P1, Vector3 P2, Vector3 P3, Vector3 P4, float w1,
        float w2, float w3, float w4, Vector3 f1, Vector3 f2, Vector3 f3, Vector3 f4, float h1, float h2, float h3,
        float h4, int step)
    {
        // 1(i)-2(j) opposite, 3(k)-4(l) opposite
        // this function will return json list of vertices that defines the plane of cross 
        // this includes bend part of 1-3, 1-4 and 2-3, 2-4 
        // this also include four triangles towards P1, P2, P3 and P4

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        List<List<Vector3>> PointList = new();
        //
        // for bend part 1 : 1-3 : (X, P1, P3, w1, w3, f1, f3, h1, h3)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P1, P3, P4, w1, w3, f1, f3, h1, h3, step), jsonSerializerOption),
            jsonSerializerOption));
        // for bend part 2 : 1-4 : (X, P1, P4, w1, w4, f1, f4, h1, h4)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P1, P4, P3, w1, w4, f1, f4, h1, h4, step), jsonSerializerOption),
            jsonSerializerOption));
        // for bend part 3 : 2-3 : (X, P2, P3, w2, w3, f2, f3, h2, h3)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P2, P3, P4, w2, w3, f2, f3, h2, h3, step), jsonSerializerOption),
            jsonSerializerOption));
        // for bend part 4 : 2-4 : (X, P2, P4, w2, w4, f2, f4, h2, h4)
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(GetBendPoints(X, P2, P4, P3, w2, w4, f2, f4, h2, h4, step), jsonSerializerOption),
            jsonSerializerOption));
        //
        // for triangle part 1 (X, P1-w/2, P1+w/2)
        var W1 = Vector3.Normalize(Vector3.Cross(P1 - X, f1));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P1 - w1 / 2 * W1, P1 + w1 / 2 * W1 }, jsonSerializerOption),
            jsonSerializerOption));
        // for triangle part 2 (X, P2-w/2, P2+w/2)
        var W2 = Vector3.Normalize(Vector3.Cross(P2 - X, f2));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P2 - w2 / 2 * W2, P2 + w2 / 2 * W2 }, jsonSerializerOption),
            jsonSerializerOption));
        // for triangle part 3 (X, P3-w/2, P3+w/2)
        var W3 = Vector3.Normalize(Vector3.Cross(P3 - X, f3));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P3 - w3 / 2 * W3, P3 + w3 / 2 * W3 }, jsonSerializerOption),
            jsonSerializerOption));
        // for triangle part 4 (X, P4-w/2, P3+w/2)
        var W4 = Vector3.Normalize(Vector3.Cross(P4 - X, f4));
        PointList.Add(JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(new List<Vector3> { X, P4 - w4 / 2 * W4, P4 + w4 / 2 * W4 }, jsonSerializerOption),
            jsonSerializerOption));
        //
        return JsonSerializer.Serialize(PointList);
    }

    //
    //
    public List<Vector3> GetBendPoints(Vector3 X, Vector3 P1, Vector3 P2, Vector3 P_opposite, float w1, float w2,
        Vector3 f1, Vector3 f2, float h1, float h2, int step)
    {
        // this function will return json list of vertices that defines the plane of bend and 
        List<Vector3> PointList = new();
        //curve of a bend with starting axis point P1, end axis point P2, intersection of axis X
        // width w1 and w2 respectively, height h1 and h2 respectively
        // bending radious is not required
        // Point A is starting bend point, i.e., P1 -w1/2
        // Similarly B is end point corresponding to P2 : P2-w2/2

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        var colinear = IsColinear(P1, X, X, P2);
        var W1 = Vector3.Normalize(Vector3.Cross(P1 - X, f1));
        if (Vector3.Dot(W1, P2 - X) < 0) W1 = -W1;
        var W2 = Vector3.Normalize(Vector3.Cross(P2 - X, f2));
        if (Vector3.Dot(W2, P1 - X) < 0) W2 = -W2;
        if (colinear) W2 = W1;
        var angle = AngleBetween(W1, W2);
        var A = P1 + w1 / 2 * W1;
        var B = P2 + w2 / 2 * W2;
        //Vector3 XX = X + (w1 / 2) * W1 + (w2 / 2) * W2;
        var XX = Math.Abs(Math.Sin(angle)) < 0.05
            ? X
            : X + Vector3.Normalize(P2 - X) * (w1 / 2) / (float)Math.Sin(angle) +
              Vector3.Normalize(P1 - X) * (w2 / 2) / (float)Math.Sin(angle);
        // point AT is the start point of t step, similarly BT is the end point of t step
        // AT = A at t = 0; BT = B at t = step-1
        // ATH is the corresponding top point on the bend, similarly BTH
        // three planes to be drawn for each step t
        // plane # 1: traiagle X-AT-BT, #2: triangle AT-ATH-BT, #3: ATH-BTH-BT
        //
        if (colinear)
        {
            if (Vector3.Dot(W1, P_opposite - X) > 0) W1 = W2 = -W1;
            A = P1 + w1 / 2 * W1;
            B = P2 + w2 / 2 * W2;
            // there is no curve, its the straight side of the bend
            var AH = A + h1 * f1;
            var BH = B + h2 * f2;
            var points = new List<Vector3> { X, A, B, A, AH, B, AH, BH, B };
            PointList.AddRange(
                JsonSerializer.Deserialize<List<Vector3>>(JsonSerializer.Serialize(points, jsonSerializerOption),
                    jsonSerializerOption));
        }
        else
        {
            Vector3 AT = new(A.X, A.Y, A.Z);
            for (var i = 0; i < step; i++)
            {
                var ATH = AT + h1 * f1;
                var t = (float)(i + 1) / step;
                var BT = GetPointQuadraticBezier(t, A, XX, B);
                var BTH = BT + h2 * f2;
                var points = new List<Vector3> { X, AT, BT, AT, ATH, BT, ATH, BTH, BT };
                PointList.AddRange(JsonSerializer.Deserialize<List<Vector3>>(
                    JsonSerializer.Serialize(points, jsonSerializerOption), jsonSerializerOption));
                //AT = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(BT));
                AT = new Vector3(BT.X, AT.Y, AT.Z);
            }
        }

        return PointList;
    }

    //
    public Vector3 GetPointQuadraticBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        //https://www.codeproject.com/Articles/25237/Bezier-Curves-Made-Simple
        // p0 start point, p1 is the control point or the intersection point, or mid point, p2 is the last point
        //Vector3 a = (float)(1 - t )* (1-t) * p0;
        //Vector3 b = (float)(2 * t * (1 - t)) * p1;
        //Vector3 c = (float)(t * t )* p2;
        return (1 - t) * (1 - t) * p0 + 2 * t * (1 - t) * p1 + t * t * p2;
    }


    /// <summary> Generate additional points for the bends based on the bending radious </summary>
    public string DrawSleeveJSONPoints(List<Vector3> points, double bendingRadius)
    {
        List<Vector3> pointsWithBends = new();

        var jsonSerializerOption = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        // finding the bend points
        if (points.Count == 2)
        {
            pointsWithBends.Add(new Vector3(points[0].X, points[0].Y, points[0].Z));
            pointsWithBends.Add(new Vector3(points[1].X, points[1].Y, points[1].Z));
        }
        else
        {
            for (var i = 0; i < points.Count - 2; i++)
            {
                var p0 = new Vector3(points[i].X, points[i].Y, points[i].Z);
                var p1 = new Vector3(points[i + 1].X, points[i + 1].Y, points[i + 1].Z);
                var p2 = new Vector3(points[i + 2].X, points[i + 2].Y, points[i + 2].Z);
                var angleInRadians = Math.Acos(Vector3.Dot(Vector3.Normalize(p0 - p1), Vector3.Normalize(p1 - p2)));


                if (angleInRadians > 0.01)
                {
                    var r = (float)bendingRadius;
                    // 1st bend point
                    var p01 = p1 + Vector3.Normalize(p0 - p1) * r;
                    // 2nd bend point
                    var p12 = p1 + Vector3.Normalize(p2 - p1) * r;
                    // mid point of bend
                    var p11 = (p01 + p12) / 2 + (p1 - (p01 + p12) / 2) * (float)((1 - Math.Pow(2, -0.5)) * r);
                    pointsWithBends.Add(p0);
                    pointsWithBends.Add(p01);
                    pointsWithBends.Add(p11);
                    points[i + 1] = p12;
                }
                else
                {
                    pointsWithBends.Add(p0);
                }

                if (i == points.Count - 3) // for the last check, add remaining last two points
                {
                    pointsWithBends.Add(points[i + 1]);
                    pointsWithBends.Add(points[i + 2]);
                }
            }
        }


        // Convert Unity Vector3 to SerializableVector3
        //var serializableList = pointsWithBends.Select(vector => SerializableVector3.FromVector3(vector)).ToList();

        // Serialize to JSON
        //var jsonPoints = JsonConvert.SerializeObject(serializableList, Formatting.Indented) ?? throw new ArgumentNullException("JsonConvert.SerializeObject(serializableList, Formatting.Indented)");

        // Deserialize back to list of SerializableVector3
        //var deserializedList = JsonConvert.DeserializeObject<List<SerializableVector3>>(jsonPoints);

        // Convert SerializableVector3 back to Unity Vector3
        //var deserializedVector3List = deserializedList.Select(serializableVector => serializableVector.ToVector3()).ToList();
        var jsonPoints = JsonSerializer.Serialize(pointsWithBends, jsonSerializerOption);
        return jsonPoints;
    }


    public string DrawCableJSONPoints(Cable cable, List<Segment> Segment)
    {
        var PointSetSegments = new List<Vector3>();
        List<Guid> SegGuid = new();
        cable.RouteVectorAuto = JsonSerializer.Deserialize<List<Vector3>>(cable.RouteVectorAutoS);

        cable.RouteVectorAuto.ForEach(p =>
        {
            var segs = Segment.Where(seg => PointInsideSegment(p, seg).Item1).ToList();
            if (segs.Count > 0)
                SegGuid.Add(segs[0].UID);
            else
                SegGuid.Add(new Guid());
        });
        //
        if (cable.RouteVectorAuto.Count < 4 && cable.RouteVectorAuto.Count > 1)
        {
            Vector3 v = new();
            if (cable.RouteVectorAuto.Count == 3)
                v = cable.RouteVectorAuto[1];
            else
                v = (cable.RouteVectorAuto[0] + cable.RouteVectorAuto[^1]) / 2;
            PointSetSegments.Add(cable.RouteVectorAuto[0]);
            PointSetSegments.Add(Vector3.Normalize(v - cable.RouteVectorAuto[0]));
            PointSetSegments.Add(Vector3.Normalize(v - cable.RouteVectorAuto[^1]));
            PointSetSegments.Add(cable.RouteVectorAuto[^1]);
        }
        else if (cable.RouteVectorAuto.Count >= 4)
        {
            for (var i = 0; i < cable.RouteVectorAuto.Count - 1; i++)
            {
                // for stretch i and i+1

                var v1 = new Vector3(0, 0, 0); // tension vector, default 0 for straight segment
                var v2 = new Vector3(0, 0, 0); // tension vector, default 0 for straight segment
                //
                // either this cable stretch laid in the same segment -- straight line
                // or between two different segments -- bend
                // 1st and last stretch is always straight
                var bendOrStraight = i == 0 || i == cable.RouteVectorAuto.Count - 2 ? "straight" :
                    SegGuid[i] == SegGuid[i + 1] ? "straight" : "bend";
                // default case
                // either the 1st section or the last section


                if (bendOrStraight == "bend")
                {
                    // case of bend
                    var v0 = Vector3.Normalize(cable.RouteVectorAuto[i] - cable.RouteVectorAuto[i - 1]);
                    var v3 = Vector3.Normalize(cable.RouteVectorAuto[i + 2] - cable.RouteVectorAuto[i + 1]);
                    var v12 = cable.RouteVectorAuto[i + 1] - cable.RouteVectorAuto[i];
                    var tension1 = Vector3.Dot(v0, v12);
                    var tension2 = Vector3.Dot(v3, v12);
                    v1 = cable.RouteVectorAuto[i] +
                         Vector3.Normalize(cable.RouteVectorAuto[i] - cable.RouteVectorAuto[i - 1]) * tension1 / 2;
                    v2 = cable.RouteVectorAuto[i + 1] +
                         Vector3.Normalize(cable.RouteVectorAuto[i + 1] - cable.RouteVectorAuto[i + 2]) * tension2 / 2;
                }

                PointSetSegments.Add(cable.RouteVectorAuto[i]);
                PointSetSegments.Add(v1);
                PointSetSegments.Add(v2);
                PointSetSegments.Add(cable.RouteVectorAuto[i + 1]);
            }
        }

        //
        return JsonSerializer.Serialize(PointSetSegments);
    }


    public Tuple<String, String, float, String, String> DrawCableJSONPointsArranged(Cable cable,
        List<Segment> segments)
    {
        // this function returns cable path JSON string from Arranged Vector Points
        // bends are based on the segment information
        List<Vector3> PointSetSegments = [];
        List<Vector3> Points = [];
        List<Guid> SegGuid = new();
        List<string> RouteList = new();
        List<string> RouteWayList = new();
        List<string> RouteType = new();
        var length = 0f;

        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };


        var tempRouteVectorAuto =
            JsonSerializer.Deserialize<List<Vector3>>(cable.RouteVectorAutoArrangedS, jsonSerializerOption);
        // remove duplicate points in RouteVectorAuto, if any
        cable.RouteVectorAuto = tempRouteVectorAuto.Distinct().ToList();
        //

        try
        {
            cable.RouteVectorAuto.ForEach(p =>
            {
                var segs = segments.Where(seg => PointInsideSegment(p, seg).Item1).ToList();
                var sleeves = _globalData.Sleeves.Where(slv => Vector3.Distance(slv.End1, p) < slv.Dia / 2).ToList();
                if (segs.Count > 0)
                {
                    // this Route Vector is part of this Segment (Ladder)
                    RouteType.Add("Segment");
                    SegGuid.Add(segs[0].UID);
                    var rawSegs = _globalData.RawSegments.Where(seg => seg.UID == segs[0].ParentUID).ToList();

                    if (rawSegs.Count > 0)
                    {
                        RouteWayList.Add(rawSegs[0].CableWay);
                        RouteList.Add(segs[0].Tag);
                    }
                }
                else if (sleeves.Count > 0)
                {
                    // this Route Vector is part of this Sleeve
                    SegGuid.Add(sleeves[0].UID);
                    RouteType.Add("Sleeve");
                    RouteWayList.Add(sleeves[0].Tag);
                    RouteList.Add(sleeves[0].Tag);
                }
                else
                {
                    RouteType.Add("Other");
                    SegGuid.Add(new Guid());
                }
            });

            Points.Add(cable.DestinationV);
            //
            if (cable.RouteVectorAuto.Count == 1)
            {
                Points.Add(cable.RouteVectorAuto[0]);
            }
            else if (cable.RouteVectorAuto.Count < 4 && cable.RouteVectorAuto.Count > 1)
            {
                Vector3 v = new();
                if (cable.RouteVectorAuto.Count == 3)
                    v = cable.RouteVectorAuto[1];
                else
                    v = (cable.RouteVectorAuto[0] + cable.RouteVectorAuto[^1]) / 2;
                PointSetSegments.Add(cable.RouteVectorAuto[0]);
                PointSetSegments.Add(Vector3.Normalize(v - cable.RouteVectorAuto[0]));
                PointSetSegments.Add(Vector3.Normalize(v - cable.RouteVectorAuto[^1]));
                PointSetSegments.Add(cable.RouteVectorAuto[^1]);
                //
                Points.Add(cable.RouteVectorAuto[0]);
                Points.Add(cable.RouteVectorAuto[1]);
                if (cable.RouteVectorAuto.Count > 2) Points.Add(cable.RouteVectorAuto[2]);
            }
            else if (cable.RouteVectorAuto.Count >= 4)
            {
                for (var i = 0; i < cable.RouteVectorAuto.Count - 1; i++)
                {
                    // for stretch i and i+1
                    // tension vector, default 0 for straight segment
                    var v1 = new Vector3(0, 0, 0);
                    var v2 = new Vector3(0, 0, 0);
                    //
                    // either this cable stretch is laid in the same segment -- straight line
                    // or between two different segments -- bend
                    // 1st and last stretch is always straight
                    //var bendOrStraight = (i == 0 || i == cable.RouteVectorAuto.Count - 2) ? "straight" : SegGuid[i] == SegGuid[i + 1] ? "straight" : "bend";
                    var bendOrStraight = "straight";
                    if (i > 0 && i < cable.RouteVectorAuto.Count - 2)
                        if (SegGuid[i - 1] == SegGuid[i] && SegGuid[i + 1] == SegGuid[i + 2])
                            bendOrStraight = "bend";

                    // check if this section passes through sleeve
                    if (i > 0 && i < cable.RouteVectorAuto.Count - 2)
                        if (RouteType[i] == "Sleeve" && RouteType[i + 1] == "Sleeve")
                            bendOrStraight = "sleeve";
                    // default case
                    // either the 1st section or the last section


                    if (bendOrStraight == "bend")
                    {
                        // case of bend
                        var v0 = Vector3.Normalize(cable.RouteVectorAuto[i] - cable.RouteVectorAuto[i - 1]);
                        var v3 = Vector3.Normalize(cable.RouteVectorAuto[i + 2] - cable.RouteVectorAuto[i + 1]);
                        var v12 = cable.RouteVectorAuto[i + 1] - cable.RouteVectorAuto[i];
                        var tension1 = Vector3.Dot(v0, v12);
                        var tension2 = Vector3.Dot(v3, v12);
                        v1 = cable.RouteVectorAuto[i] +
                             Vector3.Normalize(cable.RouteVectorAuto[i] - cable.RouteVectorAuto[i - 1]) * tension1 / 2;
                        v2 = cable.RouteVectorAuto[i + 1] +
                             Vector3.Normalize(cable.RouteVectorAuto[i + 1] - cable.RouteVectorAuto[i + 2]) * tension2 /
                             2;
                        // cubic bezier curve points along the bend of step t 
                        for (var step = 0; step < _globalData.CurveStep; step++)
                        {
                            var t = (float)step / _globalData.CurveStep;
                            var pt = (1 - t) * (1 - t) * (1 - t) * cable.RouteVectorAuto[i] +
                                     3 * t * (1 - t) * (1 - t) * v1 + 3 * t * t * (1 - t) * v2 +
                                     t * t * t * cable.RouteVectorAuto[i + 1];
                            Points.Add(new Vector3(MathF.Round(pt.X, 3), MathF.Round(pt.Y, 3), MathF.Round(pt.Z, 3)));
                        }
                    }
                    else if (bendOrStraight == "sleeve")
                    {
                        var sleeves = _globalData.Sleeves.Where(slv => slv.UID == SegGuid[i]).ToList();
                        var sleeve = sleeves[0];

                        sleeve.Routes.ForEach(p => { Points.Add(p); });
                    }
                    else
                    {
                        Points.Add(cable.RouteVectorAuto[i]);
                    }

                    PointSetSegments.Add(cable.RouteVectorAuto[i]);
                    PointSetSegments.Add(v1);
                    PointSetSegments.Add(v2);
                    PointSetSegments.Add(cable.RouteVectorAuto[i + 1]);
                    //
                }
            }

            Points.Add(cable.OriginV);
            //
            // determine length

            for (var i = 0; i < Points.Count - 1; i++) length += Vector3.Distance(Points[i + 1], Points[i]);
            //
            // removing repeat RouteList
            for (var i = RouteList.Count - 1; i >= 1; i--)
                if (RouteList[i] == RouteList[i - 1])
                    RouteList.RemoveAt(i);

            for (var i = RouteWayList.Count - 1; i >= 1; i--)
                if (RouteWayList[i] == RouteWayList[i - 1])
                    RouteWayList.RemoveAt(i);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{e.Message}");
        }

        //


        return new Tuple<string, string, float, string, string>(
            JsonSerializer.Serialize(PointSetSegments, jsonSerializerOption),
            JsonSerializer.Serialize(Points, jsonSerializerOption), length, string.Join(", ", RouteWayList),
            string.Join(", ", RouteList));
    }

    public Tuple<List<Node>, List<Cable>> UpdateNodeCable(List<Cable> cables, List<Cable> Cable,
        List<Segment> segments, List<Sleeve> Sleeve, List<Node> nodes)
    {
        // this function is only for set of cables (typically 5), and not for entire Cable - Mar 2024
        // this function checks vector data of a laid cable and update the nodetag data in the same cable based on node and segment
        // this function also returns the node data with laid cable information
        // updation of node data in the laid cable is only for calculation purpose or for arranged vector in arranged cable
        // it is assumed that node and segment data are always updated and saved in the DB
        // i.e., node and segment information are expected to be in consistent to each other. i.e., segment info in Node and node info in Segment must match
        // in case some of the vectors of laid cable not matching with the exiting node (may be outside the segment) or in dummy nodes
        // create dummy nodes for calculation purpose
        // this function takes input of existing node, segment and laid cable vector information
        // and returns filled cable with node info and node data with additional nodes, if any, and filled cable information
        // this function to be called for aranging cable based on laid cables.
        // node data fills only laid cable information
        // 

        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        var CableList = JsonSerializer.Deserialize<List<Cable>>(JsonSerializer.Serialize(Cable, jsonSerializerOption),
            jsonSerializerOption);
        CableList.ForEach(item => { item.Update(); });
        var SegmentList =
            JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(segments, jsonSerializerOption),
                jsonSerializerOption);
        var SleeveList =
            JsonSerializer.Deserialize<List<Sleeve>>(JsonSerializer.Serialize(Sleeve, jsonSerializerOption),
                jsonSerializerOption);
        var NodeList = JsonSerializer.Deserialize<List<Node>>(JsonSerializer.Serialize(nodes, jsonSerializerOption),
            jsonSerializerOption);
        List<Vector3> PointSetSegments = [];
        List<Guid> SegGuid = new();
        // create fresh nodelist as per segments
        // Nov 2023: fresh nodes are not created due to the reason below
        //Segment.ForEach(seg =>
        //{
        //    var n1tag = "N" + NodeList.Count.ToString("D6");
        //    var n2tag = "N" + (NodeList.Count + 1).ToString("D6");
        //    NodeList.Add(new Data.Node(n1tag, seg.End1, seg.Width, seg.Face, seg.UID, 1, new List<string> { n2tag }, ""));
        //    NodeList.Add(new Data.Node(n2tag, seg.End1, seg.Width, seg.Face, seg.UID, 2, new List<string> { n1tag }, ""));
        //    var nodeLayDirection = Vector3.Normalize(Vector3.Cross(seg.End2 - seg.End1, seg.Face));
        //    NodeList[^2].LayDirection = JsonConvert.DeserializeObject<Vector3>(JsonConvert.SerializeObject(nodeLayDirection));
        //    NodeList[^1].LayDirection = JsonConvert.DeserializeObject<Vector3>(JsonConvert.SerializeObject(nodeLayDirection));
        //    seg.Node1 = n1tag;
        //    seg.Node2 = n2tag;
        //});
        //
        //CableList = CableList.Where(c => c.RouteVectorAuto.Count >0).ToList();
        CableList.ForEach(cable =>
        {
            if (cable.RouteVectorAuto.Count == 0 || !cables.Any(c => c.Tag == cable.Tag)) return;
            cable.SeqTag = "C" + CableList.IndexOf(cable).ToString("D6");
            cable.RouteVectorAuto =
                JsonSerializer.Deserialize<List<Vector3>>(cable.RouteVectorAutoS, jsonSerializerOption);
            cable.AutoRouteNodeTags.Clear();
            cable.RouteVectorAuto.ForEach(p =>
            {
                // skip the first and last nodes as they may be common for multiple cables routing and therefore the tree-branch logic may fail due to rejoined cables
                //if (cable.RouteVectorAuto.IndexOf(p) == 0 || cable.RouteVectorAuto.IndexOf(p) == cable.RouteVectorAuto.Count - 1) return;
                var segs = SegmentList.Where(seg => PointInsideSegment(p, seg).Item1).ToList();
                
                if (segs.Count > 0)
                {
                    cable.RouteSegmentUIDAuto.Add(segs[0].UID);
                    var side = PointInsideSegment(p, segs[0]).Item2;
                    if (side == 1)
                    {
                        cable.AutoRouteNodeTags.Add(segs[0].Node1);
                    }
                    else if (side == 2)
                    {
                        cable.AutoRouteNodeTags.Add(segs[0].Node2);
                    }
                    else
                    {
                        // side must be = 9, i.e., either the jump node or a landing point from source/destination
                        // if jump node then no need to create a new node
                        var JumpNodes = NodeList.Where(n => n.SegmentUID == segs[0].UID && n.SegmentEnd == 9)
                            .ToList();
                        if (JumpNodes.Count > 0)
                        {
                            // jump node
                            //order by the nearest jump node just in case there are multiple jump nodes
                            JumpNodes.OrderBy(o => Vector3.Distance(o.Point, p)).ToList();
                            cable.AutoRouteNodeTags.Add(JumpNodes[0].Tag);
                        }
                        else
                        {
                            // this is landing cable from source/destination to this segment and thus in between segment ends but not on any existing jump nodes
                            // so new node to be created
                            var ntag = "N" + NodeList.Count.ToString("D6");
                            var lndingCount = NodeList.Where(n => n.SegmentUID == segs[0].UID).ToList().Count;
                            NodeList.Add(new Node(ntag, p, segs[0].Width, segs[0].Face, "Segment", segs[0].UID,
                                10 + lndingCount, new List<string> { segs[0].Node1, segs[0].Node2 }));
                            cable.AutoRouteNodeTags.Add(ntag);
                        }
                    }
                }
                else
                {
                    // check if cable route point p is coinciding with any nodes from sleeves
                    var sleeves = SleeveList.Where(sleeve =>
                        Vector3.Distance(p, sleeve.Points[0]) < sleeve.Dia / 2 ||
                        Vector3.Distance(p, sleeve.Points[^1]) < sleeve.Dia / 2).ToList();
                    if (sleeves.Count > 0)
                    {
                        if (Vector3.Distance(p, sleeves[0].Points[0]) < sleeves[0].Dia / 2)
                            cable.AutoRouteNodeTags.Add(sleeves[0].Node1);
                        else
                            cable.AutoRouteNodeTags.Add(sleeves[0].Node2);
                    }
                    else
                    {
                        // new/unknown segment and node
                        cable.RouteSegmentUIDAuto.Add(new Guid());
                        var ntag = "N" + NodeList.Count.ToString("D6");
                        NodeList.Add(new Node(ntag, p, cable.ODm, new Vector3(), "Segment", new Guid(), 0,
                            new List<string>()));
                        cable.AutoRouteNodeTags.Add(ntag);
                    }
                }
            });
        });
        // node connections to be created
        // to check later as the node connections are already done
        if (false)
        {
            CableList.ForEach(cable =>
            {
                if (cable.RouteVectorAuto.Count == 0) return;
                for (var i = 0; i < cable.AutoRouteNodeTags.Count - 1; i++)
                {
                    var nodei = NodeList.Where(n => n.Tag == cable.AutoRouteNodeTags[i]).ToList();
                    var nodei1 = NodeList.Where(n => n.Tag == cable.AutoRouteNodeTags[i + 1]).ToList();
                    if (nodei.Count > 0 && nodei1.Count > 0)
                        if (nodei[0].SegmentUID != nodei1[0].SegmentUID)
                        {
                            if (!nodei[0].ConnectedNodesTag.Contains(nodei1[0].Tag))
                                nodei[0].ConnectedNodesTag.Add(nodei1[0].Tag);

                            if (!nodei1[0].ConnectedNodesTag.Contains(nodei[0].Tag))
                                nodei1[0].ConnectedNodesTag.Add(nodei[0].Tag);
                        }
                }
            });
            //
            NodeList.ForEach(n =>
            {
                n.ConnectedNodesTagS = JsonSerializer.Serialize(n.ConnectedNodesTag, jsonSerializerOption);
            });
        }

        //
        // NodeList, SegmentList is created as per laid cable vector points and available Segment Data
        //
        //create Arranged vector only for newly routed (laid) cables
        CableList.ForEach(cable =>
        {
            if (cable.RouteVectorAuto.Count == 0) return;
            cable.RouteVectorAutoArranged =
                JsonSerializer.Deserialize<List<Vector3>>(
                    JsonSerializer.Serialize(cable.RouteVectorAuto, jsonSerializerOption), jsonSerializerOption);
        });
        // arranging newly routed cables in each of the nodes (general arrangement without any logic)
        NodeList.ForEach(node =>
        {
            var totalLaidCable = CableList.Where(cable => cable.AutoRouteNodeTags.Contains(node.Tag)).ToList();
            // initialisation of Arranged Cable Position in this node
            totalLaidCable.ForEach(item => { node.ArrangedcablePosition.Add(node.Point); });
            // arrange cables as per the existng sequence (Tree Branch method not applied yet)
            var stratEdge = node.Point - (float)(node.Width / 2 - node.MarginSide1) * node.LayDirection;
            for (var i = 0; i < totalLaidCable.Count; i++)
            {
                // c1 is i-1 th cable, c2 is ith cable
                var c2 = totalLaidCable[i];
                var cableSequenceInThisNode = totalLaidCable[i].AutoRouteNodeTags.IndexOf(node.Tag);
                Vector3 p2 = new();
                var gap = 0f;
                if (i == 0)
                {
                    p2 = stratEdge + c2.ODm / 2 * node.LayDirection + c2.ODm / 2 * node.Face;
                }
                else
                {
                    // p1, p2 are arranged position of c1 and c2 cable respectively (stored in node ArrangedcablePosition) 
                    var c1 = totalLaidCable[i - 1];
                    var p1 = node.ArrangedcablePosition[i - 1];
                    gap = SpacingBeteenCables(c1, c2, _globalData.Spacings);
                    p2 = p1 + (c1.ODm / 2 + gap + c2.ODm / 2) * node.LayDirection +
                         (c2.ODm / 2 - c1.ODm / 2) * node.Face;
                }

                node.ArrangedcablePosition[i] =
                    JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(p2, jsonSerializerOption),
                        jsonSerializerOption);
                c2.RouteVectorAutoArranged[cableSequenceInThisNode] =
                    JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(p2, jsonSerializerOption),
                        jsonSerializerOption);
                var pos = Math.Round((p2 - node.Point).Length() / (node.Width / 2) * 100);
            }
        });

        return new Tuple<List<Node>, List<Cable>>(NodeList, CableList);
    }


    public Tuple<bool, int> PointInsideSegment(Vector3 X, Segment seg)
    {
        //https://math.stackexchange.com/questions/1472049/check-if-a-point-is-inside-a-rectangular-shaped-area-3d
        // in this example  Segment End1 is p1 p2 and end2 is p5 p6. p4 is hight for p1 , similarly p3, p8 and p7 is height of p2, p5, p6
        // w the width vector is p1-p2, l the length vector end1-end2, i.e., p1-p5, face is the height vector p1-p4
        //
        var inside = false;
        var side = 0;
        var L = seg.End2 - seg.End1;
        var W = (float)seg.Width;
        var H = (float)seg.Height;
        var l = Vector3.Normalize(L);
        var h = Vector3.Normalize(seg.Face);
        var w = Vector3.Cross(l, h);
        //
        var p1 = seg.End1 - W / 2 * w;
        var p1x = X - p1;
        var lx = Vector3.Dot(l, p1x);
        var wx = Vector3.Dot(w, p1x);
        var hx = Vector3.Dot(h, p1x);
        //
        var err = 0.001;
        if (lx >= -err && lx < L.Length() + err && wx >= -err && wx < W + err && hx >= -err && hx < H + err)
        {
            inside = true;
            if (Math.Abs(lx) < err)
                side = 1;
            else if (Math.Abs(L.Length() - lx) < err)
                side = 2;
            else if (Math.Abs(lx) > err && Math.Abs(L.Length() - lx) > err) side = 9;
            // 9 is the anywhere in between the segment , not edges (jump case)
        }

        return new Tuple<bool, int>(inside, side);
    }



    public Tuple<List<String>, List<Vector3>, String> AStar(
        Cable cable, double od, double marginFor, Vector3 startV, Vector3 goalV, String routeCriteria,
        List<Node> nodeR, List<Segment> segments, List<Cable> cables, int n, List<Spacing> spacing)
    {
        //     A* Algorithm to find the shortest routes: returns array of nodes between start and end coordinates
        //     dependents: segments, nodes, filling info to be built later
        
        
        var startTime = DateTime.Now;
        var cableTag = cable.Tag;
        //adding new node connections for jumping to the nearby nodes of same cable criteria and available space
        //List<Node> NodeR = WithJumpNodes(routeCriteria, thisCable, NodeRR, CableR);

        List<String> routeNodeTag = [];
        List<Vector3> routeVectorAuto = [];
        n = 5;
        //create new set of n nearest nodes near the start point and goal point
        List<Node> nearestNodesStart = [];
        List<Node> nearestNodesGoal = [];
        // list of cable tags that cannot be laid together, e.g., UPS A and UPS B
        var jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };
        var exclusionTags = string.IsNullOrWhiteSpace(cable.ExclusionTags)
            ? []
            : JsonSerializer.Deserialize<List<String>>(cable.ExclusionTags, jsonSerializerOptions);
        // list of cable tags that must be laid together e.g., parallel run cables, trefoil cables
        var associatedTags = string.IsNullOrWhiteSpace(cable.AssociatedTags)
            ? []
            : JsonSerializer.Deserialize<List<String>>(cable.AssociatedTags, jsonSerializerOptions);
        // find exclusion nodes through which this cable cannot be routed
        List<string> exclusionNodes = [];
        if (exclusionTags.Count > 0)
            exclusionTags.ForEach(exclusionTag =>
            {
                if (cables.Where(c => c.Tag == exclusionTag).ToList().Count > 0)
                {
                    var exclusionCable = cables.Where(c => c.Tag == exclusionTag).ToList()[0];
                    if (exclusionCable.AutoRouteNodeTags != null)
                        exclusionCable.AutoRouteNodeTags.ForEach(nodeTag => { exclusionNodes.Add(nodeTag); });
                }
            });
        //
        var whileLoopCount = 0;
        // at outset, add dummy nodes nearer to the start/end point, dummy nodes are on segments within a reasonable reach
        var dummyNodeStartTag = "DummyNodeStart" + cableTag;
        var dummyNodeGoalTag = "DummyNodeGoal" + cableTag;
        // first check if there are nearby sleeve nodes
        // distance range 5m for the near sleeve nodes -- to change later
        const int rangeSleeve = 5; // start/end point to nearest segment range
        var nearStartSleeveNodes = NearestSleeveNodes(startV, rangeSleeve, routeCriteria, od, nodeR);
        var nearGoalSleeveNodes = NearestSleeveNodes(goalV, rangeSleeve, routeCriteria, od, nodeR);
        var rangeSegment = 10; // start/end point to nearest segment range
        // if sleevenodes are available then no need for nearest segments as the cables must go through sleeves
        // for start
        if (nearStartSleeveNodes.Count != 0)
        {
            nearestNodesStart = nearStartSleeveNodes;
        }
        else
        {
            // also check segments 
            var tempnearestNodesStart = NearestDummyNodesOnSegments(startV, rangeSegment, segments, dummyNodeStartTag,
                routeCriteria, od, nodeR);
            nearestNodesStart = tempnearestNodesStart.Concat(nearStartSleeveNodes).ToList();
        }

        // for goal
        if (nearGoalSleeveNodes.Count != 0)
        {
            nearestNodesGoal = nearGoalSleeveNodes;
        }
        else
        {
            // also check segments 
            var tempnearestNodesGoal = NearestDummyNodesOnSegments(goalV, rangeSegment, segments, dummyNodeGoalTag,
                routeCriteria, od, nodeR);
            nearestNodesGoal = tempnearestNodesGoal.Concat(nearGoalSleeveNodes).ToList();
        }

        //
        //
        if (nearestNodesStart.Count == 0)
            return new Tuple<List<string>, List<Vector3>, string>(routeNodeTag, routeVectorAuto,
                "no nearest segment/sleeve available within range from starting point");
        if (nearestNodesGoal.Count == 0)
            return new Tuple<List<string>, List<Vector3>, string>(routeNodeTag, routeVectorAuto,
                "no nearest segment/sleeve available within range from end point");
        //
        nearestNodesStart.RemoveAll(item => item == null);
        nearestNodesStart = nearestNodesStart.OrderBy(o => (o.Point - startV).Length()).ToList();
        nearestNodesStart = nearestNodesStart.Take(Math.Min(n, nearestNodesStart.Count)).ToList();
        nearestNodesStart.ForEach(n =>
        {
            if (exclusionNodes.Contains(n.Tag)) nearestNodesStart.Remove(n);
            ;
        });
        nearestNodesGoal.RemoveAll(item => item == null);
        nearestNodesGoal = nearestNodesGoal.OrderBy(o => (o.Point - goalV).Length()).ToList();
        nearestNodesGoal = nearestNodesGoal.Take(Math.Min(n, nearestNodesGoal.Count)).ToList();
        //
        nearestNodesGoal.ForEach(n =>
        {
            if (exclusionNodes.Contains(n.Tag)) nearestNodesGoal.Remove(n);
            ;
        });
        List<string> startNeighbourNodeList = new();
        List<string> goalNeighbourNodeList = new();
        //
        for (var i = 0; i < n; i++)
        {
            // this nearest strat/end nodes can be either of sleeves or of segments
            // in case of segments:
            // add this newly created node on segment to NodeR list
            // provide connections of the newly created dummy nodes on the segments with the both end nodes of the corresponding segment
            // in case of sleeves:
            // No need to add this node in NodeR as its already there
            // Only provide connection of this sleeve node to the DummyNodeStartTag/DummyNodeGoalTag
            if (nearestNodesStart.Count > i)
            {
                startNeighbourNodeList.Add(nearestNodesStart[i].Tag);
                if (nearestNodesStart[i].Type == "Segment")
                {
                    nodeR.Add(nearestNodesStart[i]);
                    try
                    {
                        nodeR.Where(node => node.Tag == nearestNodesStart[i].ConnectedNodesTag[0]).ToList()[0]
                            .ConnectedNodesTag.Add(nearestNodesStart[i].Tag);
                        nodeR.Where(node => node.Tag == nearestNodesStart[i].ConnectedNodesTag[1]).ToList()[0]
                            .ConnectedNodesTag.Add(nearestNodesStart[i].Tag);
                    }
                    catch (Exception e)
                    {
                        var a = nodeR.Where(node => node.Tag == nearestNodesStart[i].ConnectedNodesTag[0]).ToList();
                        var b = nodeR.Where(node => node.Tag == nearestNodesStart[i].ConnectedNodesTag[1]).ToList();
                        Debug.WriteLine($"Error : index error {e.Message}");
                    }
                }
                else
                {
                    // sleeve
                    nearestNodesStart[i].ConnectedNodesTag.Add(dummyNodeStartTag);
                }
            }

            if (nearestNodesGoal.Count > i)
            {
                goalNeighbourNodeList.Add(nearestNodesGoal[i].Tag);
                if (nearestNodesGoal[i].Type == "Segment")
                {
                    nodeR.Add(nearestNodesGoal[i]);
                    try
                    {
                        nodeR.Where(node => node.Tag == nearestNodesGoal[i].ConnectedNodesTag[0]).ToList()[0]
                            .ConnectedNodesTag.Add(nearestNodesGoal[i].Tag);
                        nodeR.Where(node => node.Tag == nearestNodesGoal[i].ConnectedNodesTag[1]).ToList()[0]
                            .ConnectedNodesTag.Add(nearestNodesGoal[i].Tag);
                    }
                    catch (Exception e)
                    {
                        var a = nodeR.Where(node => node.Tag == nearestNodesGoal[i].ConnectedNodesTag[0]).ToList();
                        var b = nodeR.Where(node => node.Tag == nearestNodesGoal[i].ConnectedNodesTag[1]).ToList();
                        Debug.WriteLine($"Error : index error {e.Message}");
                    }
                }
                else
                {
                    // sleeve
                    nearestNodesGoal[i].ConnectedNodesTag.Add(dummyNodeGoalTag);
                }
            }
        }

        Node dummyNodeStart = new(dummyNodeStartTag,
            startV, od + 0.75, new Vector3(0, 0, 0), "Cable",
            Guid.Empty, 0, startNeighbourNodeList);
        Node dummyNodeGoal = new(dummyNodeGoalTag,
            goalV, od + 0.75, new Vector3(0, 0, 0), "Cable",
            Guid.Empty, 0, goalNeighbourNodeList);
        //
        List<Node> openSet = [];
        nodeR.Add(dummyNodeStart);
        nodeR.Add(dummyNodeGoal);
        //
        openSet.Add(dummyNodeStart);
        // For node n, cameFrom[n] is the node immediately preceding it
        // on the cheapest path from start to n currently known
        List<Node> cameFrom = [];
        // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
        List<double> fScore = [];
        List<double> gScore = [];
        // h is the heuristic function. h(n) estimates the distance to reach goal from node n.
        List<double> h = [];
        //Parallel.ForEach(NodeR, node =>
        var tasksNodeR = nodeR.Select(async node =>
            //NodeR.ForEach(node =>
        {
            //with default value of Infinity
            gScore.Add(999999);
            //with default value of Infinity
            fScore.Add(999999);
            cameFrom.Add(node); // initialised
            h.Add(999999); //initiated
        });
        Task.WhenAll(tasksNodeR);
        gScore[nodeR.IndexOf(dummyNodeStart)] = 0;
        // g[start] = 0
        // For node n, fScore[n] = gScore[n] + h(n).
        // fScore[n] represents our current best guess as to
        // how short a path from start to finish can be if it goes through n.
        //
        //var tasksNodeR1 = NodeR.Select(async node =>
        Parallel.ForEach(nodeR, node =>
            //NodeR.ForEach(node =>
        {
            // if available space is not sufficient then this node is no longer available for laying
            // occupied space = actually occupied space + the margin required for the last laid cables
            //if (node.AvailableWidth > cableOD ) // this is oversimplified July 13, 2023. revert to the below logic
            if (node.AvailableWidth > od + SpaceForCable(cableTag, node, cables, spacing)
                && !exclusionNodes.Contains(node.Tag))
                // heuristic Pithagoras/Manhattan distance of the current node from the final destination
                h[nodeR.IndexOf(node)] = Math.Abs(node.Point.X - goalV.X)
                                         + Math.Abs(node.Point.Y - goalV.Y)
                                         + Math.Abs(node.Point.Z - goalV.Z);
            else
                h[nodeR.IndexOf(node)] = 999999; // not available
            ;
        });
        //Task.WhenAll(tasksNodeR1);
        fScore[nodeR.IndexOf(dummyNodeStart)] =
            h[nodeR.IndexOf(dummyNodeStart)]; //  g[start] = 0                                                                     //
        var nearest = dummyNodeStart;
        var farFromDest = 999999.0;
        while (openSet.Count != 0)
        {
            // while openSet is not empty
            whileLoopCount++;
            if (whileLoopCount % 1000 == 0)
                Debug.WriteLine($"Route for {cableTag} still checking : While Loop Count : {whileLoopCount}");
            var current = LowestfScore(nodeR, openSet, fScore); // the node in openSet having the lowest fScore[] value
            var farFromDestTemp =
                Math.Round(
                    Math.Abs(current.Point.X - dummyNodeGoal.Point.X) +
                    Math.Abs(current.Point.Y - dummyNodeGoal.Point.Y) +
                    Math.Abs(current.Point.Z - dummyNodeGoal.Point.Z), 3);
            if (farFromDestTemp <= farFromDest)
            {
                nearest = current;
                farFromDest = farFromDestTemp;
            }

            //System.Diagnostics.Debug.WriteLine($"Current Tag {current.Tag} -  {h[NodeR.IndexOf(current)]}  OpenSet Count {openSet.Count}"); 
            if (exclusionNodes.Contains(current.Tag))
                // no exclusive routes available
                Console.Write("AStar no exclusive routes available");
            if (current.Tag == dummyNodeGoal.Tag)
            {
                var tuple = ReconstructPath(cameFrom, nodeR, current, dummyNodeStart, dummyNodeGoal);
                routeNodeTag = tuple.Item1;
                routeVectorAuto = tuple.Item2;
                nearest.Tag = dummyNodeGoal.Tag;
                break;
            }

            ;
            //
            var index = openSet.IndexOf(current);
            if (index != -1) openSet.Remove(current);
            //
            // for each neighborNode / adjacent of current node
            var NeighbourNodes = nodeR.Where(node => current.ConnectedNodesTag.Contains(node.Tag)).ToList();
            NeighbourNodes.ForEach(neighborNode =>
            {
                // available space at this neighbor node = width of the node - occupied space in this node
                // occupied space = actually occupied space + the margin required for the last laid cables
                // this node is no longer available for laying if the available space is < cable dia and cable gap
                // this node is no longer available for laying if the segmenttype of the segment between this node (neighbor)
                // and previous node (current) does not match with the cable type
                double distanceCurrentToNeighbour = 999999999;
                if (neighborNode.AvailableWidth > od + SpaceForCable(cableTag, neighborNode, cables, spacing))
                    // d(current,neighbor) is the distance of this neighbor from current node
                    distanceCurrentToNeighbour = Math.Abs(neighborNode.Point.X - current.Point.X)
                                                 + Math.Abs(neighborNode.Point.Y - current.Point.Y)
                                                 + Math.Abs(neighborNode.Point.Z - current.Point.Z);
                //
                ;
                // tentative_gScore is the distance from start to the neighbor through current
                var tentative_gScore = gScore[nodeR.IndexOf(current)] + distanceCurrentToNeighbour;
                // if this path to neighbor is better than any previous one. Record it!
                if (tentative_gScore < gScore[nodeR.IndexOf(neighborNode)])
                {
                    cameFrom[nodeR.IndexOf(neighborNode)] = current;
                    gScore[nodeR.IndexOf(neighborNode)] = tentative_gScore;
                    fScore[nodeR.IndexOf(neighborNode)] =
                        gScore[nodeR.IndexOf(neighborNode)] + h[nodeR.IndexOf(neighborNode)];
                    if (openSet.Contains(neighborNode) == false) openSet.Add(neighborNode);
                    ;
                }

                ;
            });
            //
        }

        ;
        //        
        var endTime = DateTime.Now;
        Debug.WriteLine(
            $"Route for {cableTag} While Loop Count : {whileLoopCount} : Time: {endTime - startTime} ms : {routeNodeTag.Count} nodes  ::  {string.Join(", ", routeNodeTag)}");
        //
        // in case of route not found, check the nearest current node to the destination node
        var remark = "";
        if (nearest.Tag != dummyNodeGoal.Tag)
        {
            var tuple = ReconstructPath(cameFrom, nodeR, nearest, dummyNodeStart, dummyNodeGoal);
            var partRouteNodeTag = tuple.Item1;
            var partRouteVectorAuto = tuple.Item2;
            var path = partRouteNodeTag.Count > 0 ? string.Join(", ", partRouteNodeTag) : "";
            remark = $"Route not found beyond {nearest.Tag}({nearest.Point}) {farFromDest} away. Path : {path}.";
            Debug.WriteLine(remark);
        }

        ;
        //
        return new Tuple<List<string>, List<Vector3>, string>(routeNodeTag, routeVectorAuto, remark);

        //
        // function to reconstruct path from the current node
        Tuple<List<string>, List<Vector3>> ReconstructPath(List<Node> cameFrom, List<Node> NodeR, Node currentNode,
            Node startNode, Node goalNode)
        {
            List<string> RouteNodeTag = [];
            List<Vector3> RouteVectorAuto = [];
            var newCurrentNode = cameFrom[NodeR.IndexOf(currentNode)];
            while (newCurrentNode != startNode)
            {
                // add currentNode to the start of the RouteVector and RouteTag array
                RouteNodeTag.Insert(0, newCurrentNode.Tag);
                RouteVectorAuto.Insert(0, newCurrentNode.Point);
                newCurrentNode = cameFrom[NodeR.IndexOf(newCurrentNode)];
            }

            ;
            RouteNodeTag.Insert(0, startNode.Tag);
            RouteVectorAuto.Insert(0, startNode.Point);
            RouteNodeTag.Add(goalNode.Tag);
            RouteVectorAuto.Add(goalNode.Point);
            //removing duplicate adjescent nodes
            //List<Vector3> DuplicateAdJescentNodes = RouteVectorAuto.GroupBy(x => x).Where(g => g.Count() > 1).Select(x => x.Key).ToList();

            //DuplicateAdJescentNodes.ForEach(duplicateNodes =>
            //{
            //    var index = RouteNodeTag.IndexOf(RouteNodeTag.Where(tag => tag.Contains("Dummy") && RouteVectorAuto[RouteNodeTag.IndexOf(tag)]==duplicateNodes).ToList()[0]);
            //    RouteVectorAuto.RemoveAt(index);
            //    RouteNodeTag.RemoveAt(index);
            //});
            //
            return new Tuple<List<string>, List<Vector3>>(RouteNodeTag, RouteVectorAuto);
        }

        // function to find the node in openSet having the lowest fScore[] value
        Node LowestfScore(List<Node> NodeR, List<Node> openSet, List<double> fScore)
        {
            var templowestfScore = 99999999.9;
            var lowestfScoreNode = openSet[0];
            var tasks = openSet.Select(async node =>
                //openSet.ForEach(node =>
            {
                var i = NodeR.IndexOf(node);
                if (fScore[i] < templowestfScore)
                {
                    templowestfScore = fScore[i];
                    lowestfScoreNode = node; //the node in openSet having the lowest fScore[] value
                }

                ;
            });
            Task.WhenAll(tasks);
            return lowestfScoreNode;
        }
        
        //function to find the nearest point on the segment
        Vector3 NearestPointOnTheSegment(Vector3 P, Segment segment)
        {
            var Q = PerpendicularPointOnLine(P, segment.End1, segment.End2);
            if ((Q - segment.End1).Length() + (Q - segment.End2).Length() >
                (segment.End1 - segment.End2).Length() + 0.001)
                // Q is outside the segment
                Q = (Q - segment.End1).Length() < (Q - segment.End2).Length() ? segment.End1 : segment.End2;
            return Q;
        }

        //
        // add jumpNodes for jumping between nearby nodes with applicable route criteria where this cable is allowed to jump
        List<Node> WithJumpNodes(string cableRouteCriteria, Cable thisCable, List<Node> Node, List<Cable> Cable,
            double d = 1, double dx = 0.5, double dy = 0.5)
        {
            // jump cable between nearby nodes
            // add connection of nodes based on the cable route criteria, available space in nodes and dx, dy, dz and d between nodes
            // dx = 0.5, dy = 0.5, dz = ?, d = 2 assumed can be changed later

            //List<Segment> ApplicableSegments = Segments.Where(segment => segment.AllowableTypes.Contains(cableRouteCriteria) || cableRouteCriteria == "").ToList();
            //
            List<Node> ApplicableNodesTemp = Node.Where(node =>
                node.AllowableTypes.Contains(cableRouteCriteria) || node.AllowableTypes.Count == 0).ToList();
            // further filter based on available space in each of the nodes considering the spacing w.r.t the last laid cables
            List<Node> ApplicableNodes = ApplicableNodesTemp.Where(node =>
                    node.AvailableWidth >=
                    SpaceForCable(thisCable.Tag, node, Cable, _globalData.Spacings, "LaidcableTags"))
                .ToList();
            if (ApplicableNodes.Count > 1)
                //for (int i = 0; i < ApplicableNodes.Count - 1; i++)
                Parallel.For(0, ApplicableNodes.Count - 1, i =>
                {
                    var N1 = ApplicableNodes[i];
                    //for (int j = i + 1; j < ApplicableNodes.Count; j++)
                    Parallel.For(i + 1, ApplicableNodes.Count, j =>
                    {
                        var N2 = ApplicableNodes[j];
                        if (!N1.ConnectedNodesTag.Contains(N2.Tag) && !N2.ConnectedNodesTag.Contains(N1.Tag))
                            if ((N1.Point - N2.Point).Length() < d && Math.Abs(N1.Point.X - N2.Point.X) < dx &&
                                Math.Abs(N1.Point.Y - N2.Point.Y) < dy)
                            {
                                if (!N1.ConnectedNodesTag.Contains(N2.Tag)) N1.ConnectedNodesTag.Add(N2.Tag);
                                if (!N2.ConnectedNodesTag.Contains(N1.Tag)) N2.ConnectedNodesTag.Add(N1.Tag);
                            }
                    });
                });
            Parallel.For(0, ApplicableNodes.Count, i =>
            {
                // for debug purpose
                ApplicableNodes[i].ConnectedNodesTagS =
                    JsonSerializer.Serialize(ApplicableNodes[i].ConnectedNodesTag, jsonSerializerOptions);
            });
            //System.Diagnostics.Debug.WriteLine($"Jumpnodes node count : before {Node.Count} after {ApplicableNodes.Count}");
            return ApplicableNodes;
        }

        //
        // add jumpNodes for jumping between crossing segments where this cable is allowed to jump
        List<Node> WithJumpSegments(string cableRouteCriteria, double od, List<Node> Node, List<Segment> Segment)
        {
            // jump cable between nearby crossing segments where there is no nodes, i.e., crossing segments midway
            // add connection of nodes based on the cable route criteria and dx, dy, dz and d between nodes
            // dx = 0.5, dy = 0.5, dz = ?, d = 10 assumed can be changed later
            List<Node> nodes = new();
            var applicableSegment = Segment.Where(seg => string.IsNullOrWhiteSpace(cableRouteCriteria) ||
                                                         seg.AllowableTypes.Contains(cableRouteCriteria)).ToList();
            if (applicableSegment.Count > 1)
                Parallel.For(0, applicableSegment.Count, i =>
                {
                    Parallel.For(0, applicableSegment.Count, j =>
                    {
                        if (i != j)
                        {
                            var S1 = applicableSegment[i];
                            var S2 = applicableSegment[j];
                            if (IsFarAway(S1, S2)) return;
                            if (IsParallel(S1.End1, S1.End2, S2.End1, S2.End2)) return;
                            if (Coplaner(S1.End1, S1.End2, S2.End1, S2.End2)) return;

                            //
                            {
                                // assume S1 and S2 are skew lines
                                var PQ = IntersectionPoints(S1.End1, S1.End2, S2.End1, S2.End2);
                                if (PQ.Count != 0)
                                {
                                    // skewd line
                                    var P = PQ[0];
                                    var Q = PQ[1];
                                    var d = (P - Q).Length();
                                    if (d > 0.1 && d < 0.6)
                                    {
                                        // assumed cable would jump within  0.6 m
                                        // introduce one node each in two segments
                                        var nodeCount = Node.Count;
                                        var n1Tag = "N" + nodeCount.ToString("D6");
                                        var n2Tag = "N" + (nodeCount + 1).ToString("D6");
                                        var n1 = new Node(n1Tag, P, S1.Width, S1.Face, "Segment", S1.UID, 9,
                                            new List<string> { S1.Node1, S1.Node2, n2Tag });
                                        var n2 = new Node(n2Tag, Q, S2.Width, S2.Face, "Segment", S2.UID, 9,
                                            new List<string> { S2.Node1, S2.Node2, n1Tag });
                                        var N11 = Node.Where(x => x.Tag == S1.Node1).ToList()[0];
                                        var N12 = Node.Where(x => x.Tag == S1.Node2).ToList()[0];
                                        var N21 = Node.Where(x => x.Tag == S2.Node1).ToList()[0];
                                        var N22 = Node.Where(x => x.Tag == S2.Node2).ToList()[0];
                                        if (N11.ConnectedNodesTag.Contains(N12.Tag))
                                            N11.ConnectedNodesTag.Remove(N12.Tag);
                                        if (N12.ConnectedNodesTag.Contains(N11.Tag))
                                            N12.ConnectedNodesTag.Remove(N11.Tag);
                                        N11.ConnectedNodesTag.Add(n1Tag);
                                        N12.ConnectedNodesTag.Add(n1Tag);
                                        if (N21.ConnectedNodesTag.Contains(N22.Tag))
                                            N21.ConnectedNodesTag.Remove(N22.Tag);
                                        if (N22.ConnectedNodesTag.Contains(N21.Tag))
                                            N22.ConnectedNodesTag.Remove(N21.Tag);
                                        N21.ConnectedNodesTag.Add(n2Tag);
                                        N22.ConnectedNodesTag.Add(n2Tag);
                                        Node.Add(n1);
                                        Node.Add(n2);
                                    }
                                }
                            }
                        }
                    });
                });
            //
            return Node;
        }
    }
    
    
    public List<Vector3> FindPath(List<Node> nodelist, Vector3 startPoint, Vector3 goalPoint, float pathWidth, float jumpDistance)
    {
        // create NodePath lists
        List<NodePath?> nodes = [];

        nodelist.ForEach(node=> nodes.Add(new NodePath(node.Tag, node.Point, node.ConnectedNodesTag, (float)node.Width)));
        
        var startNode = nodes.FirstOrDefault(n => n.Point == startPoint);
        var goalNode = nodes.FirstOrDefault(n => n.Point == goalPoint);

        if (startNode == null || goalNode == null)
        {
            return null; // Start or goal node not found
        }

        var forwardOpenSet = new PriorityQueue<NodePath?, float>();
        var backwardOpenSet = new PriorityQueue<NodePath?, float>();
        var forwardClosedSet = new HashSet<String>();
        var backwardClosedSet = new HashSet<String>();
        var forwardNodes = new Dictionary<string, NodePath?>();
        var backwardNodes = new Dictionary<string, NodePath?>();

        forwardOpenSet.Enqueue(startNode, 0);
        backwardOpenSet.Enqueue(goalNode, 0);
        forwardNodes[startNode.Tag] = startNode;
        backwardNodes[goalNode.Tag] = goalNode;

        NodePath? meetingNode = null;

        while (forwardOpenSet.Count > 0 && backwardOpenSet.Count > 0 && meetingNode == null)
        {
            meetingNode = ProcessOpenSet(forwardOpenSet, forwardClosedSet, forwardNodes, backwardClosedSet, goalPoint, nodes, pathWidth);
            if (meetingNode == null)
            {
                meetingNode = ProcessOpenSet(backwardOpenSet, backwardClosedSet, backwardNodes, forwardClosedSet, startPoint, nodes, pathWidth);
            }
        }

        if (meetingNode == null)
        {
            // Direct path not found, try jump distance
            //meetingNode = FindPathWithJump(forwardNodes, backwardNodes, nodes, jumpDistance);
            meetingNode = FindPathWithBestJump(forwardNodes, backwardNodes, nodes, jumpDistance);
        }

        if (meetingNode == null)
        {
            return null; // No path found
        }

        return ReconstructPath(forwardNodes, backwardNodes, meetingNode);
    }

    public NodePath? ProcessOpenSet(PriorityQueue<NodePath?, float> openSet, HashSet<String> closedSet, 
        Dictionary<string, NodePath?> nodesDict, HashSet<string> otherClosedSet, Vector3 otherStartGoal, 
        List<NodePath?> allNodes, float pathWidth)
    {
        NodePath? current = openSet.Dequeue();
        closedSet.Add(current.Tag);

        if (otherClosedSet.Contains(current.Tag))
        {
            return current; // Meeting point found
        }

        foreach (var neighborTag in current.Connections)
        {
            var neighbor = allNodes.FirstOrDefault(n => n.Tag == neighborTag);
            if (neighbor == null || closedSet.Contains(neighbor.Tag) || neighbor.Width < pathWidth) continue;

            var tentativeG = nodesDict[current.Tag].G + Vector3.Distance(current.Point, neighbor.Point);

            if (!nodesDict.ContainsKey(neighbor.Tag) || tentativeG < neighbor.G)
            {
                nodesDict.TryAdd(neighbor.Tag, neighbor);
                nodesDict[neighbor.Tag].G = tentativeG;
                nodesDict[neighbor.Tag].Parent = current;
                var h = Vector3.Distance(neighbor.Point, otherStartGoal);
                openSet.Enqueue(neighbor, tentativeG + h);
            }
        }

        return null;
    }

    public NodePath? FindPathWithJump(Dictionary<String, NodePath?> forwardNodes, 
        Dictionary<string, NodePath?> backwardNodes, List<NodePath?> allNodes, float jumpDistance)
    {
        foreach (var forwardNode in forwardNodes.Values)
        {
            foreach (var backwardNode in backwardNodes.Values)
            {
                if (Vector3.Distance(forwardNode.Point, backwardNode.Point) <= jumpDistance && forwardNode.Width >= jumpDistance && backwardNode.Width >= jumpDistance)
                {
                    return forwardNode; // Arbitrarily choose the forward node as meeting point
                }
            }
        }
        return null;
    }
    
    public NodePath? FindPathWithBestJump(Dictionary<String, NodePath> forwardNodes, 
        Dictionary<String, NodePath?> backwardNodes, List<NodePath?> allNodes, float jumpDistance)
    {
        var jumpCandidates = new Dictionary<String, (NodePath ForwardNode, NodePath BackwardNode, float TotalCost)>();

        // Find initial jump candidates
        foreach (var forwardNode in forwardNodes.Values)
        {
            foreach (var backwardNode in backwardNodes.Values)
            {
                if (Vector3.Distance(forwardNode.Point, backwardNode.Point) <= jumpDistance && forwardNode.Width >= jumpDistance && backwardNode.Width >= jumpDistance)
                {
                    float totalCost = forwardNode.G + backwardNode.G + Vector3.Distance(forwardNode.Point, backwardNode.Point);
                    jumpCandidates[forwardNode.Tag] = (forwardNode, backwardNode, totalCost);
                }
            }
        }

        if (jumpCandidates.Count == 0)
        {
            return null; // No jump candidates found
        }

        // Find the best jump candidate (lowest total cost)
        var bestJumpCandidate = jumpCandidates.Values.OrderBy(c => c.TotalCost).First();

        // Reconstruct the path using the best jump candidate
        return bestJumpCandidate.ForwardNode;
    }
    
    

    public List<Vector3> ReconstructPath(Dictionary<string, NodePath?> forwardNodes, 
        Dictionary<string, NodePath?> backwardNodes, NodePath? meetingNode)
    {
        var forwardPath = new List<Vector3>();
        var backwardPath = new List<Vector3>();

        NodePath? current = meetingNode;
        while (current != null)
        {
            forwardPath.Add(current.Point);
            current = forwardNodes.TryGetValue(current.Tag, out var node) ? node.Parent : null;
        }
        forwardPath.Reverse();

        current = backwardNodes[meetingNode.Tag];
        while (current != null)
        {
            backwardPath.Add(current.Point);
            current = backwardNodes.TryGetValue(current.Tag, out var node) ? node.Parent : null;
        }
        backwardPath.Reverse();
        backwardPath.RemoveAt(0); // Remove the meeting node duplicate

        forwardPath.AddRange(backwardPath);
        return forwardPath;
    }
    
    
    

    public float SpacingBeteenCables(Cable cable1, Cable cable2, List<Spacing> spacings)
    {
        var cat1 = cable1.RouteCriteria;
        var cat2 = cable2.RouteCriteria;
        var spacing = 0f;

        var temp1 = spacings.Where(spacing1 => (spacing1.Type1 == cat1 && spacing1.Type2 == cat2) ||
                                               (spacing1.Type1 == cat2 && spacing1.Type2 == cat1)).ToList();
        var spacingString = temp1.Count > 0 ? temp1[0].Space : "0"; // check later for "0" default
        while (spacingString.Contains(' ')) spacingString = spacingString.Replace(" ", "");
        //
        if (spacingString.Contains("D")) spacingString = spacingString.Replace("D", "d");
        if (spacingString.Contains("d"))
        {
            spacingString = spacingString.Replace("d", "");
            if (spacingString == "") spacingString = "1";
            if (float.TryParse(spacingString, out var spacingTemp))
                spacing = spacingTemp * Math.Max(cable1.ODm, cable2.ODm);
            else
                Debug.WriteLine("Error : Spacing");
        }
        else
        {
            if (float.TryParse(spacingString, out var spacingTemp)) spacing = spacingTemp;
        }

        return spacing;
    }


    public double SpaceForCable(string cableTag, Node node, List<Cable> cables, List<Spacing> spacings,
        string fieldCriteria = "")
    {
        double spacing = 0;
        var lastLaidCableTag = "";
        if (fieldCriteria == "ArrangedcableTags")
            lastLaidCableTag = node.ArrangedcableTags.Count == 0 ? "" : node.ArrangedcableTags[^1];
        else
            lastLaidCableTag = node.LaidcableTags.Count == 0 ? "" : node.LaidcableTags[^1];

        if (cableTag != "" && lastLaidCableTag != "")
        {
            var Cable1 = cables.Where(cable => cable.Tag == cableTag).ToList()[0];
            var Cable2 = cables.Where(cable => cable.Tag == lastLaidCableTag).ToList()[0];
            var temp1 = spacings.Where(spacing =>
                (spacing.Type1 == Cable1.RouteCriteria && spacing.Type2 == Cable2.RouteCriteria) ||
                (spacing.Type1 == Cable2.RouteCriteria && spacing.Type2 == Cable1.RouteCriteria)).ToList();
            var spacingString = temp1.Count > 0 ? temp1[0].Space : "0"; // check later for "0" default
            while (spacingString.Contains(" ")) spacingString = spacingString.Replace(" ", "");

            if (spacingString.Contains("D")) spacingString = spacingString.Replace("D", "d");
            if (spacingString.Contains("d"))
            {
                spacingString = spacingString.Replace("d", "");
                if (spacingString == "") spacingString = "1";
                if (double.TryParse(spacingString, out var spacingTemp))
                    spacing = spacingTemp * Math.Max(Cable1.OD, Cable2.OD);
                //System.Diagnostics.Debug.WriteLine($"Error : Spacing");
            }
            else
            {
                if (double.TryParse(spacingString, out var spacingTemp)) spacing = spacingTemp;
            }
        }

        return spacing;
    }

    public List<Node> NearestDummyNodesOnSegments(Vector3 p, double d, List<Segment> segments, string originTag,
        string routeCriteria, double od, List<Node> nodes)
    {
        // this function returns the list of all the "dummy" created nodes on the nearest segments from this point P provided meeting routeCriteria
        List<Node> nearestNodes = new();
        List<Node> orderedNearestNodes = new();

        // filter segments within the reach of vector P within distance d
        var filteredSegmentsTemp = segments.Where(seg =>
                ((Math.Abs(seg.End1.X - p.X) < d &&
                  Math.Abs(seg.End1.Y - p.Y) < d &&
                  Math.Abs(seg.End1.Z - p.Z) < d &&
                  Vector3.Distance(seg.End1, p) < d) ||
                 (Math.Abs(seg.End2.X - p.X) < d &&
                  Math.Abs(seg.End2.Y - p.Y) < d &&
                  Math.Abs(seg.End2.Z - p.Z) < d &&
                  Vector3.Distance(seg.End2, p) < d)) &&
                seg.AllowableTypes != null &&
                (seg.AllowableTypes.Contains(routeCriteria) || seg.AllowableTypes.Count == 0)
            //&& Node.Any(n=> n.SegmentUID==seg.UID && n.AvailableWidth > od)
        ).ToList();
        var filteredSegments = filteredSegmentsTemp
            .Where(seg => nodes.Any(n => n.SegmentUID == seg.UID && n.AvailableWidth > od)).ToList();
        if (filteredSegments.Count == 0) return orderedNearestNodes;
        filteredSegments.ForEach(seg =>
            //Parallel.For(0, FilteredSegments.Count, i =>
        {
            //var seg = FilteredSegments[i];
            var point = PerpendicularPointOnLine(p, seg.End1, seg.End2);
            //string tag, string point, double width, string face, string segmentTag, int segmentEnd, string
            var node = new Node("Seg" + segments.IndexOf(seg).ToString("D6") + "-Dummy", point, seg.Width, seg.Face,
                "Segment", seg.UID, 0, new List<string> { seg.Node1, seg.Node2, originTag }, seg.AllowableTypesS);
            //var node = new Node(seg.TagNo + "-Dummy", point, seg.Width, seg.Face, seg.UID, 0, new List<string>() { seg.Node1, seg.Node2 }, seg.AllowableTypesS);
            nearestNodes.Add(node);
        });
        if (nearestNodes.Count > 0)
        {
            // taking two nearest nodes change later based on SS side or field side
            orderedNearestNodes = nearestNodes.OrderBy(n => (n.Point - p).Length()).ToList()
                .Take(Math.Min(2, nearestNodes.Count)).ToList();
            ;
        }

        return orderedNearestNodes;
    }


    public List<Node> NearestSleeveNodes(Vector3 p, double range, String routeCriteria, double od,
        List<Node> nodes)
    {
        // this function checks if there are nearby sleeve nodes within the range from point p

        return nodes.Where(n =>
            n.Type == "Sleeve" &&
            Math.Abs(n.Point.X - p.X) < range &&
            Math.Abs(n.Point.Y - p.Y) < range &&
            Math.Abs(n.Point.Z - p.Z) < range &&
            Vector3.Distance(n.Point, p) < range &&
            (n.AllowableTypes.Count == 0 || n.AllowableTypes.Contains(routeCriteria)) &&
            n.AvailableWidth >= od).ToList();
    }

    
    
    
}
