using UnityEngine;
using System.Collections;

public class ParticleSortingLayer : MonoBehaviour {

    public string sortingLayer;
    public int sortingOrder;

    void Start()
    {
        //Change Foreground to the layer you want it to display on 
        //You could prob. make a public variable for this
        GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingLayerName = sortingLayer;
        GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingOrder = sortingOrder;
    }
}
