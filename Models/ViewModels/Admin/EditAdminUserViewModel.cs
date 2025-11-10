using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models.ViewModels.Admin
{
    public class EditAdminUserViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password (leave blank to keep current)")]
        public string? NewPassword { get; set; }
    }
}
