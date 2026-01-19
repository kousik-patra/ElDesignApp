using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
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
    private readonly ILayoutFunctionService _layoutFunction; 
    private readonly IGlobalDataService _globalData; 
    
    // ===== Segment Selection State =====
    private HashSet<string> _selectedSegmentTags = new();
    
    /// <summary>
    /// Event fired when segment selection changes
    /// </summary>
    public event Action<IReadOnlyList<string>>? OnSegmentSelectionChanged;
    
    /// <summary>
    /// Get currently selected segment tags
    /// </summary>
    public IReadOnlySet<string> SelectedSegmentTags => _selectedSegmentTags;
    
    
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
    
    


    // Inject ILayoutFunctionService into the constructor
    public Draw(ILayoutFunctionService layoutFunction,  IGlobalDataService globalData)
    {
        _layoutFunction = layoutFunction;
        _globalData = globalData;
    }
    
    [JSInvokable("SaveSceneInfo")]
    public void SaveSceneInfo(string? sceneInfoJson)
    {
        return;
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

    
    
    
    [JSInvokable("OnWindowResize")]
    public void OnWindowResize(double rendererWidth, double rendererHeight)  // Use double, not float!
    {
        try
        {
            Console.WriteLine($"Draw.cs (OnWindowResize): resized to {rendererWidth}, {rendererHeight}");
            _globalData.sceneDataCurrent.RendererWidth = (float)rendererWidth;
            _globalData.sceneDataCurrent.RendererHeight = (float)rendererHeight;
            var message = SceneMessage.RendererSize(rendererWidth, rendererHeight);
            SendMessage(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Draw.cs (OnWindowResize) ERROR: {ex.Message}");
            // Don't rethrow - this would break the dotNetRef
        }
    }
    
    
[JSInvokable("OnSceneClick")]
public void OnSceneClick(string clickDataJson)
{
    try
    {
        Console.WriteLine($"Draw.OnSceneClick received: {clickDataJson?.Substring(0, Math.Min(500, clickDataJson?.Length ?? 0))}...");
        
        if (string.IsNullOrEmpty(clickDataJson))
        {
            Console.WriteLine("OnSceneClick: Received null or empty JSON");
            return;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Important for JS interop!
        };
        
        SceneClickData? clickData;
        try
        {
            clickData = JsonSerializer.Deserialize<SceneClickData>(clickDataJson, options);
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
            Console.WriteLine($"JSON Path: {jsonEx.Path}");
            Console.WriteLine($"Raw JSON: {clickDataJson}");
            return;
        }
        
        if (clickData == null)
        {
            Console.WriteLine("OnSceneClick: Deserialized to null");
            return;
        }
        
        Console.WriteLine($"Draw.OnSceneClick: Type={clickData.ClickType}, " +
                         $"World=({clickData.WorldX:F2}, {clickData.WorldY:F2}, {clickData.WorldZ:F2}), " +
                         $"Object={clickData.ObjectTag ?? "none"}, " +
                         $"Segments={clickData.IntersectedSegments?.Count ?? 0}");
        
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
            case "pinPlaced":
                HandlePinPlaced(clickData);
                break;
            default:
                Console.WriteLine($"Unknown click type: {clickData.ClickType}");
                break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"OnSceneClick EXCEPTION: {e.GetType().Name}: {e.Message}");
        Console.WriteLine($"Stack trace: {e.StackTrace}");
    }
}
    
/// <summary>
/// Handle single click - select single segment or show coordinates
/// Includes system coordinates calculated from intersection point
/// </summary>
private void HandleSingleClick(SceneClickData clickData)
{
    try
    {
        Console.WriteLine($"Draw.HandleSingleClick at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");

        // Clear previous selection
        _selectedSegmentTags.Clear();

        // Determine the coordinate point to use:
        // If we hit a segment, use the actual intersection point
        // Otherwise, use the world position (on Z=0 plane)
        float coordX = clickData.WorldX;
        float coordY = clickData.WorldY;
        float coordZ = clickData.WorldZ;
        
        // Safely get primary tag and intersection point
        string? primaryTag = null;
        IntersectPoint? intersectPoint = null;
        
        try
        {
            // Check if we clicked on a segment
            primaryTag = clickData.GetPrimaryTag();
            intersectPoint = clickData.IntersectedSegments?.FirstOrDefault()?.Point;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting segment info: {ex.Message}");
        }

        if (intersectPoint != null)
        {
            // Use actual intersection point on the segment
            coordX = intersectPoint.X;
            coordY = intersectPoint.Y;
            coordZ = intersectPoint.Z;
        }

        // Build coordinate list from all systems using the determined point
        var systemCoordinates = new List<SystemCoordinate>();

        var coordinateSystemJson = _globalData.SelectedProject?.CoordinateSystemJson;
        if (!string.IsNullOrEmpty(coordinateSystemJson))
        {
            try
            {
                var coordinateManager = new CoordinateSystemManager();
                var dtos = JsonSerializer.Deserialize<List<CoordinateSystemDto>>(coordinateSystemJson);

                if (dtos != null && dtos.Count > 0)
                {
                    coordinateManager.ImportAll(dtos);

                    // Calculate coordinates from intersection point (or world position)
                    systemCoordinates = coordinateManager.GetAllSystemCoordinates(coordX, coordY);

                    // Log all coordinates to console
                    Console.WriteLine($"  Scene: X={coordX:F3}, Y={coordY:F3}, Z={coordZ:F3}");
                    foreach (var sc in systemCoordinates)
                    {
                        Console.WriteLine($"  {sc.SystemName}: E={sc.E:F3}, N={sc.N:F3} {sc.Unit} [XEW={sc.XEW}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing coordinate systems: {ex.Message}");
            }
        }

        // Create and send message
        SceneMessage message;

        if (!string.IsNullOrEmpty(primaryTag))
        {
            // Clicked on a segment - add to selection
            _selectedSegmentTags.Add(primaryTag);

            // Create message with object tag AND system coordinates
            message = SceneMessage.ObjectSelectedWithCoordinates(
                primaryTag,
                coordX,
                coordY,
                coordZ,
                systemCoordinates
            );

            // Update Z coordinate (Coordinates method doesn't set it)
            message.WorldZ = coordZ;

            Console.WriteLine($"  Selected segment: {primaryTag}");
        }
        else
        {
            // Clicked on empty space - just coordinates
            message = SceneMessage.Coordinates(
                coordX,
                coordY,
                null,
                systemCoordinates
            );
        }

        SendMessage(message);
        NotifySelectionChanged();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HandleSingleClick EXCEPTION: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
    }
}
    
/// <summary>
/// Handle double-click - select and zoom/focus
/// </summary>
private void HandleDoubleClick(SceneClickData clickData)
{
    Console.WriteLine($"Draw.HandleDoubleClick at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");
        
    var primaryTag = clickData.GetPrimaryTag();
        
    if (!string.IsNullOrEmpty(primaryTag))
    {
        // Select this segment
        _selectedSegmentTags.Clear();
        _selectedSegmentTags.Add(primaryTag);
            
        var intersectPoint = clickData.IntersectedSegments?.FirstOrDefault()?.Point;
            
        SendMessage(SceneMessage.ObjectSelected(
            primaryTag,
            $"Double-clicked: {primaryTag} - Opening details...",
            intersectPoint?.X,
            intersectPoint?.Y,
            intersectPoint?.Z
        ));
            
        NotifySelectionChanged();
            
        // TODO: Trigger zoom-to-object, open detail panel, etc.
        Console.WriteLine($"  Double-clicked on: {primaryTag}");
    }
    else
    {
        SendCoordinatesMessage(clickData);
    }
}
    
    /// <summary>
    /// Handle Shift+Click - add to selection (extend)
    /// </summary>
    private void HandleShiftClick(SceneClickData clickData)
    {
        Console.WriteLine($"Draw.HandleShiftClick at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");
        
        var primaryTag = clickData.GetPrimaryTag();
        
        if (string.IsNullOrEmpty(primaryTag))
        {
            SendCoordinatesMessage(clickData);
            return;
        }
        
        // Add to selection (no toggle - shift always adds)
        if (!_selectedSegmentTags.Contains(primaryTag))
        {
            _selectedSegmentTags.Add(primaryTag);
            Console.WriteLine($"  Extended selection with: {primaryTag}");
        }
        
        SendMessage(SceneMessage.ObjectsSelected(_selectedSegmentTags));
        NotifySelectionChanged();
    }
    
    /// <summary>
    /// Handle Ctrl+Click - toggle segment in selection (multi-select)
    /// </summary>
    private void HandleCtrlClick(SceneClickData clickData)
    {
        Console.WriteLine($"Draw.HandleCtrlClick at ({clickData.WorldX:F2}, {clickData.WorldY:F2})");
        
        var primaryTag = clickData.GetPrimaryTag();
        
        if (string.IsNullOrEmpty(primaryTag))
        {
            SendCoordinatesMessage(clickData);
            return;
        }
        
        // Toggle selection
        if (_selectedSegmentTags.Contains(primaryTag))
        {
            _selectedSegmentTags.Remove(primaryTag);
            Console.WriteLine($"  Deselected: {primaryTag}");
            SendMessage(SceneMessage.Info($"Removed from selection: {primaryTag}"));
        }
        else
        {
            _selectedSegmentTags.Add(primaryTag);
            Console.WriteLine($"  Added to selection: {primaryTag}");
            SendMessage(SceneMessage.Info($"Added to selection: {primaryTag}"));
        }
        
        // Send updated selection message
        SendMessage(SceneMessage.ObjectsSelected(_selectedSegmentTags));
        NotifySelectionChanged();
    }
    
    private void HandlePinPlaced(SceneClickData clickData)
    {
        var tag = clickData.ObjectTag;
        var x = clickData.WorldX;
        var y = clickData.WorldY;
        var z = clickData.WorldZ;
    
        Console.WriteLine($"Draw.cs (HandlePinPlaced) Pin placed: {tag} at ({x:F2}, {y:F2}, {z:F2})");
        var message = SceneMessage.Info($"📍 Placed '{tag}' at X:{x:F2}, Y:{y:F2}");
    
        // Calculate E/N if needed
        var enu = _layoutFunction.XY2EN(new Vector3(x, y, z));
    
        // Send message to UI
        //var message = SceneMessage.Info($"📍 Placed '{tag}' at E:{enu.E:F2}, N:{enu.N:F2}");
        message.WorldX = x;
        message.WorldY = y;
        message.ObjectTag = tag;
        SendMessage(message);
    
        // Store in global data if needed
        // _globalData.RefPoints?.Add(new Vector3(x, y, z));
    }
    
    /// <summary>
    /// Clear all segment selections
    /// </summary>
    public void ClearSelection()
    {
        _selectedSegmentTags.Clear();
        SendMessage(SceneMessage.Info("Selection cleared"));
        NotifySelectionChanged();
    }
    
    /// <summary>
    /// Select multiple segments programmatically
    /// </summary>
    public void SelectSegments(IEnumerable<string> tags)
    {
        _selectedSegmentTags.Clear();
        foreach (var tag in tags)
        {
            _selectedSegmentTags.Add(tag);
        }
        
        SendMessage(SceneMessage.ObjectsSelected(_selectedSegmentTags));
        NotifySelectionChanged();
    }
    
    /// <summary>
    /// Notify listeners that selection changed
    /// </summary>
    private void NotifySelectionChanged()
    {
        OnSegmentSelectionChanged?.Invoke(_selectedSegmentTags.ToList());
    }
    
    /// <summary>
    /// Send coordinates message with all coordinate systems
    /// </summary>
    private void SendCoordinatesMessage(SceneClickData clickData)
    {
        // Build coordinate list from all systems
        var systemCoordinates = new List<SystemCoordinate>();
        
        var coordinateSystemJson = _globalData.SelectedProject?.CoordinateSystemJson;
        if (!string.IsNullOrEmpty(coordinateSystemJson))
        {
            try
            {
                var coordinateManager = new CoordinateSystemManager();
                var dtos = JsonSerializer.Deserialize<List<CoordinateSystemDto>>(coordinateSystemJson);
                
                if (dtos != null && dtos.Count > 0)
                {
                    coordinateManager.ImportAll(dtos);
                    systemCoordinates = coordinateManager.GetAllSystemCoordinates(
                        clickData.WorldX, 
                        clickData.WorldY
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing coordinate systems: {ex.Message}");
            }
        }
        
        var message = SceneMessage.Coordinates(
            clickData.WorldX, 
            clickData.WorldY, 
            null, // No object tag for empty click
            systemCoordinates
        );
        
        SendMessage(message);
    }
    
    /// <summary>
    /// Get all intersection data for external processing
    /// Returns array of (tag, x, y, z) tuples
    /// </summary>
    public IEnumerable<(string Tag, float X, float Y, float Z)> GetIntersectedSegmentsFromClick(
        SceneClickData clickData)
    {
        if (clickData.IntersectedSegments == null) 
            yield break;
            
        foreach (var segment in clickData.IntersectedSegments)
        {
            if (segment.Point != null)
            {
                yield return (segment.Tag, segment.Point.X, segment.Point.Y, segment.Point.Z);
            }
        }
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

/// <summary>
/// Represents intersection point coordinates
/// </summary>
public class IntersectPoint
{
    [JsonPropertyName("x")]
    public float X { get; set; }
    
    [JsonPropertyName("y")]
    public float Y { get; set; }
    
    [JsonPropertyName("z")]
    public float Z { get; set; }
    
    public Vector3 ToVector3() => new Vector3(X, Y, Z);
    
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}


/// <summary>
/// Represents a single intersected segment from raycasting
/// </summary>
public class IntersectedSegment
{
    /// <summary>
    /// The tag/identifier of the segment
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";
    
    /// <summary>
    /// The 3D point where the ray intersected this segment
    /// </summary>
    [JsonPropertyName("point")]
    public IntersectPoint? Point { get; set; }
    
    /// <summary>
    /// Type of mesh (e.g., "ladder", "pipe", "equipment")
    /// </summary>
    [JsonPropertyName("meshType")]
    public string? MeshType { get; set; }
    
    /// <summary>
    /// Index of segment within merged mesh (-1 for non-merged)
    /// </summary>
    [JsonPropertyName("segmentIndex")]
    public int SegmentIndex { get; set; } = -1;
    
    /// <summary>
    /// Distance from camera to intersection point
    /// </summary>
    [JsonPropertyName("distance")]
    public float Distance { get; set; }
    
    public override string ToString() => $"{Tag} at {Point}";
}

/// <summary>
/// Complete click event data from the Three.js scene
/// </summary>
public class SceneClickData
{
/// <summary>
    /// Type of click: "single", "double", "shift", "ctrl", "alt", "pinPlaced", "rightClick"
    /// </summary>
    [JsonPropertyName("eventType")]
    public string ClickType { get; set; } = "";
    
    /// <summary>
    /// Screen X coordinate (pixels from left of canvas)
    /// </summary>
    [JsonPropertyName("screenX")]
    public float ScreenX { get; set; }
    
    /// <summary>
    /// Screen Y coordinate (pixels from top of canvas)
    /// </summary>
    [JsonPropertyName("screenY")]
    public float ScreenY { get; set; }
    
    /// <summary>
    /// World X coordinate (on Z=0 plane by default)
    /// </summary>
    [JsonPropertyName("worldX")]
    public float WorldX { get; set; }
    
    /// <summary>
    /// World Y coordinate
    /// </summary>
    [JsonPropertyName("worldY")]
    public float WorldY { get; set; }
    
    /// <summary>
    /// World Z coordinate
    /// </summary>
    [JsonPropertyName("worldZ")]
    public float WorldZ { get; set; }
    
    /// <summary>
    /// Tag of the first/primary intersected object
    /// </summary>
    [JsonPropertyName("objectTag")]
    public string? ObjectTag { get; set; }
    
    /// <summary>
    /// Layer of the intersected object
    /// </summary>
    [JsonPropertyName("objectLayer")]
    public int? ObjectLayer { get; set; }
    
    /// <summary>
    /// Exact 3D point where ray hit the object
    /// </summary>
    [JsonPropertyName("intersectPoint")]
    public IntersectPoint? IntersectPoint { get; set; }
    
    /// <summary>
    /// Total number of objects intersected by the ray
    /// </summary>
    [JsonPropertyName("intersectCount")]
    public int IntersectCount { get; set; }
    
    /// <summary>
    /// Type of mesh for the primary hit (e.g., "ladder")
    /// </summary>
    [JsonPropertyName("meshType")]
    public string? MeshType { get; set; }
    
    /// <summary>
    /// Segment index within merged mesh (-1 for non-merged)
    /// </summary>
    [JsonPropertyName("segmentIndex")]
    public int SegmentIndex { get; set; } = -1;
    
    /// <summary>
    /// List of all intersected object tags (simple list)
    /// </summary>
    [JsonPropertyName("allIntersectedTags")]
    public List<string>? AllIntersectedTags { get; set; }
    
    /// <summary>
    /// Full information about all intersected segments
    /// </summary>
    [JsonPropertyName("intersectedSegments")]
    public List<IntersectedSegment>? IntersectedSegments { get; set; }
    
    /// <summary>
    /// Whether Shift key was held during click
    /// </summary>
    [JsonPropertyName("shiftKey")]
    public bool ShiftKey { get; set; }
    
    /// <summary>
    /// Whether Ctrl/Cmd key was held during click
    /// </summary>
    [JsonPropertyName("ctrlKey")]
    public bool CtrlKey { get; set; }
    
    /// <summary>
    /// Whether Alt key was held during click
    /// </summary>
    [JsonPropertyName("altKey")]
    public bool AltKey { get; set; }
    
    /// <summary>
    /// Mouse button (0=left, 1=middle, 2=right)
    /// </summary>
    [JsonPropertyName("button")]
    public int Button { get; set; }
    
    /// <summary>
    /// Timestamp of the event (ms since epoch)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    // ===== Helper Methods =====
    
    /// <summary>
    /// Check if any object was clicked
    /// </summary>
    public bool HasIntersection => 
        !string.IsNullOrEmpty(ObjectTag) || 
        (IntersectedSegments != null && IntersectedSegments.Count > 0);
    
    /// <summary>
    /// Get world position as Vector3
    /// </summary>
    public Vector3 WorldPosition => new Vector3(WorldX, WorldY, WorldZ);
    
    /// <summary>
    /// Get exact intersection point as Vector3 (or world position if not available)
    /// </summary>
    public Vector3 GetIntersectionPoint()
    {
        return IntersectPoint?.ToVector3() ?? WorldPosition;
    }
    
    /// <summary>
    /// Get all tags from intersected segments
    /// </summary>
    public IEnumerable<string> GetAllTags()
    {
        if (IntersectedSegments != null && IntersectedSegments.Count > 0)
        {
            return IntersectedSegments.Select(s => s.Tag).Where(t => !string.IsNullOrEmpty(t));
        }
        
        if (AllIntersectedTags != null && AllIntersectedTags.Count > 0)
        {
            return AllIntersectedTags.Where(t => !string.IsNullOrEmpty(t));
        }
        
        if (!string.IsNullOrEmpty(ObjectTag))
        {
            return new[] { ObjectTag };
        }
        
        return Enumerable.Empty<string>();
    }
    
    /// <summary>
    /// Get the first/primary tag
    /// </summary>
    public string? GetPrimaryTag()
    {
        if (IntersectedSegments != null && IntersectedSegments.Count > 0)
        {
            return IntersectedSegments[0].Tag;
        }
        return ObjectTag;
    }

    
    /// <summary>
    /// Get intersection points for all segments
    /// </summary>
    public IEnumerable<(string Tag, Vector3 Point)> GetAllIntersectionPoints()
    {
        if (IntersectedSegments == null) yield break;
        
        foreach (var segment in IntersectedSegments)
        {
            if (segment.Point != null)
            {
                yield return (segment.Tag, segment.Point.ToVector3());
            }
        }
    }
}