// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fundamentals;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class PersonalIdentifiableInformationAttribute : Attribute
{
    public PersonalIdentifiableInformationAttribute(string reasonForCollecting = "")
    {
        ReasonForCollecting = reasonForCollecting;
    }

    public string ReasonForCollecting { get; }
}
