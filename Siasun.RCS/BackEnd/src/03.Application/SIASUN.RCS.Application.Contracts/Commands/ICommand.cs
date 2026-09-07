using MediatR;

namespace SIASUN.RCS.Commands
{
    /// <summary>
    /// RCS 领域命令基接口（无返回值变更命令）
    /// 遵循 CQRS 架构，表征一个明确的业务状态变更意图
    /// </summary>
    public interface ICommand : IRequest
    {
    }

    /// <summary>
    /// RCS 领域命令基接口（带强类型返回值变更命令）
    /// 遵循 CQRS 架构，表征一个明确的业务状态变更意图并返回操作结果
    /// </summary>
    /// <typeparam name="TResponse">命令执行返回结果类型</typeparam>
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }
}

