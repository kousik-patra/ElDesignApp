using System;
using System.Collections.Generic;

namespace ElDesignApp.Models;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using ElDesignApp.Data;
using JsonSerializer = System.Text.Json.JsonSerializer;


public class LayoutClass
{
    
}


[Serializable]
public class SerializableVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    // Convert Unity Vector3 to SerializableVector3
    public static SerializableVector3 FromVector3(Vector3 vector)
    {
        return new SerializableVector3
        {
            X = vector.X,
            Y = vector.Y,
            Z = vector.Z
        };
    }

    // Convert SerializableVector3 back to Unity Vector3
    public Vector3 ToVector3()
    {
        return new Vector3(X, Y, Z);
    }
}

public class SceneInfo
{
    public SceneInfo()
    {
        SceneJSON = "";
        ClickPoints = new List<ClickedPoint>();
        Display = new List<string> { "", "", "", "" };
    }

    public string? SceneName { get; set; }
    public float RendererWidth { get; set; }
    public float RendererHeight { get; set; }
    public string RendererWidthPX { get; set; }
    public string RendererHeightPX { get; set; }
    public Vector3 CameraPosition { get; set; }
    public Vector3 CameraRotation { get; set; }
    public List<ClickedPoint> ClickPoints { get; set; }
    public string? SceneJSON { get; set; } = "";
    public List<string> Display { get; set; }
}

public class ENU
{
    public ENU(float e, float n, float u = 0)
    {
        E = e;
        N = n;
        U = u;
    }

    public ENU()
    {
    }

    public float E { get; set; }
    public float N { get; set; }
    public float U { get; set; }
}

public class PlotPlan : BaseInfo, IBaseMethod
{
    public string? ImgThumbString { get; set; }
    public string? ImgString { get; set; }
    public bool KeyPlan { get; set; } // true if its the key plan, else false
    public int Width { get; set; } // original image width
    public int Height { get; set; } // original image height
    public float ScaleX { get; set; }
    public float ScaleY { get; set; }
    public float CentreX { get; set; }
    public float CentreY { get; set; }

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "CentreX-Reference")]
    public float CentreXRef { get; set; } = 0;

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "CentreY-Reference")]
    public float CentreYRef { get; set; } = 0;

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Global E")]
    public float GlobalE { get; set; } = 0;

    [Required]
    [RegularExpression(@"[+-]?(\d{1,12}|\d{0,9}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Global N")]
    public float GlobalN { get; set; } = 0;
    
    [Required]
    [RegularExpression(@"[+-]?(\d{1,12}|\d{0,9}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Local E")]
    public float LocalE { get; set; } = 0;

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Local N")]
    public float LocalN { get; set; } = 0;
    
    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Angle True North")]
    public float AngleTrueNorth { get; set; } = 0f; // Angle (degree) of True North with respect to Plant North (Clock wise is +ve)

    public float RendererWidth { get; set; }
    public float RendererHeight { get; set; }
    public float Rotation { get; set; } // Image to te rotated in radians to align E axis to E
    public bool XEW { get; set; } = true; // X-Axis East-West , if false, X-Axis is North-South
    public string XY { get; set; } = "EN"; // XY= EN or NE X direction is E direction so Y direction is N direction

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "X-Axis left hand side reference")]
    public float X1 { get; set; }

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "X-Axis right hand side reference")]
    public float X2 { get; set; }

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Y-Axis bottom side reference")]
    public float Y1 { get; set; }

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Y-Axis top side reference")]
    public float Y2 { get; set; }

    [Required]
    [RegularExpression(@"[+-]?(\d{1,8}|\d{0,5}\.\d{1,3}|\.\d{1,3})$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Z-Elevation")]
    public float Z { get; set; }

    // [Required]
    // [Range(0.01f, 1.00f, ErrorMessage = "Opacity must be between 0.01 and 1.00")]
    // [RegularExpression(@"^(0\.[0-9]{1,2}|1\.0{1,2})$", ErrorMessage = "Opacity must be between 0.01 and 1.00")]
    // [Display(Name = "Opacity in display")]
    public float Opacity { get; set; }

    public PlotPlan()
    {
    }


    public void Update()
    {
        throw new NotImplementedException();
    }
}

