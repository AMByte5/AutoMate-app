
namespace AutoMate_app.Services.Interfaces
{
    public interface ILocationService
    {
        //Best practice is to return raw values than the reqDTO for :less coupling, Testing, Future Implementations.
        Task<string?> ReverseGeocodeAsync(double lat, double lng);
    }
}
