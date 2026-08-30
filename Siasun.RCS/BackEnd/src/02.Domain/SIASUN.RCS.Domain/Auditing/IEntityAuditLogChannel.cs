namespace SIASUN.RCS.Auditing
{
    public interface IEntityAuditLogChannel
    {
        bool TryWrite(EntityAuditLogMessage message);
    }
}
