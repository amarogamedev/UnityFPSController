using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sway : MonoBehaviour
{
    //public variables
    public float amount; //amount of sway to be added to every frame based on the movement
    public float maxAmount; //maximum amount of sway to be added in a single frame
    public float smooth; //smoothness of the sway

    //private variables
    Vector3 def;
    Player player;

    private void Start()
    {
        def = transform.localPosition;
        player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        float x = 0;
        float y = 0;

        //get input from the mouse
        if (!player.paused)
        {
            x = Mathf.Clamp(-Input.GetAxis("Mouse X") * amount, -maxAmount, maxAmount);
            y = Mathf.Clamp(-Input.GetAxis("Mouse Y") * amount, -maxAmount, maxAmount);
        }

        //smoothly move the object by interpolating the input
        Vector3 final = new Vector3(def.x + x, def.y + y, def.z);
        transform.localPosition = Vector3.Lerp(transform.localPosition, final, smooth * Time.deltaTime);
    }
}