public class PlotScale
{
    [Required]
    [RegularExpression(@"^[+-]?(\d{1,8})(\.\d{1,3})?$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "X-Axis left hand side reference")]
    public float X1 { get; set; }


    [Required]
    [RegularExpression(@"^[+-]?(\d{1,8})(\.\d{1,3})?$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "X-Axis right hand side reference")]
    public float X2 { get; set; }


    [Required]
    [RegularExpression(@"^[+-]?(\d{1,8})(\.\d{1,3})?$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Y-Axis bottom side reference")]
    public float Y1 { get; set; }


    [Required]
    [RegularExpression(@"^[+-]?(\d{1,8})(\.\d{1,3})?$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Y-Axis top side reference")]
    public float Y2 { get; set; }
    [Required]
    [RegularExpression(@"^(EN|NE)$",
        ErrorMessage = "More than three decimals and characters are not allowed.")]
    [Display(Name = "Select 'EN' for E/W along X-axis and N/S along the Y-axis, NE for N/S along X-axis")]
    public string XY { get; set; }
    
}

public class PlotName
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\ ]{4,30}$", ErrorMessage = "Enter valid text without any special characters.")]
    [Display(Name = "Unique File Name")]
    public string Name { get; set; }
}

public class DivDimension
{
    public double Left, Top, Right, Bottom, Width, Height;

    public DivDimension(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = right - left;
        Height = bottom - top;
    }
}

[Serializable]
public class ClickedPoint
{
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float MouseX { get; set; }
    public float MouseY { get; set; }
    public float EventX { get; set; }
    public float EventY { get; set; }
    public float EventclientX { get; set; }
    public float EventclientY { get; set; }
    public float EventpageX { get; set; }
    public float EventpageY { get; set; }
    public float EventoffsetX { get; set; }
    public float EventoffsetY { get; set; }
    public float EventlayerX { get; set; }
    public float EventlayerY { get; set; }
    public float LinePositionX { get; set; }
    public float LinePositionY { get; set; }
    public float LinePositionZ { get; set; }
    public float E { get; set; }
    public float N { get; set; }
    public float U { get; set; }
}

[Serializable]
public class SegmentResult
{
    public SegmentResult()
    {
    }

    // to store SegmentResult result in DB
    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string IsolatedStraightSegmentListJSON { get; set; }
    public List<Segment> IsolatedStraightSegment { get; set; }
    public string StraightSegmentListJSON { get; set; }
    public List<Segment> StraightSegment { get; set; }
    public string BendListJSON { get; set; }
    public List<Bend> BendList { get; set; }
    public string TeeListJSON { get; set; }
    public List<Tee> TeeList { get; set; }
    public string CrossListJSON { get; set; }
    public List<Cross> CrossList { get; set; }
    public string NodeListJSON { get; set; }
    public List<Node> NodeList { get; set; }
    public string SleeveListJSON { get; set; }
    public List<Sleeve> SleeveList { get; set; }
    public string List1JSON { get; set; } // used to store Segment
    public string List2JSON { get; set; } // used to store Node
    public string List3JSON { get; set; } // used to store Sleeve
    public string List4JSON { get; set; } // spare
    public string List5JSON { get; set; } // spare
    public string List6JSON { get; set; } // spare
    public string UpdatedBy { get; set; }
    public DateTime UpdatedDateTime { get; set; }
}

[Serializable]
public class Segment : BaseInfo, IBaseMethod
{
    public List<string> ChildrenSegment = new();
    public List<Vector3> End9 = new(); // corresponding jump node points
    public List<string> Node9 = new(); // jump node tags as there are multiple jump nodes possible in a segment

    public Segment()
    {
    }

    public Segment(string recordId, string tag, double width, double height, string face, string end1, string end2,
        string allowableTypes, string wbs, string cableWay = "", string cableWayBranch = "")
    {
        RecordId = recordId;
        Tag = tag;
        Width = width;
        Height = height;
        FaceS = face;
        End1S = end1;
        End2S = end2;
        AllowableTypesS = allowableTypes;
        WBS = wbs;
        CableWay = cableWay;
        CableWayBranch = cableWayBranch;
    }

    public Segment(string tag, double width, double height, string face, string end1, string end2,
        string allowableTypes, string end1SegmentConnectionTags, string end2SegmentConnectionTags,
        string end1ArrayS, string end2ArrayS, string allowableTypesArrayS)
    {
        Tag = tag;
        Width = width;
        Height = height;
        FaceS = face;
        End1S = end1;
        End2S = end2;
        AllowableTypesS = allowableTypes;
        End1SegmentConnectionTagsS = end1SegmentConnectionTags ?? ""; // checks for null value
        End2SegmentConnectionTagsS = end2SegmentConnectionTags ?? ""; // checks for null value
        End1ArrayS = end1ArrayS ?? ""; // checks for null value
        End2ArrayS = end2ArrayS ?? ""; // checks for null value
        AllowableTypesArrayS = allowableTypesArrayS ?? ""; // checks for null value
    }

    public string? Service { get; set; } // EHT/Lighting/Power
    public double Width { get; set; } // width in m
    public double Height { get; set; } // width in m
    public Vector3 Face { get; set; }
    public string? FaceS { get; set; }
    public Vector3 End1 { get; set; }
    public string? End1S { get; set; }
    public string? Node1 { get; set; } // node tag
    public Vector3 End2 { get; set; }
    public string? End2S { get; set; }

    public string? CoordSystem { get; set; } // coordinate system "GLOBAL"/ "LOCAL" / "UTM"  based on which End1S and End2S data is entered

    public string? WBS { get; set; }
    public string? Node2 { get; set; } // node tag
    public string? Node9S { get; set; }
    public string? End9S { get; set; }
    public List<string>? AllowableTypes { get; set; } // allowable cable type in this segment
    public string? AllowableTypesS { get; set; }
    public bool EntryExit { get; set; } // cable drop or exit allowed
    public List<Guid>? End1SegmentConnection { get; set; }
    public string? End1SegmentConnectionTagsS { get; set; }
    public List<Guid>? End2SegmentConnection { get; set; }
    public string? End2SegmentConnectionTagsS { get; set; }
    public string? ParentSegment { get; set; } // parent RecordId
    public Guid ParentUID { get; set; }
    public string? ChildrenSegmentS { get; set; }
    public List<Vector3>? End1Array { get; set; }
    public string? End1ArrayS { get; set; }
    public List<Vector3>? End2Array { get; set; }
    public string? End2ArrayS { get; set; }
    public List<List<string>>? AllowableTypesArray { get; set; } // multiple array of segments as one entry
    public string? AllowableTypesArrayS { get; set; }
    public string? ParentTagForArrayS { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedDateTime { get; set; }
    public string? UpdatedDateTimeString { get; set; }
    public string? Remarks { get; set; }
    public bool Selected { get; set; }
    public string? CableWay { get; set; }
    public string? CableWayBranch { get; set; }
    public double Length { get; set; } // length in m
    public string? JSONstring { get; set; } // for drawing on plot
    public bool Isolated { get; set; } // true for Isolated , false for connected segments

    public void Update()
    {
        throw new NotImplementedException();
    }

    public void FaceUpdate()
    {
    }

    //public void FaceUpdate()
    //{
    //    // determine the face
    //    // if few trays are connected together then the face depends on the way the segment group are arranged
    //    // find group of trays for this tray
    //    if (Face != Vector3.Zero) { return; }

    //    List<Segment> segmentsGroupList = new() { };
    //    segmentsGroupList.Add(this);
    //    var d1 = 0.1;
    //    var groupLength = 0;

    //    while (groupLength < segmentsGroupList.Count)
    //    {
    //        var segmentsGroupListTemp = JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(segmentsGroupList));
    //        groupLength = segmentsGroupList.Count;
    //        segmentsGroupListTemp.ForEach(seg =>
    //        {
    //            var nearBySegmentListEnd1 = GlobalData.Segments.Where(sg => sg.UID != seg.UID && (Vector3.Distance(sg.End1, seg.End1) < d1 || Vector3.Distance(sg.End2, seg.End1) < d1)).ToList();
    //            if (nearBySegmentListEnd1.Count > 0)
    //            {
    //                nearBySegmentListEnd1.ForEach(s =>
    //                {
    //                    if (!seg.End1SegmentConnection.Contains(s.UID))
    //                    {
    //                        seg.End1SegmentConnection.Add(s.UID);
    //                    };
    //                    if (!s.End1SegmentConnection.Contains(seg.UID) && Vector3.Distance(s.End1, seg.End1) < d1)
    //                    {
    //                        s.End1SegmentConnection.Add(seg.UID);
    //                    };
    //                    if (!s.End2SegmentConnection.Contains(seg.UID) && Vector3.Distance(s.End2, seg.End1) < d1)
    //                    {
    //                        s.End2SegmentConnection.Add(seg.UID);
    //                    };
    //                    if (segmentsGroupList.Where(o => o.UID == s.UID).ToList().Count == 0) { segmentsGroupList.Add(s); }
    //                });
    //            }
    //            var nearBySegmentListEnd2 = GlobalData.Segments.Where(sg => sg.UID != seg.UID && (Vector3.Distance(sg.End1, seg.End2) < d1 || Vector3.Distance(sg.End2, seg.End2) < d1)).ToList();
    //            if (nearBySegmentListEnd2.Count > 0)
    //            {
    //                nearBySegmentListEnd2.ForEach(s =>
    //                {
    //                    if (!seg.End2SegmentConnection.Contains(s.UID))
    //                    {
    //                        seg.End2SegmentConnection.Add(s.UID);
    //                    };
    //                    if (!s.End1SegmentConnection.Contains(seg.UID) && Vector3.Distance(s.End1, seg.End2) < d1)
    //                    {
    //                        s.End1SegmentConnection.Add(seg.UID);
    //                    };
    //                    if (!s.End2SegmentConnection.Contains(seg.UID) && Vector3.Distance(s.End2, seg.End2) < d1)
    //                    {
    //                        s.End2SegmentConnection.Add(seg.UID);
    //                    };
    //                    if (segmentsGroupList.Where(o => o.UID == s.UID).ToList().Count == 0) { segmentsGroupList.Add(s); }
    //                });
    //            }
    //        });
    //        //segmentsGroupList = segmentsGroupListTemp;
    //    }
    //    //find the longest horizontal segment which is expected to be facing upwards
    //    var temp2 = segmentsGroupList.Where(seg => Math.Abs(seg.End1.Z - seg.End2.Z) < 0.1).ToList();
    //    var temp3 = temp2.OrderByDescending(seg => (seg.End1 - seg.End2).Length()).ToList();

    //    // order grouped segment from one end 
    //    // select the starting seg which is not connected at one and and not vertical
    //    var temp = segmentsGroupList.Where(o => (o.End1SegmentConnection.Count == 0 || o.End2SegmentConnection.Count == 0)
    //                                    && (Math.Abs(o.End1.X - o.End2.X) > d1 || Math.Abs(o.End1.Y - o.End2.Y) > d1)).ToList();


    //    Segment startSeg;
    //    if (temp3.Count > 0)
    //    {
    //        startSeg = temp3[0];
    //        startSeg.Face = new Vector3(0, 0, 1); // non vertical tray
    //        List<string> stk = new() { };
    //        segmentsGroupList.ForEach(k1 => { stk.Add(k1.UID + " " + k1.RecordId + " " + string.Join(" ", k1.End1SegmentConnection) + " " + string.Join(" ", k1.End2SegmentConnection)); });
    //        var k = string.Join(" ", stk);
    //        //var k = k1.UID + " " + k1.RecordId + " " + String.Join(" ", k1.End1SegmentConnection) + " " + String.Join(" ", k1.End2SegmentConnection);
    //        segmentsGroupList.Remove(segmentsGroupList.Where(o => o.UID == startSeg.UID).ToList()[0]);
    //        while (segmentsGroupList.Count > 0)
    //        {
    //            if (startSeg.End2SegmentConnection.Count > 0)
    //            {
    //                startSeg.End2SegmentConnection.ForEach(o =>
    //                {
    //                    var tempSeg = GlobalData.Segments.Where(sg => sg.UID == o).ToList()[0];
    //                    if (segmentsGroupList.Where(o => o.UID == tempSeg.UID).ToList().Count > 0)
    //                    {
    //                        if (Vector3.Distance(startSeg.End2, tempSeg.End1) < d1)
    //                        {
    //                            tempSeg.Face = Vector3.Normalize(Vector3.Cross(Vector3.Cross(startSeg.End2 - startSeg.End1, tempSeg.End2 - tempSeg.End1), startSeg.Face));
    //                        }
    //                        else
    //                        {
    //                            tempSeg.Face = Vector3.Normalize(Vector3.Cross(Vector3.Cross(startSeg.End2 - startSeg.End1, tempSeg.End1 - tempSeg.End2), startSeg.Face));
    //                        }
    //                        segmentsGroupList.Remove(segmentsGroupList.Where(o => o.UID == tempSeg.UID).ToList()[0]);
    //                    }
    //                });
    //                startSeg.End2SegmentConnection.Clear();
    //            }
    //            if (startSeg.End1SegmentConnection.Count > 0)
    //            {
    //                startSeg.End1SegmentConnection.ForEach(o =>
    //                {
    //                    var tempSeg = GlobalData.Segments.Where(sg => sg.UID == o).ToList()[0];
    //                    if (segmentsGroupList.Where(o => o.UID == tempSeg.UID).ToList().Count > 0)
    //                    {
    //                        if (Vector3.Distance(startSeg.End1, tempSeg.End1) < d1)
    //                        {
    //                            tempSeg.Face = Vector3.Normalize(Vector3.Cross(Vector3.Cross(startSeg.End1 - startSeg.End2, tempSeg.End2 - tempSeg.End1), startSeg.Face));
    //                        }
    //                        else
    //                        {
    //                            tempSeg.Face = Vector3.Normalize(Vector3.Cross(Vector3.Cross(startSeg.End1 - startSeg.End2, tempSeg.End1 - tempSeg.End2), startSeg.Face));
    //                        }
    //                        segmentsGroupList.Remove(segmentsGroupList.Where(o => o.UID == tempSeg.UID).ToList()[0]);
    //                    }
    //                });
    //            }

    //        }
    //    }
    //    else
    //    {
    //        // something wrong or its a standalone vertical segment
    //        this.Face = new Vector3(1, 0, 0); // vertical tray face x assumed

    //    }


    //}
    
}

[Serializable]
public class Bend
{
    public Bend()
    {
    }

    public Bend(string tag, double width, double height, double radious, string vOrh, string p1, string p2, string x,
        string face, int step)
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        if (UID == Guid.Empty) UID = Guid.NewGuid();
        ;
        Tag = tag;
        Width = width;
        Height = height;
        Radious = radious;
        VOrh = vOrh;
        P1S = p1;
        P2S = p2;
        XS = x;
        FaceS = face;
        Step = step;
        P1 = JsonSerializer.Deserialize<Vector3>(p1, jsonSerializerOption);
        P2 = JsonSerializer.Deserialize<Vector3>(p2, jsonSerializerOption);
        X = JsonSerializer.Deserialize<Vector3>(x, jsonSerializerOption);
        Face = JsonSerializer.Deserialize<Vector3>(face, jsonSerializerOption);
    }

    public Bend(string tag, double width, double height, double radious, string vOrh, Vector3 p1, Vector3 p2, Vector3 x,
        Vector3 face, int step, Vector3 f1, Vector3 f2, double h1, double h2, double w1, double w2, Vector3 p01,
        Vector3 p02, string node1, string node2)
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        if (UID == Guid.Empty) UID = Guid.NewGuid();
        ;
        Tag = tag;
        Width = width;
        Height = height;
        Radious = radious;
        VOrh = vOrh;
        Step = step;
        P1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(p1, jsonSerializerOption),
            jsonSerializerOption);
        P2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(p2, jsonSerializerOption),
            jsonSerializerOption);
        X = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(x, jsonSerializerOption),
            jsonSerializerOption);
        Face = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(face, jsonSerializerOption),
            jsonSerializerOption);
        F1 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(f1, jsonSerializerOption),
            jsonSerializerOption);
        F2 = JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(f2, jsonSerializerOption),
            jsonSerializerOption);
        H1 = (float)h1;
        H2 = (float)h2;
        W1 = (float)w1;
        W2 = (float)w2;
        P01 = p01;
        P02 = p02;
        Node1 = node1;
        Node2 = node2;
    }

    public Bend(string tag, Vector3 x, Vector3 p1, Vector3 p2, float w1, float w2, Vector3 f1, Vector3 f2, float h1,
        float h2, int step, string node1, string node2)
    {
        UID = Guid.NewGuid();
        Tag = tag;
        X = x;
        P1 = p1;
        P2 = p2;
        F1 = f1;
        F2 = f2;
        W1 = w1;
        W2 = w2;
        H1 = h1;
        H2 = h2;
        Step = step;
        Node1 = node1;
        Node2 = node2;
    }

    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string Tag { get; set; }
    public double Width { get; set; } // width in m
    public double Height { get; set; } // height in m
    public double Radious { get; set; } // radious in m
    public string VOrh { get; set; } // Verticle or Horizontal
    public Vector3 P1 { get; set; } // bend start point
    public string Node1 { get; set; } // Node Tag of bend start point
    public Vector3 P01 { get; set; } // other end of the segment foor corresponding start point of bend
    public string P1S { get; set; }
    public Vector3 P2 { get; set; } // bend end point
    public string Node2 { get; set; } // Node Tag of bend end point
    public Vector3 P02 { get; set; } // other end of the segment foor corresponding end point of bend
    public string P2S { get; set; }
    public Vector3 X { get; set; } // intersection of two parent segments
    public string XS { get; set; }
    public Vector3 Face { get; set; }
    public Vector3 F1 { get; set; } // face at P1
    public Vector3 F2 { get; set; } // face at P2
    public float W1 { get; set; } // width in m at P1
    public float W2 { get; set; } // width in m at P2
    public float H1 { get; set; } // height in m at P1
    public float H2 { get; set; } // height in m at P2
    public string FaceS { get; set; }

    public int Step { get; set; }

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which OriginVS and DestinationVS values are calculated and vice versa
    //public string CoordSystem { get; set; } as default calculated items (tee, bend cross) are in XYZ
    public string JSONstring { get; set; } // for drawing on plot
}

