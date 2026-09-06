using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Localization;
using Volo.Abp.OpenIddict.Applications;

namespace SIASUN.RCS.Pages;

/// <summary>
/// 宿主主页视图模型
/// </summary>
public class IndexModel : AbpPageModel
{
    /// <summary>
    /// OpenIddict 客户端应用列表
    /// </summary>
    public List<OpenIddictApplication>? Applications { get; protected set; }

    /// <summary>
    /// 系统语言列表
    /// </summary>
    public IReadOnlyList<LanguageInfo>? Languages { get; protected set; }

    /// <summary>
    /// 当前语言
    /// </summary>
    public string? CurrentLanguage { get; protected set; }

    /// <summary>
    /// OpenIddict 客户端仓储
    /// </summary>
    protected IOpenIddictApplicationRepository OpenIdApplicationRepository { get; }

    /// <summary>
    /// 语言提供者
    /// </summary>
    protected ILanguageProvider LanguageProvider { get; }

    /// <summary>
    /// 初始化主页视图模型
    /// </summary>
    /// <param name="openIdApplicationmRepository">应用仓储</param>
    /// <param name="languageProvider">语言提供者</param>
    public IndexModel(IOpenIddictApplicationRepository openIdApplicationmRepository, ILanguageProvider languageProvider)
    {
        OpenIdApplicationRepository = openIdApplicationmRepository;
        LanguageProvider = languageProvider;
    }

    public async Task OnGetAsync()
    {
        Applications = await OpenIdApplicationRepository.GetListAsync();

        Languages = await LanguageProvider.GetLanguagesAsync();
        CurrentLanguage = CultureInfo.CurrentCulture.DisplayName;
    }
}
