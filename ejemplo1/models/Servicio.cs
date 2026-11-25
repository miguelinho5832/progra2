using System.ComponentModel.DataAnnotations;

namespace ejemplo1.models
{
    public class Servicio : EntidadBase
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120,
            ErrorMessage = "El nombre no puede superar 120 caracteres")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El tipo de servicio es obligatorio")]
        [StringLength(50,
            ErrorMessage = "El tipo no puede superar 50 caracteres")]
        public string Tipo { get; set; } = "";

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(255,
            ErrorMessage = "La descripción no puede superar 255 caracteres")]
        public string Descripcion { get; set; } = "";

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0, 99999999.99,
            ErrorMessage = "El precio debe ser mayor o igual a 0")]
        public decimal? Precio { get; set; }

        [Required(ErrorMessage = "La duración estimada es obligatoria")]
        [Range(1, int.MaxValue,
            ErrorMessage = "La duración estimada debe ser mayor a 0")]
        public int? DuracionEstimada { get; set; }

        [Required(ErrorMessage = "La unidad de duración es obligatoria")]
        [StringLength(30,
            ErrorMessage = "La unidad de duración no puede superar 30 caracteres")]
        public string UnidadDuracion { get; set; } = "";
    }
}