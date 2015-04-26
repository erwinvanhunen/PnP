using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Extensions;

namespace OfficeDevPnP.Core.Framework.Provisioning.Model
{
    public partial class View : IEquatable<View>
    {
        #region Private Members
        private List<FieldRef> _viewFields = new List<FieldRef>();
        private int _rowLimit = 30;
        private bool _paged = true;
        #endregion

        #region Public Properties

        public string Name { get; set; }
        public string DisplayName { get; set; }

        public bool DefaultView { get; set; }

        public ViewType ViewType { get; set; }
        public List<FieldRef> ViewFields { get { return _viewFields; } private set { _viewFields = value; } }

        public string Query { get; set; }

        public bool Paged { get { return _paged; } set { _paged = value; } }
        public int RowLimit { get { return _rowLimit; } set { _rowLimit = value; } }

        #endregion

        #region Constructors
        public View()
        {

        }

        public View(List<FieldRef> viewFields)
        {
            this.ViewFields.AddRange(viewFields);
        }

        #endregion
        #region Comparison code

        public override int GetHashCode()
        {

            return (String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                this.ViewType,
                this.DefaultView,
                this.DisplayName,
                this.Name,
                this.Paged,
                this.Query,
                this.RowLimit,
                 this.ViewFields.Aggregate(0, (acc, next) => acc += next.GetHashCode())
               ).GetHashCode());

        }

        public override bool Equals(object obj)
        {
            if (!(obj is View))
            {
                return (false);
            }
            return (Equals((View)obj));
        }

        public bool Equals(View other)
        {
            return (this.DefaultView == other.DefaultView &&
                    this.Paged == other.Paged &&
                    this.ViewType == other.ViewType &&
                    this.DisplayName == other.DisplayName &&
                    this.Name == other.Name &&
                    this.Query == other.Query &&
                    this.RowLimit == other.RowLimit &&
                    this.ViewFields.DeepEquals(other.ViewFields));
        }

        #endregion
    }
}
