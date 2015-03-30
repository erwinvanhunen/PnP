using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using ContentType = OfficeDevPnP.Core.Framework.Provisioning.Model.ContentType;

namespace OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers
{
    public class ObjectContentType : ObjectHandlerBase
    {
        public override void ProvisionObjects(Web web, ProvisioningTemplate template)
        {
            Stopwatch p = new Stopwatch();
            p.Start();
            var existingCts = web.AvailableContentTypes;
            web.Context.Load(existingCts, cts => cts.Include(ct => ct.StringId));
            web.Context.ExecuteQueryRetry();

            var existingCtsIds = existingCts.Select(cts => cts.StringId.ToLower()).ToList();

            foreach (var ct in template.ContentTypes)
            {
                // find the id of the content type
                XDocument document = XDocument.Parse(ct.SchemaXml);
                var contentTypeId = document.Root.Attribute("ID").Value;
                if (!existingCtsIds.Contains(contentTypeId.ToLower()))
                {
                    CreateContentType(web, document.Root);
                    //web.CreateContentTypeFromXML(document);
                    existingCtsIds.Add(contentTypeId);
                }
            }
            p.Stop();
            var p1 = p.ElapsedMilliseconds;
        }

        private void CreateContentType(Web web, XElement ct)
        {
            var scope = new ExceptionHandlingScope(web.Context);
            using (scope.StartScope())
            {
                using (scope.StartTry())
                {
                    var ctid = ct.Attribute("ID").Value;
                    var name = ct.Attribute("Name").Value;

                    var description = ct.Attribute("Description") != null ? ct.Attribute("Description").Value : string.Empty;
                    var group = ct.Attribute("Group") != null ? ct.Attribute("Group").Value : string.Empty;


                    ContentTypeCollection contentTypes = web.ContentTypes;

                    ContentTypeCreationInformation newCtCI = new ContentTypeCreationInformation();


                    // Set the properties for the content type
                    newCtCI.Name = name;
                    newCtCI.Id = ctid;
                    newCtCI.Description = description;
                    newCtCI.Group = group;

                    var newCt = contentTypes.Add(newCtCI);

                    // Add fields to content type 
                    var fieldRefs = from fr in ct.Descendants("FieldRefs").Elements("FieldRef") select fr;
                    foreach (var fieldRef in fieldRefs)
                    {
                        var fieldID = fieldRef.Attribute("ID").Value;
                        var required = fieldRef.Attribute("Required") != null ? bool.Parse(fieldRef.Attribute("Required").Value) : false;
                        var hidden = fieldRef.Attribute("Hidden") != null ? bool.Parse(fieldRef.Attribute("Hidden").Value) : false;

                        var field = web.Fields.GetById(Guid.Parse(fieldID));

                        FieldLinkCreationInformation fieldLinkCI = new FieldLinkCreationInformation();
                        fieldLinkCI.Field = field;
                        newCt.FieldLinks.Add(fieldLinkCI);
                        newCt.Update(true);

                        var fieldLink = newCt.FieldLinks.GetById(Guid.Parse(fieldID));


                        if (required || hidden)
                        {
                            // Update FieldLink
                            fieldLink.Required = required;
                            fieldLink.Hidden = hidden;
                            newCt.Update(true);
                        }
                    }
                }
                using (scope.StartCatch())
                {
                    
                }
                using (scope.StartFinally())
                {
                    
                }
            }
            web.Context.ExecuteQueryRetry();
        }

        public override ProvisioningTemplate CreateEntities(Web web, ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {
            var cts = web.ContentTypes;
            web.Context.Load(cts);
            web.Context.ExecuteQueryRetry();

            foreach (var ct in cts)
            {
                if (!BuiltInContentTypeId.Contains(ct.StringId))
                {
                    template.ContentTypes.Add(new ContentType() { SchemaXml = ct.SchemaXml });
                }
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
            foreach (var ct in baseTemplate.ContentTypes)
            {
                XDocument xDoc = XDocument.Parse(ct.SchemaXml);
                var id = xDoc.Root.Attribute("ID") != null ? xDoc.Root.Attribute("ID").Value : null;
                if (id != null)
                {
                    int index = template.ContentTypes.FindIndex(f => f.SchemaXml.IndexOf(id, StringComparison.InvariantCultureIgnoreCase) > -1);

                    if (index > -1)
                    {
                        template.ContentTypes.RemoveAt(index);
                    }
                }
            }

            return template;
        }
    }
}
