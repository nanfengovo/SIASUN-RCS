namespace SIASUN.RCS.Auditing
{
    public class EntityTypeDiscoveryDto
    {
        public string FullName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public bool HasRule { get; set; }
    }
}
