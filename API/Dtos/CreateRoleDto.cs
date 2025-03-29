using System.ComponentModel.DataAnnotations;

namespace API.Dtos;

public class CreateRoleDto
{
    [Required(ErrorMessage = @$"{nameof(RoleName)} is required.")]
    public string RoleName { get; set; }
}
