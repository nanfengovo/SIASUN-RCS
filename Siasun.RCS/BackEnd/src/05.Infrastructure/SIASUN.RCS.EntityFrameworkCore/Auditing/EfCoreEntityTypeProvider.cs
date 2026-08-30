using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.EntityFrameworkCore.Auditing
{
    public class EfCoreEntityTypeProvider : IEntityTypeProvider, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;

        public EfCoreEntityTypeProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<Type> GetEntityTypes()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RCSDbContext>();
            return dbContext.Model.GetEntityTypes().Select(x => x.ClrType).ToList();
        }
    }
}
