// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Agent.Sdk
{
    public enum KnobSource
    {
        BuiltinDefault,
        EnvironmentVariable,
        RuntimeVariable,
    };

    public class KnobValue
    {
        public KnobSource Source { get;  private set;}
        public string Which { get; private set; }
        private string _value;
        public KnobValue(string value, KnobSource source, string which=null)
        {
            _value = value;
            Source = source;
            Which = which;
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

    public class AgentKnobs : KnobPanel
    {
        public static readonly IKnob UseNode10 = new Knob()
        {
            Name="UseNode10",
            EnvironmentVariableNames=new List<string>{ "AGENT_USE_NODE10" },
            RuntimeVariableNames=new List<string>{ "AGENT_USE_NODE10" },
            Description="Forces the agent to use Node 10 handler for all Node-based tasks",
            DefaultValue="false"
        };
    }

    public class KnobPanel
    {
        public class Knob : IKnob
        {
            public string Name { get; set; }
            public List<string> EnvironmentVariableNames { get; set; }
            public List<string> RuntimeVariableNames { get; set; }
            // public TBDType CommandLineOption {get; private set;}
            public string Description { get; set; }
            public bool IsDeprecated {get; set; } = false;  // is going away at a future date
            public bool IsExperimental {get; set; } = false; // may go away at a future date
            public string DefaultValue {get; set; } = "";

            public KnobValue GetValue(IKnobValueContext context)
            {
                //TODO: First thing we need to do is check if deprecated or experimental and throw an event that
                //      can be logged

                // proposed order of operations is
                // 1. Run time variables
                // 2. Command Line Options (Not Yet Implemented)
                // 3. Environment Variables
                // 4. Default
                if (context != null)
                {
                    if (RuntimeVariableNames != null)
                    {
                        foreach (var runTimeVar in RuntimeVariableNames)
                        {
                            var value = context.GetVariableValueOrDefault(runTimeVar);
                            if (value != null)
                            {
                                return new KnobValue(value, KnobSource.RuntimeVariable, runTimeVar);
                            }
                        }
                    }
                    if (EnvironmentVariableNames != null)
                    {
                        var scopedEnvironment = context.GetScopedEnvironment();
                        foreach (var envVar in EnvironmentVariableNames)
                        {
                            var value = scopedEnvironment.GetEnvironmentVariable(envVar);
                            if (!string.IsNullOrEmpty(value))
                            {
                                return new KnobValue(value, KnobSource.EnvironmentVariable, envVar);
                            }
                        }
                    }
                }
                return new KnobValue(DefaultValue, KnobSource.BuiltinDefault);
            }
        }

        public static ReadOnlyCollection<IKnob> GetAllKnobs()
        {
            List<IKnob> allKnobs = new List<IKnob>();
            Type type = typeof(IKnob);
            foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var instance = new Knob();
                var locatedValue = info.GetValue(instance) as IKnob;

                if (locatedValue != null)
                {
                    allKnobs.Add(locatedValue);
                }
            }
            return allKnobs.AsReadOnly();
        }
    }

    public interface IKnob
    {
        string Name { get; }
        List<string> EnvironmentVariableNames { get; }
        List<string> RuntimeVariableNames { get; }
        string Description { get; }
        bool IsDeprecated { get; }
        bool IsExperimental { get; }
        KnobValue GetValue(IKnobValueContext context);
    }
}
