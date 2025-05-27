using LTWNC.API;
using LTWNC.Data;
using LTWNC.Entities;
using LTWNC.Middleware;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LTWNC.Services
{
    public class JwtServices
    {
        private readonly IConfiguration _configuration;
        private readonly MongoDbService _mongoService;

        public JwtServices(IConfiguration configuration, MongoDbService mongoService)
        {
            _configuration = configuration;
            _mongoService = mongoService;
        }

        public async Task<LoginResponseModle?> Authenticate(LoginRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return null;


            var usersCollection = _mongoService.Database.GetCollection<Users>("users");

            var userAccount = await usersCollection.Find(x => x.Email == request.Email).FirstOrDefaultAsync();

            if (userAccount == null || !HashPassword.VerifyHashedPassword(request.Password, userAccount.Password))
            {
                Console.WriteLine($"[DEBUG] No user found with email: {request.Email}");
                return null;
            }
                

            Console.WriteLine($"[DEBUG] Found user: {userAccount.Email}, hashed password in DB: {userAccount.Password}");
            var issuer = _configuration["JwtConfig:Issuer"];
            var audience = _configuration["JwtConfig:Audience"];
            var key = _configuration["JwtConfig:Key"];
            var tokenValidityMins = _configuration.GetValue<int>("JwtConfig:TokenValidityMins");
            var tokenExpiryTimeStamp = DateTime.UtcNow.AddMinutes(tokenValidityMins);
           
            var tokenDiscriptor = new SecurityTokenDescriptor
            {
               Subject = new ClaimsIdentity(new[]
               {
                   new Claim(JwtRegisteredClaimNames.Name, request.Email),
                   new Claim(ClaimTypes.Role, userAccount.Role.ToString())

               }),
                Expires = tokenExpiryTimeStamp,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDiscriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);

            return new LoginResponseModle
            {
                Email = request.Email,
                AccessToken = accessToken,
                ExpriesIn = (int)(tokenExpiryTimeStamp - DateTime.UtcNow).TotalSeconds

            };
        }
    }
}
