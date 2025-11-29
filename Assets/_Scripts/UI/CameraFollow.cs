using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public float yOffset = 1f;
    public float xOffset = 1f;
    public float smoothTime = 0.25f;

    private Vector3 velocity = Vector3.zero;
    void Update()
    {
        //Balanceamento pro GameDesigner!
        Vector3 newPos = new Vector3(
            target.position.x + xOffset,
            target.position.y + yOffset,
            -10f
           );

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            newPos, 
            ref velocity,
            smoothTime
            );
    }
}
