namespace SIASUN.RCS.Permissions;

public static class RCSPermissions
{
    public const string GroupName = "RCS";

    public static class AuditLogFilterRules
    {
        public const string Default = GroupName + ".AuditLogFilterRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
}
