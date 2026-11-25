using System.ComponentModel.DataAnnotations;

namespace ejemplo1.models
{
    public class Vehiculo : EntidadBase
    {
        [Required(ErrorMessage = "La placa es obligatoria")]
        [StringLength(15, MinimumLength = 5,
            ErrorMessage = "La placa debe tener entre 5 y 15 caracteres")]
        [RegularExpression(
            @"^[0-9]{3,4}[A-Za-z]{3}$",
            ErrorMessage = "La placa debe tener 3 o 4 números seguidos de 3 letras. Ejemplo: 5214BEX")]
        public string Placa { get; set; } = "";

        [Required(ErrorMessage = "La marca es obligatoria")]
        [StringLength(60,
            ErrorMessage = "La marca no puede superar 60 caracteres")]
        public string Marca { get; set; } = "";

        [Required(ErrorMessage = "El modelo es obligatorio")]
        [StringLength(60,
            ErrorMessage = "El modelo no puede superar 60 caracteres")]
        public string Modelo { get; set; } = "";

        [Required(ErrorMessage = "El año es obligatorio")]
        [Range(1901, 2026,
            ErrorMessage = "Ingrese un año válido")]
        public int? Anio { get; set; }

        [Required(ErrorMessage = "El color es obligatorio")]
        [StringLength(40,
            ErrorMessage = "El color no puede superar 40 caracteres")]
        public string Color { get; set; } = "";

        [Required(ErrorMessage = "El tipo de vehículo es obligatorio")]
        [StringLength(40,
            ErrorMessage = "El tipo no puede superar 40 caracteres")]
        public string Tipo { get; set; } = "";

        [Required(ErrorMessage = "El kilometraje es obligatorio")]
        [Range(0, 2000000,
            ErrorMessage = "El kilometraje debe estar entre 0 y 2.000.000")]
        public int? Kilometraje { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un cliente")]
        public int ClienteId { get; set; }

        public string ClienteNombre { get; set; } = "";

        public DateTime CreadoEn { get; set; }
    }
}