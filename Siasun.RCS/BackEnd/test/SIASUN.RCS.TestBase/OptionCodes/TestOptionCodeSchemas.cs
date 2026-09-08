using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// 测试环境专用的 OptionCode Schema 快速构建器（解析自标准 JSON 定义，不侵入 Domain 核心微内核）
    /// </summary>
    public static class TestOptionCodeSchemas
    {
        private const string TxcJson = @"{
  ""code"": ""txc_demo"",
  ""version"": 1,
  ""title"": ""台湾晶技 TXC 标准治具位图"",
  ""description"": ""适用于台湾晶技双臂/单臂协作机器人的标准 32 位 CodeA/CodeB 治具控制协议"",
  ""wire"": { ""join"": "","", ""lsbBit1"": true },
  ""parts"": [
    {
      ""key"": ""codeA"",
      ""label"": ""TaskCodeA (车体与工装指令)"",
      ""width"": 32,
      ""fields"": [
        { ""key"": ""armSide"", ""label"": ""机械臂运行侧"", ""bitStart"": 1, ""bitEnd"": 8, ""required"": false, ""source"": ""Master"", ""defaultValue"": 1, ""enum"": { ""1"": ""左侧"", ""2"": ""右侧"" }, ""description"": ""机械臂朝向机台的动作侧方向"" },
        { ""key"": ""agvSlot"", ""label"": ""AGV车身槽位"", ""bitStart"": 9, ""bitEnd"": 16, ""required"": true, ""source"": ""Const"", ""constValue"": 0, ""description"": ""AGV车身放置槽位索引，固定为0"" },
        { ""key"": ""boxType"", ""label"": ""料盒类型"", ""bitStart"": 17, ""bitEnd"": 24, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0, ""description"": ""晶圆/晶体料盒规格代号"" },
        { ""key"": ""machineIndex"", ""label"": ""机台索引"", ""bitStart"": 25, ""bitEnd"": 32, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0, ""description"": ""车间机台作业索引编号"" }
      ]
    },
    {
      ""key"": ""codeB"",
      ""label"": ""TaskCodeB (设备与动作指令)"",
      ""width"": 32,
      ""fields"": [
        { ""key"": ""equipmentType"", ""label"": ""设备类型"", ""bitStart"": 1, ""bitEnd"": 8, ""required"": true, ""source"": ""Master"", ""enum"": { ""1"": ""Rack货架"", ""2"": ""H099机台"", ""3"": ""H044机台"" }, ""description"": ""现场目标设备硬件类型"" },
        { ""key"": ""equipmentSlot"", ""label"": ""设备库位编号"", ""bitStart"": 9, ""bitEnd"": 16, ""required"": true, ""source"": ""Port"", ""description"": ""目标设备库位/槽位序号"" },
        { ""key"": ""pickPlace"", ""label"": ""取放标识"", ""bitStart"": 17, ""bitEnd"": 24, ""required"": true, ""source"": ""Leg"", ""enum"": { ""1"": ""车身到设备(P/Put)"", ""2"": ""设备到车身(G/Fetch)"" }, ""description"": ""取货或放货物理动作方向"" },
        { ""key"": ""machineNo"", ""label"": ""机台编号"", ""bitStart"": 25, ""bitEnd"": 32, ""required"": false, ""source"": ""Master"", ""defaultValue"": 0, ""description"": ""现场物理机台编号"" }
      ]
    }
  ]
}";

        private const string ErackJson = @"{
  ""code"": ""erack"",
  ""version"": 1,
  ""title"": ""NXP ERACK 晶圆仓储治具位图"",
  ""description"": ""适用于 NXP ERACK 晶圆盒出入库与机台对接的 32 位 TaskCode1/TaskCode2 位图协议"",
  ""wire"": { ""join"": "","", ""lsbBit1"": true },
  ""parts"": [
    {
      ""key"": ""TaskCode1"",
      ""label"": ""TaskCode1 (机台索引与料盒模板)"",
      ""width"": 32,
      ""fields"": [
        { ""key"": ""cameraTemplateIndex"", ""label"": ""视觉模板索引"", ""bitStart"": 1, ""bitEnd"": 8, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0 },
        { ""key"": ""agvLocationIndex"", ""label"": ""车辆槽位索引"", ""bitStart"": 9, ""bitEnd"": 16, ""required"": false, ""source"": ""Const"", ""constValue"": 0 },
        { ""key"": ""boxType"", ""label"": ""料盒类型"", ""bitStart"": 17, ""bitEnd"": 20, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0 },
        { ""key"": ""machineIndex"", ""label"": ""机台索引"", ""bitStart"": 21, ""bitEnd"": 32, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0 }
      ]
    },
    {
      ""key"": ""TaskCode2"",
      ""label"": ""TaskCode2 (机台编号与设备动作)"",
      ""width"": 32,
      ""fields"": [
        { ""key"": ""deviceType"", ""label"": ""设备类型"", ""bitStart"": 1, ""bitEnd"": 8, ""required"": true, ""source"": ""Master"", ""enum"": { ""1"": ""DBErack"", ""2"": ""Stocker"", ""3"": ""WB"" } },
        { ""key"": ""machineLocationIndex"", ""label"": ""设备槽位索引"", ""bitStart"": 9, ""bitEnd"": 16, ""required"": true, ""source"": ""Port"" },
        { ""key"": ""pGMark"", ""label"": ""取放标志"", ""bitStart"": 17, ""bitEnd"": 20, ""required"": true, ""source"": ""Leg"", ""enum"": { ""1"": ""Fetch(取料)"", ""2"": ""Put(放料)"" } },
        { ""key"": ""machineNumber"", ""label"": ""物理机台号"", ""bitStart"": 21, ""bitEnd"": 32, ""required"": false, ""source"": ""Master"", ""defaultValue"": 0 }
      ]
    }
  ]
}";

        private const string MoldingJson = @"{
  ""code"": ""molding"",
  ""version"": 1,
  ""title"": ""NXP 天津 Molding 封测车间位图"",
  ""description"": ""适用于 NXP 天津 Molding 车间弹匣搬运与立库交互的标准 32 位位图协议"",
  ""wire"": { ""join"": "","", ""lsbBit1"": true },
  ""parts"": [
    {
      ""key"": ""TaskCode1"",
      ""label"": ""TaskCode1 (数量/弹匣类型/TT/批次)"",
      ""width"": 32,
      ""fields"": [
        { ""key"": ""lotId"", ""label"": ""批次低8位"", ""bitStart"": 1, ""bitEnd"": 8, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0 },
        { ""key"": ""ttStart"", ""label"": ""TT起点编号"", ""bitStart"": 9, ""bitEnd"": 16, ""required"": false, ""source"": ""Args"", ""defaultValue"": 0 },
        { ""key"": ""carrierType"", ""label"": ""弹匣料盒类型"", ""bitStart"": 17, ""bitEnd"": 24, ""required"": true, ""source"": ""Args"", ""enum"": { ""1"": ""小弹匣(L)"", ""2"": ""中弹匣(B)"", ""3"": ""大弹匣(H)"" } },
        { ""key"": ""count"", ""label"": ""物料数量"", ""bitStart"": 25, ""bitEnd"": 32, ""required"": true, ""source"": ""Args"", ""defaultValue"": 1 }
      ]
    },
    {
      ""key"": ""TaskCode2"",
      ""label"": ""TaskCode2 (数量/取放/机台/设备类型)"",
      ""width"": 32,
      ""fields"": [
        { ""key"": ""machineType"", ""label"": ""设备类型"", ""bitStart"": 1, ""bitEnd"": 8, ""required"": true, ""source"": ""Master"", ""enum"": { ""1"": ""TT01"", ""2"": ""TT02"", ""3"": ""SP170"", ""4"": ""Y series"" } },
        { ""key"": ""machineLocationId"", ""label"": ""机台点位编号"", ""bitStart"": 9, ""bitEnd"": 16, ""required"": true, ""source"": ""Port"" },
        { ""key"": ""putOrFetchFlag"", ""label"": ""取放标志"", ""bitStart"": 17, ""bitEnd"": 24, ""required"": true, ""source"": ""Leg"", ""enum"": { ""1"": ""Put(放料)"", ""2"": ""Fetch(取料)"" } },
        { ""key"": ""count"", ""label"": ""物料数量"", ""bitStart"": 25, ""bitEnd"": 32, ""required"": true, ""source"": ""Args"", ""defaultValue"": 1 }
      ]
    }
  ]
}";

        /// <summary>
        /// 获取从 JSON 解析的 TXC 标准 Schema
        /// </summary>
        public static OptionCodeSchemaDefinition CreateTxcDemoV1() => OptionCodeJsonSchemaParser.Parse(TxcJson);

        /// <summary>
        /// 获取从 JSON 解析的 ERACK 标准 Schema
        /// </summary>
        public static OptionCodeSchemaDefinition CreateErackV1() => OptionCodeJsonSchemaParser.Parse(ErackJson);

        /// <summary>
        /// 获取从 JSON 解析的 Molding 标准 Schema
        /// </summary>
        public static OptionCodeSchemaDefinition CreateMoldingV1() => OptionCodeJsonSchemaParser.Parse(MoldingJson);

        /// <summary>
        /// 获取全部预置测试 Schema 集合
        /// </summary>
        public static IEnumerable<OptionCodeSchemaDefinition> GetDefaultTestSchemas()
        {
            yield return CreateTxcDemoV1();
            yield return CreateErackV1();
            yield return CreateMoldingV1();
        }
    }
}

