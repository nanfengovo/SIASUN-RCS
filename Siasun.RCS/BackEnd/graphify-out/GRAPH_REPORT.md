# Graph Report - BackEnd  (2026-08-28)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 922 nodes · 1386 edges · 71 communities (69 shown, 2 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 81 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `cf5b2a63`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- RCSDbMigrationService
- Index.cshtml
- AuditLogFilterRule
- IndexModel
- .WithUnitOfWorkAsync
- SIASUN.RCS.Auditing
- RCSHttpApiHostModule
- RCSDbContext
- InboundAuditMiddleware
- OpenIddictDataSeedContributor
- OutboundAuditDelegatingHandler
- SIASUN.RCS.slnx
- DbMigratorHostedService
- .Should_Create_Update_Toggle_And_Delete_FilterRule
- SIASUN.RCS.HttpApi.Host
- ClientDemoService
- AuditLogFilterRuleAppService
- ApiAuditLogEntry
- RCSEntityFrameworkCoreTestModule
- ApiAuditLogConsumer
- .SaveBatchAsync_WithRealSqlite_ShouldInsertSuccessfully
- ChangeIdentityPasswordPolicySettingDefinitionProvider
- SIASUN.RCS.EntityFrameworkCore
- SIASUN.RCS.Domain
- SIASUN.RCS.sln
- SIASUN.RCS.EntityFrameworkCore
- AuditLogSqliteDbContext
- SIASUN.RCS.Domain.Shared
- SIASUN.RCS.HttpApi.Client
- net10.0
- SIASUN.RCS.TestBase
- GetAuditLogFilterRulesInput
- CreateAuditLogFilterRuleDto
- RCSTestBase
- SIASUN.RCS
- AuditLogFilterRuleDto
- UpdateAuditLogFilterRuleDto
- Initial
- package.json
- RCSTestBaseModule
- AbpModule
- ApiAuditLogChannel
- Added_AuditLogFilterRules
- .BuildModel
- .SaveBatchAsync
- RCSApplicationContractsModule
- .ConfigureServices
- FakeCurrentPrincipalAccessor
- SIASUN.RCS.HttpApi
- TestModel
- .BuildTargetModel
- .BuildTargetModel
- RCSHttpApiClientModule
- List
- RCSEntityFrameworkCoreTestBase
- generate-coverage.sh

## God Nodes (most connected - your core abstractions)
1. `RCSDbContext` - 33 edges
2. `SIASUN.RCS.Auditing` - 32 edges
3. `ApiAuditLogEntry` - 29 edges
4. `AuditLogFilterRuleDto` - 25 edges
5. `SIASUN.RCS.HttpApi.Host` - 25 edges
6. `SIASUN.RCS` - 24 edges
7. `AuditLogFilterRule` - 22 edges
8. `SIASUN.RCS.Domain` - 22 edges
9. `SIASUN.RCS.EntityFrameworkCore` - 20 edges
10. `RCSDbMigrationService` - 19 edges

## Surprising Connections (you probably didn't know these)
- `OutboundAuditDelegatingHandlerTests` --references--> `ApiAuditLogChannel`  [EXTRACTED]
  test/SIASUN.RCS.Infrastructure.Tests/OutboundAuditDelegatingHandlerTests.cs → src/05.Infrastructure/SIASUN.RCS.Infrastructure.Logging/ApiAuditLogChannel.cs
- `OutboundAuditDelegatingHandlerTests` --references--> `IAuditLogFilterEvaluator`  [EXTRACTED]
  test/SIASUN.RCS.Infrastructure.Tests/OutboundAuditDelegatingHandlerTests.cs → src/05.Infrastructure/SIASUN.RCS.Infrastructure.Logging/Filtering/IAuditLogFilterEvaluator.cs
- `ApiAuditLogConsumerTests` --references--> `IApiAuditLogStore`  [EXTRACTED]
  test/SIASUN.RCS.Infrastructure.Tests/ApiAuditLogConsumerTests.cs → src/02.Domain/SIASUN.RCS.Domain/Auditing/IApiAuditLogStore.cs
