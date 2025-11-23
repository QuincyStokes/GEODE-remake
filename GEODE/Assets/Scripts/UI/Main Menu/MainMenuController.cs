using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The purpose of this class is to control the flow of the main menu.
/// Should essentially control the flow of logic, not implement it.
/// </summary>
public  class MainMenuController : MonoBehaviour
{
    //* Static Instance
    public static MainMenuController Instance;

    [SerializeField] private List<GameObject> uiPanels;










    //* --------------------- Methods ------------------ */
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }   
        ShowPanel("MainMenuPanel");
    }


    /// <summary>
    /// Enables the given panel by name, and disables all others.
    /// </summary>
    /// <param name="panelName">Name of desired panel to show.</param>
    public void ShowPanel(string panelName)
    {
        foreach (GameObject go in uiPanels)
        {
            go.SetActive(go.name == panelName);
        }
    }

    

    public void QuitGame()
    {
        Application.Quit();
    }
}
