﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AutoValidateAntiforgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var antiforgery = serviceProvider.GetRequiredService<IAntiforgery>();
            return new ValidateAntiforgeryTokenAuthorizationFilter(antiforgery);
        }
    }
}
