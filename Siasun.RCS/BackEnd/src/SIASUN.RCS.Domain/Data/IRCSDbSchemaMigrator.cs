using System.Threading.Tasks;

namespace SIASUN.RCS.Data;

public interface IRCSDbSchemaMigrator
{
    Task MigrateAsync();
}
