using Markdig;
using LoreForge.Contracts.Agent;
using LoreForge.Web.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace LoreForge.Web.Pages.Agent;

public partial class Agent
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] private MainLayout? Layout { get; set; }

    private List<ConversationSummaryDto> _conversations = [];
    private string? _conversationId;
    private readonly List<ChatMessage> _messages = [];
    private string _input = "";
    private bool _sending;
    private string? _errorMessage;
    private bool _scrollPending;
    private ElementReference _messagesEnd;
    private ElementReference _textareaRef;

    protected override async Task OnInitializedAsync()
    {
        Layout?.SetPageTitle("Agente");
        _conversations = await Http.GetFromJsonAsync<List<ConversationSummaryDto>>("agent/conversations") ?? [];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JS.InvokeVoidAsync("preventEnterDefault", _textareaRef);

        if (_scrollPending)
        {
            _scrollPending = false;
            await JS.InvokeVoidAsync("scrollIntoView", _messagesEnd);
        }
    }

    private void NewConversation()
    {
        _conversationId = null;
        _messages.Clear();
        _errorMessage = null;
        _input = "";
    }

    private async Task SelectConversationAsync(string conversationId)
    {
        _conversationId = conversationId;
        _messages.Clear();
        _errorMessage = null;
        _input = "";

        var history = await Http.GetFromJsonAsync<List<ConversationMessageDto>>($"agent/chat/{conversationId}/history");
        if (history is not null)
            _messages.AddRange(history.Select(m => new ChatMessage(m.Role, m.Content)));

        _scrollPending = true;
    }

    private async Task DeleteConversationAsync(string conversationId)
    {
        await Http.DeleteAsync($"agent/chat/{conversationId}/history");
        _conversations.RemoveAll(c => c.ConversationId == conversationId);

        if (_conversationId == conversationId)
        {
            _conversationId = null;
            _messages.Clear();
            _errorMessage = null;
        }
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await SendAsync();
    }

    private async Task SendAsync()
    {
        var message = _input.Trim();
        if (string.IsNullOrWhiteSpace(message) || _sending) return;

        _input = "";
        _errorMessage = null;
        _messages.Add(new ChatMessage("user", message));
        _sending = true;
        _scrollPending = true;

        try
        {
            string reply;

            if (_conversationId is null)
            {
                var response = await Http.PostAsJsonAsync("agent/chat", new ChatMessageRequest(message));
                if (!response.IsSuccessStatusCode)
                {
                    _errorMessage = "Não foi possível enviar a mensagem. Tente novamente.";
                    _messages.RemoveAt(_messages.Count - 1);
                    _input = message;
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<StartChatResponse>();
                _conversationId = result!.ConversationId;
                reply = result.Reply;

                var summary = message.Length <= 100 ? message : message[..100];
                _conversations.Insert(0, new ConversationSummaryDto(
                    _conversationId,
                    summary,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            }
            else
            {
                var response = await Http.PostAsJsonAsync($"agent/chat/{_conversationId}", new ChatMessageRequest(message));
                if (!response.IsSuccessStatusCode)
                {
                    _errorMessage = "Não foi possível enviar a mensagem. Tente novamente.";
                    _messages.RemoveAt(_messages.Count - 1);
                    _input = message;
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                reply = result!.Reply;
            }

            _messages.Add(new ChatMessage("assistant", reply));
            _scrollPending = true;
        }
        finally
        {
            _sending = false;
        }
    }

    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    internal static MarkupString RenderMarkdown(string content) =>
        new(Markdown.ToHtml(content, MarkdownPipeline));

    private record ChatMessage(string Role, string Content);
}
