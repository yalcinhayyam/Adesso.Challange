using System.ComponentModel.DataAnnotations;

namespace WebAPI.Contracts;
 public class DrawRequest
    {
        [Required(ErrorMessage = "Drawer name is required")]
        [MinLength(2, ErrorMessage = "Drawer name must be at least 2 characters")]
        public string DrawerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of groups is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of groups must be specified")]
        public int NumberOfGroups { get; set; }
    }