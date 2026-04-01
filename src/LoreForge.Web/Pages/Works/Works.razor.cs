using LoreForge.Contracts.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Primitives;
using LoreForge.Web.Layout;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace LoreForge.Web.Pages.Works;

public partial class Works
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter] private MainLayout? Layout { get; set; }

    private PagedResult<WorkSummary>? _result;
    private bool _loading = true;
    private int _currentPage = 1;

    private const int PageSize = 12;

    private HashSet<WorkType> SelectedTypes { get; } = new();
    private HashSet<WorkStatus> SelectedStatuses { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        Layout?.SetPageTitle("Obras");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;

        var query = BuildQuery();
        _result = await Http.GetFromJsonAsync<PagedResult<WorkSummary>>($"logbook/works{query}");

        _loading = false;
    }

    private string BuildQuery()
    {
        var parts = new List<string>
        {
            $"page={_currentPage}",
            $"pageSize={PageSize}"
        };

        foreach (var t in SelectedTypes)
            parts.Add($"types={(int)t}");

        foreach (var s in SelectedStatuses)
            parts.Add($"statuses={(int)s}");

        return "?" + string.Join("&", parts);
    }

    private async Task ToggleType(WorkType type)
    {
        if (!SelectedTypes.Remove(type)) SelectedTypes.Add(type);
        _currentPage = 1;
        await LoadAsync();
    }

    private async Task ToggleStatus(WorkStatus status)
    {
        if (!SelectedStatuses.Remove(status)) SelectedStatuses.Add(status);
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

    private void OpenWork(Guid id) => Nav.NavigateTo($"/works/{id}");

    // ── Add modal ──

    private bool _showAddModal;
    private bool _addSaving;
    private string? _addErrorMessage;
    private AddForm _addForm = new();

    private string AddTitleInputClass =>
        _addErrorMessage is not null && string.IsNullOrWhiteSpace(_addForm.Title)
            ? "field-input field-invalid"
            : "field-input";

    private string AddNotesGridClass =>
        _addErrorMessage is not null && AddNoNotesFilledIn()
            ? "notes-grid notes-grid-invalid"
            : "notes-grid";

    private bool AddNoNotesFilledIn() =>
        string.IsNullOrWhiteSpace(_addForm.Worldbuilding)
        && string.IsNullOrWhiteSpace(_addForm.Magic)
        && string.IsNullOrWhiteSpace(_addForm.Characters)
        && string.IsNullOrWhiteSpace(_addForm.Themes)
        && string.IsNullOrWhiteSpace(_addForm.PlotStructure)
        && string.IsNullOrWhiteSpace(_addForm.WhatILiked);

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
        if (string.IsNullOrWhiteSpace(_addForm.Title))
        {
            _addErrorMessage = "O título é obrigatório.";
            return;
        }

        if (AddNoNotesFilledIn())
        {
            _addErrorMessage = "Preencha pelo menos um campo de notas.";
            return;
        }

        _addSaving = true;

        var request = new
        {
            Title = _addForm.Title,
            Type = _addForm.Type,
            Genres = SplitCsv(_addForm.Genres),
            Status = _addForm.Status,
            Progress = (object?)null,
            Notes = new
            {
                Worldbuilding = NullIfEmpty(_addForm.Worldbuilding),
                Magic = NullIfEmpty(_addForm.Magic),
                Characters = NullIfEmpty(_addForm.Characters),
                Themes = NullIfEmpty(_addForm.Themes),
                PlotStructure = NullIfEmpty(_addForm.PlotStructure),
                WhatILiked = NullIfEmpty(_addForm.WhatILiked)
            },
            Tags = SplitCsv(_addForm.Tags)
        };

        var response = await Http.PostAsJsonAsync("logbook/works", request);

        if (response.IsSuccessStatusCode)
        {
            _showAddModal = false;
            _currentPage = 1;
            await LoadAsync();
        }
        else
        {
            _addErrorMessage = "Não foi possível adicionar a obra. Tente novamente.";
        }

        _addSaving = false;
    }

    private static string[] SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private class AddForm
    {
        public string Title { get; set; } = "";
        public WorkType Type { get; set; } = WorkType.Book;
        public WorkStatus Status { get; set; } = WorkStatus.InProgress;
        public string Genres { get; set; } = "";
        public string Tags { get; set; } = "";
        public string? Worldbuilding { get; set; }
        public string? Magic { get; set; }
        public string? Characters { get; set; }
        public string? Themes { get; set; }
        public string? PlotStructure { get; set; }
        public string? WhatILiked { get; set; }
    }

    // ── Delete ──

    private Guid? _confirmingDeleteId;

    private void RequestDelete(Guid id) => _confirmingDeleteId = id;

    private void CancelDelete() => _confirmingDeleteId = null;

    private async Task ConfirmDeleteAsync(Guid id)
    {
        await Http.DeleteAsync($"logbook/works/{id}");
        _confirmingDeleteId = null;
        await LoadAsync();
    }

    private static string DotClass(WorkType type) => type switch
    {
        WorkType.Game   => "dot-game",
        WorkType.Book   => "dot-book",
        WorkType.Series => "dot-series",
        WorkType.Movie  => "dot-movie",
        _               => "dot-other"
    };

    private static string StatusClass(WorkStatus status) => status switch
    {
        WorkStatus.Completed  => "status-completed",
        WorkStatus.InProgress => "status-inprogress",
        WorkStatus.Dropped    => "status-dropped",
        _                     => ""
    };

    private static string TypeLabel(WorkType type) => type switch
    {
        WorkType.Game   => "Jogo",
        WorkType.Book   => "Livro",
        WorkType.Movie  => "Filme",
        WorkType.Series => "Série",
        WorkType.Other  => "Outro",
        _               => type.ToString()
    };

    private static string StatusLabel(WorkStatus status) => status switch
    {
        WorkStatus.InProgress => "Em andamento",
        WorkStatus.Completed  => "Concluído",
        WorkStatus.Dropped    => "Abandonado",
        _                     => status.ToString()
    };
}
