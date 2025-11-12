namespace SarfiyumAPI.Models
{
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new(); // opsiyonel, ileride roller için
    }
}
