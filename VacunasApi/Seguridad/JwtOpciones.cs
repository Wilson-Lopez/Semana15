namespace VacunasApi.Seguridad;

// Sección "Jwt" de la configuración. Debe tener la misma clave, emisor y audiencia que Veterinaria.Api,
// porque este servicio no emite tokens: solo valida los que emite Veterinaria.Api.
// Es una clase propia (no se comparte con Veterinaria.Api). La Clave no va en el repositorio.
public class JwtOpciones
{
    public string Clave { get; set; } = string.Empty;

    public string Emisor { get; set; } = string.Empty;

    public string Audiencia { get; set; } = string.Empty;
}
