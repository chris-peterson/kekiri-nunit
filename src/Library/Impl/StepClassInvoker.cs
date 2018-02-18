﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kekiri.Impl
{
    internal class StepClassInvoker : IStepInvoker
    {
        private readonly Type _stepClass;
        private readonly IExceptionHandler _exceptionHandler;

        public StepClassInvoker(StepType stepType, Type stepClass, KeyValuePair<string,object>[] supportedParameters, IExceptionHandler exceptionHandler)
        {
            if (!typeof(Step).GetTypeInfo().IsAssignableFrom(stepClass))
                throw new ArgumentException("The stepClass must inherit from Step", "stepClass");
            _stepClass = stepClass;
            _exceptionHandler = exceptionHandler;
            Type = stepType;
            Name = new StepName(Type, _stepClass.Name, supportedParameters);
            Parameters = _stepClass.GetTypeInfo().GetConstructors().Single().BindParameters(supportedParameters);
        }

        public StepType Type { get; private set; }

        public StepName Name { get; private set; }

        public KeyValuePair<string, object>[] Parameters { get; private set; } 

        public bool ExceptionExpected { get; set; }

        public string SourceDescription
        {
            get { return _stepClass.FullName; }
        }

        public void Invoke(object test)
        {
            var contextContainer = test as IContextAccessor;
            if(contextContainer == null)
                throw new InvalidOperationException("The test must implement IContextContainer");
            Step.InstanceFor(contextContainer, _stepClass, Parameters, _exceptionHandler).Execute();
        }
    }
}