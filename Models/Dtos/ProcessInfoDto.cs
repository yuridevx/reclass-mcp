namespace McpPlugin.Models.Dtos
{
    /// <summary>
    /// Process information DTO.
    /// </summary>
    public class ProcessInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Is64Bit { get; set; }
        public bool IsValid { get; set; }
    }
}
