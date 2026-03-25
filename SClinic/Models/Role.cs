using System.ComponentModel.DataAnnotations;

namespace SClinic.Models;

public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    // Navigation
    public ICollection<Account> Accounts { get; set; } = [];
}
