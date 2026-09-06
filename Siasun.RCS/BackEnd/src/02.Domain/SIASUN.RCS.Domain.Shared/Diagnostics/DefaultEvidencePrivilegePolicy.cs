using System;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 默认审计与诊断证据特权裁决策略实现（单例真实源）
    /// 严格遵循 SIASUN RCS 工业级三层审计规范：异常、操作轨、核心调度任务与车辆实体变更绝不可丢弃
    /// </summary>
    public class DefaultEvidencePrivilegePolicy : IEvidencePrivilegePolicy
    {
        /// <summary>
        /// 全局默认静态单例实例（供未接入 DI 的纯静态拦截器或高性能热路径便捷使用）
        /// </summary>
        public static DefaultEvidencePrivilegePolicy Instance { get; } = new();

        /// <summary>
        /// 判定事件是否具有特权级别（如 Warning, Error, Fatal, Critical，或 HTTP 状态码 >= 400，或带有未处理异常）
        /// </summary>
        /// <param name="level">事件或日志级别</param>
        /// <param name="statusCode">HTTP 响应状态码（可选）</param>
        /// <param name="exception">异常信息摘要（可选）</param>
        /// <returns>是否属于特权级别</returns>
        public bool IsPrivilegedLevel(string? level, int? statusCode = null, string? exception = null)
        {
            if (statusCode.HasValue && statusCode.Value >= 400)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(exception))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(level))
            {
                return false;
            }

            return string.Equals(level, DiagnosticLevels.Warning, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(level, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(level, DiagnosticLevels.Fatal, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(level, "Critical", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判定业务实体或分类是否具有特权级别（如 AgvTask, AgvVehicle, Operation, Dispatch 等核心领域资产）
        /// </summary>
        /// <param name="category">实体名或事件分类</param>
        /// <returns>是否属于特权分类</returns>
        public bool IsPrivilegedCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return false;
            }

            return string.Equals(category, DiagnosticCategories.Operation, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, DiagnosticTracks.Operator, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, DiagnosticCategories.SelfHeal, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, DiagnosticCategories.Dispatch, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, DiagnosticCategories.Task, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "AgvTask", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Task", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, DiagnosticCategories.Vehicle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "AgvVehicle", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Vehicle", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "OperationLog", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "AppOperationLogs", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "AppAgvTasks", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "AppAgvVehicles", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "TM", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "MES", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Exception", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判定 API 路由路径是否属于特权级别（如 /dispatch, /task, /vehicle, /operation, /tm, /mes 等关键工控端点）
        /// 采用分段与分词匹配，杜绝 query 参数或无关子串偶然碰撞（如 multitasking）导致误判
        /// </summary>
        /// <param name="path">HTTP 请求路径</param>
        /// <returns>是否属于特权路径</returns>
        public bool IsPrivilegedPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // 1. 去除 URL Query String 与 Fragment
            var pathSpan = path.AsSpan().Trim();
            var qIndex = pathSpan.IndexOfAny('?', '#');
            if (qIndex >= 0)
            {
                pathSpan = pathSpan.Slice(0, qIndex);
            }

            if (pathSpan.IsEmpty)
            {
                return false;
            }

            // 2. 按路径分隔符与分词符拆分检查各段独立 Token
            var rawPath = pathSpan.ToString();
            var tokens = rawPath.Split(new[] { '/', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (token.Equals("dispatch", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("task", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("tasks", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("agvtask", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("agvtasks", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("vehicle", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("vehicles", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("agvvehicle", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("agvvehicles", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("operation", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("operations", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("tm", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("mes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判定对接方标识是否属于特权级别（如 TM, MES, Dispatch 等关键上游或底盘通信方）
        /// </summary>
        /// <param name="peer">对接系统名称</param>
        /// <returns>是否属于特权对接方</returns>
        public bool IsPrivilegedPeer(string? peer)
        {
            if (string.IsNullOrWhiteSpace(peer))
            {
                return false;
            }

            return string.Equals(peer, "TM", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(peer, "MES", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(peer, "Dispatch", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判定事件轨道是否属于特权级别（如 Operator 操作轨、Exception 异常轨、Task 任务轨等）
        /// </summary>
        /// <param name="track">时序泳道轨道</param>
        /// <returns>是否属于特权轨道</returns>
        public bool IsPrivilegedTrack(string? track)
        {
            if (string.IsNullOrWhiteSpace(track))
            {
                return false;
            }

            return string.Equals(track, DiagnosticTracks.Operator, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(track, DiagnosticTracks.Task, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(track, "Exception", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 综合判定一项诊断或审计实体是否属于工业级特权铁证
        /// </summary>
        /// <param name="category">实体或分类名</param>
        /// <param name="level">级别</param>
        /// <param name="path">路由路径</param>
        /// <param name="peer">通信对端</param>
        /// <param name="statusCode">状态码</param>
        /// <param name="exception">异常描述</param>
        /// <param name="track">轨道</param>
        /// <returns>是否属于不可丢弃的特权铁证</returns>
        public bool IsPrivileged(
            string? category = null,
            string? level = null,
            string? path = null,
            string? peer = null,
            int? statusCode = null,
            string? exception = null,
            string? track = null)
        {
            if (IsPrivilegedLevel(level, statusCode, exception))
            {
                return true;
            }

            if (IsPrivilegedCategory(category))
            {
                return true;
            }

            if (IsPrivilegedPath(path))
            {
                return true;
            }

            if (IsPrivilegedPeer(peer))
            {
                return true;
            }

            if (IsPrivilegedTrack(track))
            {
                return true;
            }

            return false;
        }
    }
}
