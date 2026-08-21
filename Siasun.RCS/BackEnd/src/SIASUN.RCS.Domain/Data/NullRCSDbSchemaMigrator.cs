using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Data;

/* This is used if database provider does't define
 * IRCSDbSchemaMigrator implementation.
 */
public class NullRCSDbSchemaMigrator : IRCSDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
