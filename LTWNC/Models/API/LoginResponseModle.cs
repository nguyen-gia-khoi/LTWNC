namespace LTWNC.Models.API
{
    public class LoginResponseModle
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? AccessToken { get; set; }
        public int ExpriesIn { get; set; }
    }
}
