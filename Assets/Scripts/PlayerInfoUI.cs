using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuUI : MonoBehaviour
{
    [Header("Botón icono de usuario")]
    [SerializeField] private Button botonUsuario;

    [Header("Paneles del juego")]
    [SerializeField] private CanvasGroup panelInventario;
    [SerializeField] private CanvasGroup panelObjetos;
    [SerializeField] private CanvasGroup panelPerfil;
    [SerializeField] private CanvasGroup panelTienda;

    [Header("Textos del perfil")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI idText;

    [Header("Monedas")]
    [SerializeField] private TextMeshProUGUI dinerText;

    [Header("Botón cerrar sesión")]
    [SerializeField] private Button botonCerrarSesion;

    [Header("Animación")]
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float fadeInDuration  = 0.4f;

    private bool perfilVisible = false;
    // Qué panel secundario está abierto: null = ninguno, "objetos" o "tienda"
    private string panelSecundarioActivo = null;

    void Start()
    {
        string username = PlayerPrefs.GetString("CurrentUsername", "Desconocido");
        int userId      = PlayerPrefs.GetInt("CurrentUserID", -1);

        if (usernameText != null)
            usernameText.text = "Username: " + username;

        if (idText != null)
            idText.text = "ID: " + (userId != -1 ? userId.ToString() : "No disponible");

        RefreshDiners();

        // Estado inicial: solo inventario visible, el resto oculto
        SetCanvasGroup(panelPerfil,     visible: false, instant: true);
        SetCanvasGroup(panelObjetos,    visible: false, instant: true);
        SetCanvasGroup(panelTienda,     visible: false, instant: true);
        SetCanvasGroup(panelInventario, visible: true,  instant: true);

        if (botonUsuario != null)
            botonUsuario.onClick.AddListener(OnUsuarioClick);

        if (botonCerrarSesion != null)
            botonCerrarSesion.onClick.AddListener(CerrarSesion);
    }

    private void OnUsuarioClick()
    {
        if (perfilVisible)
            MostrarJuego();
        else
            MostrarPerfil();
    }

    private void MostrarPerfil()
    {
        perfilVisible = true;
        botonUsuario.interactable = false;

        FadeOut(panelInventario, fadeOutDuration);
        FadeOut(panelObjetos,    fadeOutDuration);
        FadeOut(panelTienda,     fadeOutDuration, onComplete: () =>
        {
            FadeIn(panelPerfil, fadeInDuration, onComplete: () =>
                botonUsuario.interactable = true);
        });
        panelSecundarioActivo = null;
    }

    private void MostrarJuego()
    {
        perfilVisible = false;
        botonUsuario.interactable = false;

        FadeOut(panelPerfil, fadeOutDuration, onComplete: () =>
        {
            FadeIn(panelInventario, fadeInDuration, onComplete: () =>
                botonUsuario.interactable = true);
        });
    }

    // Llamado por el botón "Objetos"
    public void TogglePanelObjetos()
    {
        if (perfilVisible) return;

        if (panelSecundarioActivo == "objetos")
        {
            FadeOut(panelObjetos, fadeOutDuration);
            panelSecundarioActivo = null;
        }
        else
        {
            if (panelSecundarioActivo == "tienda")
                FadeOut(panelTienda, fadeOutDuration, onComplete: () => FadeIn(panelObjetos, fadeInDuration));
            else
                FadeIn(panelObjetos, fadeInDuration);
            panelSecundarioActivo = "objetos";
        }
    }

    // Llamado por el botón "Tienda"
    public void TogglePanelTienda()
    {
        if (perfilVisible) return;

        if (panelSecundarioActivo == "tienda")
        {
            FadeOut(panelTienda, fadeOutDuration);
            panelSecundarioActivo = null;
        }
        else
        {
            if (panelSecundarioActivo == "objetos")
                FadeOut(panelObjetos, fadeOutDuration, onComplete: () => FadeIn(panelTienda, fadeInDuration));
            else
                FadeIn(panelTienda, fadeInDuration);
            panelSecundarioActivo = "tienda";
        }
    }

    private void CerrarSesion()
    {
        PlayerPrefs.DeleteKey("CurrentUsername");
        PlayerPrefs.DeleteKey("CurrentUserID");
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }

    public void Salir()
    {
        // Vuelve al MainMenu manteniendo la sesión activa
        SceneManager.LoadScene("MainMenu");
    }

    // Actualiza el texto de monedas leyendo la BD. Llámalo tras comprar/vender.
    public void RefreshDiners()
    {
        if (dinerText == null) return;
        int userId = PlayerPrefs.GetInt("CurrentUserID", -1);
        if (userId == -1) { dinerText.text = "$ --"; return; }
        var db = FindObjectOfType<DatabaseManager>();
        if (db == null) { dinerText.text = "$ --"; return; }
        float diners = db.GetDiners(userId);
        dinerText.text = "$ " + diners.ToString("0");
    }

    // --- Helpers ---

    private void FadeOut(CanvasGroup cg, float duration, System.Action onComplete = null)
    {
        if (cg == null) { onComplete?.Invoke(); return; }
        cg.blocksRaycasts = false;
        cg.DOKill(true);
        cg.DOFade(0f, duration).OnComplete(() =>
        {
            cg.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void FadeIn(CanvasGroup cg, float duration, System.Action onComplete = null)
    {
        if (cg == null) { onComplete?.Invoke(); return; }
        cg.gameObject.SetActive(true);
        cg.alpha = 0f;
        cg.DOKill(true);
        cg.DOFade(1f, duration).OnComplete(() =>
        {
            cg.blocksRaycasts = true;
            onComplete?.Invoke();
        });
    }

    private void SetCanvasGroup(CanvasGroup cg, bool visible, bool instant)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(visible);
        cg.alpha          = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
    }
}