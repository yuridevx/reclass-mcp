namespace McpPlugin.Models.Dtos
{
    /// <summary>
    /// Node information DTO.
    /// </summary>
    public class NodeInfoDto
    {
        public int Offset { get; set; }
        public string OffsetHex { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public string Comment { get; set; }
        public string Value { get; set; }
        public NodeInfoDto InnerNode { get; set; }
        public int? ArrayCount { get; set; }
        public string InnerClass { get; set; }
    }
}
