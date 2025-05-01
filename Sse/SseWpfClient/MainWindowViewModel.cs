using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SseWpfClient
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private CancellationTokenSource? _cancellationTokenSource;

        public string Title => "SseWpfClient";
        public IEnumerable<string> Actions { get; set; }= ["MSFT", "AAPL", "TSLA"];

        [ObservableProperty] public partial string SelectedAction { get; set; } = "MSFT";
        [ObservableProperty] public partial DateTime ReceptionDate { get; set; }
        [ObservableProperty] public partial decimal Value { get; set; }
        [ObservableProperty] public partial string? EventType { get; set; }
        [ObservableProperty] public partial string? ReceptionAction { get; set; }


        [RelayCommand(CanExecute = nameof(CanExecuteStartSseCommand))] private async Task StartSseAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync($"http://localhost:5176/sse?action={SelectedAction}",_cancellationTokenSource.Token);

            try
            {
                await foreach (var item in SseParser.Create(stream, (eventType, bytes) =>
                                   JsonSerializer.Deserialize<SseResponse>(bytes, JsonSerializerOptions.Web)).EnumerateAsync(_cancellationTokenSource.Token))
                {
                    if (item.Data == null || !string.Equals(item.EventType, "info")) continue;

                    ReceptionDate = item.Data.Date;
                    Value = item.Data.Value;
                    ReceptionAction = item.Data.Action;
                    EventType = item.EventType;
                }
            }
            catch (TaskCanceledException e)
            {
                // annulation
            }
        }
        [RelayCommand] private void StopSse()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        private bool CanExecuteStartSseCommand()
        {
            return _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        }
    }
}
