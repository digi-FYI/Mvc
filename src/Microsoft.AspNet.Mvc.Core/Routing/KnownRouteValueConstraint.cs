// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class KnownRouteValueConstraint : IRouteConstraint
    {
        private RouteValuesCollection _cachedValuesCollection;

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            object obj;
            if (values.TryGetValue(routeKey, out obj))
            {
                var value = obj as string;
                if (value != null)
                {
                    var allValues = GetAndCacheAllMatchingValues(routeKey, httpContext);
                    foreach (var existingValue in allValues)
                    {
                        if (string.Equals(value, existingValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string[] GetAndCacheAllMatchingValues(string routeKey, HttpContext httpContext)
        {
            var actionDescriptors = GetAndValidateActionDescriptorsCollection(httpContext);
            var version = actionDescriptors.Version;
            var valuesCollection = _cachedValuesCollection;

            if (valuesCollection == null ||
                version != valuesCollection.Version)
            {
                var routeValueCollection = actionDescriptors
                                            .Items
                                            .Select(ad => ad.RouteConstraints
                                                            .FirstOrDefault(
                                                                c => c.RouteKey == routeKey &&
                                                                c.KeyHandling == RouteKeyHandling.RequireKey))
                                            .Where(rc => rc != null)
                                            .Select(rc => rc.RouteValue)
                                            .Distinct()
                                            .ToArray();

                valuesCollection = new RouteValuesCollection(version, routeValueCollection);
                _cachedValuesCollection = valuesCollection;
            }

            return _cachedValuesCollection.Items;
        }

        private static ActionDescriptorsCollection GetAndValidateActionDescriptorsCollection(HttpContext httpContext)
        {
            var provider = httpContext.RequestServices
                                      .GetRequiredService<IActionDescriptorsCollectionProvider>();
            var descriptors = provider.ActionDescriptors;

            if (descriptors == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull("ActionDescriptors",
                                                               provider.GetType()));
            }

            return descriptors;
        }

        private class RouteValuesCollection
        {
            public RouteValuesCollection(int version, string[] items)
            {
                Version = version;
                Items = items;
            }

            public int Version { get; private set; }

            public string[] Items { get; private set; }
        }
    }
}
