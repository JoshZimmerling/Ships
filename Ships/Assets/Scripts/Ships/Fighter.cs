using TMPro;
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

    public new void FixedUpdate()
    {
        if (targetShip == null) 
        {
            targetShip = goliath;
        }

        float currentRadius = (targetShip.transform.position - transform.position).magnitude;
        if (currentRadius > orbitRadius + 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, moveSpeed * Time.deltaTime);
        }
        else if (currentRadius > orbitRadius + 0.2f) 
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, moveSpeed / 5f * Time.deltaTime);
        }
        else if (currentRadius < orbitRadius - 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, -moveSpeed * Time.deltaTime);
        }
        else if (currentRadius < orbitRadius - 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetShip.transform.position, -moveSpeed / 5f * Time.deltaTime);
        }

        if (currentRadius > orbitRadius - 1f && currentRadius < orbitRadius + 1f)
        {
            transform.RotateAround(targetShip.transform.position, Vector3.forward, orbitSpeed * Time.deltaTime);
        }

        Vector2 direction = targetShip.transform.position - transform.position;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

        base.FixedUpdate();
    }

    public override void OnDestroy()
    {
        goliath.GetComponent<GoliathAbility>().fighters.Remove(gameObject);
    }

    public void SetupFighter(GameObject ship)
    {
        goliath = ship;
        targetShip = goliath;
        orbitRadius += Random.Range(-orbitRadiusRandomness, orbitRadiusRandomness);
        orbitSpeed += Random.Range(-orbitSpeedRandomness, orbitSpeedRandomness);
    }

    public void SetTarget(GameObject ship)
    {
        targetShip = ship;
    }
}
