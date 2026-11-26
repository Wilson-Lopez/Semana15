using VacunasApi.Modelos;

namespace VacunasApi.Datos;

// Datos en memoria: este microservicio no usa base de datos ni comparte la de Veterinaria.Api
public static class VacunasEnMemoria
{
    public static readonly IReadOnlyList<Vacuna> Lista =
    [
        new() { Id = 1, Nombre = "Puppy DP", Especie = "Perro", Previene = "Parvovirus y moquillo", EdadAplicacion = "6 semanas", Refuerzo = "Única (inicio del plan)", Precio = 45.00m },
        new() { Id = 2, Nombre = "Séxtuple canina", Especie = "Perro", Previene = "Moquillo, hepatitis, parvovirus, parainfluenza y leptospirosis", EdadAplicacion = "8 semanas", Refuerzo = "Anual", Precio = 60.00m },
        new() { Id = 3, Nombre = "Antirrábica canina", Especie = "Perro", Previene = "Rabia", EdadAplicacion = "3 meses", Refuerzo = "Anual", Precio = 30.00m },
        new() { Id = 4, Nombre = "Tos de las perreras", Especie = "Perro", Previene = "Bordetella bronchiseptica", EdadAplicacion = "8 semanas", Refuerzo = "Anual", Precio = 55.00m },
        new() { Id = 5, Nombre = "Triple felina", Especie = "Gato", Previene = "Rinotraqueítis, calicivirus y panleucopenia", EdadAplicacion = "8 semanas", Refuerzo = "Anual", Precio = 65.00m },
        new() { Id = 6, Nombre = "Leucemia felina", Especie = "Gato", Previene = "Leucemia felina (FeLV)", EdadAplicacion = "9 semanas", Refuerzo = "Anual", Precio = 70.00m },
        new() { Id = 7, Nombre = "Antirrábica felina", Especie = "Gato", Previene = "Rabia", EdadAplicacion = "3 meses", Refuerzo = "Anual", Precio = 30.00m }
    ];
}
