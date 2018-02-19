using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kekiri.Impl
{
    internal class StepMethodInvoker : IStepInvoker
    {
        protected MethodBase Method { get; private set; }

        public bool ExceptionExpected { get; set; }
        
        public StepName Name { get; private set; }

        public StepType Type { get; private set; }

        public string SourceDescription { get; private set; }

        public KeyValuePair<string, object>[] Parameters { get; private set; } 

        public StepMethodInvoker(MethodBase method, KeyValuePair<string, object>[] supportedParameters = null)
            : this(GetStepType(method).Value, method, supportedParameters) { }

        public StepMethodInvoker(StepType stepType, MethodBase method, KeyValuePair<string, object>[] supportedParameters = null)
        {
            Method = method;
            Type = stepType;
            Parameters = method.BindParameters(supportedParameters);
            Name = new StepName(Type, method.Name, supportedParameters);
            ExceptionExpected = method.AttributeOrDefault<ThrowsAttribute>() != null;
            SourceDescription = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
        }

        public virtual void Invoke(object test)
        {
            Method.Invoke(Method.IsStatic ? null : test, Parameters.Select(p => p.Value).ToArray());
        }

        public static StepType? GetStepType(MethodBase method)
        {
            var given = method.AttributeOrDefault<GivenAttribute>();
            if (given != null)
            {
                return StepType.Given;
            }
            var when = method.AttributeOrDefault<WhenAttribute>();
            if (when != null)
            {
                return StepType.When;
            }
            var then = method.AttributeOrDefault<ThenAttribute>();
            if (then != null)
            {
                return StepType.Then;
            }
            return null;
        }
    }
}