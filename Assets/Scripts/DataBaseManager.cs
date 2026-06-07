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

    // Inserta los ítems del catálogo solo si la tabla está vacía.
    // El orden de inserción fija los IDs 1=Espada, 2=Comida, 3=Lingote, 4=EnderPearl.
    private void SeedItems()
    {
        if (_db.Table<Item>().Count() > 0)
            return;

        _db.InsertAll(new[]
        {
            new Item { Nombre = "Espada",     Descripcion = "Arma de combate cuerpo a cuerpo", MaxStack = 1 },
            new Item { Nombre = "Comida",     Descripcion = "Restaura puntos de vida",         MaxStack = 15 },
            new Item { Nombre = "Lingote",    Descripcion = "Material de crafteo básico",      MaxStack = 20 },
            new Item { Nombre = "EnderPearl", Descripcion = "Permite teletransportarse",       MaxStack = 6 },
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

            _db.Insert(new Usuari { Username = username, Password = password });
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
}