- `ApiAuditLogConsumerTests` --references--> `ApiAuditLogChannel`  [EXTRACTED]
  test/SIASUN.RCS.Infrastructure.Tests/ApiAuditLogConsumerTests.cs → src/05.Infrastructure/SIASUN.RCS.Infrastructure.Logging/ApiAuditLogChannel.cs
- `InboundAuditMiddlewareTests` --references--> `ApiAuditLogChannel`  [EXTRACTED]
  test/SIASUN.RCS.Infrastructure.Tests/InboundAuditMiddlewareTests.cs → src/05.Infrastructure/SIASUN.RCS.Infrastructure.Logging/ApiAuditLogChannel.cs

## Import Cycles
- None detected.

## Communities (71 total, 2 thin omitted)

### Community 0 - "RCSDbMigrationService"
Cohesion: 0.05
Nodes (32): AbpEndpointRouterOptions, Action, SIASUN.RCS.Data, SIASUN.RCS.HealthChecks, HealthCheckContext, HealthCheckResult, IEnumerable, IHealthCheck (+24 more)

### Community 1 - "Index.cshtml"
Cohesion: 0.05
Nodes (35): AbpControllerBase, AccountResource, SIASUN.RCS.Localization, SIASUN.RCS.Controllers, SIASUN.RCS.Permissions, DefaultBrandingProvider, IBrandingProvider, ICurrentUser (+27 more)

### Community 2 - "AuditLogFilterRule"
Cohesion: 0.06
Nodes (37): Expression, FullAuditedAggregateRoot, Regex, Guid, AuditLogFilterRule, Description, Direction, HttpMethod (+29 more)

### Community 3 - "IndexModel"
Cohesion: 0.05
Nodes (28): AbpExceptionLocalizationOptions, AbpPageModel, SIASUN.RCS.Pages, ILanguageProvider, IOpenIddictApplicationRepository, LanguageInfo, OpenIddictApplication, AbpLocalizationOptions (+20 more)

### Community 4 - ".WithUnitOfWorkAsync"
Cohesion: 0.05
Nodes (31): AbpUnitOfWorkOptions, SIASUN.RCS.EntityFrameworkCore.Domains, SIASUN.RCS.Samples, SIASUN.RCS.EntityFrameworkCore.Applications, SIASUN.RCS.EntityFrameworkCore.Samples, IdentityUserManager, IIdentityUserRepository, IUnitOfWorkManager (+23 more)

### Community 5 - "SIASUN.RCS.Auditing"
Cohesion: 0.10
Nodes (16): SIASUN.RCS.Auditing, SIASUN.RCS.Infrastructure.Logging.Filtering, SIASUN.RCS.Infrastructure.AuditLog.Sqlite, SIASUN.RCS.Infrastructure.Logging, SIASUN.RCS.Infrastructure.Tests, SIASUN.RCS.MultiTenancy, AuditLogFilterRuleConsts, Direction (+8 more)

### Community 6 - "RCSHttpApiHostModule"
Cohesion: 0.10
Nodes (19): AbpAspNetCoreMvcOptions, AbpBundlingOptions, AbpClaimsPrincipalFactoryOptions, AbpMvcLibsOptions, AbpOpenIddictAspNetCoreOptions, AbpStudioClientOptions, AppUrlOptions, ForwardedHeadersOptions (+11 more)

### Community 7 - "RCSDbContext"
Cohesion: 0.06
Nodes (30): AbpDbContext, IConfigurationRoot, IdentityClaimType, IdentityLinkUser, IdentityRole, IdentitySecurityLog, IdentitySession, IdentityUserDelegation (+22 more)

### Community 8 - "InboundAuditMiddleware"
Cohesion: 0.12
Nodes (19): HttpContext, ILocalEventHandler, InlineData, RequestDelegate, Task, AuditFilterRulesChangedEventHandler, Direction, Task (+11 more)

### Community 9 - "OpenIddictDataSeedContributor"
Cohesion: 0.09
Nodes (18): SIASUN.RCS.OpenIddict, IDataSeedContributor, IGuidGenerator, OpenIddictDataSeedContributorBase, DataSeedContext, Guid, IRepository, IServiceProvider (+10 more)

