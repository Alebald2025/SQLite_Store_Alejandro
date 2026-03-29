using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopOpener : MonoBehaviour
{
    [SerializeField] private CanvasManager canvasManager;

    private void Awake()
    {
        if (canvasManager == null)
            canvasManager = FindObjectOfType<CanvasManager>();
    }

    public void OpenShop()
    {
        if (canvasManager != null)
            canvasManager.MostrarPanelPorNombre("StorePanel");   // o l'índex del teu panell
        else
            Debug.LogError("CanvasManager no trobat!");
    }
}
