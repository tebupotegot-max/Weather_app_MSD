namespace WeatherApp.Services;

public class LocationService
{
    public async Task<(double Lat, double Lon)> GetLocationAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            throw new PermissionException("Location permission was denied.");

        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
        var location = await Geolocation.Default.GetLocationAsync(request);

        if (location is null)
            throw new InvalidOperationException("Unable to determine your location.");

        return (location.Latitude, location.Longitude);
    }
}
