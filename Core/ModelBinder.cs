using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Web.Mvc;
using RunningObjects.Core.Mapping;

namespace RunningObjects.Core
{
    public class ModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var modelType = ModelBinders.Binders[typeof(Type)].BindModel(controllerContext, bindingContext) as Type;

            var key = controllerContext.RouteData.Values["key"];
            var mapping = ModelMappingManager.MappingFor(modelType);
            var descriptor = new ModelDescriptor(mapping);

            using (var repository = mapping.Configuration.Repository())
            {
	            var keyValues = descriptor.KeyProperty.PropertyType == typeof(Guid)
						                ? Guid.Parse(key.ToString())
						                : Convert.ChangeType(key, descriptor.KeyProperty.PropertyType);
                
				var instance = repository.Find(keyValues);
				
				var model = new Model(modelType, descriptor, instance);
                foreach (var property in model.Properties)
                {
	                if (property.IsModelCollection)
                    {
                        property.Value = CreateCollection(bindingContext, property);
                    }
                    else
                    {
	                    var result = bindingContext.ValueProvider.GetValue(property.Name);
	                    if (result != null)
                        {
                            property.Value = !property.IsModel
                                ? GetNonModelValue(result, property.MemberType)
                                : GetModelValue(result, property.MemberType);
                        }
                    }
                }
                return model;
            }
        }

        private static object CreateCollection(ModelBindingContext bindingContext, Property property)
        {
            var type = typeof(List<>).MakeGenericType(property.UnderliningModel.ModelType);
            var collection = Activator.CreateInstance(type);
            var index = 0;
            var result = bindingContext.ValueProvider.GetValue(string.Format("{0}[{1}]", property.Name, index));
            do
            {
                if (result != null)
                    type.GetMethod("Add").Invoke(collection, new[] {CreateItem(bindingContext, property, result, index)});
                index++;
            }
            while ((result = bindingContext.ValueProvider.GetValue(string.Format("{0}[{1}]", property.Name, index))) != null);
            return collection;
        }

        private static object CreateItem(ModelBindingContext bindingContext, Property property, ValueProviderResult result, int index)
        {
            var item = GetModelValue(result, property.UnderliningModel.ModelType)
                       ?? Activator.CreateInstance(property.UnderliningModel.ModelType);

            var propertyDescriptor = new ModelDescriptor(ModelMappingManager.MappingFor(property.UnderliningModel.ModelType));
            var propertyModel = new Model(property.UnderliningModel.ModelType, propertyDescriptor, item);

            foreach (var itemProperty in propertyModel.Properties)
            {
                var itemPropertyName = string.Format("{0}[{1}].{2}", property.Name, index, itemProperty.Name);
                var itemResult = bindingContext.ValueProvider.GetValue(itemPropertyName);
                if (itemResult != null)
                {
                    itemProperty.Value = !itemProperty.IsModel
                                             ? GetNonModelValue(itemResult, itemProperty.MemberType)
                                             : GetModelValue(itemResult, itemProperty.MemberType);
                }
            }
            return item;
        }

        internal static object GetNonModelValue(ValueProviderResult result, Type memberType)
        {
            var innerType = Nullable.GetUnderlyingType(memberType) ?? memberType;

            if (innerType.IsEnum)
                return Enum.Parse(innerType, result.AttemptedValue);

            var value = innerType == typeof(Boolean)
                            ? result.AttemptedValue.Split(',')[0]
                            : result.AttemptedValue;

            return TypeDescriptor.GetConverter(innerType).ConvertFrom(null, CultureInfo.CurrentCulture, value);
        }

        internal static object GetModelValue(ValueProviderResult result, Type memberType)
        {
            var memberMapping = ModelMappingManager.MappingFor(memberType);
            var descriptor = new ModelDescriptor(memberMapping);
            var value = result.ConvertTo(descriptor.KeyProperty.PropertyType);
            return memberMapping.Configuration.Repository().Find(value);
        }

        public static Type GetModelType(ControllerContext controllerContext)
        {
            var bindingContext = new ModelBindingContext
            {
                ModelState = controllerContext.Controller.ViewData.ModelState,
                ModelMetadata = controllerContext.Controller.ViewData.ModelMetadata,
                ModelName = TypeBinder.ModeTypeKey,
                ValueProvider = controllerContext.Controller.ValueProvider
            };

            return (Type)ModelBinders.Binders[typeof(Type)].BindModel(controllerContext, bindingContext);
        }
    }
}