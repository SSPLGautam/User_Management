using API.Dtos;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
    {
        List<RoleResponseDto> rolesWithUserCount = [];
        var roles = await roleManager.Roles.ToListAsync();

        foreach (var role in roles)
        {
            var totalUsers = await userManager.GetUsersInRoleAsync(role.Name!);
            rolesWithUserCount.Add(new RoleResponseDto()
            {
                Id = role.Id,
                Name = role.Name,
                TotalUsers = totalUsers.Count
            });
        }
        return Ok(rolesWithUserCount);
    }

    [HttpPost]
    public async Task<ActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        if (string.IsNullOrWhiteSpace(createRoleDto.RoleName))
            return BadRequest("Role name is required");

        if (await roleManager.RoleExistsAsync(createRoleDto.RoleName))
            return BadRequest("Role already exists");

        var role = new IdentityRole(createRoleDto.RoleName);
        var result = await roleManager.CreateAsync(role);
        if (result.Succeeded)
            return Ok(new {
                message= "Role created successfully"
            });

        return BadRequest(result.Errors);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await roleManager.FindByIdAsync(id);

        if (role is null)
            return NotFound("Role not found");

        var result = await roleManager.DeleteAsync(role);

        if (result.Succeeded)
            return Ok(new { Message = "Role Deleted successfully." });

        return BadRequest(result.Errors);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignDto assignDto)
    {
        var user = await userManager.FindByIdAsync(assignDto.UserId);
        if (user is null)
            return NotFound("User not found");

        var role = await roleManager.FindByIdAsync(assignDto.RoleId);
        if (role is null)
            return NotFound("Role not found");

        var result = await userManager.AddToRoleAsync(user, role.Name!);

        if (result.Succeeded)
            return Ok(new { Message = "Role assigned successfully" });

        return BadRequest(result.Errors);
    }

}
