using CommerceSphere.InventoryService.Application.DTOs.Requests;
using CommerceSphere.InventoryService.Application.DTOs.Responses;
using CommerceSphere.Shared.Common.Models;

namespace CommerceSphere.InventoryService.Application.Interfaces;

public interface IInventoryManager
{
    Task<ReservationResponse> ReserveAsync(
        ReserveInventoryRequest request, string correlationId, CancellationToken ct = default);

    Task<ReservationResponse> ReleaseAsync(
        ReleaseReservationRequest request, string correlationId, CancellationToken ct = default);

    Task<InventoryItemResponse> GetByProductIdAsync(Guid productId, CancellationToken ct = default);

    Task<PagedResult<InventoryItemResponse>> GetInventoryPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default);

    Task<InventoryItemResponse> AdjustStockAsync(
        AdjustStockRequest request, string correlationId, CancellationToken ct = default);

    Task<InventoryItemResponse> ReceiveStockAsync(
        ReceiveStockRequest request, string correlationId, CancellationToken ct = default);
}
