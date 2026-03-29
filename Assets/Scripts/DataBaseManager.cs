using System;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.Collections.Generic;

public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    public struct ShopItemData
    {
        public int ItemID;
        public string Nombre;
        public double PreuCompra;
        public double PreuVenda;
    }

    public struct InventoryItemData
    {
        public int ItemID;
        public string Nombre;
        public int Cantidad;
    }

    private void Awake()
    {
        // Ruta persistente para que se guarde al cerrar/abrir el juego
        dbPath = "URI=file:" + Application.persistentDataPath + "/rpg_inventory.db";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // Tabla Usuaris
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Usuaris (
                            UserID      INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username    TEXT    UNIQUE NOT NULL,
                            Password    TEXT    NOT NULL
                        )";
                    cmd.ExecuteNonQuery();

                    // Tabla Item
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Item (
                            ID          INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nombre      TEXT NOT NULL UNIQUE,
                            Descripcion TEXT,
                            MaxStack    INTEGER NOT NULL
                        )";
                    cmd.ExecuteNonQuery();

                    // Tabla Inventario
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Inventario (
                            InventarioID INTEGER PRIMARY KEY AUTOINCREMENT,
                            userId       INTEGER NOT NULL,
                            itemId       INTEGER NOT NULL,
                            Cantidad     INTEGER NOT NULL DEFAULT 0,
                            FOREIGN KEY (userId) REFERENCES Usuaris(UserID) ON DELETE CASCADE,
                            FOREIGN KEY (itemId) REFERENCES Item(ID) ON DELETE RESTRICT,
                            UNIQUE(userId, itemId)
                        )";
                    cmd.ExecuteNonQuery();

                    // Datos iniciales
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO Item (Nombre, Descripcion, MaxStack) VALUES
                        ('Item1', 'Descripción del Item 1', 1),
                        ('Item2', 'Descripción del Item 2', 15),
                        ('Item3', 'Descripción del Item 3', 20),
                        ('Item4', 'Descripción del Item 4', 6);
                    ";
                    cmd.ExecuteNonQuery();
                }
            }

            AddDinersColumnIfNotExists();
            CreateShopTableAndSampleData();

            Debug.Log("Base de datos inicializada en: " + dbPath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error inicializando DB: " + e.Message);
        }
    }

    // MÉTODOS DE LOGIN Y REGISTER

    public string RegisterUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "El nombre de usuario no puede estar vacío";

        if (password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres";

        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Usuaris WHERE Username = @user";
                    cmd.Parameters.AddWithValue("@user", username);
                    long count = (long)cmd.ExecuteScalar();

                    if (count > 0)
                        return "Este usuario ya existe";

                    cmd.CommandText = "INSERT INTO Usuaris (Username, Password) VALUES (@user, @pass)";
                    cmd.Parameters.AddWithValue("@user", username);
                    cmd.Parameters.AddWithValue("@pass", password);
                    cmd.ExecuteNonQuery();

                    return "OK";
                }
            }
        }
        catch (SqliteException ex)
        {
            if (ex.Message.Contains("UNIQUE constraint failed"))
                return "Este usuario ya existe";
            return "Error de base de datos: " + ex.Message;
        }
        catch (Exception ex)
        {
            return "Error inesperado: " + ex.Message;
        }
    }

    public (bool success, string message, int userId) LoginUser(string username, string password)
    {
        try
        {
            using (var conn = new SqliteConnection(dbPath))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT UserID FROM Usuaris WHERE Username = @user AND Password = @pass";
                    cmd.Parameters.AddWithValue("@user", username);
                    cmd.Parameters.AddWithValue("@pass", password);

                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        int userId = Convert.ToInt32(result);
                        return (true, "Login correcto", userId);
                    }
                    else
                    {
                        return (false, "Usuario o contraseña incorrectos", -1);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return (false, "Error de conexión: " + ex.Message, -1);
        }
    }

    // MÉTODOS PARA INVENTARIO

    public bool AddItem(int userId, int itemId)
    {
        int actual = GetCantidad(userId, itemId);
        int max = GetMaxStack(itemId);

        if (actual >= max) return false;

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Inventario (userId, itemId, Cantidad)
                    VALUES (@uid, @iid, 1)
                    ON CONFLICT(userId, itemId) DO UPDATE SET Cantidad = Cantidad + 1";
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@iid", itemId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }

    public bool RestarItem(int userId, int itemId)
    {
        int actual = GetCantidad(userId, itemId);
        if (actual <= 0) return false;

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                if (actual - 1 <= 0)
                {
                    cmd.CommandText = "DELETE FROM Inventario WHERE userId = @uid AND itemId = @iid";
                }
                else
                {
                    cmd.CommandText = "UPDATE Inventario SET Cantidad = Cantidad - 1 WHERE userId = @uid AND itemId = @iid";
                }
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@iid", itemId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }

    public int GetCantidad(int userId, int itemId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Cantidad FROM Inventario WHERE userId = @uid AND itemId = @iid";
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@iid", itemId);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
    }

    private int GetMaxStack(int itemId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT MaxStack FROM Item WHERE ID = @iid";
                cmd.Parameters.AddWithValue("@iid", itemId);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 99;
            }
        }
    }

    #region Botiga i Diners

    // Afegir columna Diners de forma segura (crida-ho des de InitializeDatabase)
    private void AddDinersColumnIfNotExists()
    {
        if (ColumnExists("Usuaris", "Diners")) return;

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE Usuaris ADD COLUMN Diners REAL NOT NULL DEFAULT 500.0";
                cmd.ExecuteNonQuery();
            }
        }
    }

    private bool ColumnExists(string tableName, string columnName)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({tableName});";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.GetString(1).Equals(columnName, System.StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    // Crida aquest mètode dins InitializeDatabase() després de crear la taula Usuaris
    // AddDinersColumnIfNotExists();

    public double GetDiners(int userId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Diners FROM Usuaris WHERE UserID = @uid";
                cmd.Parameters.AddWithValue("@uid", userId);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDouble(result) : 0.0;
            }
        }
    }

    private bool UpdateDiners(int userId, double newAmount, SqliteConnection conn, SqliteTransaction trans)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = trans;
            cmd.CommandText = "UPDATE Usuaris SET Diners = @amount WHERE UserID = @uid";
            cmd.Parameters.AddWithValue("@amount", newAmount);
            cmd.Parameters.AddWithValue("@uid", userId);
            return cmd.ExecuteNonQuery() == 1;
        }
    }

    // Taula Botiga (crida-ho també des de InitializeDatabase)
    private void CreateShopTableAndSampleData()
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Botiga (
                    ItemID      INTEGER PRIMARY KEY,
                    PreuCompra  REAL DEFAULT NULL,
                    PreuVenda   REAL DEFAULT NULL,
                    FOREIGN KEY (ItemID) REFERENCES Item(ID) ON DELETE CASCADE
                )";
                cmd.ExecuteNonQuery();

                // Dades de prova (només s'insereixen una vegada)
                cmd.CommandText = @"
                INSERT OR IGNORE INTO Botiga (ItemID, PreuCompra, PreuVenda) VALUES
                (1, 120.0, 40.0),
                (2,  25.0, 10.0),
                (3,  80.0, 30.0),
                (4, 350.0, 120.0);";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public List<ShopItemData> GetShopItems()
    {
        var items = new List<ShopItemData>();

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT b.ItemID, i.Nombre, b.PreuCompra, b.PreuVenda
                    FROM Botiga b
                    INNER JOIN Item i ON i.ID = b.ItemID";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ShopItemData
                        {
                            ItemID = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            PreuCompra = reader.GetDouble(2),
                            PreuVenda = reader.GetDouble(3)
                        });
                    }
                }
            }
        }

        return items;
    }

    public List<InventoryItemData> GetInventoryItems(int userId)
    {
        var items = new List<InventoryItemData>();

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT inv.itemId, itm.Nombre, inv.Cantidad
                    FROM Inventario inv
                    INNER JOIN Item itm ON itm.ID = inv.itemId
                    WHERE inv.userId = @uid";
                cmd.Parameters.AddWithValue("@uid", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new InventoryItemData
                        {
                            ItemID = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            Cantidad = reader.GetInt32(2)
                        });
                    }
                }
            }
        }

        return items;
    }

    public double GetSellPrice(int itemId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT PreuVenda FROM Botiga WHERE ItemID = @iid";
                cmd.Parameters.AddWithValue("@iid", itemId);
                var result = cmd.ExecuteScalar();
                return result != null && !Convert.IsDBNull(result) ? Convert.ToDouble(result) : 0.0;
            }
        }
    }

    // Mètode principal de COMPRA amb transacció i rollback
    public (bool success, string message) ComprarItem(int userId, int itemId, int quantitat = 1)
    {
        if (quantitat <= 0) return (false, "Quantitat no vàlida");

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. Preu de compra
                    double preuCompra = 0;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "SELECT PreuCompra FROM Botiga WHERE ItemID = @iid";
                        cmd.Parameters.AddWithValue("@iid", itemId);
                        var res = cmd.ExecuteScalar();
                        if (res == null || Convert.IsDBNull(res))
                            return (false, "Aquest item no està disponible per comprar");
                        preuCompra = Convert.ToDouble(res);
                    }

                    double costTotal = preuCompra * quantitat;

                    // 2. Comprovar diners
                    double dinersActuals = GetDiners(userId);
                    if (dinersActuals < costTotal)
                        return (false, $"No tens prou diners. Necessites {costTotal} i tens {dinersActuals}");

                    // 3. Restar diners
                    if (!UpdateDiners(userId, dinersActuals - costTotal, conn, transaction))
                        throw new System.Exception("Error actualitzant diners");

                    // 4. Afegir al inventari (amb límit de stack)
                    int maxStack = GetMaxStack(itemId);
                    int actual = GetCantidad(userId, itemId);
                    if (actual + quantitat > maxStack)
                        return (false, $"No pots portar més de {maxStack} unitats d'aquest item");

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                        INSERT INTO Inventario (userId, itemId, Cantidad)
                        VALUES (@uid, @iid, @q)
                        ON CONFLICT(userId, itemId) 
                        DO UPDATE SET Cantidad = Cantidad + @q;";
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@iid", itemId);
                        cmd.Parameters.AddWithValue("@q", quantitat);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return (true, $"Has comprat {quantitat}x Item per {costTotal} monedes.");
                }
                catch (System.Exception ex)
                {
                    transaction.Rollback();
                    Debug.LogError("Rollback en compra: " + ex.Message);
                    return (false, "Error intern durant la compra.");
                }
            }
        }
    }

    // Mètode principal de VENDA amb transacció i rollback
    public (bool success, string message) VendreItem(int userId, int itemId, int quantitat = 1)
    {
        if (quantitat <= 0) return (false, "Quantitat no vàlida");

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // 1. Preu de venda
                    double preuVenda = 0;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "SELECT PreuVenda FROM Botiga WHERE ItemID = @iid";
                        cmd.Parameters.AddWithValue("@iid", itemId);
                        var res = cmd.ExecuteScalar();
                        if (res == null || Convert.IsDBNull(res))
                            return (false, "Aquest item no es pot vendre");
                        preuVenda = Convert.ToDouble(res);
                    }

                    // 2. Comprovar quantitat disponible
                    int disponible = GetCantidad(userId, itemId);
                    if (disponible < quantitat)
                        return (false, $"Només tens {disponible} unitats per vendre");

                    double guany = preuVenda * quantitat;

                    // 3. Restar de l'inventari
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        if (disponible - quantitat <= 0)
                        {
                            cmd.CommandText = "DELETE FROM Inventario WHERE userId = @uid AND itemId = @iid";
                        }
                        else
                        {
                            cmd.CommandText = "UPDATE Inventario SET Cantidad = Cantidad - @q WHERE userId = @uid AND itemId = @iid";
                            cmd.Parameters.AddWithValue("@q", quantitat);
                        }
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@iid", itemId);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Sumar diners
                    double dinersActuals = GetDiners(userId);
                    if (!UpdateDiners(userId, dinersActuals + guany, conn, transaction))
                        throw new System.Exception("Error actualitzant diners");

                    transaction.Commit();
                    return (true, $"Has venut {quantitat}x Item i has guanyat {guany} monedes.");
                }
                catch (System.Exception ex)
                {
                    transaction.Rollback();
                    Debug.LogError("Rollback en venda: " + ex.Message);
                    return (false, "Error intern durant la venda.");
                }
            }
        }
    }

    #endregion
}