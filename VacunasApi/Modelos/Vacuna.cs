namespace VacunasApi.Modelos;

// Vacuna del catálogo de la clínica
public class Vacuna
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    // Perro o Gato
    public string Especie { get; set; } = string.Empty;

    // Enfermedades que previene
    public string Previene { get; set; } = string.Empty;

    // Edad mínima para la primera dosis
    public string EdadAplicacion { get; set; } = string.Empty;

    // Cada cuánto se repite la dosis
    public string Refuerzo { get; set; } = string.Empty;

    // Precio en soles
    public decimal Precio { get; set; }
}
