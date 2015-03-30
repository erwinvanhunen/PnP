﻿using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Model;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    class ObjectComposedLook : ObjectHandlerBase
    {
        public override void ProvisionObjects(Web web, ProvisioningTemplate template)
        {
            TokenParser parser = new TokenParser(web);
            if (!web.IsPropertyAvailable("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }
            var relativeUrl = web.ServerRelativeUrl;
            if (!string.IsNullOrEmpty(template.ComposedLook.AlternateCSS))
            {
                var alternateCssUrl = parser.Parse(template.ComposedLook.AlternateCSS);
                web.AlternateCssUrl = alternateCssUrl;
                web.Update();
            }
            if (!string.IsNullOrEmpty(template.ComposedLook.SiteLogo))
            {
                var siteLogoUrl = parser.Parse(template.ComposedLook.SiteLogo);
                web.SiteLogoUrl = siteLogoUrl;
                web.Update();
            }
            if (!string.IsNullOrEmpty(template.ComposedLook.MasterPage))
            {
                var masterUrl = parser.Parse(template.ComposedLook.MasterPage);
                web.MasterUrl = masterUrl;
            }
            string colorFile = null;
            if (!string.IsNullOrEmpty(template.ComposedLook.ColorFile))
            {
                colorFile = parser.Parse(template.ComposedLook.ColorFile);
            }
            string backgroundFile = null;
            if (!string.IsNullOrEmpty(template.ComposedLook.BackgroundFile))
            {
                backgroundFile = parser.Parse(template.ComposedLook.BackgroundFile);
            }
            string fontFile = null;
            if (!string.IsNullOrEmpty(template.ComposedLook.FontFile))
            {
                fontFile = parser.Parse(template.ComposedLook.FontFile);
            }

            if (colorFile != null || fontFile != null || backgroundFile != null)
            {
                web.ApplyTheme(colorFile, fontFile, backgroundFile, true);
                web.Context.ExecuteQueryRetry();
            }
            // TODO: Add theme handling
        }

        public override ProvisioningTemplate CreateEntities(Web web, ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {
            // Load object if not there
            if (!web.IsObjectPropertyInstantiated("AlternateCssUrl"))
            {
                web.Context.Load(web);
                web.Context.ExecuteQuery();
            }

            // TODO: review
            template.ComposedLook = null;

            //var theme = web.GetComposedLook("Current");

            //// Get needed data from the site
            //// TODO: Access currently set theme for details
            //template.ComposedLook.AlternateCSS = web.AlternateCssUrl;
            //template.ComposedLook.BackgroundFile = theme.BackgroundImage;
            //template.ComposedLook.ColorFile = theme.Theme;
            //template.ComposedLook.FontFile = theme.Font;
            //template.ComposedLook.MasterPage = web.MasterUrl;
            //template.ComposedLook.Name = "";
            //template.ComposedLook.SiteLogo = web.SiteLogoUrl;

            // If a base template is specified then use that one to "cleanup" the generated template model
            if (baseTemplate != null)
            {
                template = CleanupEntities(template, baseTemplate);
            }

            return template;
        }

        private ProvisioningTemplate CleanupEntities(ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {

            return template;
        }
    }
}