### Community 10 - "OutboundAuditDelegatingHandler"
Cohesion: 0.17
Nodes (16): DelegatingHandler, HttpRequestException, CancellationToken, HttpRequestMessage, HttpResponseMessage, Task, OutboundAuditDelegatingHandler, CancellationToken (+8 more)

### Community 11 - "SIASUN.RCS.slnx"
Cohesion: 0.13
Nodes (22): Volo.Abp.Account.Application (10.5.0), Volo.Abp.Account.Application.Contracts (10.5.0), Volo.Abp.FeatureManagement.Application (10.5.0), Volo.Abp.FeatureManagement.Application.Contracts (10.5.0), Volo.Abp.Identity.Application (10.5.0), Volo.Abp.Identity.Application.Contracts (10.5.0), Volo.Abp.PermissionManagement.Application (10.5.0), Volo.Abp.PermissionManagement.Application.Contracts (10.5.0) (+14 more)

### Community 12 - "DbMigratorHostedService"
Cohesion: 0.14
Nodes (11): SIASUN.RCS.DbMigrator, IHostApplicationLifetime, IHostBuilder, IHostedService, CancellationToken, IConfiguration, Task, DbMigratorHostedService (+3 more)

### Community 13 - ".Should_Create_Update_Toggle_And_Delete_FilterRule"
Cohesion: 0.20
Nodes (9): IApplicationService, RCSApplicationTestBase, Guid, PagedResultDto, Task, IAuditLogFilterRuleAppService, Fact, Task (+1 more)

### Community 14 - "SIASUN.RCS.HttpApi.Host"
Cohesion: 0.11
Nodes (18): AspNetCore.HealthChecks.UI (9.0.0), AspNetCore.HealthChecks.UI.Client (9.0.0), AspNetCore.HealthChecks.UI.InMemory.Storage (9.0.0), IdentityModel (7.0.0), KubernetesClient (18.0.5), Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation (10.0.9), Microsoft.EntityFrameworkCore.InMemory (10.0.9), Volo.Abp.Account.Web.OpenIddict (10.5.0) (+10 more)

### Community 15 - "ClientDemoService"
Cohesion: 0.14
Nodes (10): AbpHttpClientBuilderOptions, SIASUN.RCS.HttpApi.Client.ConsoleTestApp, IProfileAppService, IIdentityUserAppService, Task, ClientDemoService, Task, Program (+2 more)

### Community 16 - "AuditLogFilterRuleAppService"
Cohesion: 0.29
Nodes (8): Authorize, ILocalEventBus, AuditFilterRulesChangedEvent, Guid, IRepository, PagedResultDto, Task, AuditLogFilterRuleAppService

### Community 17 - "ApiAuditLogEntry"
Cohesion: 0.12
Nodes (15): DateTime, ApiAuditLogEntry, ClientIpAddress, ClientName, CreationTime, Direction, ElapsedMs, HttpMethod (+7 more)

### Community 18 - "RCSEntityFrameworkCoreTestModule"
Cohesion: 0.17
Nodes (10): AbpSqliteOptions, ApplicationShutdownContext, FeatureManagementOptions, IRelationalDatabaseCreator, PermissionManagementOptions, SqliteConnection, AbpDbContextOptions, IServiceCollection (+2 more)

### Community 19 - "ApiAuditLogConsumer"
Cohesion: 0.29
Nodes (10): BackgroundService, InvalidOperationException, ILogger, ApiAuditLogConsumer, CancellationToken, Fact, ILogger, IReadOnlyList (+2 more)

### Community 20 - ".SaveBatchAsync_WithRealSqlite_ShouldInsertSuccessfully"
Cohesion: 0.19
Nodes (11): CancellationToken, DateTime, ILogger, IReadOnlyList, IServiceScopeFactory, Task, SqliteApiAuditLogStore, Fact (+3 more)

### Community 21 - "ChangeIdentityPasswordPolicySettingDefinitionProvider"
Cohesion: 0.15
Nodes (8): SIASUN.RCS.Identity, SIASUN.RCS.Settings, SettingDefinitionProvider, ISettingDefinitionContext, ChangeIdentityPasswordPolicySettingDefinitionProvider, ISettingDefinitionContext, RCSSettingDefinitionProvider, RCSSettings

