using UnityEngine;
using TMPro;

public class StoreUI : MonoBehaviour
{
    [Header("Precios de compra")]
    [SerializeField] private TextMeshProUGUI precioCompraEspada;
    [SerializeField] private TextMeshProUGUI precioCompraComida;
    [SerializeField] private TextMeshProUGUI precioCompraLingote;
    [SerializeField] private TextMeshProUGUI precioCompraEnderPearl;

    private DatabaseManager db;
    private MainMenuUI mainMenuUI;
    private InventoryUI inventoryUI;
    private int userId;

    void Start()
    {
        db = FindObjectOfType<DatabaseManager>();
        if (db == null) { Debug.LogError("StoreUI: No se encontró DatabaseManager."); return; }

        mainMenuUI  = FindObjectOfType<MainMenuUI>();
        inventoryUI = FindObjectOfType<InventoryUI>();

        userId = PlayerPrefs.GetInt("CurrentUserID", -1);
        if (userId == -1) { Debug.LogError("StoreUI: No hay usuario logueado."); return; }

        MostrarPrecios();
    }

    // --- Botones de compra ---

    public void ComprarEspada()     => Comprar(1);
    public void ComprarComida()     => Comprar(2);
    public void ComprarLingote()    => Comprar(3);
    public void ComprarEnderPearl() => Comprar(4);

    private void Comprar(int itemId)
    {
        var (success, message) = db.ComprarItem(userId, itemId);
        if (success)
        {
            mainMenuUI?.RefreshDiners();
            inventoryUI?.ActualizarCantidades();
        }
        else
            Debug.LogWarning("Compra fallida: " + message);
    }

    // --- Precios ---

    private void MostrarPrecios()
    {
        SetPrecioCompra(1, precioCompraEspada);
        SetPrecioCompra(2, precioCompraComida);
        SetPrecioCompra(3, precioCompraLingote);
        SetPrecioCompra(4, precioCompraEnderPearl);
    }

    private void SetPrecioCompra(int itemId, TextMeshProUGUI texto)
    {
        if (texto == null) return;
        var item = db.GetItem(itemId);
        if (item != null)
            texto.text = item.Preu.ToString("0") + "$";
    }
}
