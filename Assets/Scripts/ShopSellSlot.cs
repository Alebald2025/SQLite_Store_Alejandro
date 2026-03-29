using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSellSlot : MonoBehaviour
{
    [Header("Referències del Slot de Venda")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Image itemIcon;           // Opcional

    private int itemId;
    private ShopUIController shopUIController;

    public void Setup(int _itemId, string itemName, int quantity, double sellPrice, ShopUIController controller)
    {
        itemId = _itemId;
        shopUIController = controller;

        if (itemNameText != null)
            itemNameText.text = itemName;

        if (quantityText != null)
            quantityText.text = $"x{quantity}";

        if (priceText != null)
            priceText.text = $"{sellPrice:F0} 🪙";

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);
        }
    }

    private void OnSellClicked()
    {
        if (shopUIController != null)
        {
            shopUIController.SellItem(itemId, 1);   // pots canviar a quantitat variable
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (itemIcon != null)
            itemIcon.sprite = icon;
    }
}
