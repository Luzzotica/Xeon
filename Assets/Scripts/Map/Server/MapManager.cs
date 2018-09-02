using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour {

    string current_map;

    //List of environmental artifacts on map
    //NEED TO ADD

	// Use this for initialization
	void Start () {
	}

    public string StartMap()
    {
        current_map = "test_map";
        return current_map;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
