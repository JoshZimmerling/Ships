using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStartup : MonoBehaviour
{
    public async void Start()
    {
        // Get and disable button
        Button playButton = transform.GetComponentInChildren<Button>();
        playButton.onClick.AddListener(() => SceneManager.LoadScene("Main Menu"));
        playButton.gameObject.SetActive(false);

        // Ititialize services and sign in
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        // Reactivate button
        playButton.gameObject.SetActive(true);
    }
}
