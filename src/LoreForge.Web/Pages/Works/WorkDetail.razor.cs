using LoreForge.Contracts.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Primitives;
using LoreForge.Web.Layout;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using WorkDetailDto = LoreForge.Contracts.Logbook.WorkDetail;

namespace LoreForge.Web.Pages.Works;

public partial class WorkDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter] private MainLayout? Layout { get; set; }

    private WorkDetailDto? _work;
    private List<JournalEntrySummary> _entries = [];
    private bool _loading = true;
    private bool _entriesLoading = true;
    private bool _notFound;
    private bool _editing;
    private bool _saving;
    private bool _confirmingDelete;
    private string? _errorMessage;
    private EditForm _form = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        _ = LoadEntriesAsync();
    }

    private async Task LoadAsync()
    {
        var response = await Http.GetAsync($"logbook/works/{Id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _notFound = true;
        }
        else
        {
            _work = await response.Content.ReadFromJsonAsync<WorkDetailDto>();
            Layout?.SetPageTitle(_work?.Title ?? "Obra");
        }

        _loading = false;
    }

    private async Task LoadEntriesAsync()
    {
        _entriesLoading = true;
        var result = await Http.GetFromJsonAsync<PagedResult<JournalEntrySummary>>($"logbook/journal-entries?workId={Id}&pageSize=50");
        _entries = result?.Items ?? [];
        _entriesLoading = false;
        StateHasChanged();
    }

    private void StartEdit()
    {
        if (_work is null) return;

        _form = new EditForm
        {
            Title = _work.Title,
            Status = _work.Status,
            Genres = string.Join(", ", _work.Genres),
            Tags = string.Join(", ", _work.Tags),
            Worldbuilding = _work.Notes.Worldbuilding,
            Magic = _work.Notes.Magic,
            Characters = _work.Notes.Characters,
            Themes = _work.Notes.Themes,
            PlotStructure = _work.Notes.PlotStructure,
            WhatILiked = _work.Notes.WhatILiked
        };

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
        await Http.DeleteAsync($"logbook/works/{Id}");
        Nav.NavigateTo("/works");
    }

    private async Task SaveAsync()
    {
        _errorMessage = Validate();
        if (_errorMessage is not null) return;

        _saving = true;

        var request = new
        {
            Title = _form.Title,
            Genres = SplitCsv(_form.Genres),
            Status = _form.Status,
            Progress = (object?)null,
            Notes = new
            {
                Worldbuilding = NullIfEmpty(_form.Worldbuilding),
                Magic = NullIfEmpty(_form.Magic),
                Characters = NullIfEmpty(_form.Characters),
                Themes = NullIfEmpty(_form.Themes),
                PlotStructure = NullIfEmpty(_form.PlotStructure),
                WhatILiked = NullIfEmpty(_form.WhatILiked)
            },
            Tags = SplitCsv(_form.Tags)
        };

        var response = await Http.PutAsJsonAsync($"logbook/works/{Id}", request);

        if (response.IsSuccessStatusCode)
        {
            _editing = false;
            await LoadAsync();
        }
        else
        {
            _errorMessage = "Não foi possível salvar as alterações. Verifique os campos e tente novamente.";
        }

        _saving = false;
    }

    private bool NoNotesFilledIn() =>
        string.IsNullOrWhiteSpace(_form.Worldbuilding)
        && string.IsNullOrWhiteSpace(_form.Magic)
        && string.IsNullOrWhiteSpace(_form.Characters)
        && string.IsNullOrWhiteSpace(_form.Themes)
        && string.IsNullOrWhiteSpace(_form.PlotStructure)
        && string.IsNullOrWhiteSpace(_form.WhatILiked);

    private string TitleInputClass =>
        _errorMessage is not null && string.IsNullOrWhiteSpace(_form.Title)
            ? "field-input field-title field-invalid"
            : "field-input field-title";

    private string NotesGridClass =>
        _errorMessage is not null && NoNotesFilledIn()
            ? "notes-grid notes-grid-invalid"
            : "notes-grid";

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(_form.Title))
            return "O título é obrigatório.";

        var hasNotes = !string.IsNullOrWhiteSpace(_form.Worldbuilding)
            || !string.IsNullOrWhiteSpace(_form.Magic)
            || !string.IsNullOrWhiteSpace(_form.Characters)
            || !string.IsNullOrWhiteSpace(_form.Themes)
            || !string.IsNullOrWhiteSpace(_form.PlotStructure)
            || !string.IsNullOrWhiteSpace(_form.WhatILiked);

        if (!hasNotes)
            return "Preencha pelo menos um campo de notas.";

        return null;
    }

    private static string[] SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

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

    private static string SourceLabel(JournalSource source) => source switch
    {
        JournalSource.Chat      => "Chat",
        JournalSource.PlainText => "Texto",
        JournalSource.File      => "Arquivo",
        _                       => source.ToString()
    };

    private static string SourceClass(JournalSource source) => source switch
    {
        JournalSource.Chat      => "source-badge source-chat",
        JournalSource.File      => "source-badge source-file",
        _                       => "source-badge source-plaintext"
    };

    private static string FormatDate(DateTime date) =>
        date.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    // ── Add journal entry modal ──

    private bool _showEntryModal;
    private bool _entrySaving;
    private string? _entryErrorMessage;
    private EntryForm _entryForm = new();

    private string EntryContentClass =>
        _entryErrorMessage is not null && string.IsNullOrWhiteSpace(_entryForm.RawContent)
            ? "field-textarea field-invalid"
            : "field-textarea";

    private void OpenEntryModal()
    {
        _entryForm = new EntryForm();
        _entryErrorMessage = null;
        _showEntryModal = true;
    }

    private void CloseEntryModal()
    {
        _showEntryModal = false;
        _entryErrorMessage = null;
    }

    private async Task SaveEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(_entryForm.RawContent))
        {
            _entryErrorMessage = "O conteúdo é obrigatório.";
            return;
        }

        _entrySaving = true;

        var request = new
        {
            WorkId = Id,
            ProgressSnapshot = NullIfEmpty(_entryForm.ProgressSnapshot),
            _entryForm.Source,
            _entryForm.RawContent,
            FileRef = (string?)null
        };

        var response = await Http.PostAsJsonAsync("logbook/journal-entries", request);

        if (response.IsSuccessStatusCode)
        {
            _showEntryModal = false;
            await LoadEntriesAsync();
        }
        else
        {
            _entryErrorMessage = "Não foi possível adicionar a anotação. Tente novamente.";
        }

        _entrySaving = false;
    }

    private class EditForm
    {
        public string Title { get; set; } = "";
        public WorkStatus Status { get; set; }
        public string Genres { get; set; } = "";
        public string Tags { get; set; } = "";
        public string? Worldbuilding { get; set; }
        public string? Magic { get; set; }
        public string? Characters { get; set; }
        public string? Themes { get; set; }
        public string? PlotStructure { get; set; }
        public string? WhatILiked { get; set; }
    }

    private class EntryForm
    {
        public JournalSource Source { get; set; } = JournalSource.PlainText;
        public string? ProgressSnapshot { get; set; }
        public string RawContent { get; set; } = "";
    }
}
