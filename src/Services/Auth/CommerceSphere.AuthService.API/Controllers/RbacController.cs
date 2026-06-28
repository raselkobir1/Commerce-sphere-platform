using System.Security.Claims;
using CommerceSphere.AuthService.Application.DTOs.Requests;
using CommerceSphere.AuthService.Application.Interfaces;
using CommerceSphere.Shared.Common.Authorization;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class RbacController(IRbacManager rbac) : ControllerBase
{
    private string Cid => HttpContext.GetCorrelationId();
    private string Trace => HttpContext.TraceIdentifier;

    // ── The signed-in user's own menus (drives the dynamic sidebar) ──
    [HttpGet("me/permissions")]
    [Authorize]
    public async Task<IActionResult> MyPermissions(CancellationToken ct)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        var menus = await rbac.GetMenusForRoleAsync(role, ct);
        return Ok(ApiResponse<object>.Ok(menus, "Menus retrieved", Trace, Cid));
    }

    // ── Roles ──
    [HttpGet("roles")]
    [HasPermission("roles:view")]
    public async Task<IActionResult> GetRoles(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.GetRolesAsync(ct), "Roles", Trace, Cid));

    [HttpPost("roles")]
    [HasPermission("roles:create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.CreateRoleAsync(request, ct), "Role created", Trace, Cid));

    [HttpPut("roles/{id:guid}")]
    [HasPermission("roles:edit")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.UpdateRoleAsync(id, request, ct), "Role updated", Trace, Cid));

    [HttpDelete("roles/{id:guid}")]
    [HasPermission("roles:delete")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken ct)
    {
        await rbac.DeleteRoleAsync(id, ct);
        return Ok(ApiResponse.Ok("Role deleted", Trace, Cid));
    }

    // ── Menus ──
    [HttpGet("menus")]
    [HasPermission("menus:view")]
    public async Task<IActionResult> GetMenus(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.GetMenusAsync(ct), "Menus", Trace, Cid));

    [HttpPost("menus")]
    [HasPermission("menus:create")]
    public async Task<IActionResult> CreateMenu([FromBody] CreateMenuRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.CreateMenuAsync(request, ct), "Menu created", Trace, Cid));

    [HttpPut("menus/{id:guid}")]
    [HasPermission("menus:edit")]
    public async Task<IActionResult> UpdateMenu(Guid id, [FromBody] UpdateMenuRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.UpdateMenuAsync(id, request, ct), "Menu updated", Trace, Cid));

    [HttpDelete("menus/{id:guid}")]
    [HasPermission("menus:delete")]
    public async Task<IActionResult> DeleteMenu(Guid id, CancellationToken ct)
    {
        await rbac.DeleteMenuAsync(id, ct);
        return Ok(ApiResponse.Ok("Menu deleted", Trace, Cid));
    }

    // ── Role ↔ menu permissions ──
    [HttpGet("roles/{roleId:guid}/permissions")]
    [HasPermission("permissions:view")]
    public async Task<IActionResult> GetRolePermissions(Guid roleId, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await rbac.GetRolePermissionsAsync(roleId, ct), "Permissions", Trace, Cid));

    [HttpPut("roles/{roleId:guid}/permissions")]
    [HasPermission("permissions:edit")]
    public async Task<IActionResult> SetRolePermissions(Guid roleId, [FromBody] SetPermissionsRequest request, CancellationToken ct)
    {
        await rbac.SetRolePermissionsAsync(roleId, request, ct);
        return Ok(ApiResponse.Ok("Permissions updated", Trace, Cid));
    }
}