[Serializable]
public class Tee // not complete later
{
    public Tee()
    {
    }

    public Tee(string tag, double widtha, double widthb, double height, double radious, string p1, string p2, string p3,
        string x, string face, int step, string node1, string node2, string node3)
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        if (UID == Guid.Empty) UID = Guid.NewGuid();
        ;
        Tag = tag;
        Widtha = widtha;
        Widthb = widthb;
        Height = height;
        Radious = radious;
        P1S = p1;
        P2S = p2;
        P3S = p3;
        XS = x;
        FaceS = face;
        Step = step;
        P1 = JsonSerializer.Deserialize<Vector3>(p1, jsonSerializerOption);
        P2 = JsonSerializer.Deserialize<Vector3>(p2, jsonSerializerOption);
        P3 = JsonSerializer.Deserialize<Vector3>(p3, jsonSerializerOption);
        X = JsonSerializer.Deserialize<Vector3>(x, jsonSerializerOption);
        Face = JsonSerializer.Deserialize<Vector3>(face, jsonSerializerOption);
        Node1 = node1;
        Node2 = node2;
        Node3 = node3;
    }

    public Tee(string tag, Vector3 x, Vector3 p1, Vector3 p2, Vector3 p3, float w1, float w2, float w3, Vector3 f1,
        Vector3 f2, Vector3 f3, float h1, float h2, float h3, int step, string node1, string node2, string node3)
    {
        UID = Guid.NewGuid();
        Tag = tag;
        X = x;
        P1 = p1;
        P2 = p2;
        P3 = p3;
        F1 = f1;
        F2 = f2;
        F3 = f3;
        W1 = w1;
        W2 = w2;
        W3 = w3;
        H1 = h1;
        H2 = h2;
        H3 = h3;
        Step = step;
        Node1 = node1;
        Node2 = node2;
        Node3 = node3;
    }

    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string Tag { get; set; }
    public float W1 { get; set; } // width in m
    public float W2 { get; set; } // width in m
    public float W3 { get; set; } // width in m
    public double Widtha { get; set; } // width in m
    public double Widthb { get; set; } // width in m
    public double Height { get; set; } // width in m
    public float H1 { get; set; } // height in m at side 1
    public float H2 { get; set; } // width in m
    public float H3 { get; set; } // width in m
    public double Radious { get; set; } // width in m
    public Vector3 P1 { get; set; } // straight start point
    public string Node1 { get; set; } // Node Tag of straight start point
    public string P1S { get; set; }
    public Vector3 P2 { get; set; } // straight end point
    public string Node2 { get; set; } // Node Tag straight end point
    public string P2S { get; set; }
    public Vector3 P3 { get; set; } // bend point
    public string Node3 { get; set; } // Node Tag of bend point
    public string P3S { get; set; }
    public Vector3 X { get; set; } // intersection of parent segments
    public string XS { get; set; }
    public Vector3 Face { get; set; }
    public Vector3 F1 { get; set; }
    public Vector3 F2 { get; set; }
    public Vector3 F3 { get; set; }
    public string FaceS { get; set; }

    public int Step { get; set; }

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which OriginVS and DestinationVS values are calculated and vice versa
    //public string CoordSystem { get; set; } as default calculated items (tee, bend cross) are in XYZ
    public string JSONstring { get; set; } // for drawing on plot
}

