using System.Threading.Tasks;
using api.DTOs.Auth;

namespace api.Services;

public interface IAuthService
{
    Task<JwtResponse> AdminLoginAsync(AdminLoginRequest req);
    Task<JwtResponse> PlayerRegisterAsync(PlayerRegisterRequest req);
    Task<JwtResponse> PlayerLoginAsync(PlayerLoginRequest req);
}