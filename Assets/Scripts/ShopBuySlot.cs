using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBuySlot : MonoBehaviour
{
    [Header("Referències del Slot de Compra")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image itemIcon;

    private int itemId;
    private ShopUIController shopUIController;

    public void Setup(int _itemId, string itemName, double buyPrice, ShopUIController controller)
    {
        itemId = _itemId;
        shopUIController = controller;

        // Assignar text
        if (itemNameText != null)
            itemNameText.text = itemName;

        if (priceText != null)
            priceText.text = $"{buyPrice:F0} 🪙";

        // Assignar botó
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnBuyClicked()
    {
        if (shopUIController != null)
        {
            shopUIController.BuyItem(itemId, 1);   // pots canviar a quantitat variable més endavant
        }
    }

    // Opcional: si vols icones
    public void SetIcon(Sprite icon)
    {
        if (itemIcon != null)
            itemIcon.sprite = icon;
    }