[Serializable]
public class Cross
{
    public Cross()
    {
    }

    public Cross(string tag, double width, double height, double radious, string p1, string p2, string p3, string p4,
        string x, string face, int step, string node1, string node2, string node3, string node4)
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        if (UID == Guid.Empty) UID = Guid.NewGuid();
        ;
        Tag = tag;
        Width = width;
        Height = height;
        Radious = radious;
        P1S = p1;
        P2S = p2;
        P3S = p3;
        P4S = p4;
        XS = x;
        FaceS = face;
        Step = step;
        P1 = JsonSerializer.Deserialize<Vector3>(p1, jsonSerializerOption);
        P2 = JsonSerializer.Deserialize<Vector3>(p2, jsonSerializerOption);
        P3 = JsonSerializer.Deserialize<Vector3>(p3, jsonSerializerOption);
        P4 = JsonSerializer.Deserialize<Vector3>(p4, jsonSerializerOption);
        X = JsonSerializer.Deserialize<Vector3>(x, jsonSerializerOption);
        Face = JsonSerializer.Deserialize<Vector3>(face, jsonSerializerOption);
        Node1 = node1;
        Node2 = node2;
        Node3 = node3;
        Node4 = node4;
    }

    public Cross(string tag, Vector3 x, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
        float w1, float w2, float w3, float w4, Vector3 f1, Vector3 f2, Vector3 f3, Vector3 f4,
        float h1, float h2, float h3, float h4, int step, string node1, string node2, string node3, string node4)
    {
        UID = Guid.NewGuid();
        Tag = tag;
        X = x;
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        F1 = f1;
        F2 = f2;
        F3 = f3;
        F4 = f4;
        W1 = w1;
        W2 = w2;
        W3 = w3;
        W4 = w4;
        H1 = h1;
        H2 = h2;
        H3 = h3;
        H4 = h4;
        Step = step;
        Node1 = node1;
        Node2 = node2;
        Node3 = node3;
        Node4 = node4;
    }

    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string Tag { get; set; }
    public double Width { get; set; } // width in m
    public float W1 { get; set; } // width in m
    public float W2 { get; set; } // width in m
    public float W3 { get; set; } // width in m
    public float W4 { get; set; } // width in m
    public double Height { get; set; } // width in m
    public float H1 { get; set; } // height in m at side 1
    public float H2 { get; set; } // width in m
    public float H3 { get; set; } // width in m
    public float H4 { get; set; } // width in m
    public double Radious { get; set; } // width in m
    public Vector3 P1 { get; set; } // cross point1
    public string Node1 { get; set; } // Node Tag of cross point1
    public string P1S { get; set; }
    public Vector3 P2 { get; set; } // cross point2
    public string Node2 { get; set; } // Node Tag of cross point2
    public string P2S { get; set; }
    public Vector3 P3 { get; set; } // cross point3
    public string Node3 { get; set; } // Node Tag of cross point3
    public string P3S { get; set; }
    public Vector3 P4 { get; set; } // cross point4
    public string Node4 { get; set; } // Node Tag of cross point4
    public string P4S { get; set; }
    public Vector3 X { get; set; } // intersection of four parent segments
    public string XS { get; set; }
    public Vector3 Face { get; set; }
    public Vector3 F1 { get; set; }
    public Vector3 F2 { get; set; }
    public Vector3 F3 { get; set; }
    public Vector3 F4 { get; set; }
    public string FaceS { get; set; }

    public int Step { get; set; }

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which OriginVS and DestinationVS values are calculated and vice versa
    //public string CoordSystem { get; set; } as default calculated items (tee, bend cross) are in XYZ
    public string JSONstring { get; set; } // for drawing on plot
}

