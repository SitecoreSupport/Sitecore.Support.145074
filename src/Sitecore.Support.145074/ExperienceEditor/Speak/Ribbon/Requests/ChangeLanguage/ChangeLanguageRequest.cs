using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Extensions;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using System;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.ChangeLanguage
{
    public class ChangeLanguageRequest : PipelineProcessorRequest<ValueItemContext>
    {
        private const string LanguageQueryKey = "sc_lang";

        protected UrlString BuidUrl(string initialUrl) =>
            new UrlString(WebUtil.GetRequestUri404(initialUrl));

        [Obsolete("This method is obsolete and will be removed in the next product version.")]
        protected string GetLanguageItem(Database database, string languageItemId)
        {
            Item item = database.GetItem(HttpUtility.UrlDecode(languageItemId));
            Assert.IsNotNull(item, "Could not find language with id {0}", new object[] { languageItemId });
            return item.Name;
        }

        public override PipelineProcessorResponseValue ProcessRequest()
        {
            string[] strArray = base.RequestContext.Value.Split(new char[] { '|' });
            if (strArray.Length != 2)
            {
                return new PipelineProcessorResponseValue { AbortMessage = "Missing language item id or current url" };
            }
            string languageName = strArray[0];
            UrlString url = this.BuidUrl(strArray[1]);
            ItemUri itemUri = new ItemUri(ID.Parse(base.RequestContext.ItemId), Language.Parse(base.RequestContext.Language), base.RequestContext.Item.Version, base.RequestContext.Database);
            UrlString str3 = BuildChangeLanguageUrl(url, itemUri, languageName);
            if (str3 == null)
            {
                return new PipelineProcessorResponseValue { AbortMessage = "Could not build change language URL" };
            }
            return new PipelineProcessorResponseValue { Value = str3.ToString() };
        }

        public static UrlString BuildChangeLanguageUrl(UrlString url, ItemUri itemUri, string languageName)
        {
            UrlString str = new UrlString(url.GetUrl());
            if (itemUri == null)
            {
                return null;
            }
            Item item = Database.GetItem(itemUri);
            SiteContext site = LinkManager.GetPreviewSiteContext(item) ?? SiteContext.GetSite(Settings.Preview.DefaultSite);
            if (site == null)
            {
                return null;
            }
            Item itemNotNull = Client.GetItemNotNull(itemUri);
            using (new SiteContextSwitcher(site))
            {
                using (new LanguageSwitcher(itemNotNull.Language))
                {
                    str = BuildChangeLanguageNewUrl(languageName, url, itemNotNull);
                    if (LinkManager.LanguageEmbedding == LanguageEmbedding.Never)
                    {
                        str["sc_lang"] = languageName;
                        return str;
                    }
                    str.Remove("sc_lang");
                    return str;
                }
            }
        }

        private static UrlString BuildChangeLanguageNewUrl(string languageName, UrlString url, Item item)
        {
            Language language;
            Assert.IsTrue(Language.TryParse(languageName, out language), $"Cannot parse the language ({languageName}).");
            UrlOptions defaultOptions = UrlOptions.DefaultOptions;
            defaultOptions.Language = language;
            Item item2 = item.Database.GetItem(item.ID, language);
            Assert.IsNotNull(item2, $"Item not found ({item.ID}, {language}).");
            string itemUrl = LinkManager.GetItemUrl(item2, defaultOptions);
            UrlString str2 = EnsureChangeLanguageUrlDomain(url, new UrlString(itemUrl));
            foreach (string str3 in url.Parameters.Keys)
            {
                str2.Parameters[str3] = url.Parameters[str3];
            }
            return str2;
        }

        private static UrlString EnsureChangeLanguageUrlDomain(UrlString oldUrl, UrlString newUrl)
        {
            if (newUrl.IsRelative() || oldUrl.IsRelative())
            {
                return newUrl;
            }
            Uri uri = oldUrl.ToAbsoluteUri(true);
            Uri uri2 = newUrl.ToAbsoluteUri(true);
            if (uri.DnsSafeHost.Equals(uri2.DnsSafeHost, StringComparison.OrdinalIgnoreCase))
            {
                return newUrl;
            }
            return new UrlString($"{uri2.Scheme}://{uri.DnsSafeHost}/{uri2.AbsolutePath.TrimStart(new char[] { '/' })}");
        }
    }
}