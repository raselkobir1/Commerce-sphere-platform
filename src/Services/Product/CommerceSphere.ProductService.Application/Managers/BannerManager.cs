using CommerceSphere.ProductService.Application.DTOs.Requests;
using CommerceSphere.ProductService.Application.DTOs.Responses;
using CommerceSphere.ProductService.Application.Interfaces;
using CommerceSphere.ProductService.Domain.Entities;
using CommerceSphere.ProductService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ProductService.Application.Managers;

public class BannerManager(IUnitOfWork uow, ILogger<BannerManager> logger) : IBannerManager
{
    public async Task<IReadOnlyList<BannerResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var banners = await uow.Banners.GetAllAsync(ct);
        return banners.Select(MapToResponse).ToList();
    }

    public async Task<BannerResponse> CreateAsync(CreateBannerRequest request, CancellationToken ct = default)
    {
        var banner = Banner.Create(request.Title, request.Subtitle, request.ImageUrl, request.LinkUrl, request.SortOrder);
        await uow.Banners.AddAsync(banner, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Banner created. BannerId: {BannerId}, Title: {Title}", banner.Id, banner.Title);
        return MapToResponse(banner);
    }

    public async Task<BannerResponse> UpdateAsync(Guid id, UpdateBannerRequest request, CancellationToken ct = default)
    {
        var banner = await uow.Banners.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Banner), id);

        banner.Update(request.Title, request.Subtitle, request.ImageUrl, request.LinkUrl, request.IsActive, request.SortOrder);
        uow.Banners.Update(banner);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Banner updated. BannerId: {BannerId}", banner.Id);
        return MapToResponse(banner);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var banner = await uow.Banners.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Banner), id);

        uow.Banners.Remove(banner);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Banner deleted. BannerId: {BannerId}", id);
    }

    private static BannerResponse MapToResponse(Banner b) =>
        new(b.Id, b.Title, b.Subtitle, b.ImageUrl, b.LinkUrl, b.IsActive, b.SortOrder, b.CreatedAt, b.UpdatedAt);
}