/// <summary>
/// </summary>
public class Sleeve : BaseInfo, ICloneable
{
    public Sleeve()
    {
    }

    public Sleeve(string recordId, string tag, double dia, double bendRadius, string pointsS, string wbs,
        string cableWay = "", string cableWayBranch = "")
    {
        RecordId = recordId;
        Tag = tag;
        Dia = dia;
        BendRadius = bendRadius == 0 ? 6 * dia : bendRadius;
        PointsS = pointsS;
        WBS = wbs;
        CableWay = cableWay;
        CableWayBranch = cableWayBranch;
    }

    public double Dia { get; set; } // diameter in m

    /// <summary>
    ///     Bend radius in m
    /// </summary>
    public double BendRadius { get; set; }

    /// <summary>
    ///     coordinate system "GLOBAL"/ "LOCAL" / "UTM" / "XYZ"
    /// </summary>
    public string CoordSystem { get; set; }

    /// <summary>
    ///     array of points as in 3D model in ENU
    /// </summary>
    public string PointsS { get; set; }

    /// <summary>
    ///     Area / WBS
    /// </summary>
    public string WBS { get; set; }

    /// <summary>
    ///     List of Vector3 main Points as in 3D model
    /// </summary>
    public List<Vector3> Points { get; set; }


    public bool Selected { get; set; }

    /// <summary>
    ///     3D Model CableWay, e.g., /A10U-ECS-562
    /// </summary>
    public string CableWay { get; set; }

    /// <summary>
    ///     3D Model CableWay Branch e.g. /A10U-ECS-562_B1
    /// </summary>
    public string CableWayBranch { get; set; }

    /// <summary>
    ///     For drawing on plot, calculated based on the bending radious curvature
    /// </summary>
    public string JsoNstring { get; set; }

    public Vector3 End1 { get; set; }
    public string Node1 { get; set; } // node tag
    public Vector3 End2 { get; set; }
    public string Node2 { get; set; } // node tag
    public List<Vector3>? Routes { get; set; }

    public object Clone()
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        var sleeve = (Sleeve)MemberwiseClone();
        sleeve.Points =
            JsonSerializer.Deserialize<List<Vector3>>(JsonSerializer.Serialize(Points, jsonSerializerOption),
                jsonSerializerOption);
        return sleeve;
    }

    

    
}

public class NodeParentChild
{
    public NodeParentChild()
    {
        Tag = "";
        Parent = "";
        Children = new List<string>();
        Left = "";
        Centre = "";
        Right = "";
        AllocationComplete = false;
    }

    public NodeParentChild(string tag, string parent, string childrenJSON, string left, string right, string centre,
        bool allocationComplete)
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        Tag = tag;
        Parent = parent;
        Children = JsonSerializer.Deserialize<List<string>>(childrenJSON, jsonSerializerOption);
        Left = left;
        Centre = centre;
        Right = right;
        AllocationComplete = allocationComplete;
    }

    public string Tag { get; set; }
    public string Parent { get; set; }
    public List<string> Children { get; set; }
    public string Left { get; set; }
    public string Centre { get; set; }
    public string Right { get; set; }
    public bool AllocationComplete { get; set; }
}

[Serializable]
public class Node : ICloneable
{
    public Node()
    {
    }