### Community 22 - "SIASUN.RCS.EntityFrameworkCore"
Cohesion: 0.15
Nodes (13): Microsoft.EntityFrameworkCore.Design (10.0.9), Microsoft.EntityFrameworkCore.Tools (10.0.9), Volo.Abp.AuditLogging.EntityFrameworkCore (10.5.0), Volo.Abp.BackgroundJobs.EntityFrameworkCore (10.5.0), Volo.Abp.BlobStoring.Database.EntityFrameworkCore (10.5.0), Volo.Abp.EntityFrameworkCore.SqlServer (10.5.0), Volo.Abp.FeatureManagement.EntityFrameworkCore (10.5.0), Volo.Abp.Identity.EntityFrameworkCore (10.5.0) (+5 more)

### Community 23 - "SIASUN.RCS.Domain"
Cohesion: 0.15
Nodes (13): Volo.Abp.AuditLogging.Domain (10.5.0), Volo.Abp.BackgroundJobs.Domain (10.5.0), Volo.Abp.BlobStoring.Database.Domain (10.5.0), Volo.Abp.Caching (10.5.0), Volo.Abp.Emailing (10.5.0), Volo.Abp.FeatureManagement.Domain (10.5.0), Volo.Abp.Identity.Domain (10.5.0), Volo.Abp.OpenIddict.Domain (10.5.0) (+5 more)

### Community 24 - "SIASUN.RCS.sln"
Cohesion: 0.17
Nodes (5): SIASUN.RCS.DbMigrator, Microsoft.Extensions.Hosting (10.0.9), Serilog.AspNetCore (9.0.0), Serilog.Sinks.Async (2.1.0), Volo.Abp.Autofac (10.5.0)

### Community 25 - "SIASUN.RCS.EntityFrameworkCore"
Cohesion: 0.21
Nodes (6): SIASUN.RCS.EntityFrameworkCore, ICollectionFixture, IDisposable, RCSEntityFrameworkCoreCollection, RCSEntityFrameworkCoreCollectionFixtureBase, RCSEntityFrameworkCoreFixture

### Community 26 - "AuditLogSqliteDbContext"
Cohesion: 0.18
Nodes (8): DbContext, DbSet, ModelBuilder, AuditLogSqliteDbContext, ApiAuditLogs, ApplicationInitializationContext, ServiceConfigurationContext, RCSInfrastructureAuditLogSqliteModule

### Community 27 - "SIASUN.RCS.Domain.Shared"
Cohesion: 0.17
Nodes (12): Microsoft.Extensions.FileProviders.Embedded (10.0.9), Volo.Abp.AuditLogging.Domain.Shared (10.5.0), Volo.Abp.BackgroundJobs.Domain.Shared (10.5.0), Volo.Abp.BlobStoring.Database.Domain.Shared (10.5.0), Volo.Abp.FeatureManagement.Domain.Shared (10.5.0), Volo.Abp.GlobalFeatures (10.5.0), Volo.Abp.Identity.Domain.Shared (10.5.0), Volo.Abp.OpenIddict.Domain.Shared (10.5.0) (+4 more)

### Community 28 - "SIASUN.RCS.HttpApi.Client"
Cohesion: 0.17
Nodes (12): Microsoft.Extensions.Http.Polly (10.0.9), Volo.Abp.Account.HttpApi.Client (10.5.0), Volo.Abp.FeatureManagement.HttpApi.Client (10.5.0), Volo.Abp.Http.Client.IdentityModel (10.5.0), Volo.Abp.Identity.HttpApi.Client (10.5.0), Volo.Abp.PermissionManagement.HttpApi.Client (10.5.0), Volo.Abp.SettingManagement.HttpApi.Client (10.5.0), Volo.Abp.TenantManagement.HttpApi.Client (10.5.0) (+4 more)

### Community 29 - "net10.0"
Cohesion: 0.20
Nodes (12): Microsoft.IO.RecyclableMemoryStream (3.0.1), Serilog.Formatting.Compact (3.0.0), Volo.Abp.Core (10.5.0), SIASUN.RCS.Infrastructure.AuditLog.Sqlite, Volo.Abp.EntityFrameworkCore.Sqlite (10.5.0), SIASUN.RCS.Infrastructure.Logging, Serilog.Sinks.Async (2.1.0), SIASUN.RCS.Infrastructure.Tests (+4 more)

