// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if NET451
using System.ComponentModel;
#endif
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.DecisionTree;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <inheritdoc />
    public class ActionSelectionDecisionTree : IActionSelectionDecisionTree
    {
        private readonly DecisionTreeNode<ActionDescriptor> _root;

        /// <summary>
        /// Creates a new <see cref="ActionSelectionDecisionTree"/>.
        /// </summary>
        /// <param name="actions">The <see cref="ActionDescriptorsCollection"/>.</param>
        public ActionSelectionDecisionTree(ActionDescriptorsCollection actions)
        {
            Version = actions.Version;

            _root = DecisionTreeBuilder<ActionDescriptor>.GenerateTree(
                actions.Items,
                new ActionDescriptorClassifier());
        }

        /// <inheritdoc />
        public int Version { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<ActionDescriptor> Select(IDictionary<string, object> routeValues)
        {
            var results = new List<ActionDescriptor>();
            Walk(results, routeValues, _root);

            return results;
        }

        private void Walk(
            List<ActionDescriptor> results,
            IDictionary<string, object> routeValues,
            DecisionTreeNode<ActionDescriptor> node)
        {
            for (var i = 0; i < node.Matches.Count; i++)
            {
                results.Add(node.Matches[i]);
            }

            for (var i = 0; i < node.Criteria.Count; i++)
            {
                var criterion = node.Criteria[i];
                var key = criterion.Key;

                object value;
                routeValues.TryGetValue(key, out value);

                DecisionTreeNode<ActionDescriptor> branch;
                if (criterion.Branches.TryGetValue(value ?? string.Empty, out branch))
                {
                    Walk(results, routeValues, branch);
                }
            }
        }

        private class ActionDescriptorClassifier : IClassifier<ActionDescriptor>
        {
            public ActionDescriptorClassifier()
            {
                ValueComparer = new RouteValueEqualityComparer();
            }

            public IEqualityComparer<object> ValueComparer { get; private set; }

            public IDictionary<string, DecisionCriterionValue> GetCriteria(ActionDescriptor item)
            {
                var results = new Dictionary<string, DecisionCriterionValue>(StringComparer.OrdinalIgnoreCase);

                if (item.RouteConstraints != null)
                {
                    foreach (var constraint in item.RouteConstraints)
                    {
                        DecisionCriterionValue value;
                        if (constraint.KeyHandling == RouteKeyHandling.DenyKey)
                        {
                            // null and string.Empty are equivalent for route values, so just treat nulls as
                            // string.Empty.
                            value = new DecisionCriterionValue(value: string.Empty);
                        }
                        else if (constraint.KeyHandling == RouteKeyHandling.RequireKey)
                        {
                            value = new DecisionCriterionValue(value: constraint.RouteValue);
                        }
                        else
                        {
                            // We'd already have failed before getting here. The RouteDataActionConstraint constructor
                            // would throw.
#if NET451
                            throw new InvalidEnumArgumentException(
                                nameof(item),
                                (int)constraint.KeyHandling,
                                typeof(RouteKeyHandling));
#else
                            throw new ArgumentOutOfRangeException(nameof(item));
#endif
                        }

                        results.Add(constraint.RouteKey, value);
                    }
                }

                return results;
            }
        }
    }
}
