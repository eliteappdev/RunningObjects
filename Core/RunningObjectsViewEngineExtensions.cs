﻿using System;
using RunningObjects.Core.Security;

namespace RunningObjects.Core
{
    public static class RunningObjectsViewEngineExtensions
    {
        public static void UseTheme<T>(this ISecurityPolicyContainer<T> container, string name) where T : class
        {
            container.UseTheme(name, () => true);
        }

        public static void UseTheme<T>(this ISecurityPolicyContainer<T> container, string name, Func<bool> expression) where T : class
        {
            RunningObjectsViewEngine.RegisterTheme(container, name, expression);
        }
    }
}
