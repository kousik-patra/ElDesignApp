using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ElDesignApp.Data;

using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using ElDesignApp.Models;
using Microsoft.JSInterop;
using ElDesignApp.Services;
using ElDesignApp.Services.Global;



public class Draw
{
    // Event for UI updates
    public event Action<SceneMessage>? OnSceneMessage;

    // Helper method to send messages
    private void SendMessage(SceneMessage message)
    {
        Console.WriteLine($"Draw.SendMessage: {message.Text}");
        Console.WriteLine($"Draw.SendMessage: OnSceneMessage has subscribers: {OnSceneMessage != null}");
    
        if (OnSceneMessage != null)
        {
            Console.WriteLine($"Draw.SendMessage: Invoking event...");
            OnSceneMessage.Invoke(message);
            Console.WriteLine($"Draw.SendMessage: Event invoked successfully");
        }
        else
        {
            Console.WriteLine($"Draw.SendMessage: WARNING - No subscribers to OnSceneMessage!");
        }
    }
    
    
    private readonly ILayoutFunctionService _layoutFunction; 
    private IGlobalDataService _globalData; 

    // Inject ILayoutFunctionService into the constructor
    public Draw(ILayoutFunctionService layoutFunction,  IGlobalDataService globalData)
    {
        _layoutFunction = layoutFunction;
        _globalData = globalData;
    }
    
