using System;
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
    public bool moving;
    bool backingUp;
    bool rotateOnly;

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
        if (distToTarget < 1)
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
            if (shipTurnRate >= 1000) // Insta turn if you have really high turn rate
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, targetPos - (Vector2)transform.position);
            }
            else if (MathF.Abs(angle) > 10)
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
            if ((Mathf.Abs(angle) > 45 && !moving) || rotateOnly)
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

    public Vector2 GetFuturePosition(float seconds)
    {
        return transform.position + transform.rotation * Vector2.up * totalVelocity * seconds;
    }

    [Rpc(SendTo.Server)]
    public void BackupShipRPC()
    {
        targetPos = transform.position + (-transform.up * distToStop);
        backingUp = true;
        noTarget = false;
    }

    [Rpc(SendTo.Server)]
    public void StopShipRPC()
    {
        targetPos = transform.position + transform.up * distToStop;
    }

    [Rpc(SendTo.Server)]
    public void SetTargetDestinationRPC(Vector2 target, bool isRotateOnly)
    {
        noTarget = false;
        backingUp = false;
        targetPos = target;
        rotateOnly = isRotateOnly;

        if (isRotateOnly)
            totalVelocity = 0;
    }
}
