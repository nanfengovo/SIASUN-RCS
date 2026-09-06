using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Vehicles;

namespace SIASUN.RCS.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class RCSDbContext :
    AbpDbContext<RCSDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    public DbSet<AuditLogFilterRule> AuditLogFilterRules { get; set; } = null!;
    public DbSet<EntityAuditRule> EntityAuditRules { get; set; } = null!;
    public DbSet<SIASUN.RCS.Monitor.SystemEventLog> SystemEventLogs { get; set; } = null!;
    public DbSet<OperationLog> OperationLogs { get; set; } = null!;
    public DbSet<AgvTask> AgvTasks { get; set; } = null!;
    public DbSet<AgvVehicle> AgvVehicles { get; set; } = null!;

    #region Entities from the modules

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public RCSDbContext(DbContextOptions<RCSDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Configure your own tables/entities inside here */

        builder.Entity<AuditLogFilterRule>(b =>
        {
            b.ToTable(RCSConsts.DbTablePrefix + "AuditLogFilterRules", RCSConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(AuditLogFilterRuleConsts.MaxNameLength);
            b.Property(x => x.PathPattern).IsRequired().HasMaxLength(AuditLogFilterRuleConsts.MaxPathPatternLength);
            b.Property(x => x.HttpMethod).HasMaxLength(AuditLogFilterRuleConsts.MaxHttpMethodLength);
            b.Property(x => x.Description).HasMaxLength(AuditLogFilterRuleConsts.MaxDescriptionLength);
            b.HasIndex(x => new { x.RuleType, x.IsEnabled, x.Direction });
        });

        builder.Entity<EntityAuditRule>(b =>
        {
            b.ToTable(RCSConsts.DbTablePrefix + "EntityAuditRules", RCSConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.EntityTypePattern).IsRequired().HasMaxLength(256);
            b.HasIndex(x => x.Priority);
        });

        builder.Entity<SIASUN.RCS.Monitor.SystemEventLog>(b =>
        {
            b.ToTable(RCSConsts.DbTablePrefix + "SystemEventLogs", RCSConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.EventCategory).IsRequired().HasMaxLength(64);
            b.Property(x => x.Level).IsRequired().HasMaxLength(16);
            b.Property(x => x.Message).IsRequired().HasMaxLength(512);
            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => x.EventCategory);
        });

        builder.Entity<OperationLog>(b =>
        {
            b.ToTable(RCSConsts.DbTablePrefix + "OperationLogs", RCSConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Module).IsRequired().HasMaxLength(64);
            b.Property(x => x.Action).IsRequired().HasMaxLength(64);
            b.Property(x => x.TargetType).HasMaxLength(64);
            b.Property(x => x.TargetId).HasMaxLength(128);
            b.Property(x => x.UserName).HasMaxLength(128);
            b.Property(x => x.ClientIp).HasMaxLength(64);
            b.Property(x => x.CorrelationId).HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(1024);
            b.Property(x => x.ErrorMessage).HasMaxLength(2048);
            b.Property(x => x.BeforeState).HasMaxLength(512);
            b.Property(x => x.AfterState).HasMaxLength(512);
            b.Property(x => x.Reason).HasMaxLength(512);
            b.Property(x => x.TaskId).HasMaxLength(64);
            b.Property(x => x.AgvId).HasMaxLength(64);

            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => x.CorrelationId);
            b.HasIndex(x => x.TaskId);
            b.HasIndex(x => x.AgvId);
            b.HasIndex(x => new { x.TargetType, x.TargetId });
            b.HasIndex(x => new { x.Module, x.Action });
        });

        builder.Entity<AgvTask>(b =>
        {
            b.ToTable(RCSConsts.DbTablePrefix + "AgvTasks", RCSConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.TaskCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.WaitingEvent).HasMaxLength(128);
            b.Property(x => x.ActiveLeg).HasMaxLength(64);
            b.Property(x => x.AssignedVehicleCode).HasMaxLength(64);
            b.Property(x => x.FromStation).HasMaxLength(64);
            b.Property(x => x.ToStation).HasMaxLength(64);
            b.Property(x => x.CarrierCode).HasMaxLength(128);
            b.Property(x => x.BatchId).HasMaxLength(128);
            b.Property(x => x.OptionCode).HasMaxLength(256);
            b.Property(x => x.TraceId).HasMaxLength(64);
            b.Property(x => x.FailureReason).HasMaxLength(1024);

            b.HasIndex(x => x.TaskCode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AssignedVehicleId);
            b.HasIndex(x => x.TraceId);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<AgvVehicle>(b =>
        {
            b.ToTable(RCSConsts.DbTablePrefix + "AgvVehicles", RCSConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.VehicleCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.CurrentStation).HasMaxLength(64);
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.Property(x => x.ErrorMessage).HasMaxLength(1024);
            b.Property(x => x.CurrentTaskCode).HasMaxLength(64);

            b.HasIndex(x => x.VehicleCode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CurrentTaskId);
        });
    }
}
