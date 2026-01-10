namespace ElDesignApp.Models

{
    public enum SceneMessageType
    {
        Info,
        Coordinates,
        ObjectSelected,
        Warning,
        Error
    }

    public class SceneMessage
    {
        public SceneMessageType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ObjectTag { get; set; }
        public double? WorldX { get; set; }
        public double? WorldY { get; set; }
        public double? WorldZ { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Factory methods for common messages
        public static SceneMessage Coordinates(double x, double y, string? objectTag = null)
        {
            return new SceneMessage
            {
                Type = objectTag != null ? SceneMessageType.ObjectSelected : SceneMessageType.Coordinates,
                Text = objectTag != null 
                    ? $"Selected: {objectTag} at ({x:F2}, {y:F2})"
                    : $"Position: ({x:F2}, {y:F2})",
                ObjectTag = objectTag,
                WorldX = x,
                WorldY = y,
                WorldZ = 0
            };
        }

        public static SceneMessage Info(string text) => new() { Type = SceneMessageType.Info, Text = text };
        public static SceneMessage Warning(string text) => new() { Type = SceneMessageType.Warning, Text = text };
        public static SceneMessage Error(string text) => new() { Type = SceneMessageType.Error, Text = text };
    }
}