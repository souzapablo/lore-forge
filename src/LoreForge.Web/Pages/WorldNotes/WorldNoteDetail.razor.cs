using LoreForge.Contracts.WorldNotes;
using LoreForge.Core.Entities;
using LoreForge.Web.Layout;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.Web.Pages.WorldNotes;

public partial class WorldNoteDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter] private MainLayout? Layout { get; set; }

    private WorldNoteSummary? _note;
    private bool _loading = true;
    private bool _notFound;
    private bool _editing;
    private bool _saving;
    private bool _confirmingDelete;
    private string? _errorMessage;
    private string _editContent = "";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        var response = await Http.GetAsync($"world-notes/{Id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _notFound = true;
        }
        else
        {
            _note = await response.Content.ReadFromJsonAsync<WorldNoteSummary>();
            Layout?.SetPageTitle(_note?.Title ?? "Nota");
        }

        _loading = false;
    }

    private void StartEdit()
    {
        if (_note is null) return;
        _editContent = _note.Content;
        _errorMessage = null;
        _editing = true;
    }

    private void CancelEdit()
    {
        _editing = false;
        _errorMessage = null;
    }

    private void RequestDelete() => _confirmingDelete = true;

    private void CancelDelete() => _confirmingDelete = false;

    private async Task ConfirmDeleteAsync()
    {
        await Http.DeleteAsync($"world-notes/{Id}");
        Nav.NavigateTo("/world-notes");
    }

    private async Task SaveAsync()
    {
        if (_note is null) return;

        if (string.IsNullOrWhiteSpace(_editContent))
        {
            _errorMessage = "O conteúdo é obrigatório.";
            return;
        }

        _saving = true;

        var response = await Http.PutAsJsonAsync("world-notes", new
        {
            _note.Category,
            _note.Title,
            Content = _editContent
        });

        if (response.IsSuccessStatusCode)
        {
            _editing = false;
            await LoadAsync();
        }
        else
        {
            _errorMessage = "Não foi possível salvar as alterações. Tente novamente.";
        }

        _saving = false;
    }

    private static string CategoryLabel(WorldNoteCategory cat) => cat switch
    {
        WorldNoteCategory.Character => "Personagem",
        WorldNoteCategory.Location  => "Local",
        WorldNoteCategory.Magic     => "Magia",
        WorldNoteCategory.Lore      => "Lore",
        WorldNoteCategory.Plot      => "Trama",
        WorldNoteCategory.Freeform  => "Livre",
        _                           => cat.ToString()
    };

    private static string CategoryClass(WorldNoteCategory cat) => cat switch
    {
        WorldNoteCategory.Character => "badge-character",
        WorldNoteCategory.Location  => "badge-location",
        WorldNoteCategory.Magic     => "badge-magic",
        WorldNoteCategory.Lore      => "badge-lore",
        WorldNoteCategory.Plot      => "badge-plot",
        WorldNoteCategory.Freeform  => "badge-freeform",
        _                           => ""
    };
}
