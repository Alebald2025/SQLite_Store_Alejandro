using System.Collections.Generic;
using UnityEngine;

// Gestiona el panel de lista de usuarios.
// Se refresca automáticamente cada vez que el panel se activa (OnEnable).
public class UserPanelUI : MonoBehaviour
{
    [SerializeField] private Transform listContent;          // Content del ScrollView
    [SerializeField] private GameObject userListItemPrefab;  // Prefab con UserListItem

    private DatabaseManager db;

    private void Awake()
    {
        db = FindObjectOfType<DatabaseManager>();
    }

    // Se llama automáticamente cuando el panel se hace visible.
    private void OnEnable()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        // Borrar filas anteriores
        foreach (Transform child in listContent)
            Destroy(child.gameObject);

        List<Usuari> users = db.GetAllUsers();
        foreach (var user in users)
        {
            GameObject go = Instantiate(userListItemPrefab, listContent);
            go.GetComponent<UserListItem>().Setup(user, db, RefreshList);
        }
    }
}
