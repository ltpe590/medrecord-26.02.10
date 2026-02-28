namespace Core.Entities
{
    /// <summary>
    /// EF-mapped lookup entity. Scalar properties are init-only;
    /// construct with object-initializer syntax (HasData, new SpecialtyProfile { ... }).
    /// </summary>
    public class SpecialtyProfile
    {
        public int    SpecialtyProfileId { get; init; }
        public string Name               { get; init; } = string.Empty;
    }
}
