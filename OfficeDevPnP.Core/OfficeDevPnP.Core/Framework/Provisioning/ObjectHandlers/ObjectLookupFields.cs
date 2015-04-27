using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    public class ObjectLookupFields : ObjectHandlerBase
    {
        public override string Name
        {
            get { return "Lookup Fields"; }
        }

        public ObjectLookupFields()
        {
            this.ReportProgress = false;
        }

        public override void ProvisionObjects(Web web, ProvisioningTemplate template)
        {
            ProcessLookupFields(web, template);
        }

        public override ProvisioningTemplate CreateEntities(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo)
        {
            return template;
        }

        private void ProcessLookupFields(Web web, ProvisioningTemplate template)
        {
            var count = template.SiteFields.Count + template.Lists.Count(l => l.Fields.Any());
            if (count > 0)
            {
                web.Context.Load(web.Lists, lists => lists.Include(l => l.Id, l => l.RootFolder.ServerRelativeUrl, l => l.Fields));
                web.Context.ExecuteQueryRetry();

                foreach (var siteField in template.SiteFields)
                {
                    var fieldElement = XElement.Parse(siteField.SchemaXml);

                    if (fieldElement.Attribute("List") != null)
                    {
                        var fieldId = Guid.Parse(fieldElement.Attribute("ID").Value);
                        var listIdentifier = fieldElement.Attribute("List").Value;

                        var field = web.Fields.GetById(fieldId);
                        web.Context.Load(field, f => f.SchemaXml);
                        web.Context.ExecuteQueryRetry();

                        var listGuid = Guid.Empty;
                        if (!Guid.TryParse(listIdentifier, out listGuid))
                        {
                            var sourceListUrl = UrlUtility.Combine(web.ServerRelativeUrl, listIdentifier.ToParsedString());
                            var sourceList = web.Lists.FirstOrDefault(l => l.RootFolder.ServerRelativeUrl.Equals(sourceListUrl, StringComparison.OrdinalIgnoreCase));
                            if (sourceList != null)
                            {
                                listGuid = sourceList.Id;
                            }
                        }
                        if (listGuid != Guid.Empty)
                        {
                            var existingFieldElement = XElement.Parse(field.SchemaXml);

                            if (existingFieldElement.Attribute("List") == null)
                            {
                                existingFieldElement.Add(new XAttribute("List", listGuid.ToString()));
                            }
                            else
                            {
                                existingFieldElement.Attribute("List").SetValue(listGuid.ToString());
                            }
                            field.SchemaXml = existingFieldElement.ToString();

                            field.UpdateAndPushChanges(true);
                            web.Context.ExecuteQueryRetry();
                        }
                    }
                }

                foreach (var listInstance in template.Lists)
                {
                    foreach (var listField in listInstance.Fields)
                    {
                        var fieldElement = XElement.Parse(listField.SchemaXml);
                        if (fieldElement.Attribute("List") != null)
                        {
                            var fieldId = Guid.Parse(fieldElement.Attribute("ID").Value);
                            var listIdentifier = fieldElement.Attribute("List").Value;

                            var listUrl = UrlUtility.Combine(web.ServerRelativeUrl, listInstance.Url.ToParsedString());

                            var createdList = web.Lists.FirstOrDefault(l => l.RootFolder.ServerRelativeUrl.Equals(listUrl, StringComparison.OrdinalIgnoreCase));

                            if (createdList != null)
                            {
                                var field = createdList.Fields.GetById(fieldId);
                                web.Context.Load(field, f => f.SchemaXml);
                                web.Context.ExecuteQueryRetry();

                                var listGuid = Guid.Empty;
                                if (!Guid.TryParse(listIdentifier, out listGuid))
                                {
                                    var sourceListUrl = UrlUtility.Combine(web.ServerRelativeUrl, listIdentifier.ToParsedString());
                                    var sourceList = web.Lists.FirstOrDefault(l => l.RootFolder.ServerRelativeUrl.Equals(sourceListUrl, StringComparison.OrdinalIgnoreCase));
                                    if (sourceList != null)
                                    {
                                        listGuid = sourceList.Id;
                                    }
                                }
                                if (listGuid != Guid.Empty)
                                {
                                    var existingFieldElement = XElement.Parse(field.SchemaXml);

                                    if (existingFieldElement.Attribute("List") == null)
                                    {
                                        existingFieldElement.Add(new XAttribute("List", listGuid.ToString()));
                                    }
                                    else
                                    {
                                        existingFieldElement.Attribute("List").SetValue(listGuid.ToString());
                                    }
                                    field.SchemaXml = existingFieldElement.ToString();

                                    field.Update();
                                    web.Context.ExecuteQueryRetry();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
