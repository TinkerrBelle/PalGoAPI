// Services/ITokenService.cs
using PalGoAPI.Models;

namespace PalGoAPI.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user);
        Task<string> GenerateRefreshToken(string userId, string deviceInfo, string ipAddress);
        Task<RefreshToken> ValidateRefreshToken(string token);
        Task RevokeRefreshToken(string token);
        Task RevokeAllUserRefreshTokens(string userId);
        Task CleanupExpiredTokens();
    }
}