namespace SigmabotSync.Domain.Entities
{
    /// <summary>Proyecto Aconex dentro de un par de sincronización ProjectSync.</summary>
    public sealed class ProyectoSyncItem
    {
        public ProyectoSyncItem(string projectId, string label)
        {
            ProjectId = projectId;
            Label = label;
        }

        public string ProjectId { get; }
        public string Label { get; }
    }
}
