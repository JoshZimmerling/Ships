using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Movement : NetworkBehaviour
{
    [SerializeField] private float shipAcceleration;
    [SerializeField] private float shipMaxSpeed;
    [SerializeField] private float shipTurnRate;
    [SerializeField] float distToStop = 2;

    float angle;
    float totalVelocity;
    float distToTarget;
    //float timeToStop;
    //float brakeTimer;

    Vector2 targetPos;
    Vector2 track;

    bool noTarget = true;
    bool moving;
    bool backingUp;

    /*    LineRenderer lineRenderer;*/

    private void FixedUpdate()
    {
        if (!IsHost) return;

        track = targetPos - (Vector2)transform.position;
        angle = Vector2.SignedAngle(track, transform.up);

        //path = (Vector2)transform.position - targetPos;
        distToTarget = Vector2.Distance(transform.position, targetPos);

        //up = transform.up;

        if (noTarget) { return; }

        // Stopping
        if (distToTarget < 0.1)
        {
            noTarget = true;
            totalVelocity = 0;
            moving = false;
            backingUp = false;
            return;
        }

        if (!backingUp)
        {
            // Turning
            if (MathF.Abs(angle) > 10)
            {
                if (angle > 0)
                {
                    transform.Rotate(0, 0, -shipTurnRate * Time.deltaTime);
                }
                else
                {
                    transform.Rotate(0, 0, shipTurnRate * Time.deltaTime);
                }
            }
            // Slowing turns
            else if (MathF.Abs(angle) > 1)
            {
                if (angle > 0)
                {
                    transform.Rotate(0, 0, (-10 - (Mathf.Abs(angle) * 3)) * Time.deltaTime);
                }
                else
                {
                    transform.Rotate(0, 0, (10 + (Mathf.Abs(angle) * 3)) * Time.deltaTime);
                }
            }
            // If the angle is small enough, will lock towards target
            else
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, targetPos - (Vector2)transform.position);
            }

            // Prevents moving the ship if not moving and too high an angle
            if (Mathf.Abs(angle) > 45 && !moving)
            {
                return;
            }

            moving = true;

            if (distToTarget < distToStop)
            {
                transform.Translate(Vector2.up * distToTarget * Time.deltaTime);
            }
            else
            {
                totalVelocity += shipAcceleration;
                if (totalVelocity > shipMaxSpeed)
                {
                    totalVelocity = shipMaxSpeed;
                }
                transform.Translate(Vector2.up * totalVelocity * Time.deltaTime);
            }
        }
        else // backing up
        {
            if (distToTarget < distToStop)
            {
                transform.Translate(-Vector2.up * distToTarget * Time.deltaTime);
            }
            else
            {
                totalVelocity += shipAcceleration;
                if (totalVelocity > shipMaxSpeed)
                {
                    totalVelocity = shipMaxSpeed;
                }
                transform.Translate(-Vector2.up * totalVelocity * Time.deltaTime);
            }
        }
    }

    [ServerRpc]
    public void BackupServerRPC()
    {
        targetPos = transform.position + (-transform.up * distToStop);
        backingUp = true;
        noTarget = false;
    }

    [ServerRpc]
    public void StopShipServerRPC()
    {
        targetPos = transform.position + transform.up * distToStop;
    }

    [ServerRpc]
    public void SetTargetDestinationServerRPC(Vector2 target)
    {
        noTarget = false;
        backingUp = false;
        targetPos = target;
    }
}
