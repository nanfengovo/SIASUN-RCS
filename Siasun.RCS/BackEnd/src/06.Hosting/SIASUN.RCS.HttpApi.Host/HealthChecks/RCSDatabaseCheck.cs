using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace SIASUN.RCS.HealthChecks;

/// <summary>
/// 数据库连通性健康检查器
/// </summary>
public class RCSDatabaseCheck : IHealthCheck, ITransientDependency
{
    /// <summary>
    /// 身份角色仓储
    /// </summary>
    protected readonly IIdentityRoleRepository IdentityRoleRepository;

    /// <summary>
    /// 初始化数据库健康检查器
    /// </summary>
    /// <param name="identityRoleRepository">身份角色仓储</param>
    public RCSDatabaseCheck(IIdentityRoleRepository identityRoleRepository)
    {
        IdentityRoleRepository = identityRoleRepository;
    }

    /// <summary>
    /// 执行健康状态探测
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await IdentityRoleRepository.GetListAsync(sorting: nameof(IdentityRole.Id), maxResultCount: 1, cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy($"Could connect to database and get record.");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy($"Error when trying to get database record. ", e);
        }
    }
}
