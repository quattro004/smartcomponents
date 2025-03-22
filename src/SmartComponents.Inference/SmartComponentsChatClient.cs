// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using SmartComponents.StaticAssets.Inference;

namespace SmartComponents.Inference;

public class SmartComponentsChatClient(IChatClient client) : DelegatingChatClient(client)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var chatParameters = new ChatParameters() { Messages = [.. messages], Options = options };
#if DEBUG
        if (ResponseCache.TryGetCachedResponse(chatParameters, out var cachedResponse))
        {
            return await Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, cachedResponse)));
        }
#endif
        var response = await base.GetResponseAsync(messages, options, cancellationToken);

#if DEBUG
        ResponseCache.SetCachedResponse(chatParameters, response.Text);
#endif
        return response;
    }
}
