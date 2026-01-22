namespace McpPlugin.Models.Dtos
{
    /// <summary>
    /// Memory section information DTO.
    /// </summary>
    public class SectionInfoDto
    {
        public string Start { get; set; }
        public string End { get; set; }
        public string Size { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Protection { get; set; }
        public string Type { get; set; }
        public string ModuleName { get; set; }
    }
}
