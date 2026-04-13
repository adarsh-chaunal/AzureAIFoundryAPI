using Application.Dtos;
using AutoMapper;
using Domain.Cloud;

namespace Application.Mappers;

public class ChatProfile : Profile
{
    public ChatProfile()
    {
        // Domain to DTO mappings
        CreateMap<ChatMessage, ChatMessageDto>();
        CreateMap<ChatConversation, ConversationHistoryDto>()
            .ForMember(dest => dest.ConversationId, opt => opt.MapFrom(src => src.Id));

        // DTO to Domain mappings
        CreateMap<ChatMessageDto, ChatMessage>();
        CreateMap<SendMessageRequestDto, ChatCompletionRequest>()
            .ForMember(dest => dest.Prompt, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.ConversationHistory, opt => opt.Ignore());
    }
}