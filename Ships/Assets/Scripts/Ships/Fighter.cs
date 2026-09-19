using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static Turret;

public class Fighter : Ship
{
    private GameObject goliath;
    private GameObject targetShip;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float moveSpeedRandomness;
    [SerializeField] private float orbitRadius;
    [SerializeField] private float orbitRadiusRandomness;
    private int orbitDirection;
    private float targetedOrbitRadius;

    private Vector3 moveDirection;

    public new void FixedUpdate()
    {
        if (!IsHost) return;

        if (goliath == null)
        {
            GameSceneManager.Singleton.shipsInScene.Remove(gameObject);
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }

        if (targetShip == null) 
        {
            targetShip = goliath;
            targetedOrbitRadius = orbitRadius + targetShip.GetComponent<Ship>().correctionFactor;
        }

        Vector3 direction2Target = targetShip.transform.position - transform.position;
        Vector3 perpendicular2Target = Vector2.Perpendicular(direction2Target) * orbitDirection;

        float currentRadius = direction2Target.magnitude;
        float d = (targetedOrbitRadius - currentRadius) / 8;
        if (d > 1) d = 1;
        if (d < -1) d = -1;
        if (d >= 0)
            moveDirection = Vector3.Slerp(perpendicular2Target.normalized / 2, -direction2Target.normalized, d);
        else
            moveDirection = Vector3.Slerp(perpendicular2Target.normalized / 2, direction2Target.normalized, d * -1);

        transform.position = transform.position + moveDirection * moveSpeed * Time.deltaTime;

        Vector2 lookDirection = (targetShip.transform.position - transform.position) * (targetShip == goliath && d > -0.5f ? -1 : 1);
        transform.rotation = Quaternion.FromToRotation(Vector3.up, lookDirection);

        // Heals fighters when back at goliath
        if (d > -0.5f && currentShipHP.Value != maxShipHP)
            currentShipHP.Value = maxShipHP;

        base.FixedUpdate();
    }

    public override void OnDestroy()
    {
        if (!IsHost) return;

        goliath.GetComponent<GoliathAbility>().fighters.Remove(gameObject);
    }

    public void SetupFighter(GameObject ship)
    {
        goliath = ship;
        targetShip = goliath;
        orbitRadius += Random.Range(-orbitRadiusRandomness, orbitRadiusRandomness);
        moveSpeed += Random.Range(-moveSpeedRandomness, moveSpeedRandomness);
        orbitDirection = Random.Range(0, 2) == 0 ? 1 : -1;
        targetedOrbitRadius = orbitRadius + ship.GetComponent<Ship>().correctionFactor;
    }

    public void SetTarget(GameObject ship)
    {
        targetShip = ship;

        if (ship.GetComponent<Ship>() != null)
            targetedOrbitRadius = orbitRadius + ship.GetComponent<Ship>().correctionFactor;
        if (ship.GetComponent<NeutralShip>() != null)
            targetedOrbitRadius = orbitRadius + ship.GetComponent<NeutralShip>().correctionFactor;
    }
    public Vector2 GetFuturePosition(float seconds)
    {
        return transform.position + moveDirection * moveSpeed * seconds;
    }
}
