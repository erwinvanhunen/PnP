﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using Field = Microsoft.SharePoint.Client.Field;
using View = OfficeDevPnP.Core.Framework.Provisioning.Model.View;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    public class ObjectListInstance : ObjectHandlerBase
    {
        public override void ProvisionObjects(Web web, ProvisioningTemplate template)
        {
            var parser = new TokenParser(web);
            parser.PreCache();

            if (!web.IsPropertyAvailable("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            web.Context.Load(web.AvailableContentTypes, cts => cts.Include(ct => ct.StringId));
            web.Context.Load(web.Lists, lc => lc.IncludeWithDefaultProperties(l => l.RootFolder.ServerRelativeUrl));

            web.Context.ExecuteQueryRetry();

            var existingContentTypes = web.AvailableContentTypes.ToList();
            var existingLists = web.Lists.Select(existingList => existingList.RootFolder.ServerRelativeUrl).ToList();
            var serverRelativeUrl = web.ServerRelativeUrl;

            var defaultCtId = string.Empty;
            var errorMessage = string.Empty;

            foreach (var list in template.Lists)
            {
                if (!existingLists.Contains(UrlUtility.Combine(serverRelativeUrl, list.Url)))
                {
                    List createdList = null;
                    var scope = new ExceptionHandlingScope(web.Context);
                    using (scope.StartScope())
                    {
                        using (scope.StartTry())
                        {
                            var listCreate = new ListCreationInformation();
                            listCreate.Description = list.Description;
                            listCreate.TemplateType = list.TemplateType;
                            listCreate.Title = list.Title;
                            listCreate.QuickLaunchOption = list.OnQuickLaunch ? QuickLaunchOptions.On : QuickLaunchOptions.Off;
                            listCreate.Url = list.Url;

                            createdList = web.Lists.Add(listCreate);

                            createdList.EnableVersioning = list.EnableVersioning;
                            if (!String.IsNullOrEmpty(list.DocumentTemplate))
                            {
                                createdList.DocumentTemplateUrl = list.DocumentTemplate;
                            }
                            createdList.Hidden = list.Hidden;
                            createdList.ContentTypesEnabled = list.ContentTypesEnabled;

                            createdList.Update();

                            // TODO: handle 'removedefaultcontenttype'

                            foreach (var ctBinding in list.ContentTypeBindings)
                            {
                                var ctToAdd = existingContentTypes.FirstOrDefault(ct => ct.StringId.ToLower() == ctBinding.ContentTypeID.ToLower());
                                if (ctToAdd != null)
                                {
                                    createdList.ContentTypes.AddExistingContentType(ctToAdd);

                                    if (ctBinding.Default)
                                    {
                                        defaultCtId = ctBinding.ContentTypeID;
                                    }
                                }
                            }

                            if (list.Fields.Any())
                            {
                                foreach (var field in list.Fields)
                                {
                                    var fieldXml = parser.Parse(field.SchemaXml);
                                    createdList.Fields.AddFieldAsXml(fieldXml, false, AddFieldOptions.DefaultValue);
                                }
                                createdList.Update();
                            }

                            if (list.FieldRefs.Any())
                            {
                                foreach (var fieldRef in list.FieldRefs)
                                {
                                    var field = web.Fields.GetById(fieldRef.ID);
                                    createdList.Fields.Add(field);

                                }
                                createdList.Update();
                            }

                            foreach (var view in list.Views)
                            {

                                var viewDoc = XDocument.Parse(view.SchemaXml);

                                var displayNameXml = viewDoc.Root.Attribute("DisplayName");
                                if (displayNameXml == null)
                                {
                                    errorMessage = "Invalid View element, missing a valid value for the attribute DisplayName.";
                                }
                                else
                                {
                                    var viewTitle = displayNameXml.Value;

                                    // Type
                                    var viewTypeString = viewDoc.Root.Attribute("Type") != null ? viewDoc.Root.Attribute("Type").Value : "None";
                                    viewTypeString = viewTypeString[0].ToString().ToUpper() + viewTypeString.Substring(1).ToLower();
                                    var viewType = (ViewType) Enum.Parse(typeof (ViewType), viewTypeString);

                                    // Fields
                                    string[] viewFields = null;
                                    var viewFieldsElement = viewDoc.Descendants("ViewFields").FirstOrDefault();
                                    if (viewFieldsElement != null)
                                    {
                                        viewFields = (from field in viewDoc.Descendants("ViewFields").Descendants("FieldRef") select field.Attribute("Name").Value).ToArray();
                                    }

                                    // Default view
                                    var viewDefault = viewDoc.Root.Attribute("DefaultView") != null && Boolean.Parse(viewDoc.Root.Attribute("DefaultView").Value);

                                    // Row limit
                                    bool viewPaged = true;
                                    uint viewRowLimit = 30;
                                    var rowLimitElement = viewDoc.Descendants("RowLimit").FirstOrDefault();
                                    if (rowLimitElement != null)
                                    {
                                        viewPaged = bool.Parse(rowLimitElement.Attribute("Paged").Value);
                                        viewRowLimit = uint.Parse(rowLimitElement.Value);
                                    }

                                    // Query
                                    var viewQuery = new StringBuilder();
                                    foreach (var queryElement in viewDoc.Descendants("Query").Elements())
                                    {
                                        viewQuery.Append(queryElement.ToString());
                                    }

                                    var viewCI = new ViewCreationInformation
                                    {
                                        ViewFields = viewFields,
                                        RowLimit = viewRowLimit,
                                        Paged = viewPaged,
                                        Title = viewTitle,
                                        Query = viewQuery.ToString(),
                                        ViewTypeKind = viewType,
                                        PersonalView = false,
                                        SetAsDefaultView = viewDefault
                                    };

                                    createdList.Views.Add(viewCI);
                                }
                                createdList.Update();
                            }
                        }
                        using (scope.StartCatch())
                        {

                        }
                        using (scope.StartFinally())
                        {

                        }
                    }
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        throw new ApplicationException(errorMessage);
                    }
                    web.Context.ExecuteQueryRetry();
                    if (!string.IsNullOrEmpty(defaultCtId) && createdList != null)
                    {
                        createdList.SetDefaultContentTypeToList(defaultCtId);
                    }
                }
            }

        }

        public override ProvisioningTemplate CreateEntities(Web web, ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {
            if (!web.IsPropertyAvailable("ServerRelativeUrl"))
            {
                web.Context.Load(web, w => w.ServerRelativeUrl);
                web.Context.ExecuteQueryRetry();
            }

            var serverRelativeUrl = web.ServerRelativeUrl;


            // For each list in the site
            ListCollection lists = web.Lists;
            web.Context.Load(lists, lc => lc.IncludeWithDefaultProperties(l => l.ContentTypes, l => l.Views, l => l.RootFolder.ServerRelativeUrl, l => l.Fields));
            web.Context.ExecuteQuery();
            foreach (var item in lists)
            {
                var contentTypeFields = new List<FieldRef>();
                ListInstance list = new ListInstance();
                list.Description = item.Description;
                list.EnableVersioning = item.EnableVersioning;
                list.TemplateType = item.BaseTemplate;
                list.Title = item.Title;
                list.Hidden = item.Hidden;
                list.DocumentTemplate = item.DocumentTemplateUrl;
                list.ContentTypesEnabled = item.ContentTypesEnabled;
                list.Url = item.RootFolder.ServerRelativeUrl.Substring(serverRelativeUrl.Length);

                int count = 0;
                foreach (var ct in item.ContentTypes)
                {
                    web.Context.Load(ct.FieldLinks);
                    web.Context.ExecuteQueryRetry();
                    foreach (var fieldLink in ct.FieldLinks)
                    {
                        contentTypeFields.Add(new FieldRef() { ID = fieldLink.Id });
                    }
                    list.ContentTypeBindings.Add(new ContentTypeBinding() { ContentTypeID = ct.StringId, Default = count == 0 ? true : false });
                    count++;
                }

                foreach (var view in item.Views)
                {
                    list.Views.Add(new View() { SchemaXml = view.ListViewXml });
                }

                var siteColumns = web.Fields;
                web.Context.Load(siteColumns, scs => scs.Include(sc => sc.Id));
                web.Context.ExecuteQueryRetry();

                foreach (var field in item.Fields)
                {
                    if (siteColumns.FirstOrDefault(sc => sc.Id == field.Id) != null)
                    {
                        if (contentTypeFields.FirstOrDefault(c => c.ID == field.Id) == null)
                        {
                            list.FieldRefs.Add(new FieldRef() { ID = field.Id });
                        }
                    }
                    else
                    {
                        list.Fields.Add((new Model.Field() { SchemaXml = field.SchemaXml }));
                    }
                }
                template.Lists.Add(list);
            }

            // If a base template is specified then use that one to "cleanup" the generated template model
            if (baseTemplate != null)
            {
                template = CleanupEntities(template, baseTemplate);
            }

            return template;
        }

        private ProvisioningTemplate CleanupEntities(ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {
            foreach (var list in baseTemplate.Lists)
            {
                int index = template.Lists.FindIndex(f => f.Url.Equals(list.Url) &&
                                                          f.TemplateType.Equals(list.TemplateType));

                if (index > -1)
                {
                    template.Lists.RemoveAt(index);
                }
            }

            return template;
        }
    }
}
