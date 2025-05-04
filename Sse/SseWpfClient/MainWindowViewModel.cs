using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SseWpfClient
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts;

        public IEnumerable<string> Actions { get; set; }= ["MSFT", "AAPL", "TSLA"];

        [ObservableProperty] public partial string SelectedAction { get; set; } = "MSFT";
        [ObservableProperty] public partial DateTime ReceptionDate { get; set; }
        [ObservableProperty] public partial decimal Value { get; set; }
        [ObservableProperty] public partial string? EventType { get; set; }
        [ObservableProperty] public partial string? ReceptionAction { get; set; }


        [RelayCommand(CanExecute = nameof(CanExecuteStartSseCommand))] private async Task StartSseAsync()
        {
            _cts = new CancellationTokenSource();

            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync($"http://localhost:5176/sse?action={SelectedAction}",_cts.Token);

            try
            {
                // fonction qui lit la réponse du serveur et la transforme en SseResponse
                SseResponse? ItemParser(string eventType, ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<SseResponse>(bytes, JsonSerializerOptions.Web);

                await foreach (var item in SseParser.Create(stream, ItemParser).EnumerateAsync(_cts.Token))
                {
                    // seuls les eventType de tyoe "info" sont traités
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
            _cts?.Cancel();
        }
        
        private bool CanExecuteStartSseCommand()
        {
            return _cts == null || _cts.IsCancellationRequested;
        }
    }
}
