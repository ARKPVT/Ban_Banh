using System.ComponentModel.DataAnnotations;

namespace Ban_Banh.Models
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }  // Email readonly

        [Display(Name = "Ảnh đại diện (URL)")]
        [Url(ErrorMessage = "Liên kết ảnh không hợp lệ")]
        public string? AvatarUrl { get; set; } = "/images/default-avatar.png";

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string? Address { get; set; }
    }
}
