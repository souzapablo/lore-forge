using LoreForge.Contracts.WorldNotes;
using LoreForge.Core.Entities;
using LoreForge.Core.Primitives;
using LoreForge.Web.Layout;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace LoreForge.Web.Pages.WorldNotes;

public partial class WorldNotes
{
    [Inject] private HttpClient Http { get; set; } = default!;

    [CascadingParameter] private MainLayout? Layout { get; set; }

    private PagedResult<WorldNoteSummary>? _result;
    private bool _loading = true;
    private int _currentPage = 1;

    private const int PageSize = 20;

    private WorldNoteCategory? SelectedCategory { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Layout?.SetPageTitle("Notas do Mundo");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;

        var query = BuildQuery();
        _result = await Http.GetFromJsonAsync<PagedResult<WorldNoteSummary>>($"world-notes{query}");

        _loading = false;
    }

    private string BuildQuery()
    {
        var parts = new List<string>
        {
            $"page={_currentPage}",
            $"pageSize={PageSize}"
        };

        if (SelectedCategory is not null)
            parts.Add($"category={(int)SelectedCategory}");

        return "?" + string.Join("&", parts);
    }

    private async Task SelectCategory(WorldNoteCategory cat)
    {
        SelectedCategory = cat;
        _currentPage = 1;
        await LoadAsync();
    }

    private async Task ClearCategory()
    {
        SelectedCategory = null;
        _currentPage = 1;
        await LoadAsync();
    }

    private async Task PreviousPage()
    {
        _currentPage--;
        await LoadAsync();
    }

    private async Task NextPage()
    {
        _currentPage++;
        await LoadAsync();
    }

    // ── Add modal ──

    private bool _showAddModal;
    private bool _modalSaving;
    private string? _modalErrorMessage;
    private NoteForm _addForm = new();

    private string AddTitleInputClass =>
        _modalErrorMessage is not null && string.IsNullOrWhiteSpace(_addForm.Title)
            ? "field-input field-invalid"
            : "field-input";

    private void OpenAddModal()
    {
        _addForm = new NoteForm();
        _modalErrorMessage = null;
        _showAddModal = true;
    }

    private void CloseAddModal()
    {
        _showAddModal = false;
        _modalErrorMessage = null;
    }

    private async Task SaveAddAsync()
    {
        if (string.IsNullOrWhiteSpace(_addForm.Title))
        {
            _modalErrorMessage = "O título é obrigatório.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_addForm.Content))
        {
            _modalErrorMessage = "O conteúdo é obrigatório.";
            return;
        }

        _modalSaving = true;

        var response = await Http.PutAsJsonAsync("world-notes", new
        {
            Category = _addForm.Category,
            Title = _addForm.Title,
            Content = _addForm.Content
        });

        if (response.IsSuccessStatusCode)
        {
            _showAddModal = false;
            _currentPage = 1;
            await LoadAsync();
        }
        else
        {
            _modalErrorMessage = "Não foi possível salvar a nota. Tente novamente.";
        }

        _modalSaving = false;
    }

    // ── Edit modal ──

    private bool _showEditModal;
    private NoteForm? _editForm;
    private Guid _editNoteId;
    private bool _confirmingDelete;

    private void OpenEditModal(WorldNoteSummary note)
    {
        _editNoteId = note.Id;
        _editForm = new NoteForm
        {
            Title = note.Title,
            Category = note.Category,
            Content = note.Content
        };
        _modalErrorMessage = null;
        _confirmingDelete = false;
        _showEditModal = true;
    }

    private void CloseEditModal()
    {
        _showEditModal = false;
        _modalErrorMessage = null;
        _confirmingDelete = false;
    }

    private void RequestDelete() => _confirmingDelete = true;

    private void CancelDelete() => _confirmingDelete = false;

    private async Task ConfirmDeleteAsync()
    {
        _modalSaving = true;
        await Http.DeleteAsync($"world-notes/{_editNoteId}");
        _showEditModal = false;
        _confirmingDelete = false;
        _currentPage = 1;
        await LoadAsync();
        _modalSaving = false;
    }

    private async Task SaveEditAsync()
    {
        if (_editForm is null) return;

        if (string.IsNullOrWhiteSpace(_editForm.Content))
        {
            _modalErrorMessage = "O conteúdo é obrigatório.";
            return;
        }

        _modalSaving = true;

        var response = await Http.PutAsJsonAsync("world-notes", new
        {
            Category = _editForm.Category,
            Title = _editForm.Title,
            Content = _editForm.Content
        });

        if (response.IsSuccessStatusCode)
        {
            _showEditModal = false;
            await LoadAsync();
        }
        else
        {
            _modalErrorMessage = "Não foi possível salvar a nota. Tente novamente.";
        }

        _modalSaving = false;
    }

    // ── Helpers ──

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

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    private class NoteForm
    {
        public string Title { get; set; } = "";
        public WorldNoteCategory Category { get; set; } = WorldNoteCategory.Freeform;
        public string Content { get; set; } = "";
    }
}
