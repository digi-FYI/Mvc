// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Methods for determining how an <see cref="ITagHelper"/> should run based on the attributes that were specified.
    /// </summary>
    public static class AttributeMatcher
    {
        /// <summary>
        /// Determines the modes a <see cref="ITagHelper" /> can run in based on which modes have all their required
        /// attributes present, non null, non empty, and non whitepsace.
        /// </summary>
        /// <typeparam name="TMode">The type representing the <see cref="ITagHelper" />'s modes.</typeparam>
        /// <param name="context">The <see cref="TagHelperContext"/>.</param>
        /// <param name="modeInfos">The modes and their required attributes.</param>
        /// <returns>The <see cref="ModeMatchResult{TMode}"/>.</returns>
        public static ModeMatchResult<TMode> DetermineMode<TMode>(
            TagHelperContext context,
            IReadOnlyList<ModeAttributes<TMode>> modeInfos)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (modeInfos == null)
            {
                throw new ArgumentNullException(nameof(modeInfos));
            }

            // true == full match, false == partial match
            var matchedAttributes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var result = new ModeMatchResult<TMode>();

            // Perf: Avoid allocating enumerator
            for (var i = 0; i < modeInfos.Count; i++)
            {
                var modeInfo = modeInfos[i];
                var modeAttributes = GetPresentMissingAttributes(context, modeInfo.Attributes);

                if (modeAttributes.Present.Count > 0)
                {
                    if (modeAttributes.Missing.Count == 0)
                    {
                        // Perf: Avoid allocating enumerator
                        // A complete match, mark the attribute as fully matched
                        for (var j = 0; j < modeAttributes.Present.Count; j++)
                        {
                            matchedAttributes[modeAttributes.Present[j]] = true;
                        }

                        result.FullMatches.Add(ModeMatchAttributes.Create(modeInfo.Mode, modeInfo.Attributes));
                    }
                    else
                    {
                        // Perf: Avoid allocating enumerator
                        // A partial match, mark the attribute as partially matched if not already fully matched
                        for (var j = 0; j < modeAttributes.Present.Count; j++)
                        {
                            var attribute = modeAttributes.Present[j];
                            bool attributeMatch;
                            if (!matchedAttributes.TryGetValue(attribute, out attributeMatch))
                            {
                                matchedAttributes[attribute] = false;
                            }
                        }

                        result.PartialMatches.Add(ModeMatchAttributes.Create(
                            modeInfo.Mode, modeAttributes.Present, modeAttributes.Missing));
                    }
                }
            }

            // Build the list of partially matched attributes (those with partial matches but no full matches)
            foreach (var attribute in matchedAttributes.Keys)
            {
                if (!matchedAttributes[attribute])
                {
                    result.PartiallyMatchedAttributes.Add(attribute);
                }
            }

            return result;
        }

        private static PresentMissingAttributes GetPresentMissingAttributes(
            TagHelperContext context,
            string[] requiredAttributes)
        {
            // Check for all attribute values
            var presentAttributes = new List<string>();
            var missingAttributes = new List<string>();

            // Perf: Avoid allocating enumerator
            for (var i = 0; i < requiredAttributes.Length; i++)
            {
                var requiredAttribute = requiredAttributes[i];
                IReadOnlyTagHelperAttribute attribute;
                if (!context.AllAttributes.TryGetAttribute(requiredAttribute, out attribute))
                {
                    // Missing attribute.
                    missingAttributes.Add(requiredAttribute);
                    continue;
                }

                var valueAsString = attribute.Value as string;
                if (valueAsString != null && string.IsNullOrEmpty(valueAsString))
                {
                    // Treat attributes with empty values as missing.
                    missingAttributes.Add(requiredAttribute);
                    continue;
                }

                presentAttributes.Add(requiredAttribute);
            }

            return new PresentMissingAttributes { Present = presentAttributes, Missing = missingAttributes };
        }

        private class PresentMissingAttributes
        {
            public List<string> Present { get; set; }

            public List<string> Missing { get; set; }
        }
    }
}