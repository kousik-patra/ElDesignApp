using System.Text;

namespace ElDesignApp.Models

{
    public enum SceneMessageType
    {
        Info,
        Warning,
        Error,
        Coordinates,
        ObjectSelected,
        RendererSize 
    }

    public class SceneMessage
    {
        public SceneMessageType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ObjectTag { get; set; }
        public double? WorldX { get; set; }
        public double? WorldY { get; set; }
        public double? WorldZ { get; set; }
        public double? RenderWidth { get; set; }
        public double? RenderHeight { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public List<SystemCoordinate> SystemCoordinates { get; set; } = new();

        // Factory methods for common messages
        public static SceneMessage Coordinates(
            double x, 
            double y, 
            string? objectTag = null,
            List<SystemCoordinate>? systemCoordinates = null)
        {
            var coordText = BuildCoordinateText(x, y, systemCoordinates);
        
            return new SceneMessage
            {
                Type = objectTag != null ? SceneMessageType.ObjectSelected : SceneMessageType.Coordinates,
                Text = objectTag != null 
                    ? $"Selected: {objectTag}\n{coordText}"
                    : coordText,
                ObjectTag = objectTag,
                WorldX = x,
                WorldY = y,
                WorldZ = 0,
                SystemCoordinates = systemCoordinates ?? new List<SystemCoordinate>()
            };
        }
        
        // Helper to build formatted coordinate text
        private static string BuildCoordinateText(double sceneX, double sceneY, List<SystemCoordinate>? systemCoordinates)
        {
            var sb = new StringBuilder();
            sb.Append($"Scene: (X:{sceneX:F2}, Y:{sceneY:F2})");
        
            if (systemCoordinates != null && systemCoordinates.Count > 0)
            {
                foreach (var sc in systemCoordinates)
                {
                    sb.Append($"\n{sc.SystemName}: E:{sc.E:F3}, N:{sc.N:F3} {sc.Unit}");
                }
            }
        
            return sb.ToString();
        }
        
        public static SceneMessage RendererSize(double width, double height)
        {
            return new SceneMessage
            {
                Type = SceneMessageType.Info,
                Text = $"Canvas resized to {width:F0} x {height:F0}",
                RenderWidth = width,
                RenderHeight = height
            };
        }

        public static SceneMessage Info(string text) => new() { Type = SceneMessageType.Info, Text = text };
        public static SceneMessage Warning(string text) => new() { Type = SceneMessageType.Warning, Text = text };
        public static SceneMessage Error(string text) => new() { Type = SceneMessageType.Error, Text = text };
        
        /// <summary>
        /// Create a message for object selection (single or multiple)
        /// </summary>
        /// <param name="objectTag">Tag of selected object, or summary like "3 segments"</param>
        /// <param name="text">Description text</param>
        /// <param name="x">Optional world X coordinate</param>
        /// <param name="y">Optional world Y coordinate</param>
        /// <param name="z">Optional world Z coordinate</param>
        public static SceneMessage ObjectSelected(
            string objectTag, 
            string text, 
            double? x = null, 
            double? y = null, 
            double? z = null)
        {
            return new SceneMessage
            {
                Type = SceneMessageType.ObjectSelected,
                Text = text,
                ObjectTag = objectTag,
                WorldX = x,
                WorldY = y,
                WorldZ = z
            };
        }
        
        /// <summary>
        /// Create a message for multiple object selection
        /// </summary>
        /// <param name="tags">List of selected tags</param>
        public static SceneMessage ObjectsSelected(IEnumerable<string> tags)
        {
            var tagList = tags.ToList();
            var count = tagList.Count;
            
            if (count == 0)
            {
                return Info("Selection cleared");
            }
            
            var displayTag = count == 1 
                ? tagList[0] 
                : $"{count} items";
            
            var displayText = count <= 3 
                ? $"Selected: {string.Join(", ", tagList)}"
                : $"Selected: {string.Join(", ", tagList.Take(3))}...";
            
            return new SceneMessage
            {
                Type = SceneMessageType.ObjectSelected,
                Text = displayText,
                ObjectTag = displayTag
            };
        }
    }
}