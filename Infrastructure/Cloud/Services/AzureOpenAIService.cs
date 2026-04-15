using Azure.AI.OpenAI;
using Domain.Cloud;
using Infrastructure.Cloud.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.ClientModel;

namespace Infrastructure.Cloud.Services;

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ChatClient _chatClient;
    private readonly IConfiguration _configuration;

    public AzureOpenAIService(IConfiguration configuration)
    {
        _configuration = configuration;

        var endpoint = _configuration["AzureOpenAI:Endpoint"]!;
        var key = _configuration["AzureOpenAI:Key"]!;
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"]!;

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(key));

        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    public async Task<ChatCompletionResponse> GetChatCompletionAsync(ChatCompletionRequest request)
    {
        // Build messages using the correct v2 syntax
        var messages = new List<OpenAI.Chat.ChatMessage>();

        // Add system message if provided
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new SystemChatMessage(request.SystemPrompt));
        }

        // Add conversation history if exists with proper role mapping
        if (request.ConversationHistory != null && request.ConversationHistory.Any())
        {
            foreach (var historyMessage in request.ConversationHistory)
            {
                switch (historyMessage.Role?.ToLower())
                {
                    case "system":
                        messages.Add(new SystemChatMessage(historyMessage.Content));
                        break;
                    case "assistant":
                        messages.Add(new AssistantChatMessage(historyMessage.Content));
                        break;
                    default:
                        messages.Add(new UserChatMessage(historyMessage.Content));
                        break;
                }
            }
        }

        // Add the current prompt
        messages.Add(new UserChatMessage(request.Prompt));

        // Configure completion options
        var options = new ChatCompletionOptions
        {
            Temperature = request.Temperature,
            MaxOutputTokenCount = request.MaxTokens
        };

        // Get the completion
        var response = await _chatClient.CompleteChatAsync(messages, options);

        var firstContent = response.Value.Content.FirstOrDefault();
        var text = firstContent?.Text ?? string.Empty;

        return new ChatCompletionResponse
        {
            Content = text,
            TokensUsed = response.Value.Usage?.TotalTokenCount ?? 0,
            Model = response.Value.Model,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<string> GetChatResponseAsync(string prompt)
    {
        var request = new ChatCompletionRequest
        {
            Prompt = prompt,
            SystemPrompt = "You are a helpful assistant."
        };
        var response = await GetChatCompletionAsync(request);
        return response.Content;
    }
}