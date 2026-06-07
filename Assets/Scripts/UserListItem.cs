using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// Controla una fila del acordeón de usuarios.
// Colapsada: solo muestra el nombre. Expandida: Id, Nº Objetos y botón de borrar.
[RequireComponent(typeof(LayoutElement))]
public class UserListItem : MonoBehaviour
{
    [Header("Siempre visible")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private Button rowButton;

    [Header("Panel de detalle (oculto al inicio)")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private TextMeshProUGUI objectCountText;
    [SerializeField] private Button deleteButton;

    [Header("Alturas (px)")]
    [SerializeField] private float collapsedHeight = 55f;
    [SerializeField] private float expandedHeight  = 110f;
    [SerializeField] private float animDuration    = 0.25f;

    private LayoutElement layoutElement;
    private bool isExpanded = false;

    private Usuari usuari;
    private DatabaseManager db;
    private System.Action onDeleted;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
    }

    // Llamado por UserPanelUI al instanciar el prefab.
    public void Setup(Usuari u, DatabaseManager database, System.Action onDeletedCallback)
    {
        usuari   = u;
        db       = database;
        onDeleted = onDeletedCallback;

        usernameText.text  = u.Username;
        idText.text        = "Id: " + u.UserID;
        objectCountText.text = "Nº Objetos: " + db.GetTotalItems(u.UserID);

        // Estado inicial: colapsado
        detailPanel.SetActive(false);
        layoutElement.preferredHeight = collapsedHeight;

        rowButton.onClick.AddListener(ToggleExpand);
        deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    private void ToggleExpand()
    {
        isExpanded = !isExpanded;
        float targetHeight = isExpanded ? expandedHeight : collapsedHeight;

        if (isExpanded)
            detailPanel.SetActive(true);

        DOTween.To(
            () => layoutElement.preferredHeight,
            h  => layoutElement.preferredHeight = h,
            targetHeight,
            animDuration
        ).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            if (!isExpanded)
                detailPanel.SetActive(false);
        });
    }

    private void OnDeleteClicked()
    {
        db.DeleteUser(usuari);
        onDeleted?.Invoke();   // refresca la lista en UserPanelUI
    }
}