    public Node(string tag, Vector3 point, double width, Vector3 face, string type, Guid segmentUID, int segmentEnd,
        List<string> connectedNodesTag, string allowableTypesS = "")
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        UID = Guid.NewGuid();
        Tag = tag;
        PointS = JsonSerializer.Serialize(point, jsonSerializerOption);
        FaceS = JsonSerializer.Serialize(face, jsonSerializerOption);
        Point = JsonSerializer.Deserialize<Vector3>(PointS, jsonSerializerOption);
        Width = width;
        Face = JsonSerializer.Deserialize<Vector3>(FaceS, jsonSerializerOption);
        Type = type;
        SegmentUID = segmentUID;
        SegmentEnd = segmentEnd;
        ConnectedNodesTagS = JsonSerializer.Serialize(connectedNodesTag, jsonSerializerOption);
        ConnectedNodesTag = JsonSerializer.Deserialize<List<string>>(ConnectedNodesTagS, jsonSerializerOption);
        AllowableTypes = string.IsNullOrWhiteSpace(allowableTypesS)
            ? []
            : JsonSerializer.Deserialize<List<string>>(allowableTypesS, jsonSerializerOption);
        LaidcableTags = [];
        ArrangedcableTags = [];
        YetToArrangedcableTags = [];
        ArrangedcablePosition = [];
        ArrangedcableSide = [];
        MarginSide1 = 0; // default
        MarginSide2 = 0; // default
        MarginSpare = 0; // default
        AvailableWidth = (Width - MarginSide1 - MarginSide2) * (1 - MarginSpare);
    }

    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string Tag { get; set; }
    public Vector3 Point { get; set; }
    public string PointS { get; set; }
    public double Width { get; set; }
    public Vector3 Face { get; set; }
    public string FaceS { get; set; }
    public string Type { get; set; } // Segment, Sleeve, Trench
    public Guid SegmentUID { get; set; }
    public int SegmentEnd { get; set; } // 1 or 2 or 9 for mid-point/landing-point/jump-node
    public string ConnectedNodesTagS { get; set; }
    public List<string> ConnectedNodesTag { get; set; }
    public double AvailableWidth { get; set; }

    public List<string>
        LaidcableTags
    {
        get;
        set;
    } // list of assigned cables through this node, however not in order they are finally to be arranged

    public List<string>
        ArrangedcableTags { get; set; } // list of cables through this node in order they are finally arranged

    public List<string>
        YetToArrangedcableTags { get; set; } // list of cables through this node, which are not yet arranged

    public List<bool>
        ArrangedcableSide
    {
        get;
        set;
    } // True If this arranged cable towards the lay direction, false if from the opposite to the lay direction

    public List<Vector3> ArrangedcablePosition { get; set; }
    public Vector3 LayDirection { get; set; }
    public string LaidcableTagsS { get; set; }
    public double MarginSide1 { get; set; }
    public double MarginSide2 { get; set; }
    public double MarginSpare { get; set; }

    public List<string>
        AllowableTypes { get; set; } // allowable cable type through this node (same as the parent segment)

    public string AllowableTypesS { get; set; }

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which OriginVS and DestinationVS values are calculated and vice versa
    //public string CoordSystem { get; set; } as default calculated items (node, tee, bend cross) are in XYZ
    public string JSONstring { get; set; } // for drawing on plot

    //shallow copy https://www.youtube.com/watch?v=hxr3kviGJS4
    public object Clone()
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        var node = (Node)MemberwiseClone();
        node.ConnectedNodesTag =
            JsonSerializer.Deserialize<List<string>>(
                JsonSerializer.Serialize(node.ConnectedNodesTag, jsonSerializerOption), jsonSerializerOption);
        node.LaidcableTags =
            JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(node.LaidcableTags, jsonSerializerOption),
                jsonSerializerOption);
        node.ArrangedcableTags =
            JsonSerializer.Deserialize<List<string>>(
                JsonSerializer.Serialize(node.ArrangedcableTags, jsonSerializerOption), jsonSerializerOption);
        node.YetToArrangedcableTags = JsonSerializer.Deserialize<List<string>>(
            JsonSerializer.Serialize(node.YetToArrangedcableTags, jsonSerializerOption), jsonSerializerOption);
        node.ArrangedcablePosition = JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(node.ArrangedcablePosition, jsonSerializerOption), jsonSerializerOption);
        node.ArrangedcableSide =
            JsonSerializer.Deserialize<List<bool>>(
                JsonSerializer.Serialize(node.ArrangedcableSide, jsonSerializerOption), jsonSerializerOption);
        node.AllowableTypes =
            JsonSerializer.Deserialize<List<string>>(
                JsonSerializer.Serialize(node.AllowableTypes, jsonSerializerOption), jsonSerializerOption);
        return node;
    }



    public void JSONUpdate()
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        PointS = JsonSerializer.Serialize(Point, jsonSerializerOption);
        FaceS = JsonSerializer.Serialize(Face, jsonSerializerOption);
        ConnectedNodesTagS = JsonSerializer.Serialize(ConnectedNodesTag, jsonSerializerOption);
        LaidcableTagsS = JsonSerializer.Serialize(LaidcableTags, jsonSerializerOption);
        AllowableTypesS = JsonSerializer.Serialize(AllowableTypes, jsonSerializerOption);
    }
}

public class CablePiece
{
    public CablePiece(string tag, string end1, string end2, double dia, string type, string togetherWith,
        string separateFrom)
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        Tag = tag;
        Dia = dia;
        End1 = JsonSerializer.Deserialize<Vector3>(end1, jsonSerializerOption);
        End2 = JsonSerializer.Deserialize<Vector3>(end2, jsonSerializerOption);
        Vector3 refVector = new(-999, -999, -999);
        if ((End1 - refVector).Length() > (End1 - refVector).Length()) (End1, End2) = (End2, End1); // swap
        Type = type;
        TogetherWith = JsonSerializer.Deserialize<List<string>>(togetherWith, jsonSerializerOption);
        ;
        SeparateFrom = JsonSerializer.Deserialize<List<string>>(separateFrom, jsonSerializerOption);
        ;
    }

    public Guid UID { get; set; }
    public string Tag { get; set; }
    public Vector3 End1 { get; set; }
    public Vector3 End2 { get; set; }
    public double Dia { get; set; } // diameter in mm
    public string Type { get; set; }
    public List<string> TogetherWith { get; set; }
    public List<string> SeparateFrom { get; set; }

    public void Update()
    {
        if (UID == Guid.Empty) UID = Guid.NewGuid();
        ;
    }
}

public class Spacing
{
    public Spacing(string projectId, string optionId, string type1, string type2, string space)
    {
        ProjectId = projectId;
        OptionId = optionId;
        Type1 = type1;
        Type2 = type2;
        Space = space;
    }

    public Spacing()
    {
    }

    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string Type1 { get; set; }
    public string Type2 { get; set; }
    public string Space { get; set; } // 1d, -1/2d, 5, 0, etc.
    public float SpaceF { get; set; } // float
}

/// <summary>
///     For storing cable UIDs for those cables which are not able to route so that RouteAll function skips those UID
/// </summary>
public class MyDebug
{
    public Guid UID { get; set; }
    public string? ProjectId { get; set; }
    public string? NoRouteCablesS { get; set; }
}

public class Cable : BaseInfo
{
    public string
        AssociatedTags
    {
        get;
        set;
    } // json list of cable tags that must be laid togather e.g., parallel run cables, trefoil cables

    public string
        ExclusionTags { get; set; } // json list of cable tags that cannot be laid togather, e.g., UPS A and UPS B

    public string Type { get; set; }
    public int PhaseNo { get; set; }
    public double Psize { get; set; }
    public int NeutralNo { get; set; }
    public double Nsize { get; set; }
    public int PENo { get; set; }
    public double PEsize { get; set; }
    public double Uv { get; set; }
    public double Uvo { get; set; }
    public double Uvm { get; set; }
    public string ConductorMaterial { get; set; }
    public string Inslation { get; set; }
    public bool Armour { get; set; }
    public string ArmourMat { get; set; }
    public bool FR { get; set; }
    public bool LSZH { get; set; }
    public bool PbSeath { get; set; }
    public string SpecDescriptionDerived { get; set; }
    public double OD { get; set; } // mm
    public double InnerDia { get; set; }
    public double Rdc { get; set; }
    public double Rac { get; set; }
    public double Xac { get; set; }
    public double UnitRateCalculated { get; set; }
    public double CuFactor { get; set; }
    public double AlFactor { get; set; }
    public double PbFactor { get; set; }
    public double Weight { get; set; }
    public string DrumSizeCatalogue { get; set; }
    public int StdDrumMinLength { get; set; }
    public int StdDrumMaxLength { get; set; }
    public string GlandSize { get; set; }
    public string CableCatalogueTagID { get; set; }
    public string OriginTag { get; set; }

    public List<NearTag> OriginTagMatch { get; set; } =
        []; // if the Origin tag does not exactly match with the equipment/board/load existing tags

    public string OriginFeeder { get; set; }
    public string DestinationTag { get; set; }
    public List<NearTag> DestinationTagMatch { get; set; } = [];

    public string DestinationFeeder { get; set; }

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which OriginVS and DestinationVS values are calculated and vice versa
    public string CoordSystem { get; set; } // common for origin and destination

    public string?
        Origin
    {
        get;
        set;
    } // NE coordinates in Global/Local/UTM/XYZ Coordinate System, non-blank value means not to be generated from board/equipment DB

