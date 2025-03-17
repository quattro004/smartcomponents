// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using SmartComponents.Infrastructure;
using SmartComponents.StaticAssets.Inference;

namespace SmartComponents.Inference;

public class SmartTextAreaInference
{
    public virtual ChatParameters BuildPrompt(SmartTextAreaConfig config, string textBefore, string textAfter)
    {
        var systemMessageBuilder = new StringBuilder();
        systemMessageBuilder.Append(@"Predict what text the user in the given ROLE would insert at the cursor position indicated by ^^^.
    Only give predictions for which you have an EXTREMELY high confidence that the user would insert that EXACT text.
    Do not make up new information. If you're not sure, just reply with NO_PREDICTION.

    RULES:
    1. Reply with OK:, then in square brackets the predicted text, then END_INSERTION, and no other output.
    2. When a specific value or quantity cannot be inferred and would need to be provided, use the word NEED_INFO.
    3. If there isn't enough information to predict any words that the user would type next, just reply with the word NO_PREDICTION.
    4. NEVER invent new information. If you can't be sure what the user is about to type, ALWAYS stop the prediction with END_INSERTION.");

        if (config.UserPhrases is { Length: > 0 } stockPhrases)
        {
            systemMessageBuilder.Append("\nAlways try to use variations on the following phrases as part of the predictions:\n");
            foreach (var phrase in stockPhrases)
            {
                systemMessageBuilder.AppendFormat("- {0}\n", phrase);
            }
        }

        List<ChatMessage> messages = new()
        {
            new(ChatRole.System, systemMessageBuilder.ToString()),

            new(ChatRole.User, @"ROLE: Family member sending a text
    USER_TEXT: Hey, it's a nice day - the weather is ^^^"),
            new(ChatRole.Assistant, @"OK:[great!]END_INSERTION"),

            new(ChatRole.User, @"ROLE: Customer service assistant
    USER_TEXT: You can find more information on^^^

    Alternatively, phone us."),
            new(ChatRole.Assistant, @"OK:[ our website at NEED_INFO]END_INSERTION"),

            new(ChatRole.User, @"ROLE: Casual
    USER_TEXT: Oh I see!

    Well sure thing, we can"),
            new(ChatRole.Assistant, @"OK:[ help you out with that!]END_INSERTION"),

            new(ChatRole.User, @"ROLE: Storyteller
    USER_TEXT: Sir Digby Chicken Caesar, also know^^^"),
            new(ChatRole.Assistant, @"OK:[n as NEED_INFO]END_INSERTION"),

            new(ChatRole.User, @"ROLE: Customer support agent
    USER_TEXT: Goodbye for now.^^^"),
            new(ChatRole.Assistant, @"NO_PREDICTION END_INSERTION"),

            new(ChatRole.User, @"ROLE: Pirate
    USER_TEXT: Have you found^^^"),
            new(ChatRole.Assistant, @"OK:[ the treasure, me hearties?]END_INSERTION"),

            new(ChatRole.User, @$"ROLE: {config.UserRole}
    USER_TEXT: {textBefore}^^^{textAfter}"),
        };

        return new ChatParameters
        {
            Messages = messages,
            Options = new ChatOptions
            {
                Temperature = 0,
                MaxOutputTokens = 400,
                StopSequences = ["END_INSERTION", "NEED_INFO"],
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            }
        };
    }

    public virtual async Task<string> GetInsertionSuggestionAsync(IChatClient inference, SmartTextAreaConfig config, string textBefore, string textAfter)
    {
        var chatParameters = BuildPrompt(config, textBefore, textAfter);
        var response = await inference.GetResponseAsync(chatParameters.Messages, chatParameters.Options);
        var responseText = response.Text;

        if (responseText.Length > 5 && responseText.StartsWith("OK:[", StringComparison.Ordinal))
        {
            // Avoid returning multiple sentences as it's unlikely to avoid inventing some new train of thought.
            var trimAfter = responseText.IndexOfAny(['.', '?', '!']);
            if (trimAfter > 0 && responseText.Length > trimAfter + 1 && responseText[trimAfter + 1] == ' ')
            {
                responseText = responseText.Substring(0, trimAfter + 1);
            }

            // Leave it up to the frontend code to decide whether to add a training space
            var trimmedResponse = responseText.Substring(4).TrimEnd(']', ' ');

            // Don't have a leading space on the suggestion if there's already a space right
            // before the cursor. The language model normally gets this right anyway (distinguishing
            // between starting a new word, vs continuing a partly-typed one) but sometimes it adds
            // an unnecessary extra space.
            if (textBefore.Length > 0 && textBefore[textBefore.Length - 1] == ' ')
            {
                trimmedResponse = trimmedResponse.TrimStart(' ');
            }

            return trimmedResponse;
        }

        return string.Empty;
    }
}
