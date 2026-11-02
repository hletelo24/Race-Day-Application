namespace race_day_backend_api.Models
{
    public class ImageUploadRequest
    {
        public IFormFile Image { get; set; } = null!;
    }
}
