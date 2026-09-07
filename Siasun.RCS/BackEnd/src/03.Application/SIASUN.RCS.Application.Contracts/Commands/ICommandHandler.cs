using MediatR;

namespace SIASUN.RCS.Commands
{
    /// <summary>
    /// RCS 领域命令处理器接口（无返回值）
    /// </summary>
    /// <typeparam name="TCommand">处理的命令类型</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// RCS 领域命令处理器接口（带强类型返回值）
    /// </summary>
    /// <typeparam name="TCommand">处理的命令类型</typeparam>
    /// <typeparam name="TResponse">返回值类型</typeparam>
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
    }
}

