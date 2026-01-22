using System.Collections.Generic;

namespace McpPlugin.Models.Dtos
{
    /// <summary>
    /// Class/structure information DTO.
    /// </summary>
    public class ClassInfoDto
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string AddressFormula { get; set; }
        public int Size { get; set; }
        public string Comment { get; set; }
        public List<NodeInfoDto> Nodes { get; set; }
    }
}
