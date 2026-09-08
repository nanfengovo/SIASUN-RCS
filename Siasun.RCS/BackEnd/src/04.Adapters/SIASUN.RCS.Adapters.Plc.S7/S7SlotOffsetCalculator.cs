using System;
using System.Text;

namespace SIASUN.RCS.Adapters.Plc.S7
{
    /// <summary>
    /// 半导体洁净室 ERACK / 晶圆立库 46 字节标准槽位数据偏移计算与解析器
    /// 结构定义:
    /// - Offset 0..1 (Word): 槽位状态 (0=空闲, 1=占用, 2=预留锁定, 3=故障)
    /// - Offset 2..17 (String[16]): 载具/晶圆盒 CarrierCode
    /// - Offset 18..19 (Word): 光电传感器位域 (Bit0=在位, Bit1=斜置报警, Bit2=左光电, Bit3=右光电)
    /// - Offset 20..23 (DWord): PLC 刷新时间戳 / 心跳计数
    /// - Offset 24..45 (22 Bytes): 扩展配方 / LotId / 工艺批次号
    /// </summary>
    public static class S7SlotOffsetCalculator
    {
        /// <summary>
        /// 单个槽位占用的标准字节长度 (46 字节)
        /// </summary>
        public const int SlotByteLength = 46;

        /// <summary>
        /// 根据槽位号计算在 DB 块中的绝对字节偏移量
        /// </summary>
        /// <param name="slotIndex">槽位序号（1-indexed）</param>
        /// <param name="baseOffset">DB 块起始基准字节偏移（默认 0）</param>
        /// <returns>绝对字节偏移量</returns>
        public static int CalculateOffset(int slotIndex, int baseOffset = 0)
        {
            if (slotIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "槽位序号必须大于或等于 1。");
            }

            return baseOffset + (slotIndex - 1) * SlotByteLength;
        }

        /// <summary>
        /// 从字节缓冲区中解析指定槽位的结构化数据
        /// </summary>
        /// <param name="buffer">PLC 批量读取的原始字节数组</param>
        /// <param name="slotIndex">槽位序号</param>
        /// <param name="baseOffset">基准偏移</param>
        /// <returns>槽位解析结果</returns>
        public static S7SlotData ParseSlot(byte[] buffer, int slotIndex, int baseOffset = 0)
        {
            var offset = CalculateOffset(slotIndex, baseOffset);

            if (buffer.Length < offset + SlotByteLength)
            {
                throw new ArgumentException($"缓冲区长度不足: 需要至少 {offset + SlotByteLength} 字节，实际为 {buffer.Length} 字节。");
            }

            // 大端序解析状态 Word (Siemens 标准)
            var statusCode = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

            // 解析 CarrierCode (16 字节 ASCII)
            var carrierBytes = new ReadOnlySpan<byte>(buffer, offset + 2, 16);
            var carrierCode = Encoding.ASCII.GetString(carrierBytes).Trim('\0', ' ');

            // 解析光电传感器位域
            var sensorWord = (ushort)((buffer[offset + 18] << 8) | buffer[offset + 19]);
            var isPresent = (sensorWord & 0x0001) != 0;
            var isTilted = (sensorWord & 0x0002) != 0;
            var leftSensor = (sensorWord & 0x0004) != 0;
            var rightSensor = (sensorWord & 0x0008) != 0;

            // 解析时间戳
            var timestamp = (uint)((buffer[offset + 20] << 24) |
                                   (buffer[offset + 21] << 16) |
                                   (buffer[offset + 22] << 8) |
                                   buffer[offset + 23]);

            return new S7SlotData(
                SlotIndex: slotIndex,
                Offset: offset,
                StatusCode: statusCode,
                CarrierCode: carrierCode,
                IsPresent: isPresent,
                IsTilted: isTilted,
                LeftSensor: leftSensor,
                RightSensor: rightSensor,
                PlcTimestamp: timestamp);
        }
    }

    /// <summary>
    /// S7 46B 槽位解析实体数据
    /// </summary>
    public record S7SlotData(
        int SlotIndex,
        int Offset,
        ushort StatusCode,
        string CarrierCode,
        bool IsPresent,
        bool IsTilted,
        bool LeftSensor,
        bool RightSensor,
        uint PlcTimestamp);
}
