using System.ComponentModel.DataAnnotations;

namespace CarInfoManagementSystem.Models
{
    public class CarTransmissionType
    {
        public CarTransmissionType()
        {
            Cars = new HashSet<Car>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Transmission Type")]
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Car> Cars { get; set; }
    }
}