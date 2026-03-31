using LoreForge.Core.Entities;
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
    private bool _loading = true;
    private bool _notFound;

    protected override async Task OnInitializedAsync()
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
