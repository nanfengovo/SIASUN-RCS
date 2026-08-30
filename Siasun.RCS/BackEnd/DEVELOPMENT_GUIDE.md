# SIASUN RCS 3.0 后端开发与架构配置指南

本文档是 RCS 后端项目的核心开发指南。随着项目的演进，所有涉及核心架构的变动、中间件拦截、第三方组件集成以及各类核心配置的最佳实践，都会持续补充到此文档中。

---

## 1. HTTP 报文拦截与日志审计架构

在 RCS 系统中，我们需要对 HTTP 请求的报文（Body）进行审计日志记录。但根据请求的方向，架构设计和注册方式有本质区别。

### 1.1 入站（Inbound）—— 针对外部请求的全局门卫
**类名**：`InboundAuditMiddleware`
**作用**：拦截所有外部系统（如 MES、WMS、前端 UI）主动向我们 RCS 发起的请求。
**注册方式**：
作为 ASP.NET Core 管道的全局中间件，在 `RCSHttpApiHostModule.cs` 的 `OnApplicationInitialization` 方法中进行注册。它相当于整个服务器的大门。

```csharp
// 在管道的靠前位置注册，确保能捕获最原始的请求报文
app.UseMiddleware<InboundAuditMiddleware>();
```

### 1.2 出站（Outbound）—— 针对内部发件的精准拦截
**类名**：`OutboundAuditDelegatingHandler`
**作用**：拦截我们 RCS 内部主动向外部系统（如 TM 调度系统、第三方设备 API）发起的 HTTP 请求。
**注册限制**：
**绝对不能**使用 `UseMiddleware` 注册。它属于 `HttpClient` 专属的“发件拦截器”。必须结合 `IHttpClientFactory` 进行精确的按需注册，避免污染其他不需要记录日志的内部 HTTP 客户端（如内部健康检查、OSS 上传等）。

**如何注册**：
在对接第三方系统的对应模块的 `ConfigureServices` 中完成两步注册：

```csharp
// 第一步：将 Handler 注册到 DI 容器（必须为 Transient 瞬态生命周期）
context.Services.AddTransient<OutboundAuditDelegatingHandler>();

// 第二步：将 Handler 挂载到专属的业务 HttpClient 上
context.Services.AddHttpClient("TM_System_Client", client => 
{
    client.BaseAddress = new Uri("http://192.168.1.100/api/");
})
.AddHttpMessageHandler<OutboundAuditDelegatingHandler>(); // 串接拦截器
```

---

## 2. Swagger 接口文档配置指南

RCS 系统的 Swagger 配置集中在宿主层的 `RCSHttpApiHostModule.cs`。为了避免接口全部混杂在一个页面中，我们采用了**多分组、按路由前缀精准分流**的策略。

目前已有四个基础分组：`system`（系统底层）、`business`（核心业务）、`adapters`（适配器）、`monitor`（监控）。

### 2.1 如何新增一个 Swagger 分组（以 WMS 为例）

如果你要新增一个模块的分组（假设名字叫 `wms`），需要修改 `RCSHttpApiHostModule.cs` 中的两个核心方法：

#### 步骤 1：配置生成器与路由分流策略 (`ConfigureSwagger` 方法)
在 `ConfigureSwagger` 方法中，找到 `options.SwaggerDoc` 区域，添加你的新分组，并修改下面的 `DocInclusionPredicate` 路由匹配规则：

```csharp
options.SwaggerDoc("system", new OpenApiInfo { Title = "ABP 系统底层基础接口", Version = "v1" });
options.SwaggerDoc("business", new OpenApiInfo { Title = "RCS 核心业务接口", Version = "v1" });
// 1. 新增你自己的分组定义
options.SwaggerDoc("wms", new OpenApiInfo { Title = "WMS 仓储系统对接接口", Version = "v1" });

options.DocInclusionPredicate((docName, description) =>
{
    var path = description.RelativePath ?? string.Empty;

    var isBusiness = path.StartsWith("api/rcs/", StringComparison.OrdinalIgnoreCase);
    var isAdapters = path.StartsWith("api/adapters/", StringComparison.OrdinalIgnoreCase);
    var isMonitor = path.StartsWith("api/monitor/", StringComparison.OrdinalIgnoreCase);
    
    // 2. 识别你的专属路由前缀
    var isWms = path.StartsWith("api/wms/", StringComparison.OrdinalIgnoreCase);

    return docName switch
    {
        "business" => isBusiness,
        "adapters" => isAdapters,
        "monitor" => isMonitor,
        "wms" => isWms, // 3. 命中你的分组
        
        // 4. 必须在 system 底层兜底逻辑里，把你的新路由排除掉
        "system" => !isBusiness && !isAdapters && !isMonitor && !isWms, 
        _ => true
    };
});
```

#### 步骤 2：配置 UI 下拉菜单 (`OnApplicationInitialization` 方法)
在下方的 `OnApplicationInitialization` 方法中，找到 `app.UseAbpSwaggerUI`，把你的新分组对应的 `swagger.json` 端点添加到 UI 界面右上角的下拉菜单中：

```csharp
app.UseAbpSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/business/swagger.json", "RCS 核心业务接口");
    options.SwaggerEndpoint("/swagger/adapters/swagger.json", "RCS 硬件和三方系统适配器接口");
    
    // 添加你的新下拉选项
    options.SwaggerEndpoint("/swagger/wms/swagger.json", "WMS 仓储系统对接接口");
});
```
*配置完成后，任何路由以 `api/wms/` 开头的 Controller 接口，就会自动被归类到专属的文档分组中。*

---

## 3. 后续扩展区域 (TBD)
*(后续如新增：数据库连接池配置、Redis 缓存策略、Serilog 诊断日志策略、消息队列消费组配置等，将持续补充于此)*


### 1.3 实体操作审计（Entity Tracker）—— 精确追踪聚合根数据变更
**类名**：`EntityAuditInterceptor` 和 `EntityAuditRuleEvaluator`
**架构详解**：请参考项目根目录的详尽设计文档：[Architecture_AuditLog_Design_And_Optimization.md](docs/architecture/Architecture_AuditLog_Design_And_Optimization.md)（注：文档保存在 AI 脑区，也可直接查阅代码设计意图）
**作用**：基于 EF Core 的 `SaveChangesInterceptor`，追踪重要业务实体（如 `AgvTask`）属性的具体变动（Old/New Value），用于故障追溯。
**设计亮点**：
- **异步解耦**：采用 `Channel<EntityAuditLogMessage>` 将高频的内存对象序列化与 SQLite I/O 持久化操作从主业务线程完全剥离。
- **极致轻量**：采用按月切片（Sharding）的 SQLite WAL 模式（`api_audit_log_yyyyMM.db`），解决工控机磁盘容量与并发锁问题。无脑基于 `File.Delete` 的清理任务做到 O 锁释放。
- **富聚合与热更新**：通过 Domain Event 实时刷新 `EntityAuditRuleEvaluator` 的前缀/后缀通配规则树，实现运行时动态无缝调级（Skip -> Summary -> Full）。
