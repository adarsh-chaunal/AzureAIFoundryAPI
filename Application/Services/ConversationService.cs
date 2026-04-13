using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Cloud;
using Infrastructure.Cloud.Interfaces;
using Infrastructure.Data.Interfaces;

namespace Application.Services;

public class ConversationService : IConversationService
{
    private readonly IAzureOpenAIService _aiService;
    private readonly IChatRepository _chatRepository;
    private readonly IMapper _mapper;

    public ConversationService(
        IAzureOpenAIService aiService,
        IChatRepository chatRepository,
        IMapper mapper)
    {
        _aiService = aiService;
        _chatRepository = chatRepository;
        _mapper = mapper;
    }

    public async Task<SendMessageResponseDto> SendMessageAsync(
        SendMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message cannot be empty");

        // Create conversation if needed
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        // Create chat completion request
        var chatRequest = new ChatCompletionRequest
        {
            Prompt = request.Message,
            SystemPrompt = "You are a helpful assistant.",
            Temperature = 0.7f,
            MaxTokens = 800
        };

        // Get AI response
        var response = await _aiService.GetChatCompletionAsync(chatRequest);

        // Save to history
        var conversation = await _chatRepository.GetConversationAsync(conversationId);

        conversation.Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        conversation.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = response.Content,
            Timestamp = DateTime.UtcNow
        });

        await _chatRepository.SaveConversationAsync(conversation);

        return new SendMessageResponseDto
        {
            Response = response.Content,
            ConversationId = conversationId,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<SendMessageResponseDto> SendMessageWithContextAsync(
        SendMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message cannot be empty");

        if (string.IsNullOrWhiteSpace(request.ConversationId))
            throw new ArgumentException("ConversationId is required for context");

        // Get conversation history
        var history = await _chatRepository.GetConversationHistoryAsync(request.ConversationId, 20);

        // Create chat completion request with context
        var chatRequest = new ChatCompletionRequest
        {
            Prompt = request.Message,
            SystemPrompt = "You are a helpful assistant. Use the conversation history for context.",
            Temperature = 0.7f,
            MaxTokens = 800,
            ConversationHistory = history
        };

        // Get AI response with context
        var response = await _aiService.GetChatCompletionAsync(chatRequest);

        // Save to history
        var conversation = await _chatRepository.GetConversationAsync(request.ConversationId);

        conversation.Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        conversation.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = response.Content,
            Timestamp = DateTime.UtcNow
        });

        await _chatRepository.SaveConversationAsync(conversation);

        return new SendMessageResponseDto
        {
            Response = response.Content,
            ConversationId = request.ConversationId,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<ChatResponseDto> GetStructuredResponseAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty");

        // Create chat completion request with structured prompt
        var structuredPrompt = $@"
                Please provide a structured response to: {prompt}

                Format your response as:
                1. Main Answer:
                2. Key Points:
                3. Additional Context:
                ";

        var chatRequest = new ChatCompletionRequest
        {
            Prompt = structuredPrompt,
            SystemPrompt = "You are a helpful assistant that provides structured responses.",
            Temperature = 0.5f,  // Lower temperature for more consistent structure
            MaxTokens = 1000
        };

        // Get AI response
        var response = await _aiService.GetChatCompletionAsync(chatRequest);

        return new ChatResponseDto
        {
            Text = response.Content,
            TokensUsed = response.TokensUsed,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<ConversationHistoryDto> GetConversationHistoryAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("ConversationId cannot be empty");

        var conversation = await _chatRepository.GetConversationAsync(conversationId);
        return _mapper.Map<ConversationHistoryDto>(conversation);
    }
}