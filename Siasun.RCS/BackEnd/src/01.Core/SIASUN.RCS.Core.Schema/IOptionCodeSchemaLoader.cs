namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 配置文件加载器接口
    /// </summary>
    public interface IOptionCodeSchemaLoader
    {
        /// <summary>
        /// 从指定或默认目录扫描并加载所有 *.json Schema 配置文件并注册到 Registry
        /// </summary>
        /// <param name="directoryPath">目录物理路径（若为空则自动探测）</param>
        /// <returns>成功加载并注册的 Schema 数量</returns>
        int LoadFromDirectory(string? directoryPath = null);
    }
}

