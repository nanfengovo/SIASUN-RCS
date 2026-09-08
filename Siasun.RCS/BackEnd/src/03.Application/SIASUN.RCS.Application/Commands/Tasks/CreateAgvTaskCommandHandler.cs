using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Tasks.Dtos;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Commands.Tasks
{
    /// <summary>
    /// 创建 AGV 调度任务 CQRS 命令处理器
    /// </summary>
    public class CreateAgvTaskCommandHandler : IRequestHandler<CreateAgvTaskCommand, AgvTaskDto>
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<CreateAgvTaskCommandHandler> _logger;

        public CreateAgvTaskCommandHandler(
            IRepository<AgvTask, Guid> taskRepository,
            IGuidGenerator guidGenerator,
            ILogger<CreateAgvTaskCommandHandler> logger)
        {
            _taskRepository = taskRepository;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        public async Task<AgvTaskDto> Handle(CreateAgvTaskCommand command, CancellationToken cancellationToken)
        {
            Check.NotNull(command, nameof(command));
            Check.NotNullOrWhiteSpace(command.TaskCode, nameof(command.TaskCode));

            var existing = await _taskRepository.FirstOrDefaultAsync(t => t.TaskCode == command.TaskCode, cancellationToken: cancellationToken);
            var existing = await _taskRepository.FindAsync(t => t.TaskCode == command.TaskCode, cancellationToken: cancellationToken);
            if (existing != null)
            {
                throw new BusinessException("TASK_ALREADY_EXISTS", $"任务编号 [{command.TaskCode}] 已存在，禁止重复创建。");
            }

            var task = new AgvTask(
                _guidGenerator.Create(),
                command.TaskCode,
                fromStation: command.FromStation,
                toStation: command.ToStation,
                carrierCode: command.CarrierCode,
                batchId: command.BatchId,
                traceId: command.TraceId);

            task.SetWorkflowKey(command.WorkflowKey);

            await _taskRepository.InsertAsync(task, autoSave: true, cancellationToken: cancellationToken);

            _logger.LogInformation("已通过 CQRS 创建调度任务: [{TaskCode}], Workflow={Workflow}, TraceId={TraceId}",
                task.TaskCode, task.WorkflowKey, task.TraceId);

            return new AgvTaskDto
            {
                Id = task.Id,
                TaskCode = task.TaskCode,
                Status = task.Status,
                FromStation = task.FromStation,
                ToStation = task.ToStation,
                CarrierCode = task.CarrierCode,
                BatchId = task.BatchId,
                AssignedVehicleCode = task.AssignedVehicleCode,
                WorkflowKey = task.WorkflowKey,
                StepIndex = task.StepIndex,
                TraceId = task.TraceId,
                CreationTime = task.CreationTime,
                CreatorId = task.CreatorId,
                LastModificationTime = task.LastModificationTime,
                LastModifierId = task.LastModifierId
            };
        }
    }
}
