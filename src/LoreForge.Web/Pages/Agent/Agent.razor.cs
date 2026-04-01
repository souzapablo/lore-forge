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

    private string? _conversationId;
    private readonly List<ChatMessage> _messages = [];
    private string _input = "";
    private bool _sending;
    private string? _errorMessage;
    private bool _scrollPending;
    private ElementReference _messagesEnd;
    private ElementReference _textareaRef;

    protected override void OnInitialized()
    {
        Layout?.SetPageTitle("Agente");
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

    private async Task ClearAsync()
    {
        if (_conversationId is not null)
            await Http.DeleteAsync($"agent/chat/{_conversationId}/history");

        _conversationId = null;
        _messages.Clear();
        _errorMessage = null;
        _input = "";
    }

    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    internal static MarkupString RenderMarkdown(string content) =>
        new(Markdown.ToHtml(content, MarkdownPipeline));

    private record ChatMessage(string Role, string Content);
}
