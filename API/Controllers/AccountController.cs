using API.Dtos;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountController(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration) : ControllerBase
{
    #region Public Methods

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<string>> Register(RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var user = new AppUser()
        {
            Email = registerDto.Email,
            UserName = registerDto.Email,
            FullName = registerDto.FullName
        };
        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        if (registerDto.Roles is null)
            await userManager.AddToRoleAsync(user, "User");
        else
        {
            foreach (var role in registerDto.Roles)
            {
                if (await roleManager.RoleExistsAsync(role))
                    await userManager.AddToRoleAsync(user, role);
            }
        }

        return Ok(new AuthResponseDto()
        {
            IsSuccess = true,
            Message = "User created successfully"
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(loginDto.Email);

        if (user is null)
            return Unauthorized(new AuthResponseDto()
            {
                IsSuccess = false,
                Message = $@"User with {loginDto.Email} not found."
            });

        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);

        if (!result)
            return Unauthorized(new AuthResponseDto()
            {
                IsSuccess = false,
                Message = "Invalid password."
            });

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        _ = int.TryParse(configuration.GetSection("JWTSetting:RefreshTokenExpiryTime").Value, out int refreshTokenExpiryTime);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenExpiryTime);
        await userManager.UpdateAsync(user);
        return Ok(new AuthResponseDto()
        {
            IsSuccess = true,
            Token = token,
            Message = "Login successful",
            RefreshToken = refreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(TokenDto tokenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var principal = GetPrincipalFromExpiredToken(tokenDto.Token);
        var user = await userManager.FindByEmailAsync(tokenDto.Email);

        if (principal is null || user is null || user.RefreshToken != tokenDto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return BadRequest(new AuthResponseDto()
            {
                IsSuccess = false,
                Message = "Invalid clinet request"
            });

        var newJwtToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();
        _ = int.TryParse(configuration.GetSection("JWTSetting:RefreshTokenExpiryTime").Value, out int refreshTokenExpiryTime);
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenExpiryTime);
        await userManager.UpdateAsync(user);

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Token = newJwtToken,
            RefreshToken = newRefreshToken,
            Message = "Token refreshed successfully"
        });
    }


    [HttpGet("detail")]
    public async Task<ActionResult<UserDetailDto>> GetUserDetail()
    {
        var currentUser = await userManager.GetUserAsync(User);

        if (currentUser is null)
            return NotFound(new AuthResponseDto()
            {
                IsSuccess = false,
                Message = "User not found"
            });

        return Ok(new UserDetailDto()
        {
            Id = currentUser.Id,
            Email = currentUser.Email,
            FullName = currentUser.FullName,
            Roles = (await userManager.GetRolesAsync(currentUser)).ToArray(),
            PhoneNumber = currentUser.PhoneNumber,
            PhoneNumberConfirmed = currentUser.PhoneNumberConfirmed,
            AccessFailedCount = currentUser.AccessFailedCount,
            TwoFactorEnabled = currentUser.TwoFactorEnabled
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDetailDto>>> GetUsers()
    {
        List<UserDetailDto> usersWithRoles = [];
        var users = await userManager.Users.ToListAsync();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            usersWithRoles.Add(new UserDetailDto()
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles.ToArray(),
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                AccessFailedCount = user.AccessFailedCount,
                TwoFactorEnabled = user.TwoFactorEnabled
            });
        }

        return Ok(usersWithRoles);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
    {
        var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);
        if (user is null)
            return Ok(new AuthResponseDto
            {
                IsSuccess = false,
                Message = $"User doen't exists with {forgotPasswordDto.Email} email"
            });
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"http:localhost:4200/reset-password?email={user.Email}&token={WebUtility.UrlEncode(token)}";


        var client = new RestClient("https://send.api.mailtrap.io/api/send");
        var request = new RestRequest
        {
            Method = Method.Post,
            RequestFormat = DataFormat.Json,
        };
        request.AddHeader("Authorization", "Bearer 621b03f00fb3a4a6f93bfead12c4a839");
        request.AddJsonBody(new
        {
            from = new { email = "hello@demomailtrap.co" },
            to = new[] { new { email = user.Email } },
            template_uuid = "6009f8a0-02fd-4cb5-aee6-06f54cedb673",
            template_variables = new { user_email = user.Email, pass_reset_link = resetLink }
        });
        var response = client.Execute(request);
        if (response.IsSuccessful)
        {
            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Email sent with password reset link. Please check your mail."
            });
        }
        else
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = response.Content!.ToString()
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);
        if (user is null)
        {
            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = $"User doen't exists with {resetPasswordDto.Email} email"
            });
        }

        var result = await userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
        if (result.Succeeded)
        {
            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Password reset successfully"
            });
        }

        return BadRequest(new AuthResponseDto
        {
            IsSuccess = false,
            Message = result.Errors.FirstOrDefault()?.Description
        });

    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
    {
        var user = await userManager.FindByEmailAsync(changePasswordDto.Email);
        if (user is null)
            return NotFound(new AuthResponseDto
            {
                IsSuccess = false,
                Message = "User not found"
            });

        var result = await userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (result.Succeeded)
            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Password changed successfully"
            });

        return BadRequest(new AuthResponseDto
        {
            IsSuccess = false,
            Message = result.Errors.FirstOrDefault()?.Description
        });
    }

    #endregion

    #region Private Methods
    private string GenerateJwtToken(AppUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["JWTSetting:SecurityKey"]!);

        var roles = userManager.GetRolesAsync(user).Result;
        List<Claim> claims = new()
            {
                    new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                    new(JwtRegisteredClaimNames.Name, user.FullName?? ""),
                    new(JwtRegisteredClaimNames.NameId, user.Id ?? ""),
                    new(JwtRegisteredClaimNames.Aud, configuration["JWTSetting:ValidAudience"]!),
                    new(JwtRegisteredClaimNames.Iss, configuration["JWTSetting:ValidIssuer"]!)
            };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["JWTSetting:SecurityKey"]!)),
            ValidateLifetime = false,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
    #endregion
}
