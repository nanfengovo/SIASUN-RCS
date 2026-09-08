namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 传输协议线格式规范配置
    /// </summary>
    public class OptionCodeWireFormat
    {
        /// <summary>
        /// 多 Part 之间的拼接分隔符（默认为英文逗号 ","）
        /// </summary>
        public string Join { get; set; } = ",";

        /// <summary>
        /// 是否采用 1-based 的 LSB 位模式（若为 true，bit 1 对应二进制最低位 0 偏移）
        /// </summary>
        public bool LsbBit1 { get; set; } = true;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public OptionCodeWireFormat()
        {
        }

        /// <summary>
        /// 指定参数构造函数
        /// </summary>
        /// <param name="join">拼接分隔符</param>
        /// <param name="lsbBit1">是否采用 1-based LSB</param>
        public OptionCodeWireFormat(string join, bool lsbBit1 = true)
        {
            Join = join;
            LsbBit1 = lsbBit1;
        }
    }
}

