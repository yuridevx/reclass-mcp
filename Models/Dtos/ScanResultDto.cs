namespace McpPlugin.Models.Dtos
{
    /// <summary>
    /// Memory scan result DTO.
    /// </summary>
    public class ScanResultDto
    {
        public string Address { get; set; }
        public string Value { get; set; }
        public string PreviousValue { get; set; }
        public string Module { get; set; }
    }

    /// <summary>
    /// Pattern match result DTO.
    /// </summary>
    public class PatternMatchDto
    {
        public string Address { get; set; }
        public string Module { get; set; }
        public string Section { get; set; }
        public string Offset { get; set; }
    }

    /// <summary>
    /// Enum information DTO.
    /// </summary>
    public class EnumInfoDto
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public bool UseFlagsMode { get; set; }
        public EnumValueDto[] Values { get; set; }
    }

    /// <summary>
    /// Enum value DTO.
    /// </summary>
    public class EnumValueDto
    {
        public string Name { get; set; }
        public long Value { get; set; }
    }
}
