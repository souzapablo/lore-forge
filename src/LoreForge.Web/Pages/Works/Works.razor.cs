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

    private void OpenAddWork() => Nav.NavigateTo("/works/add");

    private void OpenWork(Guid id) => Nav.NavigateTo($"/works/{id}");

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
