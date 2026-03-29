using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopUIController : MonoBehaviour
{
    [Header("Referències UI")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Transform buyContent;      // Content del ScrollView esquerra (comprar)
    [SerializeField] private Transform sellContent;     // Content del ScrollView dreta (vendre)
    [SerializeField] private GameObject shopBuySlotPrefab;   // Prefab per slots de compra
    [SerializeField] private GameObject shopSellSlotPrefab;  // Prefab per slots de venda
    [SerializeField] private TextMeshProUGUI feedbackText;

    private DatabaseManager dbManager;
    private int currentUserId;

    private void Awake()
    {
        dbManager = FindObjectOfType<DatabaseManager>();
    }

    private void OnEnable()
    {
        currentUserId = PlayerPrefs.GetInt("CurrentUserID", -1);
        if (currentUserId == -1)
        {
            Debug.LogError("No hi ha usuari loguejat");
            return;
        }

        RefreshMoney();
        LoadBuyItems();
        LoadSellInventory();
        ClearFeedback();
    }

    public void RefreshMoney()
    {
        if (moneyText != null)
            moneyText.text = $"{dbManager.GetDiners(currentUserId):F0} 🪙";
    }

    private void LoadBuyItems()
    {
        ClearContent(buyContent);

        var shopItems = dbManager.GetShopItems();
        foreach (var item in shopItems)
        {
            GameObject slot = Instantiate(shopBuySlotPrefab, buyContent);
            ShopBuySlot slotScript = slot.GetComponent<ShopBuySlot>();
            if (slotScript != null)
            {
                slotScript.Setup(item.ItemID, item.Nombre, item.PreuCompra, this);
            }
        }
    }

    private void LoadSellInventory()
    {
        ClearContent(sellContent);

        var inventoryItems = dbManager.GetInventoryItems(currentUserId);
        foreach (var item in inventoryItems)
        {
            GameObject slot = Instantiate(shopSellSlotPrefab, sellContent);
            ShopSellSlot slotScript = slot.GetComponent<ShopSellSlot>();
            if (slotScript != null)
            {
                double sellPrice = dbManager.GetSellPrice(item.ItemID);
                slotScript.Setup(item.ItemID, item.Nombre, item.Cantidad, sellPrice, this);
            }
        }
    }

    private void ClearContent(Transform content)
    {
        if (content == null) return;
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    public void BuyItem(int itemId, int quantity = 1)
    {
        var (success, msg) = dbManager.ComprarItem(currentUserId, itemId, quantity);
        ShowFeedback(msg, success);
        if (success)
        {
            RefreshMoney();
            LoadSellInventory();   // refresca la dreta
        }
    }

    public void SellItem(int itemId, int quantity = 1)
    {
        var (success, msg) = dbManager.VendreItem(currentUserId, itemId, quantity);
        ShowFeedback(msg, success);
        if (success)
        {
            RefreshMoney();
            LoadSellInventory();   // refresca la dreta
        }
    }

    private void ShowFeedback(string message, bool isSuccess)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = isSuccess ? Color.green : Color.red;
        feedbackText.gameObject.SetActive(true);
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    // Botó tancar
    public void CloseShop()
    {
        // Si uses CanvasManager:
        // FindObjectOfType<CanvasManager>().MostrarPanel(índex del menú);
        gameObject.SetActive(false);
    }
}
