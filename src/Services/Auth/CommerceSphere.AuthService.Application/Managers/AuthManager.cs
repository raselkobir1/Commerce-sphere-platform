using BC = BCrypt.Net.BCrypt;
using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.DTOs.Responses;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.AuthService.Domain.Entities;
using CommerceSphere.AuthService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using CommerceSphere.Shared.Common.Models;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.AuthService.Application.Managers;

public class AuthManager(
    IUnitOfWork uow,
    IJwtService jwtService,
    ILogger<AuthManager> logger) : IAuthManager
{
    public async Task<AuthTokenResponse> RegisterAsync(
        RegisterRequest request, string ipAddress, string correlationId, CancellationToken ct = default)
    {
        if (await uow.Users.ExistsByEmailAsync(request.Email, ct))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var passwordHash = BC.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName, request.Role);

        await uow.Users.AddAsync(user, ct);

        var refreshToken = RefreshToken.Create(user.Id, ipAddress);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);

        await uow.SaveChangesAsync(ct);

        logger.LogInformation("User registered. UserId: {UserId}, CorrelationId: {CorrelationId}", user.Id, correlationId);

        return BuildTokenResponse(user, refreshToken);
    }

    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request, string ipAddress, string correlationId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!BC.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        user.RecordLogin();
        uow.Users.Update(user);

        var refreshToken = RefreshToken.Create(user.Id, ipAddress);
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("User logged in. UserId: {UserId}, CorrelationId: {CorrelationId}", user.Id, correlationId);

        return BuildTokenResponse(user, refreshToken);
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(
        RefreshTokenRequest request, string ipAddress, CancellationToken ct = default)
    {
        var existingToken = await uow.RefreshTokens.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!existingToken.IsActive)
            throw new UnauthorizedException("Refresh token is expired or revoked.");

        var user = await uow.Users.GetByIdAsync(existingToken.UserId, ct)
            ?? throw new NotFoundException(nameof(User), existingToken.UserId);

        var newRefreshToken = RefreshToken.Create(user.Id, ipAddress);
        existingToken.Revoke(newRefreshToken.Token);

        uow.RefreshTokens.Update(existingToken);
        await uow.RefreshTokens.AddAsync(newRefreshToken, ct);
        await uow.SaveChangesAsync(ct);

        return BuildTokenResponse(user, newRefreshToken);
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken ct = default)
    {
        var token = await uow.RefreshTokens.GetByTokenAsync(request.RefreshToken, ct)
            ?? throw new NotFoundException(nameof(RefreshToken), request.RefreshToken);

        if (!token.IsActive)
            throw new BusinessException("Token is already revoked or expired.");

        token.Revoke();
        uow.RefreshTokens.Update(token);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(PagedRequest paged, CancellationToken ct = default)
    {
        var (users, total) = await uow.Users.GetPagedAsync(paged.PageNumber, paged.PageSize, ct);
        return PagedResult<UserResponse>.Create(users.Select(MapToResponse), total, paged.PageNumber, paged.PageSize);
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return MapToResponse(user);
    }

    private AuthTokenResponse BuildTokenResponse(User user, RefreshToken refreshToken) =>
        new(
            AccessToken: jwtService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token,
            ExpiresAt: jwtService.GetAccessTokenExpiry(),
            User: MapToResponse(user)
        );

    private static UserResponse MapToResponse(User u) =>
        new(u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt);
}
