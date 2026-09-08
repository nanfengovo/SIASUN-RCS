namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 工作流 Schema 外部定义文件（JSON）动态发现与加载契约
    /// </summary>
    public interface IWorkflowDefinitionLoader
    {
        /// <summary>
        /// 从指定目录扫描并加载所有 *.json 工作流定义文件注册到中心
        /// </summary>
        /// <param name="directoryPath">目录路径，若为 null 则自动使用宿主默认配置目录</param>
        /// <returns>成功加载的工作流模板数量</returns>
        int LoadFromDirectory(string? directoryPath = null);
    }
}
