using System.ComponentModel;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SseWpfClient
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _selectedAction = "MSFT";
        private decimal _value;
        private string? _receptionAction;
        private string? _eventType;
        private DateTime _receptionDate = DateTime.Now;
        private CancellationTokenSource? _cancellationTokenSource;

        private AsyncCommand? _startSseCommand;
        private ICommand? _stopSseCommand;


        public string Title => "SseWpfClient";
        public IEnumerable<string> Actions { get; set; }= ["MSFT", "AAPL", "TSLA"];
        
        public string SelectedAction
        {
            get => _selectedAction;
            set => SetField(ref _selectedAction, value);
        }
        public DateTime ReceptionDate
        {
            get => _receptionDate;
            set => SetField(ref _receptionDate, value);
        }
        public decimal Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }

        public string? EventType
        {
            get => _eventType;
            set => SetField(ref _eventType, value);
        }
        public string? ReceptionAction
        {
            get => _receptionAction;
            set => SetField(ref _receptionAction, value);
        }





        public ICommand StartSseCommand => _startSseCommand ??= new AsyncCommand(StartSseAsync, CanExecuteStartSseCommand);
        public ICommand StopSseCommand => _stopSseCommand ??= new RelayCommand(StopSse);

        private async Task StartSseAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            await ConsumeSseAsync(_cancellationTokenSource.Token);
        }
        private bool CanExecuteStartSseCommand()
        {
            return _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        }

        private void StopSse(object? obj)
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task ConsumeSseAsync(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync($"http://localhost:5176/sse?action={SelectedAction}", cancellationToken);

            try
            {
                await foreach (var item in SseParser.Create(stream, (eventType, bytes) =>
                                   JsonSerializer.Deserialize<SseResponse>(bytes, JsonSerializerOptions.Web)).EnumerateAsync(cancellationToken))
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



        // Property changed event
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public record SseResponse(DateTime Date, string Action, decimal Value);
}
