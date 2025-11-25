using System;
using UnityEngine;

public class PauseController : MonoBehaviour
{
    [SerializeField] private PlayerController _pc;
    public static event Action<bool> OnPauseChanged;
    [SerializeField] public GameObject pauseMenu;
    [SerializeField] public GameObject settingsMenu;

    private void Start()
    {
        _pc.OnMenuButtonPressed += HandleMenuButtonPressed;
    }

    private void OnDisable()
    {
        _pc.OnMenuButtonPressed -= HandleMenuButtonPressed;
    }

    private void HandleMenuButtonPressed()
    {
        //toggle pause menu
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        
        settingsMenu.SetActive(false);
        
        OnPauseChanged?.Invoke(pauseMenu.activeSelf);
    }
}
