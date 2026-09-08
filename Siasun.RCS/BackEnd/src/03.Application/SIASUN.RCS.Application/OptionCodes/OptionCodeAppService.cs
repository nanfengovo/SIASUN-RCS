using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.OptionCodes.Dtos;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 动态位图编译、逆向反解与快照固化应用服务实现
    /// </summary>
    [Authorize]
    public class OptionCodeAppService : ApplicationService, IOptionCodeAppService
    {
        private readonly IOptionCodeSchemaRegistry _schemaRegistry;
        private readonly IOptionCodeAssembler _assembler;
        private readonly IOptionCodeEncoder _encoder;
        private readonly IOptionCodeDecoder _decoder;
        private readonly IRepository<AgvTask, Guid> _taskRepository;

        /// <summary>
        /// 构造函数，注入 Schema 注册中心、装配器、正向编码器、逆向解码器与任务仓储
        /// </summary>
        public OptionCodeAppService(
            IOptionCodeSchemaRegistry schemaRegistry,
            IOptionCodeAssembler assembler,
            IOptionCodeEncoder encoder,
            IOptionCodeDecoder decoder,
            IRepository<AgvTask, Guid> taskRepository)
        {
            _schemaRegistry = schemaRegistry;
            _assembler = assembler;
            _encoder = encoder;
            _decoder = decoder;
            _taskRepository = taskRepository;
        }

        /// <inheritdoc />
        [AllowAnonymous]
        public Task<IReadOnlyList<OptionCodeSchemaDefinition>> GetSchemasAsync()
        {
            return Task.FromResult(_schemaRegistry.GetAll());
        }

        /// <inheritdoc />
        [AllowAnonymous]
        public Task<OptionCodeSchemaDefinition> GetSchemaAsync(string schemaCode, int? version = null)
        {
            Check.NotNullOrWhiteSpace(schemaCode, nameof(schemaCode));
            return Task.FromResult(_schemaRegistry.Get(schemaCode, version));
        }

        /// <inheritdoc />
        public Task<CompileOptionCodeResultDto> CompileAsync(CompileOptionCodeInput input)
        {
            Check.NotNull(input, nameof(input));
            Check.NotNullOrWhiteSpace(input.SchemaCode, nameof(input.SchemaCode));

            var schema = _schemaRegistry.Get(input.SchemaCode, input.SchemaVersion);

            var context = new OptionCodeAssembleContext
            {
                TaskArgs = input.TaskArgs,
                MasterValues = input.MasterValues,
                ActiveLeg = input.ActiveLeg,
                Port = input.Port,
                CarrierCode = input.CarrierCode,
                AgvCode = input.AgvCode
            };

            var assembled = _assembler.Assemble(schema, context);
            var words = _encoder.EncodeToWords(schema, assembled);
            var optionCodeStr = _encoder.Encode(schema, assembled);
            var decoded = _decoder.DecodeWords(schema, words);

            return Task.FromResult(new CompileOptionCodeResultDto
            {
                OptionCode = optionCodeStr,
                SchemaCode = schema.Code,
                SchemaVersion = schema.Version,
                Words = words,
                DecodedDetail = decoded
            });
        }

        /// <inheritdoc />
        [AllowAnonymous]
        public Task<DecodedOptionCodeResult> DecodeAsync(DecodeOptionCodeInput input)
        {
            Check.NotNull(input, nameof(input));
            Check.NotNullOrWhiteSpace(input.OptionCode, nameof(input.OptionCode));
            Check.NotNullOrWhiteSpace(input.SchemaCode, nameof(input.SchemaCode));

            var schema = _schemaRegistry.Get(input.SchemaCode, input.SchemaVersion);
            var decoded = _decoder.Decode(schema, input.OptionCode);

            return Task.FromResult(decoded);
        }

        /// <inheritdoc />
        public async Task<CompileOptionCodeResultDto> FreezeTaskOptionCodeAsync(FreezeTaskOptionCodeInput input)
        {
            Check.NotNull(input, nameof(input));
            Check.NotNullOrWhiteSpace(input.SchemaCode, nameof(input.SchemaCode));

            var task = await _taskRepository.GetAsync(input.TaskId);
            var schema = _schemaRegistry.Get(input.SchemaCode, input.SchemaVersion);

            string optionCodeStr;
            IReadOnlyList<uint> words;
            DecodedOptionCodeResult decoded;

            if (!string.IsNullOrWhiteSpace(input.ExplicitOptionCode))
            {
                optionCodeStr = input.ExplicitOptionCode.Trim();
                decoded = _decoder.Decode(schema, optionCodeStr);
                var wordList = new List<uint>();
                foreach (var part in decoded.Parts)
                {
                    wordList.Add(part.RawWord);
                }
                words = wordList;
            }
            else
            {
                var context = new OptionCodeAssembleContext
                {
                    TaskArgs = input.TaskArgs,
                    MasterValues = input.MasterValues,
                    ActiveLeg = input.ActiveLeg ?? task.ActiveLeg,
                    Port = input.Port ?? task.ToStation,
                    CarrierCode = task.CarrierCode,
                    AgvCode = task.AssignedVehicleCode
                };

                var assembled = _assembler.Assemble(schema, context);
                words = _encoder.EncodeToWords(schema, assembled);
                optionCodeStr = _encoder.Encode(schema, assembled);
                decoded = _decoder.DecodeWords(schema, words);
            }

            task.FreezeOptionCode(optionCodeStr, schema.Code, schema.Version);
            await _taskRepository.UpdateAsync(task, autoSave: true);

            return new CompileOptionCodeResultDto
            {
                OptionCode = optionCodeStr,
                SchemaCode = schema.Code,
                SchemaVersion = schema.Version,
                Words = words,
                DecodedDetail = decoded
            };
        }
    }
}

