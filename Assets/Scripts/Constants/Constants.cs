using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkingConstants
{

    // Server Messages
    //public const string 
    public const string ASK_NAME = "ASKNAME";
    public const string CONNECTION = "CONN";

    // Client Messages
    public const string NAME_IS = "NAMEIS";
    public const string SPAWN_PLAYER = "SPAWNPLAYER";
    public const string ASK_POSITION = "ASKPOSITION";
    public const string MY_POSITION = "MYPOSITION";
    public const string PLAYER_FIRE = "PLAYERFIRE";
    public const string PLAYER_HIT = "PLAYERHIT";
    public const string PLAYER_DIED = "PLAYERDIED";
    public const string PLAYER_ASK_SPAWN = "PLAYERASKSPAWN";
    public const string LOAD_MAP = "LOADMAP";

    // MISC
    public const string DEBUG = "DEBUG";

}

public static class GuiScreens
{
    public const string MAIN_MENU = "MainMenu";
    public const string PLAYER_HUD = "PlayerHUD";
}