### Community 30 - "SIASUN.RCS.TestBase"
Cohesion: 0.17
Nodes (12): NSubstitute (5.3.0), NSubstitute.Analyzers.CSharp (1.0.17), Shouldly (4.3.0), Volo.Abp.Authorization (10.5.0), Volo.Abp.BackgroundJobs.Abstractions (10.5.0), Volo.Abp.TestBase (10.5.0), xunit.extensibility.execution (2.9.3), SIASUN.RCS.TestBase (+4 more)

### Community 31 - "GetAuditLogFilterRulesInput"
Cohesion: 0.17
Nodes (10): PagedAndSortedResultRequestDto, FilterDirection, Both, Inbound, Outbound, GetAuditLogFilterRulesInput, Direction, Filter (+2 more)

### Community 32 - "CreateAuditLogFilterRuleDto"
Cohesion: 0.17
Nodes (11): FilterRuleType, Blacklist, Whitelist, CreateAuditLogFilterRuleDto, Description, Direction, HttpMethod, IsEnabled (+3 more)

### Community 33 - "RCSTestBase"
Cohesion: 0.18
Nodes (6): AbpApplicationCreationOptions, AbpIntegratedTest, RCSApplicationTestBase, RCSDomainTestBase, IServiceCollection, RCSTestBase

### Community 34 - "SIASUN.RCS"
Cohesion: 0.18
Nodes (6): ApplicationService, SIASUN.RCS, RCSConsts, RCSDomainErrorCodes, RCSAppService, RCSTestConsts

### Community 35 - "AuditLogFilterRuleDto"
Cohesion: 0.18
Nodes (11): FullAuditedEntityDto, Guid, AuditLogFilterRuleDto, ConcurrencyStamp, Description, Direction, HttpMethod, IsEnabled (+3 more)

### Community 36 - "UpdateAuditLogFilterRuleDto"
Cohesion: 0.20
Nodes (10): IHasConcurrencyStamp, UpdateAuditLogFilterRuleDto, ConcurrencyStamp, Description, Direction, HttpMethod, IsEnabled, Name (+2 more)

### Community 37 - "Initial"
Cohesion: 0.22
Nodes (6): Migration, Initial, DateTime, DateTimeOffset, Guid, MigrationBuilder

### Community 38 - "package.json"
Cohesion: 0.22
Nodes (8): @abp/aspnetcore.mvc.ui.theme.leptonxlite, dependencies, @abp/aspnetcore.mvc.ui.theme.leptonxlite, name, private, resolutions, jquery, version

### Community 39 - "RCSTestBaseModule"
Cohesion: 0.28
Nodes (5): AbpBackgroundJobOptions, ApplicationInitializationContext, IDataSeeder, ServiceConfigurationContext, RCSTestBaseModule

### Community 40 - "AbpModule"
Cohesion: 0.22
Nodes (5): AbpModule, RCSApplicationModule, RCSInfrastructureLoggingModule, RCSApplicationTestModule, RCSDomainTestModule

### Community 41 - "ApiAuditLogChannel"
Cohesion: 0.22
Nodes (8): AuditLogFilterEvaluator, Channel, ChannelReader, ApiAuditLogChannel, Reader, IAuditLogFilterEvaluator, RecyclableMemoryStreamManager, ServiceConfigurationContext

### Community 42 - "Added_AuditLogFilterRules"
Cohesion: 0.28
Nodes (5): SIASUN.RCS.Migrations, DateTime, Guid, MigrationBuilder, Added_AuditLogFilterRules

### Community 43 - ".BuildModel"
Cohesion: 0.25
Nodes (6): ModelSnapshot, DateTime, DateTimeOffset, Guid, ModelBuilder, RCSDbContextModelSnapshot

### Community 44 - ".SaveBatchAsync"
Cohesion: 0.32
Nodes (5): CancellationToken, DateTime, IReadOnlyList, Task, IApiAuditLogStore

### Community 45 - "RCSApplicationContractsModule"
Cohesion: 0.25
Nodes (4): ServiceConfigurationContext, RCSApplicationContractsModule, OneTimeRunner, RCSDtoExtensions