    [JSInvokable("SaveSceneInfo")]
    public void SaveSceneInfo(string? sceneInfoJson)
    {
        if (string.IsNullOrEmpty(sceneInfoJson)) return;
    
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        
            // Deserialize to the DTO
            var jsonData = JsonSerializer.Deserialize<SceneInfoJson>(sceneInfoJson, options);
        
            if (jsonData == null) return;
        
            var rendererWidth = (int)jsonData.RendererWidth;
            var rendererHeight = (int)jsonData.RendererHeight;
            var cameraPosition = jsonData.CameraPosition?.ToVector3() ?? Vector3.Zero;
            var cameraRotation = jsonData.CameraRotation?.ToVector3() ?? Vector3.Zero;
        
            var text = $"Renderer Width: {rendererWidth}, Renderer Height: {rendererHeight}, " +
                       $"Camera position: {cameraPosition}, Camera rotation: {cameraRotation}";

            // Map to your existing SceneInfo class
            _globalData.sceneDataCurrent.SceneJSON = sceneInfoJson;
            _globalData.sceneDataCurrent.RendererWidth = jsonData.RendererWidth;
            _globalData.sceneDataCurrent.RendererHeight = jsonData.RendererHeight;
            _globalData.sceneDataCurrent.CameraPosition = cameraPosition;
            _globalData.sceneDataCurrent.CameraRotation = cameraRotation;
            _globalData.sceneDataCurrent.RendererWidthPX = $"{rendererWidth}px";
            _globalData.sceneDataCurrent.RendererHeightPX = $"{rendererHeight}px";

            UpdateSceneData();
               
            Debug.WriteLine($"Draw: SaveSceneInfo: Scene Info changed to {text}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    [JSInvokable("MouseClick")]
    public void MouseClick(float posx, float posy, float posz, float eventclientX, float eventclientY,
        float eventpageX, float eventpageY, float eventoffsetX, float eventoffsetY,
        float eventlayerX, float eventlayerY, float eventx, float eventy, float mousex, float mousey,
        float linePositionx, float linePositiony, float linePositionz)
    {
        Debug.WriteLine($"Draw: Clicked Point: pos: [{posx},{posy}] " +
                        $"client: [{eventclientX},{eventclientY}] page: [{eventpageX},{eventpageY}]  " +
                        $"offset: [{eventoffsetX},{eventoffsetY}]  layer: [{eventlayerX},{eventlayerY}] xy: [{eventx},{eventy}]  " +
                        $"mouse: [{mousex},{mousey}] linePosition: [{linePositionx},{linePositiony},{linePositionz}] ");

        var locallocation =
            _layoutFunction.VectorToLocation(new Vector3(linePositionx, linePositiony, linePositionz), "LOCAL");
        var globallocation =
            _layoutFunction.VectorToLocation(new Vector3(linePositionx, linePositiony, linePositionz), "GLOBAL");
        
        _globalData.sceneDataCurrent.Display[0] = $"Clicked at global [{globallocation}] local [{locallocation}]";
        
        //
        var MousePosition = $"[{linePositionx}, {linePositiony}, {linePositionz}]";
        //MousePosition = $"Server console (component): Clicked pos: {posx} {posy}  {posz}  LinePosition {linePositionx} {linePositiony} {linePositionz}";
        //
        var newClickedPoint = new ClickedPoint
        {
            PosX = posx,
            PosY = posy,
            PosZ = posz,
            MouseX = mousex,
            MouseY = mousey,
            EventX = eventx,
            EventY = eventy,
            EventclientX = eventclientX,
            EventclientY = eventclientY,
            EventpageX = eventpageX,
            EventpageY = eventpageY,
            EventoffsetX = eventoffsetX,
            EventoffsetY = eventoffsetY,
            EventlayerX = eventlayerX,
            EventlayerY = eventlayerY,
            LinePositionX = linePositionx,
            LinePositionY = linePositiony,
            LinePositionZ = linePositionz
        };
        var enu = _layoutFunction.XY2EN(new Vector3(linePositionx, linePositiony, linePositionz));
        newClickedPoint.E = enu.E;
        newClickedPoint.N = enu.N;
        newClickedPoint.U = enu.U;
        //
        var clickPoints = _globalData.sceneDataCurrent.ClickPoints;
        clickPoints.Insert(0, newClickedPoint);
        // retain only 4 clicked coordinates
        if (clickPoints.Count > 4) clickPoints.RemoveAt(clickPoints.Count - 1);
        ;

        Debug.WriteLine($"Clicked Point: pos: [{posx},{posy}] " +
                        $"client: [{eventclientX},{eventclientY}] page: [{eventpageX},{eventpageY}]  " +
                        $"offset: [{eventoffsetX},{eventoffsetY}]  layer: [{eventlayerX},{eventlayerY}] xy: [{eventx},{eventy}]  " +
                        $"mouse: [{mousex},{mousey}] linePosition: [{linePositionx},{linePositiony},{linePositionz}] ");
        //
        if (clickPoints.Count > 1)
        {
            // middle point
            var me = Math.Round((clickPoints[0].E + clickPoints[1].E) / 2, 3);
            var mn = Math.Round((clickPoints[0].N + clickPoints[1].N) / 2, 3);
            var mu = Math.Round((clickPoints[0].U + clickPoints[1].U) / 2, 3);
            // delta
            var de = Math.Abs(Math.Round(clickPoints[0].E - clickPoints[1].E, 3));
            var dn = Math.Abs(Math.Round(clickPoints[0].N - clickPoints[1].N, 3));
            var du = Math.Abs(Math.Round(clickPoints[0].U - clickPoints[1].U, 3));
            var u = Math.Round(Math.Min(de, dn), 2);
            // always on floor
            var location = $"E:{me} N:{mn} U:{mu}";
            var locationSize = $"E:{me} N:{mn} U:{mu + u / 2} LE:{de} LN:{dn} LU:{u}"; // change 1 laterMotorData
            _globalData.LoadListComponent_locationString = locationSize;
            _globalData.SegmentPage_locationString =
                $"E:{me} N:{mn} U:{mu + u / 2} LE:{de} LN:{dn} LU:{u}"; // change 1 later
            //
            var xew = _globalData.SelectedProject.XEW;
            var measurement = $"ΔE({(xew ? "X" : "Y")}-axis): {clickPoints[0].E - clickPoints[1].E: 0.000}, "
                              + $"ΔN({(xew ? "Y" : "X")}-axis): {clickPoints[0].N - clickPoints[1].N:0.000}, "
                              + $"ΔU(Z-axis): {clickPoints[0].U - clickPoints[1].U:0.000)}, "
                              + $"ΔL: {Math.Round(Math.Sqrt(de * de + dn * dn + du * du), 3)}";
            Debug.WriteLine(measurement);
            //MousePosition = $"pos: {posx} {posy}";
            //PositionEN.Insert(0, MyFunction.XY2EN(new XY(Math.Round(posx, 3), Math.Round(posy, 3))));
            //if (PositionEN.Count > 2) PositionEN.RemoveAt(PositionEN.Count - 1); // remove last, i.e., all records except first 2
            //var me = Math.Round((PositionEN[0].E + PositionEN[1].E) / 2, 3); // middle point
            //var mn = Math.Round((PositionEN[0].N + PositionEN[1].N) / 2, 3);
            //var de = Math.Abs(Math.Round((PositionEN[0].E - PositionEN[1].E), 3));    // delta
            //var dn = Math.Abs(Math.Round((PositionEN[0].N - PositionEN[1].N), 3));
            //var u = Math.Round(Math.Min(de, dn), 2);
            //SegmentPage.locationString = "E:" + me + " N:" + mn + " U:" + u / 2 + " LE:" + de + " LN:" + dn + " LU:" + u; // change 1 later
            _globalData.sceneDataCurrent.Display[1] = measurement;
            _globalData.sceneDataCurrent.Display[2] = locationSize;
        }
        //
        UpdateSceneData();
    }


    [JSInvokable("UpdateCastObject")]
    public void UpdateCastObject(string JSONCastObjectUIDs, string JsonCastObjectTags, string objTag, float X, float Y,
        float Z, string hiddenObjTag, float Xh, float Yh, float Zh)
    {
        Debug.WriteLine($"Draw: JSONCastObjectUIDs: {JSONCastObjectUIDs}, JsonCastObjectTags: {JsonCastObjectTags}, " +
                        $"objTag: {objTag}, X: {X}, Y: {Y}, Z{Z}, hiddenObjTag: {hiddenObjTag}, Xh{Xh}, Yh: {Yh}, Zh: {Zh} ");
    }


    private void UpdateSceneData()
    {
        var clickedPoints =
            JsonSerializer.Deserialize<List<ClickedPoint>>(
                JsonSerializer.Serialize(_globalData.sceneDataCurrent.ClickPoints));
        var display =
            JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(_globalData.sceneDataCurrent.Display));
        var cameraPosition =
            JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(_globalData.sceneDataCurrent.CameraPosition));
        var CameraRotation =
            JsonSerializer.Deserialize<Vector3>(JsonSerializer.Serialize(_globalData.sceneDataCurrent.CameraPosition));

        switch (_globalData.sceneCurrent)
        {
            case "Cable":
                _globalData.sceneDataCable.ClickPoints = clickedPoints;
                _globalData.sceneDataCable.Display = display;
                _globalData.sceneDataCable.CameraPosition = cameraPosition;
                _globalData.sceneDataCable.CameraRotation = CameraRotation;
                _globalData.sceneDataCable.RendererHeight = _globalData.sceneDataCurrent.RendererHeight;
                _globalData.sceneDataCable.RendererWidth = _globalData.sceneDataCurrent.RendererWidth;
                _globalData.sceneDataCable.RendererHeightPX = _globalData.sceneDataCurrent.RendererHeightPX;
                _globalData.sceneDataCable.RendererWidthPX = _globalData.sceneDataCurrent.RendererWidthPX;
                break;
            case "Segment":
                _globalData.sceneDataSegment.ClickPoints = clickedPoints;
                _globalData.sceneDataSegment.Display = display;
                _globalData.sceneDataSegment.CameraPosition = cameraPosition;
                _globalData.sceneDataSegment.CameraRotation = CameraRotation;
                _globalData.sceneDataSegment.RendererHeight = _globalData.sceneDataCurrent.RendererHeight;
                _globalData.sceneDataSegment.RendererWidth = _globalData.sceneDataCurrent.RendererWidth;
                _globalData.sceneDataSegment.RendererHeightPX = _globalData.sceneDataCurrent.RendererHeightPX;
                _globalData.sceneDataSegment.RendererWidthPX = _globalData.sceneDataCurrent.RendererWidthPX;
                break;
            case "Load":
            default:
                _globalData.sceneDataLoad.ClickPoints = clickedPoints;
                _globalData.sceneDataLoad.Display = display;
                _globalData.sceneDataLoad.CameraPosition = cameraPosition;
                _globalData.sceneDataLoad.CameraRotation = CameraRotation;
                _globalData.sceneDataLoad.RendererHeight = _globalData.sceneDataCurrent.RendererHeight;
                _globalData.sceneDataLoad.RendererWidth = _globalData.sceneDataCurrent.RendererWidth;
                _globalData.sceneDataLoad.RendererHeightPX = _globalData.sceneDataCurrent.RendererHeightPX;
                _globalData.sceneDataLoad.RendererWidthPX = _globalData.sceneDataCurrent.RendererWidthPX;
                break;
        }
    }


