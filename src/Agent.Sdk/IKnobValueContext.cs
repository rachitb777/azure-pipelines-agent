// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Agent.Sdk
{
    public interface IKnobValueContext
    {
        string GetVariableValueOrDefault(string variableName);
        IScopedEnvironment GetScopedEnvironment();
    }
}