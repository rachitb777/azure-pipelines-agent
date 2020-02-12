// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Agent.Sdk;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Xunit;
using Moq;


namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class KnobL0
    {

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void HasAgentKnobs()
        {
            Assert.True(ControlPanel.GetAllKnobsFor<AgentKnobs>().Count > 0, "Has at least one knob");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void BasicKnobTests()
        {
            var environment = new LocalEnvironment();

            var executionContext = new Mock<IExecutionContext>();
                executionContext
                    .Setup(x => x.GetScopedEnvironment())
                    .Returns(environment);

            {
                var knobValue = AgentKnobs.UseNode10.GetValue(executionContext.Object);
                Assert.True(knobValue.Source.GetType() == typeof(BuiltInDefaultKnobSource));
            }

            environment.SetEnvironmentVariable("AGENT_USE_NODE10","true");

            {
                var knobValue = AgentKnobs.UseNode10.GetValue(executionContext.Object);
                Assert.True(knobValue.Source.GetType() == typeof(EnvironmentKnobSource));
                Assert.True(knobValue.AsBoolean());
                Assert.True(string.Equals(knobValue.AsString(), "true", StringComparison.OrdinalIgnoreCase));
            }

            environment.SetEnvironmentVariable("AGENT_USE_NODE10","false");

            {
                var knobValue = AgentKnobs.UseNode10.GetValue(executionContext.Object);
                Assert.True(knobValue.Source.GetType() == typeof(EnvironmentKnobSource));
                Assert.True(!knobValue.AsBoolean());
                Assert.True(string.Equals(knobValue.AsString(), "false", StringComparison.OrdinalIgnoreCase));
            }

            environment.SetEnvironmentVariable("AGENT_USE_NODE10", null);

            executionContext.Setup(x => x.GetVariableValueOrDefault(It.Is<string>(s => string.Equals(s, "AGENT_USE_NODE10")))).Returns("true");

            {
                var knobValue = AgentKnobs.UseNode10.GetValue(executionContext.Object);
                Assert.True(knobValue.Source.GetType() == typeof(RuntimeKnobSource));
                Assert.True(knobValue.AsBoolean());
                Assert.True(string.Equals(knobValue.AsString(), "true", StringComparison.OrdinalIgnoreCase));
            }

            executionContext.Setup(x => x.GetVariableValueOrDefault(It.Is<string>(s => string.Equals(s, "AGENT_USE_NODE10")))).Returns("false");

            {
                var knobValue = AgentKnobs.UseNode10.GetValue(executionContext.Object);
                Assert.True(knobValue.Source.GetType() == typeof(RuntimeKnobSource));
                Assert.True(!knobValue.AsBoolean());
                Assert.True(string.Equals(knobValue.AsString(), "false", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
