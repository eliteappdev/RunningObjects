using System;
using System.Collections.Generic;

namespace RunningObjects.MVC.Mapping
{
    public class NamespaceMapping : IElementMapping
    {
        private IEnumerable<TypeMapping> types;
        private IEnumerable<NamespaceMapping> namespaces;

        public string ID { get; set; }

        public string Name { get; internal set; }

        public string FullName { get; internal set; }

        public IElementMapping Parent { get; set; }

        public bool Visible { get; set; }

        public IEnumerable<TypeMapping> Types
        {
            get { return types ?? (types = new List<TypeMapping>()); }
            internal set { types = value; }
        }

        public IEnumerable<NamespaceMapping> Namespaces
        {
            get { return namespaces ?? (namespaces = new List<NamespaceMapping>()); }
            internal set { namespaces = value; }
        }

        public bool IsRoot { get { return Parent == null; } }
    }
}