using System;
using System.Linq;
using RunningObjects.Core.Caching;
using RunningObjects.Core.Mapping;

namespace RunningObjects.Core.Query
{
    public class Query
    {
        private string cacheKey;
        public const int DefaultPageSize = 25;

        public Query(Type modelType, IQueryable source)
        {
            ModelType = modelType;
            Source = source;
        }

        protected IQueryable Source { get; set; }

        public Type ModelType { get; set; }

        public string Id { get; set; }

        public Select Select { get; set; }

        public Where Where { get; set; }

        public OrderBy OrderBy { get; set; }

        //public string GroupBy { get; set; }

        public int? Skip { get; set; }

        public int? Take { get; set; }

        public string[] Includes { get; set; }

        public object[] Parameters { get; set; }

        public string Key { get; set; }

        public string Text { get; set; }

        public bool IsEmpty { get; internal set; }

        public int CacheDuration { get; set; }

        public bool IsCacheable
        {
            get { return CacheDuration > 0; }
        }

        private string CacheKey
        {
            get
            {
                if (string.IsNullOrEmpty(cacheKey))
                    cacheKey = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", ModelType.PartialName(), Select, Where, OrderBy, Skip, Take);
                return cacheKey;
            }
        }

        public bool Paging { get; set; }

        public int PageSize { get; set; }

        public IQueryFilter Filter { get; set; }

        public IQueryable Execute(bool? flush = null)
        {
            if (IsCacheable && (!flush.HasValue || !flush.Value))
            {
                if (!CacheProvider.Current.Contains(CacheKey))
                    CacheProvider.Add(CacheKey, GetAppliedQuery(), CacheDuration);
                return CacheProvider.Current.Get(CacheKey) as IQueryable;
            }
            return GetAppliedQuery();
        }

        private IQueryable GetAppliedQuery()
        {
            if (Source != null)
            {
                if (Filter != null)
                    Source = Filter.Apply(Source);

                if (Where != null)
                {
                    RunningObjectsSetup.Configuration.Query.ParseKeywords(this);
                    Source = (Parameters != null && Parameters.Any())
                                 ? Source.Where(Where.Expression, Parameters)
                                 : Source.Where(Where.Expression);
                }

                if (OrderBy != null && OrderBy.Elements.Any())
                {
                    var orderBy = OrderBy.Elements.Aggregate(string.Empty, (current, element) => current + (element.Key + " " + element.Value + ","));
                    Source = Source.OrderBy(orderBy.Substring(0, orderBy.Length - 1));
                }
                else
                {
                    var mapping = ModelMappingManager.MappingFor(ModelType);
                    var descriptor = new ModelDescriptor(mapping);

                    if (descriptor.Properties.Any())
                    {
                        var orderedProperty = descriptor.KeyProperty ??
                                              (descriptor.TextProperty ?? descriptor.Properties.FirstOrDefault());
                        if (orderedProperty != null)
                            Source = Source.OrderBy(orderedProperty.Name + " Asc");
                    }
                }

                if (Skip.HasValue)
                    Source = Source.Skip(Skip.Value);

                if (Take.HasValue)
                    Source = Source.Take(Take.Value);

                if(Select != null && Select.Properties.Any())
                    Source = Source.Select(string.Format("new({0})", Select));
            }
            return Source;
        }
    }
}