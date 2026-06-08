using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SQLite4Unity3d;

// Versión del gestor de base de datos que usa la herramienta ORM SQLite4Unity3d.
// En lugar de escribir SQL a mano, trabajamos con objetos (Usuari, Item, Inventari)
// y el ORM se encarga de traducir las operaciones a SQL sobre la base de datos SQLite.
public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection _db;

    private void Awake()
    {
        ConnectToDatabase();
    }

    // Establecimiento de la conexión + creación de las tablas si no existen.
    private void ConnectToDatabase()
    {
        try
        {
            string dbPath = Application.persistentDataPath + "/usuaris.db";
            _db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

            // El ORM crea cada tabla a partir de la definición de la clase (atributos).
            _db.CreateTable<Usuari>();
            _db.CreateTable<Item>();
            _db.CreateTable<Inventari>();

            // Migraciones: añade columnas nuevas si la BD ya existía sin ellas.
            MigrateSchema();

            SeedItems();

            Debug.Log("Base de datos (ORM) inicializada en: " + dbPath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error inicializando base de datos: " + e.Message);
        }
    }

    // Cierre de la conexión cuando se destruye el objeto.
    private void OnDestroy()
    {
        if (_db != null)
            _db.Close();
    }

    // Añade columnas nuevas a tablas existentes y rellena filas que quedaron a 0.
    private void MigrateSchema()
    {
        if (!ColumnaExiste("Usuaris", "Diners"))
        {
            _db.Execute("ALTER TABLE Usuaris ADD COLUMN Diners REAL DEFAULT 0");
            Debug.Log("Migración: columna Diners añadida.");
        }

        if (!ColumnaExiste("Item", "Preu"))
        {
            _db.Execute("ALTER TABLE Item ADD COLUMN Preu REAL DEFAULT 0");
            Debug.Log("Migración: columna Preu añadida.");
        }

        // Usuarios existentes sin saldo → darles 100.
        _db.Execute("UPDATE Usuaris SET Diners = 100 WHERE Diners = 0");

        // Ítems existentes sin precio → asignar precios correctos.
        _db.Execute("UPDATE Item SET Preu = 50 WHERE Nombre = 'Espada'     AND Preu = 0");
        _db.Execute("UPDATE Item SET Preu = 5  WHERE Nombre = 'Comida'     AND Preu = 0");
        _db.Execute("UPDATE Item SET Preu = 10 WHERE Nombre = 'Lingote'    AND Preu = 0");
        _db.Execute("UPDATE Item SET Preu = 25 WHERE Nombre = 'EnderPearl' AND Preu = 0");
    }

    // Comprueba si una columna existe en una tabla usando PRAGMA.
    private bool ColumnaExiste(string tabla, string columna)
    {
        var cols = _db.Query<PragmaColumn>($"PRAGMA table_info({tabla})");
        foreach (var col in cols)
            if (col.name == columna) return true;
        return false;
    }

    // Clase auxiliar para leer el resultado de PRAGMA table_info.
    private class PragmaColumn
    {
        public string name { get; set; }
    }

    // Inserta los ítems del catálogo solo si la tabla está vacía.
    // El orden de inserción fija los IDs 1=Espada, 2=Comida, 3=Lingote, 4=EnderPearl.
    private void SeedItems()
    {
        if (_db.Table<Item>().Count() > 0)
            return;

        _db.InsertAll(new[]
        {
            new Item { Nombre = "Espada",     Descripcion = "Arma de combate cuerpo a cuerpo", MaxStack = 1,  Preu = 50f },
            new Item { Nombre = "Comida",     Descripcion = "Restaura puntos de vida",         MaxStack = 15, Preu = 5f  },
            new Item { Nombre = "Lingote",    Descripcion = "Material de crafteo básico",      MaxStack = 20, Preu = 10f },
            new Item { Nombre = "EnderPearl", Descripcion = "Permite teletransportarse",       MaxStack = 6,  Preu = 25f },
        });
    }

    // ---------------------------------------------------------------------
    // USUARIOS
    // ---------------------------------------------------------------------

    public string RegisterUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "El nombre de usuario no puede estar vacio";

        if (password.Length < 8)
            return "La contraseña debe tener minimo 8 caracteres";

        try
        {
            if (_db.Find<Usuari>(u => u.Username == username) != null)
                return "Este usuario ya existe";

            _db.Insert(new Usuari { Username = username, Password = password, Diners = 100f });
            return "OK";
        }
        catch (SQLiteException ex)
        {
            if (ex.Message.Contains("UNIQUE"))
                return "Este usuario ya existe";
            return "Error de base de dades: " + ex.Message;
        }
        catch (Exception ex)
        {
            return "Error inesperat: " + ex.Message;
        }
    }

    public (bool success, string message, int userId) LoginUser(string username, string password)
    {
        try
        {
            var usuari = _db.Find<Usuari>(u => u.Username == username && u.Password == password);

            if (usuari != null)
                return (true, "Login correcto", usuari.UserID);

            return (false, "Usuario o contraseña incorrectos", -1);
        }
        catch (Exception ex)
        {
            return (false, "Error de connexión: " + ex.Message, -1);
        }
    }

    // Devuelve todos los usuarios registrados (para el listado de la actividad).
    public List<Usuari> GetAllUsers()
    {
        try
        {
            return _db.Table<Usuari>().OrderBy(u => u.UserID).ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError("GetAllUsers error: " + ex.Message);
            return new List<Usuari>();
        }
    }

    // Borra un usuario concreto y su inventario asociado. Devuelve true si se borró.
    public bool DeleteUser(Usuari usuari)
    {
        if (usuari == null) return false;
        return DeleteUserById(usuari.UserID);
    }

    public bool DeleteUserById(int userId)
    {
        try
        {
            // Borramos primero su inventario (el ORM no hace borrado en cascada).
            _db.Execute("DELETE FROM Inventario WHERE userId = ?", userId);
            int filas = _db.Delete<Usuari>(userId);
            return filas > 0;
        }
        catch (Exception ex)
        {
            Debug.LogError("DeleteUserById error: " + ex.Message);
            return false;
        }
    }

    // Borra todos los usuarios y sus inventarios. Devuelve cuántos usuarios se borraron.
    public int DeleteAllUsers()
    {
        try
        {
            _db.DeleteAll<Inventari>();
            return _db.DeleteAll<Usuari>();
        }
        catch (Exception ex)
        {
            Debug.LogError("DeleteAllUsers error: " + ex.Message);
            return 0;
        }
    }

    // Suma todas las cantidades de ítems del usuario (para mostrar en el panel).
    public int GetTotalItems(int userId)
    {
        try
        {
            var rows = _db.Table<Inventari>().Where(i => i.userId == userId).ToList();
            int total = 0;
            foreach (var r in rows) total += r.Cantidad;
            return total;
        }
        catch (Exception ex)
        {
            Debug.LogError("GetTotalItems error: " + ex.Message);
            return 0;
        }
    }

    // ---------------------------------------------------------------------
    // INVENTARIO
    // ---------------------------------------------------------------------

    // Devuelve la cantidad actual del ítem en el inventario del usuario (0 si no existe).
    public int GetCantidad(int userId, int itemId)
    {
        try
        {
            var inv = _db.Find<Inventari>(i => i.userId == userId && i.itemId == itemId);
            return inv != null ? inv.Cantidad : 0;
        }
        catch (Exception ex)
        {
            Debug.LogError("GetCantidad error: " + ex.Message);
            return 0;
        }
    }

    // Añade 1 unidad del ítem respetando MaxStack. Devuelve false si ya está al máximo.
    public bool AddItem(int userId, int itemId)
    {
        try
        {
            var item = _db.Find<Item>(itemId);
            if (item == null) return false;

            var inv = _db.Find<Inventari>(i => i.userId == userId && i.itemId == itemId);
            int cantidadActual = inv != null ? inv.Cantidad : 0;
            if (cantidadActual >= item.MaxStack) return false;

            if (inv == null)
            {
                _db.Insert(new Inventari { userId = userId, itemId = itemId, Cantidad = 1 });
            }
            else
            {
                inv.Cantidad += 1;
                _db.Update(inv);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("AddItem error: " + ex.Message);
            return false;
        }
    }

    // Resta 1 unidad. Si llega a 0, elimina el registro. Devuelve false si no tiene ninguno.
    public bool RestarItem(int userId, int itemId)
    {
        try
        {
            var inv = _db.Find<Inventari>(i => i.userId == userId && i.itemId == itemId);
            if (inv == null) return false;

            if (inv.Cantidad <= 1)
            {
                _db.Delete(inv);
            }
            else
            {
                inv.Cantidad -= 1;
                _db.Update(inv);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("RestarItem error: " + ex.Message);
            return false;
        }
    }

    // ---------------------------------------------------------------------
    // TIENDA — operaciones atómicas con transacción + rollback
    // ---------------------------------------------------------------------

    // Devuelve el saldo actual del jugador (-1 si hay error).
    public float GetDiners(int userId)
    {
        try
        {
            var usuari = _db.Find<Usuari>(userId);
            return usuari != null ? usuari.Diners : -1f;
        }
        catch (Exception ex)
        {
            Debug.LogError("GetDiners error: " + ex.Message);
            return -1f;
        }
    }

    // Compra `cantidad` unidades del ítem.
    // Operación 1: descuenta Diners del jugador.
    // Operación 2: añade/actualiza la entrada en Inventario.
    // Si cualquiera falla, hace Rollback y devuelve el mensaje de error.
    public (bool success, string message) ComprarItem(int userId, int itemId, int cantidad = 1)
    {
        _db.BeginTransaction();
        try
        {
            var item = _db.Find<Item>(itemId);
            if (item == null) throw new Exception("Ítem no encontrado.");

            var usuari = _db.Find<Usuari>(userId);
            if (usuari == null) throw new Exception("Usuario no encontrado.");

            float cost = item.Preu * cantidad;
            if (usuari.Diners < cost)
                throw new Exception($"Diners insuficients. Necessites {cost}, tens {usuari.Diners}.");

            // Operació 1: descomptar diners
            usuari.Diners -= cost;
            _db.Update(usuari);

            // Operació 2: afegir/actualitzar inventari
            var inv = _db.Find<Inventari>(i => i.userId == userId && i.itemId == itemId);
            int cantidadActual = inv != null ? inv.Cantidad : 0;
            int nouTotal = cantidadActual + cantidad;
            if (nouTotal > item.MaxStack)
                throw new Exception($"No caben {cantidad} unitats. Màxim {item.MaxStack}, tens {cantidadActual}.");

            if (inv == null)
                _db.Insert(new Inventari { userId = userId, itemId = itemId, Cantidad = nouTotal });
            else
            {
                inv.Cantidad = nouTotal;
                _db.Update(inv);
            }

            _db.Commit();
            return (true, $"Compra realitzada. Saldo restant: {usuari.Diners}");
        }
        catch (Exception ex)
        {
            _db.Rollback();
            Debug.LogWarning("ComprarItem rollback: " + ex.Message);
            return (false, ex.Message);
        }
    }

    // Ven `cantidad` unidades del ítem. Precio de venta = Preu / 2.
    // Operació 1: descomptar del inventari.
    // Operació 2: afegir Diners al jugador.
    // Si qualssevol falla, fa Rollback i retorna el missatge d'error.
    public (bool success, string message) VendreItem(int userId, int itemId, int cantidad = 1)
    {
        _db.BeginTransaction();
        try
        {
            var item = _db.Find<Item>(itemId);
            if (item == null) throw new Exception("Ítem no encontrado.");

            var usuari = _db.Find<Usuari>(userId);
            if (usuari == null) throw new Exception("Usuario no encontrado.");

            // Operació 1: restar de l'inventari
            var inv = _db.Find<Inventari>(i => i.userId == userId && i.itemId == itemId);
            if (inv == null || inv.Cantidad < cantidad)
                throw new Exception($"No tens prou unitats de {item.Nombre} per vendre.");

            inv.Cantidad -= cantidad;
            if (inv.Cantidad == 0)
                _db.Delete(inv);
            else
                _db.Update(inv);

            // Operació 2: afegir diners (preu de venda = meitat del preu de compra)
            float guany = (item.Preu / 2f) * cantidad;
            usuari.Diners += guany;
            _db.Update(usuari);

            _db.Commit();
            return (true, $"Venda realitzada. Guany: {guany}. Saldo: {usuari.Diners}");
        }
        catch (Exception ex)
        {
            _db.Rollback();
            Debug.LogWarning("VendreItem rollback: " + ex.Message);
            return (false, ex.Message);
        }
    }

    // Devuelve la info de un ítem del catálogo (null si no existe).
    public Item GetItem(int itemId)
    {
        try { return _db.Find<Item>(itemId); }
        catch (Exception ex)
        {
            Debug.LogError("GetItem error: " + ex.Message);
            return null;
        }
    }

    // Devuelve todos los ítems del catálogo.
    public List<Item> GetAllItems()
    {
        try { return _db.Table<Item>().ToList(); }
        catch (Exception ex)
        {
            Debug.LogError("GetAllItems error: " + ex.Message);
            return new List<Item>();
        }
    }
}
