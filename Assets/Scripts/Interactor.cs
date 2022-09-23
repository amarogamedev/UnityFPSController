using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [Header("Settings")]
    public float range; //range of the interaction

    private void Update()
    {
        //casts a ray in the direction the player is facing
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, range))
        {
            //detects what the object is based on his tag
            switch (hit.transform.tag)
            {
                case "Item":
                    return;

                case "Door":
                    return;

                default:
                    return;
            }
        }
    }
}