### Community 46 - ".ConfigureServices"
Cohesion: 0.29
Nodes (5): AbpMultiTenancyOptions, IEmailSender, NullEmailSender, ServiceConfigurationContext, RCSDomainModule

### Community 47 - "FakeCurrentPrincipalAccessor"
Cohesion: 0.38
Nodes (4): ClaimsPrincipal, SIASUN.RCS.Security, FakeCurrentPrincipalAccessor, ThreadCurrentPrincipalAccessor

### Community 48 - "SIASUN.RCS.HttpApi"
Cohesion: 0.29
Nodes (7): Volo.Abp.Account.HttpApi (10.5.0), Volo.Abp.FeatureManagement.HttpApi (10.5.0), Volo.Abp.Identity.HttpApi (10.5.0), Volo.Abp.PermissionManagement.HttpApi (10.5.0), Volo.Abp.SettingManagement.HttpApi (10.5.0), Volo.Abp.TenantManagement.HttpApi (10.5.0), SIASUN.RCS.HttpApi

### Community 49 - "TestModel"
Cohesion: 0.33
Nodes (5): SIASUN.RCS.Models.Test, DateTime, TestModel, BirthDate, Name

### Community 50 - ".BuildTargetModel"
Cohesion: 0.40
Nodes (4): DateTime, DateTimeOffset, Guid, ModelBuilder

### Community 51 - ".BuildTargetModel"
Cohesion: 0.40
Nodes (4): DateTime, DateTimeOffset, Guid, ModelBuilder

### Community 52 - "RCSHttpApiClientModule"
Cohesion: 0.40
Nodes (3): AbpVirtualFileSystemOptions, ServiceConfigurationContext, RCSHttpApiClientModule

### Community 53 - "List"
Cohesion: 0.50
Nodes (3): List, CancellationToken, Task

## Knowledge Gaps
- **229 isolated node(s):** `AuditLogFilterRules`, `RCSSettings`, `RCSConsts`, `RCSDomainErrorCodes`, `RCSTestConsts` (+224 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SIASUN.RCS.Auditing` connect `SIASUN.RCS.Auditing` to `CreateAuditLogFilterRuleDto`, `Index.cshtml`, `AuditLogFilterRule`, `.WithUnitOfWorkAsync`, `OpenIddictDataSeedContributor`, `.SaveBatchAsync`, `.Should_Create_Update_Toggle_And_Delete_FilterRule`, `AuditLogFilterRuleAppService`, `ApiAuditLogEntry`, `SIASUN.RCS.EntityFrameworkCore`, `AuditLogSqliteDbContext`, `GetAuditLogFilterRulesInput`?**
  _High betweenness centrality (0.149) - this node is a cross-community bridge._
- **Why does `SIASUN.RCS` connect `SIASUN.RCS` to `Index.cshtml`, `RCSTestBase`, `IndexModel`, `SIASUN.RCS.Auditing`, `RCSHttpApiHostModule`, `RCSTestBaseModule`, `AbpModule`, `OpenIddictDataSeedContributor`, `RCSApplicationContractsModule`, `.ConfigureServices`, `RCSHttpApiClientModule`?**
  _High betweenness centrality (0.126) - this node is a cross-community bridge._
- **Why does `SIASUN.RCS.EntityFrameworkCore` connect `SIASUN.RCS.EntityFrameworkCore` to `RCSDbMigrationService`, `IndexModel`, `Initial`, `SIASUN.RCS.Auditing`, `RCSDbContext`, `Added_AuditLogFilterRules`, `.BuildModel`, `DbMigratorHostedService`, `RCSEntityFrameworkCoreTestBase`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `ApiAuditLogEntry` (e.g. with `.InvokeAsync()` and `.SendAsync()`) actually correct?**
  _`ApiAuditLogEntry` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `AuditLogFilterRules`, `RCSSettings`, `RCSConsts` to the rest of the system?**
  _229 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `RCSDbMigrationService` be split into smaller, more focused modules?**
  _Cohesion score 0.053109713487071976 - nodes in this community are weakly interconnected._
- **Should `Index.cshtml` be split into smaller, more focused modules?**
  _Cohesion score 0.04717853839037928 - nodes in this community are weakly interconnected._