using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.EntityFrameworkCore.Auditing
{
    /// <summary>
    /// 基于 EF Core 模型元数据的实体类型提供者
    /// </summary>
    public class EfCoreEntityTypeProvider : IEntityTypeProvider, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 初始化实体类型提供者
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public EfCoreEntityTypeProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 获取 EF Core 上下文中所有已注册的 CLR 实体类型
        /// </summary>
        /// <returns>实体 CLR 类型列表</returns>
        public List<Type> GetEntityTypes()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RCSDbContext>();
            return dbContext.Model.GetEntityTypes().Select(x => x.ClrType).ToList();
        }
    }
}