    public string?
        Destination
    {
        get;
        set;
    } // NE coordinates in Global/Local/UTM/XYZ Coordinate System, non-blank value means not to be generated from board/equipment DB

    public string Purpose { get; set; }
    public string? RouteVectorAutoS { get; set; }
    public string? RouteVectorAutoArrangedS { get; set; }
    public string? RouteVectorManualS { get; set; }
    public double LengthEst { get; set; }
    public double LengthCal { get; set; }
    public double LenMarSource { get; set; }
    public double LenMarDest { get; set; }
    public double LenMardMid { get; set; }
    public double LengthManual { get; set; }
    public string RouteCriteria { get; set; }
    public double BendRadAllowed { get; set; }
    public double BendRadActual { get; set; }
    public string LoadTag { get; set; }
    public string DesignStage { get; set; }
    public string ConstructionStatus { get; set; }
    public string DrumAssignedTag { get; set; }

    public double LengthActualLaid { get; set; }

    // above all are in sequence in the DB stored data sequence
    public string SeqTag { get; set; } // program generated sequence no. used in some function
    public float ODm { get; set; } // m , program generated from OD, not stored in DB
    public Vector3 OriginV { get; set; } // program generated OriginVS, not stored in DB
    public string? OriginVS { get; set; } // Serialised for debug purpose
    public Vector3 DestinationV { get; set; } // program generated from DestinationVS, not stored in DB
    public string? DestinationVS { get; set; } // Serialised for debug purpose
    public List<string>? AutoRouteNodeTags { get; set; } // program generated, not stored in DB
    public string? AutoRouteNodeTagsS { get; set; } // program generated, S is for debug purpose not stored in DB
    public List<Vector3>? RouteVectorAuto { get; set; } // program generated RouteVectorAutoS, not stored in DB
    public List<Vector3>? RouteVectorAutoArranged { get; set; } // program generated RouteVectorAutoS, not stored in DB
    public List<Guid> RouteSegmentUIDAuto { get; set; } // segment UID of corresponding RouteVectorAuto
    public List<Vector3>? RouteVectorManual { get; set; } // program generated RouteVectorManualS, not stored in DB
    public bool Laid { get; set; } // laid status default is false
    public bool Arranged { get; set; } // arranged status default is false
    public string? OriginTagMatchSelect { get; set; } // selected tag matc as the Tag is not matching with the database

    public string?
        DestinationTagMatchSelect { get; set; } // selected tag matc as the Tag is not matching with the database

    public string? JSONRoutePoints { get; set; } // for drawing readily

    public string? Route { get; set; } // Route Assigned through AStar
    //shallow copy https://www.youtube.com/watch?v=hxr3kviGJS4

    public void Update()
    {
        if (UID == Guid.Empty) UID = Guid.NewGuid();
        ;
        // OD in mm
        ODm = (float)(OD / 1000);
        //database is expected to store only Origin, Destination and CoordSystem data
        // Origin/Destination Vector and VS are supposed to be derived based on above
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        OriginTag = OriginTag.Trim();
        DestinationTag = DestinationTag.Trim();
        OriginV = string.IsNullOrEmpty(OriginVS)
            ? new Vector3()
            : JsonSerializer.Deserialize<Vector3>(OriginVS, jsonSerializerOption);
        DestinationV = string.IsNullOrEmpty(DestinationVS)
            ? new Vector3()
            : JsonSerializer.Deserialize<Vector3>(DestinationVS);
        AutoRouteNodeTags = string.IsNullOrEmpty(AutoRouteNodeTagsS)
            ? []
            : JsonSerializer.Deserialize<List<string>>(AutoRouteNodeTagsS, jsonSerializerOption);
        RouteVectorAuto = string.IsNullOrEmpty(RouteVectorAutoS)
            ? []
            : JsonSerializer.Deserialize<List<Vector3>>(RouteVectorAutoS, jsonSerializerOption);
        RouteVectorAutoArranged = string.IsNullOrEmpty(RouteVectorAutoArrangedS)
            ? []
            : JsonSerializer.Deserialize<List<Vector3>>(RouteVectorAutoArrangedS, jsonSerializerOption);
        RouteVectorManual = string.IsNullOrEmpty(RouteVectorManualS)
            ? []
            : JsonSerializer.Deserialize<List<Vector3>>(RouteVectorManualS, jsonSerializerOption);
        RouteSegmentUIDAuto = [];
        SpecDescriptionDerived = string.IsNullOrEmpty(SpecDescriptionDerived)
            ? PhaseNo + "Cx" + Psize + (NeutralNo == 0 ? "" : "+" + NeutralNo + "Nx" + Nsize) +
              (PENo == 0 ? "" : "+" + PENo + "Ex" + PEsize)
            : SpecDescriptionDerived;
        Laid = !string.IsNullOrEmpty(RouteVectorAutoS);
        Arranged = false;
    }

    public void ClearRoute()
    {
        AutoRouteNodeTags?.Clear();
        AutoRouteNodeTagsS = null;
        RouteVectorAuto.Clear();
        RouteVectorAutoArranged.Clear();
        RouteVectorAutoS = null;
        RouteVectorAutoArrangedS = null;
    }

    public object Clone()
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        var cable = (Cable)MemberwiseClone();
        cable.AutoRouteNodeTags = JsonSerializer.Deserialize<List<string>>(
            JsonSerializer.Serialize(cable.AutoRouteNodeTags, jsonSerializerOption), jsonSerializerOption);
        cable.RouteVectorAuto =
            JsonSerializer.Deserialize<List<Vector3>>(
                JsonSerializer.Serialize(cable.RouteVectorAuto, jsonSerializerOption), jsonSerializerOption);
        cable.RouteVectorManual = JsonSerializer.Deserialize<List<Vector3>>(
            JsonSerializer.Serialize(cable.RouteVectorManual, jsonSerializerOption), jsonSerializerOption);

        //List<string> vs = new ();

        ////this.RouteNodetagAuto.ForEach(a => { vs.Add(a); });
        //if (this.RouteNodetagAuto != undefined) { this.RouteNodetagAuto.ForEach(a => { vs.Add(a); }); }
        //cable.RouteNodetagAuto = vs;
        //List<Vector3> vv = new ();
        //this.RouteVectorAuto.ForEach(a => { vv.Add(a); });
        //cable.RouteVectorAuto = vv;
        //List<Vector3> vv1 = new ();
        //this.RouteVectorManual.ForEach(a => { vv1.Add(a); });
        //cable.RouteVectorManual = vv1;
        return cable;
    }
}

public class Board : ICloneable
{
    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string OptionId { get; set; }
    public string Tag { get; set; }
    public double Width { get; set; } // width in m
    public double Depth { get; set; } // depth in m

    public double Height { get; set; } // width in m

