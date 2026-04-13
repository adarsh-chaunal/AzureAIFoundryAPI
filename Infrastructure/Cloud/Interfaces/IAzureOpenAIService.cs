using Domain.Cloud;

namespace Infrastructure.Cloud.Interfaces;

public interface IAzureOpenAIService
{
    Task<ChatCompletionResponse> GetChatCompletionAsync(ChatCompletionRequest request);
    
    Task<string> GetChatResponseAsync(string prompt);
}