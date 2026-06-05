using System.Collections.ObjectModel;
using System.Globalization;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.ViewModels;

public class WeatherViewModel : BindableObject
{
    private readonly WeatherService _weatherService;
    private readonly LocationService _locationService;

    private long   _sunriseUnix;
    private long   _sunsetUnix;
    private double _savedLat;
    private double _savedLon;
    private bool   _hasLocation;
    private CancellationTokenSource? _realtimeCts;

    public WeatherViewModel(WeatherService weatherService, LocationService locationService)
    {
        _weatherService = weatherService;
        _locationService = locationService;
        RefreshCommand = new Command(async () => await LoadWeatherAsync(silent: true));
    }

    public Command RefreshCommand { get; }

    // ── State ──────────────────────────────────────────────────────────

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(); }
    }

    private bool _hasData;
    public bool HasData
    {
        get => _hasData;
        set { _hasData = value; OnPropertyChanged(); }
    }

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; OnPropertyChanged(); }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    // ── Identity ───────────────────────────────────────────────────────

    private string _cityName = string.Empty;
    public string CityName
    {
        get => _cityName;
        set { _cityName = value; OnPropertyChanged(); }
    }

    private string _country = string.Empty;
    public string Country
    {
        get => _country;
        set { _country = value; OnPropertyChanged(); }
    }

    // ── Temperature ────────────────────────────────────────────────────

    // Raw number strings (no degree symbol — added in XAML Spans)
    private string _temperature = string.Empty;
    public string Temperature
    {
        get => _temperature;
        set { _temperature = value; OnPropertyChanged(); }
    }

    private string _feelsLike = string.Empty;
    public string FeelsLike
    {
        get => _feelsLike;
        set { _feelsLike = value; OnPropertyChanged(); }
    }

    private string _tempMin = string.Empty;
    public string TempMin
    {
        get => _tempMin;
        set { _tempMin = value; OnPropertyChanged(); }
    }

    private string _tempMax = string.Empty;
    public string TempMax
    {
        get => _tempMax;
        set { _tempMax = value; OnPropertyChanged(); }
    }

    // ── Condition ──────────────────────────────────────────────────────

    private string _conditionDesc = string.Empty;
    public string ConditionDesc
    {
        get => _conditionDesc;
        set { _conditionDesc = value; OnPropertyChanged(); }
    }

    private string _weatherIcon = "☀️";
    public string WeatherIcon
    {
        get => _weatherIcon;
        set { _weatherIcon = value; OnPropertyChanged(); }
    }

    // ── Details ────────────────────────────────────────────────────────

    private string _humidity = string.Empty;
    public string Humidity
    {
        get => _humidity;
        set { _humidity = value; OnPropertyChanged(); }
    }

    private string _windSpeed = string.Empty;
    public string WindSpeed
    {
        get => _windSpeed;
        set { _windSpeed = value; OnPropertyChanged(); }
    }

    private string _windDirection = string.Empty;
    public string WindDirection
    {
        get => _windDirection;
        set { _windDirection = value; OnPropertyChanged(); }
    }

    private string _pressure = string.Empty;
    public string Pressure
    {
        get => _pressure;
        set { _pressure = value; OnPropertyChanged(); }
    }

    private string _visibility = string.Empty;
    public string Visibility
    {
        get => _visibility;
        set { _visibility = value; OnPropertyChanged(); }
    }

    private string _feelsLikeDesc = string.Empty;
    public string FeelsLikeDesc
    {
        get => _feelsLikeDesc;
        set { _feelsLikeDesc = value; OnPropertyChanged(); }
    }

    private string _humidityDesc = string.Empty;
    public string HumidityDesc
    {
        get => _humidityDesc;
        set { _humidityDesc = value; OnPropertyChanged(); }
    }

    // ── Sun ────────────────────────────────────────────────────────────

    private string _sunriseTime = string.Empty;
    public string SunriseTime
    {
        get => _sunriseTime;
        set { _sunriseTime = value; OnPropertyChanged(); }
    }

    private string _sunsetTime = string.Empty;
    public string SunsetTime
    {
        get => _sunsetTime;
        set { _sunsetTime = value; OnPropertyChanged(); }
    }

    private double _sunProgress;
    public double SunProgress
    {
        get => _sunProgress;
        set { _sunProgress = value; OnPropertyChanged(); }
    }

    private string _currentTimeStr = string.Empty;
    public string CurrentTimeStr
    {
        get => _currentTimeStr;
        set { _currentTimeStr = value; OnPropertyChanged(); }
    }

    private string _lastUpdated = string.Empty;
    public string LastUpdated
    {
        get => _lastUpdated;
        set { _lastUpdated = value; OnPropertyChanged(); }
    }

    // ── Hourly forecast ────────────────────────────────────────────────

    public ObservableCollection<HourlyItem> HourlyForecast { get; } = [];

    // ── Gradient (8-digit AARRGGBB — required by Color.FromArgb in .NET MAUI) ──

    private string _gradientStart = "#FF87CEEB";
    public string GradientStart
    {
        get => _gradientStart;
        set { _gradientStart = value; OnPropertyChanged(); }
    }

    private string _gradientMid = "#FFADD8E6";
    public string GradientMid
    {
        get => _gradientMid;
        set { _gradientMid = value; OnPropertyChanged(); }
    }

    private string _gradientEnd = "#FFD4EEF9";
    public string GradientEnd
    {
        get => _gradientEnd;
        set { _gradientEnd = value; OnPropertyChanged(); }
    }

    // ── Load ───────────────────────────────────────────────────────────

    public async Task LoadWeatherAsync(bool silent = false)
    {
        if (!silent)
        {
            IsLoading = true;
            HasData = false;
            HasError = false;
        }
        else
        {
            IsRefreshing = true;
        }

        try
        {
            if (!_hasLocation || !silent)
            {
                var (lat, lon) = await _locationService.GetLocationAsync();
                _savedLat = lat;
                _savedLon = lon;
                _hasLocation = true;
            }

            var weatherTask  = _weatherService.GetWeatherAsync(_savedLat, _savedLon);
            var forecastTask = _weatherService.GetForecastAsync(_savedLat, _savedLon);
            await Task.WhenAll(weatherTask, forecastTask);
            var weather  = await weatherTask;
            var forecast = await forecastTask;

            ApplyWeather(weather, forecast);
            StartRealtime();
        }
        catch (PermissionException)
        {
            ErrorMessage = "Location access is required.\nPlease enable location permission in Settings.";
            HasError = true;
        }
        catch (Exception ex)
        {
            // Include exception type to help diagnose unexpected errors
            ErrorMessage = $"Could not load weather data.\n({ex.GetType().Name}) {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private void ApplyWeather(WeatherResponse data, ForecastResponse forecast)
    {
        CityName = data.Name;
        Country  = data.Sys.Country;

        Temperature = Round(data.Main.Temp).ToString(CultureInfo.InvariantCulture);
        FeelsLike   = Round(data.Main.FeelsLike).ToString(CultureInfo.InvariantCulture);
        TempMin     = Round(data.Main.TempMin).ToString(CultureInfo.InvariantCulture);
        TempMax     = Round(data.Main.TempMax).ToString(CultureInfo.InvariantCulture);

        var w = data.Weather.FirstOrDefault();
        ConditionDesc = Capitalize(w?.Description ?? string.Empty);

        var icon = w?.Icon ?? "01d";
        WeatherIcon = MapIcon(icon);

        Humidity      = data.Main.Humidity.ToString(CultureInfo.InvariantCulture);
        HumidityDesc  = HumidityDescription(data.Main.Humidity);
        WindSpeed     = data.Wind.Speed.ToString("F1", CultureInfo.InvariantCulture);
        WindDirection = WindDegToCardinal(data.Wind.Deg);
        Pressure      = data.Main.Pressure.ToString(CultureInfo.InvariantCulture);
        Visibility    = (data.Visibility / 1000.0).ToString("F1", CultureInfo.InvariantCulture);
        FeelsLikeDesc = FeelsLikeDescription(data.Main.Temp, data.Main.FeelsLike);

        _sunriseUnix = data.Sys.Sunrise;
        _sunsetUnix  = data.Sys.Sunset;
        SunriseTime  = ToLocalTime(_sunriseUnix);
        SunsetTime   = ToLocalTime(_sunsetUnix);

        BuildHourlyForecast(data, forecast);
        UpdateLiveValues();
        LastUpdated = $"Updated {DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture)}";

        // Set gradient LAST — triggers animation in code-behind after all data is ready
        SetGradient(icon);

        HasData = true;
    }

    private void BuildHourlyForecast(WeatherResponse current, ForecastResponse forecast)
    {
        HourlyForecast.Clear();

        HourlyForecast.Add(new HourlyItem
        {
            Time    = "Now",
            Icon    = MapIcon(current.Weather.FirstOrDefault()?.Icon ?? "01d"),
            TempStr = Round(current.Main.Temp).ToString(CultureInfo.InvariantCulture) + "°"
        });

        var items = forecast.List ?? [];
        foreach (var item in items.Take(5))
        {
            var localTime = DateTimeOffset.FromUnixTimeSeconds(item.Dt).ToLocalTime();
            HourlyForecast.Add(new HourlyItem
            {
                Time    = localTime.Hour.ToString(CultureInfo.InvariantCulture),
                Icon    = MapIcon(item.Weather?.FirstOrDefault()?.Icon ?? "01d"),
                TempStr = Round(item.Main.Temp).ToString(CultureInfo.InvariantCulture) + "°"
            });
        }
    }

    // ── Realtime ───────────────────────────────────────────────────────

    private void StartRealtime()
    {
        _realtimeCts?.Cancel();
        _realtimeCts = new CancellationTokenSource();
        var token = _realtimeCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(30_000, token); }
                catch (OperationCanceledException) { return; }
                MainThread.BeginInvokeOnMainThread(UpdateLiveValues);
            }
        }, token);

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(300_000, token); }
                catch (OperationCanceledException) { return; }
                if (!token.IsCancellationRequested)
                    await LoadWeatherAsync(silent: true);
            }
        }, token);
    }

    public void StopRealtime() => _realtimeCts?.Cancel();

    private void UpdateLiveValues()
    {
        CurrentTimeStr = DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture);

        if (_sunriseUnix <= 0 || _sunsetUnix <= 0) return;
        var nowUnix  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var progress = (double)(nowUnix - _sunriseUnix) / (_sunsetUnix - _sunriseUnix);
        SunProgress  = Math.Clamp(progress, 0.0, 1.0);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static int    Round(double v)    => (int)Math.Round(v);
    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    private static string ToLocalTime(long unix) =>
        DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime()
            .ToString("h:mm tt", CultureInfo.InvariantCulture);

    private static string WindDegToCardinal(int deg)
    {
        string[] dirs = ["N","NNE","NE","ENE","E","ESE","SE","SSE",
                         "S","SSW","SW","WSW","W","WNW","NW","NNW"];
        return dirs[(int)Math.Round(deg / 22.5) % 16];
    }

    private static string HumidityDescription(int h) => h switch
    {
        < 30 => "Dry",
        < 60 => "Comfortable",
        < 80 => "Humid",
        _    => "Very Humid"
    };

    private static string FeelsLikeDescription(double temp, double feels) =>
        (temp - feels) switch
        {
            > 3  => "Feels cooler",
            < -3 => "Feels warmer",
            _    => "Similar to actual"
        };

    private static string MapIcon(string code) => code switch
    {
        "01d" => "☀️",
        "01n" => "🌙",
        "02d" => "⛅",
        "02n" => "🌙",
        "03d" or "03n" => "🌤️",
        "04d" or "04n" => "☁️",
        "09d" or "09n" => "🌦️",
        "10d" or "10n" => "🌧️",
        "11d" or "11n" => "⛈️",
        "13d" or "13n" => "❄️",
        "50d" or "50n" => "🌫️",
        _ => "🌡️"
    };

    // Gradients use 8-digit AARRGGBB hex to be safe with Color.FromArgb
    private void SetGradient(string iconCode)
    {
        bool isDay = iconCode.EndsWith('d');

        (string s, string m, string e) = iconCode.Length >= 2
            ? iconCode[..2] switch
            {
                "01" when isDay  => ("#FF87CEEB", "#FFADD8E6", "#FFD4EEF9"),
                "01"             => ("#FF0D1B3E", "#FF1E3A6E", "#FF2B4F91"),
                "02" when isDay  => ("#FF7FA0BE", "#FF9CB8D0", "#FFBACDE0"),
                "02"             => ("#FF1C2B3A", "#FF2C3E50", "#FF3D5068"),
                "03" when isDay  => ("#FF7FA0BE", "#FF9CB8D0", "#FFBACDE0"),
                "03"             => ("#FF1C2B3A", "#FF2C3E50", "#FF3D5068"),
                "04" when isDay  => ("#FF7A8E9E", "#FF94A6B4", "#FFAEBCC8"),
                "04"             => ("#FF2C3E50", "#FF1A1A2E", "#FF2D3561"),
                "09" when isDay  => ("#FF4F6A8A", "#FF6982A0", "#FF88A0B8"),
                "09"             => ("#FF0F1C2C", "#FF1B262C", "#FF2C3E50"),
                "10" when isDay  => ("#FF4F6A8A", "#FF6982A0", "#FF88A0B8"),
                "10"             => ("#FF0F1C2C", "#FF1B262C", "#FF2C3E50"),
                "11" when isDay  => ("#FF2E3A4F", "#FF3D4F67", "#FF4E6280"),
                "11"             => ("#FF0D0D0D", "#FF1A1A2E", "#FF302B63"),
                "13" when isDay  => ("#FFA8C8E0", "#FFBEDAEC", "#FFD4EAF6"),
                "13"             => ("#FF2D4059", "#FF4A6FA5", "#FF6989BE"),
                "50" when isDay  => ("#FF8295A2", "#FF98AABB", "#FFAEBECB"),
                "50"             => ("#FF4A4A4A", "#FF6C7A7D", "#FF8B9EA1"),
                _                => ("#FF87CEEB", "#FFADD8E6", "#FFD4EEF9"),
            }
            : ("#FF87CEEB", "#FFADD8E6", "#FFD4EEF9");

        _gradientStart = s;
        _gradientMid   = m;
        _gradientEnd   = e;
        // Notify once — code-behind reads all three together
        OnPropertyChanged(nameof(GradientStart));
    }
}
