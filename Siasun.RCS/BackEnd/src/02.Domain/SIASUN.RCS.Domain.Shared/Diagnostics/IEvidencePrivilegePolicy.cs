using System;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 审计与诊断证据特权裁决策略接口（单一真实源）
    /// 统一判定 API 报文、实体变更、操作日志及遥测事件是否属于工业级“绝对不可丢失”铁证
    /// </summary>
    public interface IEvidencePrivilegePolicy
    {
        /// <summary>
        /// 判定事件是否具有特权级别（如 Warning, Error, Fatal, Critical，或 HTTP 状态码 >= 400，或带有未处理异常）
        /// </summary>
        /// <param name="level">事件或日志级别</param>
        /// <param name="statusCode">HTTP 响应状态码（可选）</param>
        /// <param name="exception">异常信息摘要（可选）</param>
        /// <returns>是否属于特权级别</returns>
        bool IsPrivilegedLevel(string? level, int? statusCode = null, string? exception = null);

        /// <summary>
        /// 判定业务实体或分类是否具有特权级别（如 AgvTask, AgvVehicle, Operation, Dispatch 等核心领域资产）
        /// </summary>
        /// <param name="category">实体名或事件分类</param>
        /// <returns>是否属于特权分类</returns>
        bool IsPrivilegedCategory(string? category);

        /// <summary>
        /// 判定 API 路由路径是否属于特权级别（如 /dispatch, /task, /vehicle, /operation 等关键工控端点）
        /// </summary>
        /// <param name="path">HTTP 请求路径</param>
        /// <returns>是否属于特权路径</returns>
        bool IsPrivilegedPath(string? path);

        /// <summary>
        /// 判定对接方标识是否属于特权级别（如 TM, MES, Dispatch 等关键上游或底盘通信方）
        /// </summary>
        /// <param name="peer">对接系统名称</param>
        /// <returns>是否属于特权对接方</returns>
        bool IsPrivilegedPeer(string? peer);

        /// <summary>
        /// 判定事件轨道是否属于特权级别（如 Operator 操作轨、Exception 异常轨、Task 任务轨等）
        /// </summary>
        /// <param name="track">时序泳道轨道</param>
        /// <returns>是否属于特权轨道</returns>
        bool IsPrivilegedTrack(string? track);

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
        bool IsPrivileged(
            string? category = null,
            string? level = null,
            string? path = null,
            string? peer = null,
            int? statusCode = null,
            string? exception = null,
            string? track = null);
    }
}
