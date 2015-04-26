using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficeDevPnP.Core.Framework.Provisioning.Model
{
    public partial class View
    {
        #region Will be deprecated in June 2015 release

        private string _schemaXml = string.Empty;

        /// <summary>
        /// Gets a value that specifies the XML Schema representing the View type.
        /// </summary>
        [Obsolete("Use the other properties in this object to specify the view. This deprecated property will be removed in the June 2015 release.")]
        public string SchemaXml
        {
            get { return this._schemaXml; }
            set { this._schemaXml = value; }
        }

        #endregion
    }
}
