using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using System.Reflection;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.ChangeLanguage
{
  public class ChangeLanguageRequest : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.ChangeLanguage.ChangeLanguageRequest
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      AdditionalContextParameters additionalParams;
      if (!TryParseAdditionalContextParameters(RequestContext, out additionalParams))
      {
        return new PipelineProcessorResponseValue
        {
          AbortMessage = "Missing language item id or current url"
        };
      }

      var itemUri = new ItemUri(ID.Parse(RequestContext.ItemId), Language.Parse(RequestContext.Language), RequestContext.Item.Version, RequestContext.Database);

      UrlString urlToRedirect = BuildChangeLanguageUrl(additionalParams.OldUrl, itemUri, additionalParams.LanguageName);

      if (urlToRedirect == null)
      {
        return new PipelineProcessorResponseValue
        {
          AbortMessage = "Could not build change language URL"
        };
      }

      return new PipelineProcessorResponseValue
      {
        Value = urlToRedirect.ToString()
      };
    }

    public static UrlString BuildChangeLanguageUrl([NotNull] UrlString url, [CanBeNull] ItemUri itemUri, string languageName)
    {
      Assert.ArgumentNotNull(url, nameof(url));

      var changeLanguageUrl = new UrlString(url.GetUrl());
      const string ScLang = "sc_lang";

      if (itemUri == null)
      {
        return null;
      }

      //var site = SiteContext.GetSite(WebEditUtil.SiteName);
      
      var itemDatabase = itemUri.DatabaseName != null ? Database.GetDatabase(itemUri.DatabaseName) : null;
      var targetLanguage = !string.IsNullOrEmpty(languageName) ? Language.Parse(languageName) : null;
      var targetItem = (itemDatabase != null && targetLanguage != null) ? itemDatabase.GetItem(itemUri.ItemID, targetLanguage) : null;

      var site = targetItem != null ? 
        (LinkManager.GetPreviewSiteContext(targetItem) ?? SiteContext.GetSite(Settings.Preview.DefaultSite)) 
        : SiteContext.GetSite(Settings.Preview.DefaultSite);

      if (site == null)
      {
        return null;
      }

      var item = Client.GetItemNotNull(itemUri);

      using (new SiteContextSwitcher(site))
      {
        using (new LanguageSwitcher(item.Language))
        {
          var BuildChangeLanguageNewUrlMethod = typeof(WebUtility).GetMethod("BuildChangeLanguageNewUrl", BindingFlags.Static | BindingFlags.NonPublic);
          changeLanguageUrl = (UrlString)BuildChangeLanguageNewUrlMethod.Invoke(null, new object[] { languageName, url, item });
          changeLanguageUrl["sc_site"] = site.Name;

          //changeLanguageUrl = BuildChangeLanguageNewUrl(languageName, url, item);

          switch (LinkManager.LanguageEmbedding)
          {
            case LanguageEmbedding.Never:
              {
                changeLanguageUrl[ScLang] = languageName;

                break;
              }

            default:
              {
                changeLanguageUrl.Remove(ScLang);

                break;
              }
          }
        }
      }

      return changeLanguageUrl;
    }
  }
}