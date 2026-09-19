using TMPro;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Fighter : Ship
{
    private GameObject goliath;
    private GameObject targetShip;

    [SerializeField] private float moveSpeed;
    [SerializeField] private float orbitRadius;
    [SerializeField] private float orbitRadiusRandomness;
    [SerializeField] private float orbitSpeed;
    [SerializeField] private float orbitSpeedRandomness;
    private float relativeOrbitRadius;

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
            relativeOrbitRadius = orbitRadius + targetShip.GetComponent<Ship>().correctionFactor;
        }

        float currentRadius = (targetShip.transform.position - transform.position).magnitude;
        if (currentRadius > relativeOrbitRadius + 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, moveSpeed * Time.deltaTime);
        }
        else if (currentRadius > relativeOrbitRadius + 0.2f) 
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, moveSpeed / 5f * Time.deltaTime);
        }
        else if (currentRadius < relativeOrbitRadius - 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, -moveSpeed * Time.deltaTime);
        }
        else if (currentRadius < relativeOrbitRadius - 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, -moveSpeed / 5f * Time.deltaTime);
        }

        if (currentRadius > relativeOrbitRadius - 1f && currentRadius < relativeOrbitRadius + 1f)
        {
            transform.RotateAround(targetShip.transform.position, Vector3.forward, orbitSpeed * Time.deltaTime);
        }


        Vector2 direction = (targetShip.transform.position - transform.position) * (targetShip == goliath ? -1 : 1);
        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

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
        orbitSpeed += Random.Range(-orbitSpeedRandomness, orbitSpeedRandomness);
        orbitSpeed *= Random.Range(0, 2) == 0 ? 1 : -1;
        relativeOrbitRadius = orbitRadius + ship.GetComponent<Ship>().correctionFactor;
    }

    public void SetTarget(GameObject ship)
    {
        targetShip = ship;

        if (ship.GetComponent<Ship>() != null)
            relativeOrbitRadius = orbitRadius + ship.GetComponent<Ship>().correctionFactor;
        if (ship.GetComponent<NeutralShip>() != null)
            relativeOrbitRadius = orbitRadius + ship.GetComponent<NeutralShip>().correctionFactor;
    }
}
