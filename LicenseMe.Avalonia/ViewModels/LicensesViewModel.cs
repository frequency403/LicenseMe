using System.Collections.ObjectModel;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using ReactiveUI.SourceGenerators;

namespace LicenseMe.Avalonia.ViewModels;

public partial class LicensesViewModel : ViewModelBase
{
    [Reactive] private bool _isSearching;
    [Reactive] private bool _hasInternetConnection;
    
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<LicensesViewModel> _logger;
    private readonly IOsiClient _osiClient;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IProgressReporter<string> _progressReporter;
    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;

    public LicensesViewModel(ILogger<LicensesViewModel> logger, 
        IOsiClient osiClient, 
        IHttpClientFactory clientFactory, 
        [FromKeyedServices("LicenseReporter")] IProgressReporter<string> progressReporter)
    {
        _logger = logger;
        _osiClient = osiClient;
        _clientFactory = clientFactory;
        _progressReporter = progressReporter;
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _timerTask = Task.Run(async () =>
        {
            HasInternetConnection = await IsInternetAvailableAsync();
        });
        Disposables.Add(_timerTask);
        Disposables.Add(_timer);
    }

    public ObservableCollection<OsiLicense> Licenses { get; } = [];
    public IProgressReporter<string> ProgressReporter => _progressReporter;


    [ReactiveCommand(CanExecute = nameof(IsSearching))]
    private async Task CancelOperation(CancellationToken token)
    {
        try
        {
            if (_cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not cancel license-fetch operation:");
        }
    }
    
    [ReactiveCommand(CanExecute = nameof(HasInternetConnection))]
    private async Task GetLicensesAsync(CancellationToken token)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        try
        {
            IsSearching = true;
            await foreach (var license in _osiClient.GetAllLicensesAsyncEnumerable(token))
            {
                if (license != null)
                {
                    _progressReporter.TryUpdateProgress($"Adding license {license.Name}");
                    _logger.LogInformation("Adding license {LicenseName}", license.Name);
                    Licenses.Add(license);
                }
                else
                {
                    _logger.LogWarning("License was null");
                }
            }

            _progressReporter.TryUpdateProgress("");
        }
        finally
        {
            IsSearching = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task<bool> IsInternetAvailableAsync(CancellationToken ct = default)
    {
        using var client = _clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        try
        {
            using var response = await client.GetAsync("https://connectivitycheck.gstatic.com/generate_204", ct);
            return response.StatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch
        {
            return false;
        }
    }
}