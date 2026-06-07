using SQLite4Unity3d;

// Clase mapeada a la tabla "Usuaris" de la base de datos.
// Cada instancia de Usuari representa una fila de la tabla.
[Table("Usuaris")]
public class Usuari
{
    [PrimaryKey, AutoIncrement]
    public int UserID { get; set; }

    [Unique, NotNull]
    public string Username { get; set; }

    [NotNull]
    public string Password { get; set; }

    // Útil para mostrar el usuario en la consola de depuración de Unity.
    public override string ToString()
    {
        return string.Format("[Usuari: UserID={0}, Username={1}]", UserID, Username);
    }
}
