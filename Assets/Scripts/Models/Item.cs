using SQLite4Unity3d;

// Clase mapeada a la tabla "Item" (catálogo global de objetos).
[Table("Item")]
public class Item
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; set; }

    [Unique, NotNull]
    public string Nombre { get; set; }

    public string Descripcion { get; set; }

    [NotNull]
    public int MaxStack { get; set; }

    public override string ToString()
    {
        return string.Format("[Item: ID={0}, Nombre={1}, MaxStack={2}]", ID, Nombre, MaxStack);
    }
}
