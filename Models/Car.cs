using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarInfoManagementSystem.Models
{
    public class Car
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Manufacturer")]
        public int ManufacturerId { get; set; }

        [Required]
        [Display(Name = "Type")]
        public int TypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Engine { get; set; } = string.Empty;

        [Required]
        public int BHP { get; set; }

        [Required]
        [Display(Name = "Transmission")]
        public int TransmissionId { get; set; }

        [Required]
        public int Mileage { get; set; }

        [Required]
        [Display(Name = "Seats")]
        public int Seat { get; set; }

        [Required]
        [Display(Name = "Air Bag Details")]
        public string AirBagDetails { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Boot Space")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal BootSpace { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public string Features { get; set; } = string.Empty;

        [Display(Name = "Car Photo")]
        public string? PhotoUrl { get; set; }

        [NotMapped]
        [Display(Name = "Upload Photo")]
        public IFormFile? PhotoFile { get; set; }

        // Navigation properties
        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer? Manufacturer { get; set; }

        [ForeignKey("TypeId")]
        public virtual CarType? CarType { get; set; }

        [ForeignKey("TransmissionId")]
        public virtual CarTransmissionType? TransmissionType { get; set; }
    }
}