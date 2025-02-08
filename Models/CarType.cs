using System.ComponentModel.DataAnnotations;

namespace CarInfoManagementSystem.Models
{
    public class CarType
    {
        public CarType()
        {
            Cars = new HashSet<Car>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Car Type")]
        public string Type { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Car> Cars { get; set; }
    }
}