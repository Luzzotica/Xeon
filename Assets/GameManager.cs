using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour 
{

    [Header("GUIs")]

    public GameObject mainMenu;
    public GameObject playerHUD;

    public void showGuiWithName(string name)
    {
        // Hide all Guis
        mainMenu.SetActive(false);
        playerHUD.SetActive(false);

        switch (name)
        {
            case GuiScreens.MAIN_MENU:
                mainMenu.SetActive(true);
                break;
            case GuiScreens.PLAYER_HUD:
                playerHUD.SetActive(true);
                break;
        }
    }

    public GameObject getGuiForName(string name)
    {
        switch (name)
        {
            case GuiScreens.MAIN_MENU:
                return mainMenu;
                break;
            case GuiScreens.PLAYER_HUD:
                return playerHUD;
                break;
        }
        return mainMenu;
    }

}
