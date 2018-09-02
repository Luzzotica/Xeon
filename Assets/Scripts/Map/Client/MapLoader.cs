using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MapLoader: MonoBehaviour {

	// Use this for initialization
	void Start () {

	}

    public void LoadMap(string map_name)
    {
        //Load the text file
        string path = "Assets/Resources/Maps/" + map_name + ".txt";
        StreamReader file_reader = new StreamReader(path);
        

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
