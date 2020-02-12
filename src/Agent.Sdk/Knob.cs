// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Agent.Sdk
{

    public class KnobValue
    {
        public IKnobSource Source { get;  private set;}
        private string _value;

        public KnobValue(string value, IKnobSource source)
        {
            _value = value;
            Source = source;
        }

        public string AsString()
        {
            return _value;
        }

        public bool AsBoolean()
        {
            return StringUtil.ConvertToBoolean(_value);
        }
    }

    public class AgentKnobs
    {
        public static readonly Knob UseNode10 = new Knob(nameof(UseNode10), "Forces the agent to use Node 10 handler for all Node-based tasks",
                                                        new RuntimeKnobSource("AGENT_USE_NODE10"),
                                                        new EnvironmentKnobSource("AGENT_USE_NODE10"),
                                                        new BuiltInDefaultKnobSource("false"));


    }

    public class DeprecatedKnob : Knob
    {
        public override bool IsDeprecated => true;
        public DeprecatedKnob(string name, string description, params IKnobSource[] sources) : base(name, description, sources)
        {
        }
    }

    public class ExperimentalKnob : Knob
    {
        public override bool IsExperimental => true;
        public ExperimentalKnob(string name, string description, params IKnobSource[] sources) : base(name, description, sources)
        {
        }
    }

    public class Knob
    {
        public string Name { get; private set; }
        public IKnobSource Source { get; private set;}
        public string Description { get; private set; }
        public virtual bool IsDeprecated => false;  // is going away at a future date
        public virtual bool IsExperimental => false; // may go away at a future date

        public Knob(string name, string description, params IKnobSource[] sources)
        {
            Name = name;
            Description = description;
            Source = new CompositeKnobFetcher(sources);
        }

        public Knob()
        {
        }

        public KnobValue GetValue(IKnobValueContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(Source, nameof(Source));

            return Source.GetValue(context);
        }

        public static List<Knob> GetAllKnobsFor<T>()
        {
            Type type = typeof(T);
            List<Knob> allKnobs = new List<Knob>();
            foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var instance = new Knob();
                var locatedValue = info.GetValue(instance) as Knob;

                if (locatedValue != null)
                {
                    allKnobs.Add(locatedValue);
                }
            }
            return allKnobs;
        }
    }

    public interface IKnobSource
    {
        KnobValue GetValue(IKnobValueContext context);
        string GetDisplayString();
    }

    public class BuiltInDefaultKnobSource : IKnobSource
    {
        private string _value;

        public BuiltInDefaultKnobSource(string value)
        {
            _value = value;
        }

        public KnobValue GetValue(IKnobValueContext context)
        {
            return new KnobValue(_value, this);
        }

        public string GetDisplayString()
        {
            return "Default";
        }
    }

    public class EnvironmentKnobSource : IKnobSource
    {
        private string _envVar;

        public EnvironmentKnobSource(string envVar)
        {
            _envVar = envVar;
        }

        public KnobValue GetValue(IKnobValueContext context)
        {
            var scopedEnvironment = context.GetScopedEnvironment();
            var value = scopedEnvironment.GetEnvironmentVariable(_envVar);
            if (!string.IsNullOrEmpty(value))
            {
                return new KnobValue(value, this);
            }
            return null;
        }

        public string GetDisplayString()
        {
            return $"${{{_envVar}}}";
        }
    }

    public class RuntimeKnobSource : IKnobSource
    {
        private string _runTimeVar;
        public RuntimeKnobSource(string runTimeVar)
        {
            _runTimeVar = runTimeVar;
        }

        public KnobValue GetValue(IKnobValueContext context)
        {
            var value = context.GetVariableValueOrDefault(_runTimeVar);
            if (!string.IsNullOrEmpty(value))
            {
                return new KnobValue(value, this);
            }
            return null;
        }

        public string GetDisplayString()
        {
            return $"$({_runTimeVar})";
        }
    }

    public class CompositeKnobFetcher : IKnobSource
    {
        private IKnobSource[] _sources;

        public CompositeKnobFetcher(params IKnobSource[] sources)
        {
            _sources = sources;
        }

        public KnobValue GetValue(IKnobValueContext context)
        {
            foreach (var source in _sources)
            {
                var value = source.GetValue(context);
                if (!(value is null))
                {
                    return value;
                }
            }
            return null;
        }
        public string GetDisplayString()
        {
            var strings = new List<string>();
            foreach (var source in _sources)
            {
                strings.Add(source.GetDisplayString());
            }
            return string.Join(", ", strings);
        }
    }
}
