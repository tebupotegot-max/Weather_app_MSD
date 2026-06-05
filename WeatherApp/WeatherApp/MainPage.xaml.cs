using WeatherApp.ViewModels;

namespace WeatherApp;

public partial class MainPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;
    private string _currentGradientStart = "#FF87CEEB";
    private string _currentGradientMid   = "#FFADD8E6";
    private string _currentGradientEnd   = "#FFD4EEF9";
    private CancellationTokenSource? _gradientAnimCts;

    public MainPage(WeatherViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        ContentView.Opacity = 0;
        Card1.Opacity = 0;
        Card2.Opacity = 0;
        Card3.Opacity = 0;
        Card4.Opacity = 0;
        Card5.Opacity = 0;
        RefreshBtn.Opacity = 0;
        ErrorView.Opacity = 0;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyGradientImmediate(_currentGradientStart, _currentGradientMid, _currentGradientEnd);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadWeatherAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopRealtime();
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            // ViewModel sets _gradientStart/Mid/End and fires only GradientStart —
            // we read all three at once here so the animation is called exactly once.
            case nameof(WeatherViewModel.GradientStart):
                await AnimateGradientAsync(
                    _viewModel.GradientStart,
                    _viewModel.GradientMid,
                    _viewModel.GradientEnd);
                break;

            case nameof(WeatherViewModel.HasData) when _viewModel.HasData:
                await FadeInContentAsync();
                break;

            case nameof(WeatherViewModel.HasError) when _viewModel.HasError:
                ErrorView.TranslationY = 20;
                _ = ErrorView.TranslateToAsync(0, 0, 500, Easing.CubicOut);
                await ErrorView.FadeToAsync(1, 500, Easing.CubicOut);
                break;
        }
    }

    // ─── Background gradient ──────────────────────────────────────────

    private void ApplyGradientImmediate(string start, string mid, string end)
    {
        _currentGradientStart = start;
        _currentGradientMid   = mid;
        _currentGradientEnd   = end;
        Background = MakeGradientBrush(
            ParseColor(start),
            ParseColor(mid),
            ParseColor(end));
    }

    private async Task AnimateGradientAsync(string newStart, string newMid, string newEnd)
    {
        _gradientAnimCts?.Cancel();
        _gradientAnimCts = new CancellationTokenSource();
        var token = _gradientAnimCts.Token;

        Color fromS, fromM, fromE, toS, toM, toE;
        try
        {
            fromS = ParseColor(_currentGradientStart);
            fromM = ParseColor(_currentGradientMid);
            fromE = ParseColor(_currentGradientEnd);
            toS   = ParseColor(newStart);
            toM   = ParseColor(newMid);
            toE   = ParseColor(newEnd);
        }
        catch
        {
            // Fall back to immediate apply if color parsing fails
            ApplyGradientImmediate(newStart, newMid, newEnd);
            return;
        }

        for (int i = 1; i <= 40; i++)
        {
            if (token.IsCancellationRequested) break;
            float t = EaseInOut(i / 40f);
            Background = MakeGradientBrush(
                Lerp(fromS, toS, t),
                Lerp(fromM, toM, t),
                Lerp(fromE, toE, t));
            await Task.Delay(20, CancellationToken.None);
        }

        if (!token.IsCancellationRequested)
            ApplyGradientImmediate(newStart, newMid, newEnd);
    }

    // Parses #AARRGGBB or #RRGGBB — safe for both formats
    private static Color ParseColor(string hex)
    {
        var s = hex.TrimStart('#');
        if (s.Length == 6) s = "FF" + s;          // add alpha if missing
        uint argb = Convert.ToUInt32(s, 16);
        float a = ((argb >> 24) & 0xFF) / 255f;
        float r = ((argb >> 16) & 0xFF) / 255f;
        float g = ((argb >>  8) & 0xFF) / 255f;
        float b = ((argb      ) & 0xFF) / 255f;
        return Color.FromRgba(r, g, b, a);
    }

    private static LinearGradientBrush MakeGradientBrush(Color s, Color m, Color e)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        brush.GradientStops.Add(new GradientStop { Color = s, Offset = 0f });
        brush.GradientStops.Add(new GradientStop { Color = m, Offset = 0.5f });
        brush.GradientStops.Add(new GradientStop { Color = e, Offset = 1f });
        return brush;
    }

    private static Color Lerp(Color a, Color b, float t) =>
        Color.FromRgba(
            a.Red   + (b.Red   - a.Red)   * t,
            a.Green + (b.Green - a.Green) * t,
            a.Blue  + (b.Blue  - a.Blue)  * t,
            a.Alpha + (b.Alpha - a.Alpha) * t);

    private static float EaseInOut(float t) =>
        t < 0.5f ? 2f * t * t : 1f - (-2f * t + 2f) * (-2f * t + 2f) / 2f;

    // ─── Entrance animations ──────────────────────────────────────────

    private async Task FadeInContentAsync()
    {
        ContentView.TranslationY = 20;
        await Task.WhenAll(
            ContentView.FadeToAsync(1, 500, Easing.CubicOut),
            ContentView.TranslateToAsync(0, 0, 500, Easing.CubicOut));

        await EntranceAsync(Card1);
        await EntranceAsync(Card2);
        await EntranceAsync(Card3);
        await EntranceAsync(Card4);
        await EntranceAsync(Card5);
        await EntranceAsync(RefreshBtn);
    }

    private static async Task EntranceAsync(VisualElement view, int delayMs = 50)
    {
        await Task.Delay(delayMs);
        view.TranslationY = 16;
        await Task.WhenAll(
            view.FadeToAsync(1, 380, Easing.CubicOut),
            view.TranslateToAsync(0, 0, 380, Easing.CubicOut));
    }
}
