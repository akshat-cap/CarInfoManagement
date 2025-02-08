using System.ComponentModel.DataAnnotations;

namespace CarInfoManagementSystem.Models
{
    public class Manufacturer
    {
        public Manufacturer()
        {
            Cars = new HashSet<Car>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Manufacturer Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Registered Office")]
        public string RegisteredOffice { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Car> Cars { get; set; }
    }
}