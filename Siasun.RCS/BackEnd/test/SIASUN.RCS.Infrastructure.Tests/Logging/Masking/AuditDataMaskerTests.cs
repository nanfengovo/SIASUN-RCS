using Shouldly;
using SIASUN.RCS.Infrastructure.Logging.Masking;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Logging.Masking
{
    /// <summary>
    /// 日志敏感数据脱敏器单元测试
    /// </summary>
    public class AuditDataMaskerTests
    {
        [Fact]
        public void Mask_WithNullOrEmpty_ShouldReturnOriginal()
        {
            AuditDataMasker.Mask(null).ShouldBeNull();
            AuditDataMasker.Mask(string.Empty).ShouldBe(string.Empty);
            AuditDataMasker.Mask("   ").ShouldBe("   ");
        }

        [Fact]
        public void Mask_WithBearerToken_ShouldMaskToken()
        {
            var input = "GET /api/tasks HTTP/1.1\r\nAuthorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test\r\nHost: localhost";
            var result = AuditDataMasker.Mask(input);

            result.ShouldNotBeNull();
            result.ShouldNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test");
            result.ShouldContain("Authorization: Bearer ******");
        }

        [Fact]
        public void Mask_WithSensitiveJsonFields_ShouldMaskValues()
        {
            var json = "{\"username\": \"admin\", \"password\": \"P@ssw0rd123!\", \"secret\": \"my-api-secret\", \"access_token\": \"abc123xyz\"}";
            var result = AuditDataMasker.Mask(json);

            result.ShouldNotBeNull();
            result.ShouldContain("\"username\": \"admin\"");
            result.ShouldNotContain("P@ssw0rd123!");
            result.ShouldNotContain("my-api-secret");
            result.ShouldNotContain("abc123xyz");
            result.ShouldContain("\"password\": \"******\"");
            result.ShouldContain("\"secret\": \"******\"");
            result.ShouldContain("\"access_token\": \"******\"");
        }
    }
}
