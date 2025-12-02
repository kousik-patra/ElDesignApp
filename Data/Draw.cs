using System;
using System.Collections.Generic;
using System.Linq;

namespace ElDesignApp.Data;

using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using ElDesignApp.Models;
using Microsoft.JSInterop;
using ElDesignApp.Services;


public class Draw
{
    
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
        if (sceneInfoJson == null) return;
        var sceneData = JsonSerializer.Deserialize<List<float>>(sceneInfoJson);
        //
        if (sceneData == null) return;
        var rendererWidth = (int)sceneData[0];
        var rendererHeight = (int)sceneData[1];
        var cameraPosition = new Vector3(sceneData[2], sceneData[3], sceneData[4]);
        var cameraRotation = new Vector3(sceneData[5], sceneData[6], sceneData[7]);
        var text = $"Renderer Width: {rendererWidth}, Renderer Height: {rendererHeight}, " +
                   $"Camera position: , {cameraPosition}, Camera rotation.{cameraRotation}";

        _globalData.sceneDataCurrent.SceneJSON = sceneInfoJson;
            
        _globalData.sceneDataCurrent.RendererWidth = sceneData[0];
        _globalData.sceneDataCurrent.RendererHeight = sceneData[1];
        _globalData.sceneDataCurrent.CameraPosition = cameraPosition;
        _globalData.sceneDataCurrent.CameraRotation = cameraRotation;
        _globalData.sceneDataCurrent.RendererWidthPX = $"{rendererWidth}px";
        _globalData.sceneDataCurrent.RendererHeightPX = $"{rendererHeight}px";

        UpdateSceneData();
                   
        Debug.WriteLine($"Draw: SaveSceneInfo: Scene Info changed to {text}");
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
    public void UpdateRefPoints(string refPoints)
    {
     
        Debug.WriteLine($"Draw: UpdateRefPoints: {refPoints}");
        
        // refPoints.push([point.x, point.y, mouse.x, mouse.y]);
        
        try
        {
            // Split the input string by commas and trim whitespace
            var values = refPoints.Split(',').Select(s => s.Trim()).ToArray();

            // Parse to floats
            var numbers = new float[16];
            for (int i = 0; i < values.Length; i++)
            {
                if (!float.TryParse(values[i], out var number))
                {
                    throw new ArgumentException($"Invalid number format at position {i + 1}: '{values[i]}'.");
                }
                numbers[i] = number;
            }
            
            

            _globalData.RefPoints?.Clear();
            Enumerable.Range(0, numbers.Length / 4).ToList();
            new List<int>{0,1,2,3}.ForEach(i=> _globalData.RefPoints?.Add(new Vector3(numbers[i*4], numbers[i*4+1], 0)));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format.", ex);
        }
    }

    
    
}