// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace SmartComponents.StaticAssets.Inference;

public class ChatParameters
{
    public IList<ChatMessage> Messages { get; set; } = [];
    public ChatOptions? Options { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RespondJson { get; set; }
}