    // coordinate system "GLOBAL"/ "LOCAL" / "UTM"/"XYZ"  based on which CentrePointS is stored and accordingly CentrePointV is calculated
    public string CoordSystem { get; set; }
    public string CentrePointS { get; set; } // Centre of the Board in E 6494248.4 N 6243199.84 U 21150.8 mm
    public Vector3 CentrePoint { get; set; } // Centre point Vector 
    public int Panels { get; set; } // no of vertical panels
    public Vector3 Face { get; set; }
    public string FaceS { get; set; } // E or W or S or N
    public Vector3 Point { get; set; } // left side at base towards face anchor point
    public List<string> PanelTag { get; set; } // Tag of panels/cubicles
    public string PanelTagS { get; set; }
    public List<double> PanelWidth { get; set; } // width of each of the verticle panels in m or mm
    public string PanelWidthS { get; set; }
    public List<Vector3> PanelPosition { get; set; } // absolute vector position of corresponding panels/cubicles
    public string PanelPositionS { get; set; } // for debug only
    public string ColourString { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime UpdatedDateTime { get; set; }
    public string UpdatedDateTimeString { get; set; }


    //shallow copy https://www.youtube.com/watch?v=hxr3kviGJS4
    public object Clone()
    {
        var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
        var board = (Board)MemberwiseClone();
        board.PanelTag =
            JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(board.PanelTag, jsonSerializerOption),
                jsonSerializerOption);
        board.PanelWidth =
            JsonSerializer.Deserialize<List<double>>(JsonSerializer.Serialize(board.PanelWidth, jsonSerializerOption),
                jsonSerializerOption);
        return board;
    }

    public void UpdateDateTime()
    {
        UpdatedBy = "KP";
        UpdatedDateTime = DateTime.UtcNow;
    }
    
}

public class Equipment : BaseInfo, IBaseMethod
{
    public string AliasTagsS { get; set; }

    public string CentrePointS { get; set; } // Centre of the Equipment in E 6494248.4 N 6243199.84 U 21150.8 mm as in 3D Nevis

    public string CoordSystem { get; set; } // coordinate system "GLOBAL"/ "LOCAL" / "UTM"        
    public string FaceS { get; set; } // E or W or S or N
    public string Type { get; set; } //  Board , LCS, DB, Motor/Load
    public string ColourString { get; set; }
    public float Width { get; set; } // width in m
    public float Depth { get; set; } // depth in m
    public float Height { get; set; } // width in m
    public string UpdatedBy { get; set; }
    public DateTime UpdatedDateTime { get; set; }
    public Vector3 CentrePoint { get; set; }
    public Vector3 Face { get; set; }
    public List<string> AliasTags { get; set; }

    public void UpdateDateTime()
    {
        UpdatedBy = "KP";
        UpdatedDateTime = DateTime.UtcNow;
    }
    public void Update()
    {
        // TODO: Add the actual implementation logic for updating an Equipment object here or in a separate method.
    }
}

//below class is used in "FindLocationOfTagLevenshtein" function for finding the neasest match tag data
[Serializable]
public class NearTag
{
    public NearTag()
    {
    }

    public NearTag(Guid uid, string tag, string type, string locationS, Vector3 location)
    {
        UID = Guid.NewGuid();
        NearTagUID = uid;
        Tag = tag;
        Type = type;
        LocationS = locationS;
        Location = location;
    }

    public Guid UID { get; set; }
    public Guid NearTagUID { get; set; }
    public string? Tag { get; set; }
    public string? Type { get; set; }
    public string? LocationS { get; set; }
    public Vector3 Location { get; set; }
}

public class TagAlias
{
    public Guid UID { get; set; }
    public string ProjectId { get; set; }
    public string Tag { get; set; }
    public string AliasTagsS { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime UpdatedDateTime { get; set; }
    public List<string> AliasTags { get; set; }

    public void Update()
    {
        try
        {
            var jsonSerializerOption = new JsonSerializerOptions { IncludeFields = true };
            AliasTags = JsonSerializer.Deserialize<List<string>>(AliasTagsS, jsonSerializerOption);
        }
        catch (Exception e)
        {
            AliasTags = new List<string> { Tag };
            var b = e.ToString(); // some invalid or null entry as AliasTagsS
        }
    }
}

public class Accessory
    {
        public Accessory()
        {
            SegI = new List<int>();
            SegGUID = new List<Guid>();
            End = new List<int>();
            Update();
        }

        public Accessory(Vector3 p)
        {
            P = p;
            SegI = new List<int>();
            SegGUID = new List<Guid>();
            End = new List<int>();
            Update();
        }

        public Vector3 P { get; set; }
        public List<Guid> SegGUID { get; set; }
        public string SegGUIDJSON { get; set; }
        public List<int> End { get; set; }
        public List<int> SegI { get; set; } // segment seq no. for debug purpose
        public string SegIJSON { get; set; } // for debug purpose
        public string EndJSON { get; set; }

        public void Update()
        {
            SegGUIDJSON = JsonSerializer.Serialize(SegGUID);
            EndJSON = JsonSerializer.Serialize(End);
            SegIJSON = JsonSerializer.Serialize(SegI);
            ;
        }
    }

    public class NearUID
    {
        public Guid Guid1 { get; set; }
        public Guid Guid2 { get; set; }
        public bool Parallel { get; set; }
        public bool Colinear { get; set; }
    }

    public class Seg4Aac
    {
        // copy temporary segmentfor the purpose to generate accessories
        public Guid UID { get; set; } // same as original segment UID
        public string RecordId { get; set; }
        public string Tag { get; set; }
        public Guid ParentUID { get; set; } // for break away segments , the UID of the parent segment
        public Vector3 End1 { get; set; } // same as original seg
        public Vector3 End2 { get; set; } // same as original seg
        public float Width { get; set; } // width in m // same as original seg
        public Vector3 Face { get; set; } // same as original seg
        public float Height { get; set; } // height in m // same as original seg
        public Vector3 Po1 { get; set; } // Bend point with the other segment near End1
        public Vector3 Po2 { get; set; } // Bend point with the other segment near End2
        public double End1Po1 { get; set; }
        public double End1Po2 { get; set; }
        public double End1End2 { get; set; }
        public double End2Po1 { get; set; }
        public double End2Po2 { get; set; }
        public int I { get; set; } // sequence for debugging purpose

        public void DistanceUpdate()
        {
            End1Po1 = Vector3.Distance(End1, Po1);
            End1Po2 = Vector3.Distance(End1, Po2);
            End1End2 = Vector3.Distance(End1, End2);
            End2Po1 = Vector3.Distance(End2, Po1);
            End2Po2 = Vector3.Distance(End2, Po2);
        }
    }

    public class NodePath
    {
        public String Tag { get; set; }
        public Vector3 Point { get; set; }
        public List<String> Connections { get; set; }
        public float Width { get; set; }
        public float G { get; set; } // the cost of the path from the start node to the current node
        public NodePath? Parent { get; set; } // parent property, to reconstruct path.

        public NodePath(String tag, Vector3 point, List<String> connections, float width)
        {
            Tag = tag;
            Point = point;
            Connections = connections;
            Width = width;
            G = float.MaxValue; // Initialize G to infinity
        }
    }



//shallow copy https://www.youtube.com/watch?v=hxr3kviGJS4