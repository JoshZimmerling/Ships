using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Movement : NetworkBehaviour
{
    [SerializeField] private float shipAcceleration;
    [SerializeField] private float shipMaxSpeed;
    private float currentAllowedMaxSpeed;
    private float currentAllowedMaxRotationSpeed;
    [SerializeField] private float shipTurnRate;
    [SerializeField] float distToStop = 2;

    float angle;
    float totalVelocity;
    float distToTarget;

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

        if (shipTurnRate >= 1000)
            transform.rotation = Quaternion.identity;

        track = targetPos - (Vector2)transform.position;
        angle = Vector2.SignedAngle(track, transform.up);

        distToTarget = Vector2.Distance(transform.position, targetPos);

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

        // Handling movement for no turn ships
        if (shipTurnRate >= 1000) // Insta turn if you have really high turn rate
        {
            if (rotateOnly)
                return;

            moving = true;

            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

            if (distToTarget < distToStop)
            {
                transform.Translate(distToTarget * Time.deltaTime * direction);
            }
            else
            {
                totalVelocity += shipAcceleration;
                if (totalVelocity > currentAllowedMaxSpeed)
                {
                    totalVelocity = currentAllowedMaxSpeed;
                }
                transform.Translate(totalVelocity * Time.deltaTime * direction);
            }

            return;
        }

        // Handling movement for notmal ships
        if (!backingUp)
        {
            // Turning
            if (MathF.Abs(angle) > 10)
            {
                if (angle > 0)
                {
                    transform.Rotate(0, 0, -currentAllowedMaxRotationSpeed * Time.deltaTime);
                }
                else
                {
                    transform.Rotate(0, 0, currentAllowedMaxRotationSpeed * Time.deltaTime);
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
                transform.Translate(distToTarget * Time.deltaTime * Vector2.up);
            }
            else
            {
                totalVelocity += shipAcceleration;
                if (totalVelocity > currentAllowedMaxSpeed)
                {
                    totalVelocity = currentAllowedMaxSpeed;
                }
                transform.Translate(totalVelocity * Time.deltaTime * Vector2.up);
            }
        }
        else // backing up
        {
            if (distToTarget < distToStop)
            {
                transform.Translate(distToTarget * Time.deltaTime * -Vector2.up);
            }
            else
            {
                totalVelocity += shipAcceleration;
                if (totalVelocity > currentAllowedMaxSpeed)
                {
                    totalVelocity = currentAllowedMaxSpeed;
                }
                transform.Translate(totalVelocity * Time.deltaTime * -Vector2.up);
            }
        }
    }

    public Vector2 GetFuturePosition(float seconds)
    {
        if (currentAllowedMaxRotationSpeed >= 1000)
            return (Vector2)transform.position + seconds * totalVelocity * (targetPos - (Vector2)transform.position).normalized;
        else
            return transform.position + transform.rotation * Vector2.up * totalVelocity * seconds;
    }

    public float GetMaxSpeed()
    {
        return shipMaxSpeed;
    }

    public float GetRotationSpeed()
    {
        return shipTurnRate;
    }

    public float GetAcceleration()
    {
        return shipAcceleration;
    }

    public void ChangeSpeed(float speedChange, float duration)
    {
        StartCoroutine(ChangeSpeedCoroutine(speedChange, duration));
    }

    [Rpc(SendTo.Server)]
    public void BackupShipRPC()
    {
        if (shipTurnRate >= 1000) return;
        targetPos = transform.position + (-transform.up * distToStop);
        backingUp = true;
        noTarget = false;
    }

    [Rpc(SendTo.Server)]
    public void StopShipRPC()
    {
        if (shipTurnRate >= 1000) 
            targetPos = (Vector2)transform.position + targetPos.normalized * distToStop;
        else if (backingUp && distToTarget > 1.5f) 
            targetPos = transform.position + -transform.up * distToStop/1.5f;
        else if (!backingUp)
            targetPos = transform.position + transform.up * distToStop;
    }

    [Rpc(SendTo.Server)]
    public void SetTargetDestinationRPC(Vector2 target)
    {
        noTarget = false;
        backingUp = false;
        targetPos = target;
        rotateOnly = false;
        currentAllowedMaxSpeed = shipMaxSpeed;
        currentAllowedMaxRotationSpeed = shipTurnRate;
    }

    [Rpc(SendTo.Server)]
    public void SetTargetRotationRPC(Vector2 target)
    {
        noTarget = false;
        backingUp = false;
        targetPos = target;
        rotateOnly = true;
        currentAllowedMaxSpeed = shipMaxSpeed;
        currentAllowedMaxRotationSpeed = shipTurnRate;

        //Stop movement since this is for rotation only
        totalVelocity = 0;
        moving = false;
    }

    [Rpc(SendTo.Server)]
    public void SetTargetDestinationWithSpeedCapRPC(Vector2 target, float maxSpeedAllowed, float maxRotationSpeedAllowed)
    {
        noTarget = false;
        backingUp = false;
        targetPos = target;
        rotateOnly = false;
        currentAllowedMaxSpeed = maxSpeedAllowed;
        currentAllowedMaxRotationSpeed = maxRotationSpeedAllowed;
    }

    private IEnumerator ChangeSpeedCoroutine(float speedChange, float time)
    {
        shipMaxSpeed *= speedChange;
        currentAllowedMaxSpeed *= speedChange;
        shipTurnRate *= speedChange;
        currentAllowedMaxRotationSpeed *= speedChange;
        if (currentAllowedMaxSpeed > shipMaxSpeed)
            currentAllowedMaxSpeed = shipMaxSpeed;
        if (currentAllowedMaxRotationSpeed > shipTurnRate)
            currentAllowedMaxRotationSpeed = shipTurnRate;
        ShowPopupTextRPC(speedChange);

        yield return new WaitForSeconds(time);

        shipMaxSpeed *= 1f/speedChange;
        currentAllowedMaxSpeed *= 1f/speedChange;
        shipTurnRate *= 1f/speedChange;
        currentAllowedMaxRotationSpeed *= 1f/speedChange;
        if (currentAllowedMaxSpeed > shipMaxSpeed)
            currentAllowedMaxSpeed = shipMaxSpeed;
        if (currentAllowedMaxRotationSpeed > shipTurnRate)
            currentAllowedMaxRotationSpeed = shipTurnRate;
        ShowPopupTextRPC(1f/speedChange);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowPopupTextRPC(float speedChange)
    {
        bool speedUp = speedChange > 1f;

        string text = speedUp ? "^" : "-";
        Color textColor = speedUp ? Color.cornflowerBlue : Color.orangeRed;
        if (Camera_Control.Singleton.IsOnScreen(transform) && Camera_Control.Singleton.IsSeenByMyShips(transform))
        {
            PopupText popupText = Instantiate(gameObject.GetComponent<Ship>().popupTextPrefab, transform.position + new Vector3(-1, 0.5f) * gameObject.GetComponent<Ship>().correctionFactor * 0.5f, Quaternion.identity).GetComponent<PopupText>();
            popupText.SetupText(text, textColor, 1f);
            popupText = Instantiate(gameObject.GetComponent<Ship>().popupTextPrefab, transform.position + new Vector3(1, -1f) * gameObject.GetComponent<Ship>().correctionFactor * 0.5f, Quaternion.identity).GetComponent<PopupText>();
            popupText.SetupText(text, textColor, 1f);
        }
    }
}
