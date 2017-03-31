using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class loadRes : MonoBehaviour {

	// Use this for initialization
	void Start () {
        string nakemodle = "__Test/$Model/Cube.prefab";
        ResLoadParams param = new ResLoadParams();
        sdResourceMgr.Instance.LoadResourceImmediately(nakemodle, onLoadCube, param);
    }
	
    void onLoadCube(ResLoadParams param, Object obj)
    {
        Debug.Log("load complete");
        Instantiate(obj);
    }
	// Update is called once per frame
	void Update () {
		
	}
}