    // for SLD
    [JSInvokable("TagMoveUpdate")]
    public void TagMoveUpdate(string type, string tag, int x, int y)
    {
        Debug.WriteLine($"Server Side: {type} : Tag: {tag} moved to: ({x}, {y})");
    }
    
    [JSInvokable("UpdateRefPoints")]
    public void UpdateRefPoints(string refPointsJson)
    {
        Debug.WriteLine($"Draw: UpdateRefPoints: {refPointsJson}");
    
        try
        {
            _globalData.RefPoints?.Clear();
        
            if (string.IsNullOrWhiteSpace(refPointsJson) || refPointsJson == "[]")
            {
                return;
            }

            // Parse the JSON array of arrays
            var points = JsonSerializer.Deserialize<float[][]>(refPointsJson);
        
            if (points == null) return;

            foreach (var point in points)
            {
                if (point.Length >= 2)
                {
                    // Store only position (X, Y), Z = 0
                    _globalData.RefPoints?.Add(new Vector3(point[0], point[1], 0));
                }
            }
        
            Debug.WriteLine($"Parsed {_globalData.RefPoints?.Count} reference points");
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"UpdateRefPoints error: {ex.Message}");
        }
    }

    
    [JSInvokable("OnSceneClick")]
    public void OnSceneClick(string clickDataJson)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var clickData = JsonSerializer.Deserialize<SceneClickData>(clickDataJson, options);
            
            if (clickData == null) return;
            
            Console.WriteLine($"Draw: Scene Click: Type={clickData.ClickType}, " +
                             $"World=({clickData.WorldX:F2}, {clickData.WorldY:F2}, {clickData.WorldZ:F2}), " +
                             $"Object={clickData.ObjectTag ?? "none"}");
            
            switch (clickData.ClickType)
            {
                case "single":
                    HandleSingleClick(clickData);
                    break;
                case "double":
                    HandleDoubleClick(clickData);
                    break;
                case "shift":
                    HandleShiftClick(clickData);
                    break;
                case "ctrl":
                    HandleCtrlClick(clickData);
                    break;
                case "pinPlaced":  // handle pin placement
                    HandlePinPlaced(clickData);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"OnSceneClick error: {e.Message}");
        }
    }
    
    private void HandleSingleClick(SceneClickData clickData)
    {
        var text = $"Draw: Single Click at ({clickData.WorldX:F2}, {clickData.WorldY:F2})";
        Console.WriteLine(text);
        
        var message = SceneMessage.Coordinates(
            clickData.WorldX, 
            clickData.WorldY, 
            clickData.ObjectTag
        );
        SendMessage(message);
        
        if (!string.IsNullOrEmpty(clickData.ObjectTag))
        {
            Console.WriteLine($"Draw: Clicked on object: {clickData.ObjectTag}");
            SendMessage(SceneMessage.Info($"Object Type: {GetObjectType(clickData.ObjectTag)}"));
            // TODO: Select object, show properties, etc.
        }
        
        // Store click point if needed
        // _globalData.sceneDataCurrent.ClickPoints.Add(new ClickedPoint { ... });
    }
    
    private void HandleDoubleClick(SceneClickData clickData)
    {
        Console.WriteLine($"Draw: Double Click at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");
        
        SendMessage(SceneMessage.Coordinates(clickData.WorldX, clickData.WorldY, clickData.ObjectTag));
        
        if (!string.IsNullOrEmpty(clickData.ObjectTag))
        {
            Console.WriteLine($"Draw: Double-clicked on object: {clickData.ObjectTag}");
            SendMessage(SceneMessage.Info($"Double-clicked: {clickData.ObjectTag} - Opening details..."));

            // TODO: Open edit dialog, zoom to object, etc.
        }
    }
    
    private void HandleShiftClick(SceneClickData clickData)
    {
        Console.WriteLine($"Shift+Click at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");
        
        if (!string.IsNullOrEmpty(clickData.ObjectTag))
        {
            Console.WriteLine($"  Shift-clicked on object: {clickData.ObjectTag}");
            SendMessage(SceneMessage.Info($"Added to selection: {clickData.ObjectTag}"));
            // TODO: Add to selection, extend selection, etc.
        }
        else
        {
            SendMessage(SceneMessage.Coordinates(clickData.WorldX, clickData.WorldY));
        }
    }
    
    private void HandleCtrlClick(SceneClickData clickData)
    {
        Console.WriteLine($"Ctrl+Click at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");
        
        if (!string.IsNullOrEmpty(clickData.ObjectTag))
        {
            var text = $"  Ctrl-clicked on object: {clickData.ObjectTag}";
            Console.WriteLine(text);
            // TODO: Toggle selection, add/remove from multi-select, etc.
        }
    }
    
    private void HandlePinPlaced(SceneClickData clickData)
    {
        var tag = clickData.ObjectTag;
        var x = clickData.WorldX;
        var y = clickData.WorldY;
        var z = clickData.WorldZ;
    
        Console.WriteLine($"Pin placed: {tag} at ({x:F2}, {y:F2}, {z:F2})");
    
        // Calculate E/N if needed
        var enu = _layoutFunction.XY2EN(new Vector3(x, y, z));
    
        // Send message to UI
        var message = SceneMessage.Info($"📍 Placed '{tag}' at E:{enu.E:F2}, N:{enu.N:F2}");
        message.WorldX = x;
        message.WorldY = y;
        message.ObjectTag = tag;
        SendMessage(message);
    
        // Store in global data if needed
        // _globalData.RefPoints?.Add(new Vector3(x, y, z));
    }
    
    
    
    
    private string GetObjectType(string tag)
    {
        // Parse your tag format to get type
        if (tag.StartsWith("EQP_")) return "Equipment";
        if (tag.StartsWith("PIPE_")) return "Pipe";
        if (tag.StartsWith("STR_")) return "Structure";
        if (tag.StartsWith("PLT_")) return "PlotPlan";
        return "Unknown";
    }
    
    
}


public class SceneClickData
{
    [JsonPropertyName("eventType")]
    public string ClickType { get; set; } = "";  // "single", "double", "shift", "ctrl"
    public float ScreenX { get; set; }
    public float ScreenY { get; set; }
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float WorldZ { get; set; }
    public string? ObjectTag { get; set; }
    public int? ObjectLayer { get; set; }
    public Vector3Json? IntersectPoint { get; set; }
    public int IntersectCount { get; set; }
    public bool ShiftKey { get; set; }
    public bool CtrlKey { get; set; }
    public bool AltKey { get; set; }
}