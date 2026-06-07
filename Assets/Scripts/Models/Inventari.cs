using SQLite4Unity3d;

// Clase mapeada a la tabla "Inventario": cantidad de cada Item que posee un Usuari.
// El índice único compuesto (userId, itemId) evita filas duplicadas para el mismo
// usuario e ítem, igual que el UNIQUE(userId, itemId) del esquema SQL original.
[Table("Inventario")]
public class Inventari
{
    [PrimaryKey, AutoIncrement]
    public int InventarioID { get; set; }

    [Indexed(Name = "UX_user_item", Order = 1, Unique = true)]
    public int userId { get; set; }

    [Indexed(Name = "UX_user_item", Order = 2, Unique = true)]
    public int itemId { get; set; }

    [NotNull]
    public int Cantidad { get; set; }

    public override string ToString()
    {
        return string.Format("[Inventari: userId={0}, itemId={1}, Cantidad={2}]", userId, itemId, Cantidad);
    }
}
