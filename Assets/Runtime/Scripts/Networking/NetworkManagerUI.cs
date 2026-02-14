using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        clientButton.onClick.AddListener(() => {
                        NetworkManager.Singleton.StartClient();
        });

        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost(); });

    }
}
