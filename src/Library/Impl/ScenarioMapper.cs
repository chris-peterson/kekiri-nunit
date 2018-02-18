using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kekiri.Exceptions;
using NUnit.Framework;

namespace Kekiri.Impl
{
    internal static class ScenarioMapper
    {
        public static IList<IStepInvoker> GetStepInvokers(object test)
        {
            // NOTE: in the case of subclassing, we desire that the steps run in order of declaration from least to most derived 
            // (in all cases except for duplicate name [either by override or new] in which case we prefer the most derived)
            var type = test.GetType();
            var subclassTypes = new Stack<Type>(new[] {type});
            while (type != null && type.GetTypeInfo().BaseType != typeof (Test))
            {
                type = type.GetTypeInfo().BaseType;
                subclassTypes.Push(type);
            }

            return subclassTypes
                .SelectMany(AllMethods)
                .Where(IsStepMethod)
                .Select(m => GetStepFromMethod(m, GetParameters(test)))
                .ToLookup(invoker => invoker.Name)
                .Select(i => i.LastOrDefault())
                .ToList();
        }

        private static IStepInvoker GetStepFromMethod(MethodInfo method, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (method.IsPrivate)
                throw new StepMethodShouldBePublic(method.DeclaringType, method);
            if (method.GetParameters().Length > 0)
                throw new ScenarioStepMethodsShouldNotHaveParameters(method.DeclaringType,
                    "The method '" + method.Name + "' is in a ScenarioTest and cannot have parameters");

            return new StepMethodInvoker(method, parameters.ToArray());
        }

        private static IEnumerable<MethodInfo> AllMethods(Type t)
        {
            return t.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance);
        }

        private static bool IsStepMethod(MethodInfo method)
        {
            if (method.GetCustomAttributes(true).Any(a => a.GetType() == typeof (TestAttribute)))
                throw new FixtureShouldNotUseTestAttribute(method);

            return method.HasAttribute<IStepAttribute>();
        }

        private static IEnumerable<KeyValuePair<string, object>> GetParameters(object test)
        {
            var type = test.GetType().GetTypeInfo();
            var ctor = type.GetConstructors().SingleOrDefault();
            if (ctor != null)
            {
                foreach (var parameter in ctor.GetParameters())
                {
                    var backedField = type
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .SingleOrDefault(
                            p => string.Compare(p.Name.TrimStart('_'), parameter.Name,
                                StringComparison.OrdinalIgnoreCase) == 0);
                    if (backedField != null)
                    {
                        object value;
                        try
                        {
                            value = backedField.GetValue(test);
                        }
                        catch
                        {
                            value = "UNKNOWN!";
                        }
                        yield return new KeyValuePair<string, object>(parameter.Name, value);
                    }
                }
            }
        }
    }
}