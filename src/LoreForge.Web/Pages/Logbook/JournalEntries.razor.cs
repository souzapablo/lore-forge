using LoreForge.Contracts.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Primitives;
using LoreForge.Web.Layout;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace LoreForge.Web.Pages.Logbook;

public partial class JournalEntries
{
    [Inject] private HttpClient Http { get; set; } = default!;

    [CascadingParameter] private MainLayout? Layout { get; set; }

    private PagedResult<JournalEntrySummary>? _result;
    private bool _loading = true;
    private int _currentPage = 1;

    private const int PageSize = 10;

    private HashSet<JournalSource> SelectedSources { get; } = new();

    // Works loaded once for display and the add modal dropdown
    private List<WorkSummary> _works = [];
    private Dictionary<Guid, WorkSummary> _worksById = [];

    protected override async Task OnInitializedAsync()
    {
        Layout?.SetPageTitle("Anotações");
        await Task.WhenAll(LoadWorksAsync(), LoadAsync());
    }

    private async Task LoadWorksAsync()
    {
        var result = await Http.GetFromJsonAsync<PagedResult<WorkSummary>>("logbook/works?page=1&pageSize=200");
        if (result is not null)
        {
            _works = result.Items;
            _worksById = _works.ToDictionary(w => w.Id);
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _result = await Http.GetFromJsonAsync<PagedResult<JournalEntrySummary>>($"logbook/journal-entries{BuildQuery()}");
        _loading = false;
    }

    private string BuildQuery()
    {
        var parts = new List<string>
        {
            $"page={_currentPage}",
            $"pageSize={PageSize}"
        };

        foreach (var s in SelectedSources)
            parts.Add($"sources={(int)s}");

        return "?" + string.Join("&", parts);
    }

    private async Task ToggleSource(JournalSource source)
    {
        if (!SelectedSources.Remove(source)) SelectedSources.Add(source);
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
    private bool _addSaving;
    private string? _addErrorMessage;
    private AddForm _addForm = new();

    private string AddContentClass =>
        _addErrorMessage is not null && string.IsNullOrWhiteSpace(_addForm.RawContent)
            ? "field-textarea field-invalid"
            : "field-textarea";

    private void OpenAddModal()
    {
        _addForm = new AddForm();
        _addErrorMessage = null;
        _showAddModal = true;
    }

    private void CloseAddModal()
    {
        _showAddModal = false;
        _addErrorMessage = null;
    }

    private async Task SaveAddAsync()
    {
        if (string.IsNullOrWhiteSpace(_addForm.RawContent))
        {
            _addErrorMessage = "O conteúdo é obrigatório.";
            return;
        }

        _addSaving = true;

        var workId = string.IsNullOrEmpty(_addForm.WorkId) ? (Guid?)null : Guid.Parse(_addForm.WorkId);

        var request = new
        {
            WorkId = workId,
            ProgressSnapshot = NullIfEmpty(_addForm.ProgressSnapshot),
            Source = _addForm.Source,
            RawContent = _addForm.RawContent,
            FileRef = (string?)null
        };

        var response = await Http.PostAsJsonAsync("logbook/journal-entries", request);

        if (response.IsSuccessStatusCode)
        {
            _showAddModal = false;
            _currentPage = 1;
            await LoadAsync();
        }
        else
        {
            _addErrorMessage = "Não foi possível adicionar a anotação. Tente novamente.";
        }

        _addSaving = false;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    // ── Delete ──

    private Guid? _confirmingDeleteId;

    private void RequestDelete(Guid id) => _confirmingDeleteId = id;

    private void CancelDelete() => _confirmingDeleteId = null;

    private async Task ConfirmDeleteAsync(Guid id)
    {
        await Http.DeleteAsync($"logbook/journal-entries/{id}");
        _confirmingDeleteId = null;
        await LoadAsync();
    }

    // ── Helpers ──

    private static string SourceLabel(JournalSource source) => source switch
    {
        JournalSource.Chat      => "Chat",
        JournalSource.PlainText => "Texto",
        JournalSource.File      => "Arquivo",
        _                       => source.ToString()
    };

    private static string FormatDate(DateTime date) =>
        date.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    private class AddForm
    {
        public string WorkId { get; set; } = "";
        public JournalSource Source { get; set; } = JournalSource.PlainText;
        public string? ProgressSnapshot { get; set; }
        public string RawContent { get; set; } = "";
    }
}
