using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using TacoTornado;

public class TacoQueueManager : MonoBehaviour
{
    [Header("Prefabs & Positions")]
    public GameObject customerPrefab;
    public Transform[] queuePositions; // Point_1 is the window
    public Transform exitPoint;        // NPC walks here after being served or expired

    [Header("Settings")]
    [SerializeField] private float arrivalThreshold = 0.2f;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private float destroyDistance = 1.0f;

    private List<GameObject> customersInLine = new List<GameObject>();
    private bool isFirstCustomerAtWindow = false;
    private float nextSpawnTimer;

    void Start()
    {
        // Initial delay before the first customer spawns
        nextSpawnTimer = 2f;

        // Subscribe to GameManager events to know when to make the NPC walk away
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOrderFailed += HandleOrderDone;
            GameManager.Instance.OnOrderCompleted += HandleOrderDone;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnOrderFailed -= HandleOrderDone;
            GameManager.Instance.OnOrderCompleted -= HandleOrderDone;
        }
    }

    private void HandleOrderDone(TacoOrder order)
    {
        // Trigger the walk-away logic whenever an order is successfully served or expires/fails
        OnCustomerServed();
    }

    void Update()
    {
        // Only run logic during an active shift
        if (!GameManager.Instance.isShiftActive) return;

        // AUTOMATIC SPAWNING LOGIC
        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            SpawnCustomer();
            nextSpawnTimer = spawnInterval;
        }

        // Sync digital ticket creation to physical arrival
        CheckForOrderTrigger();
    }

    public void SpawnCustomer()
    {
        // Check physical slots and global max queue size
        if (customersInLine.Count < queuePositions.Length && customersInLine.Count < GameConstants.MAX_QUEUE_SIZE)
        {
            Vector3 spawnPos = queuePositions[queuePositions.Length - 1].position + Vector3.back * 5;
            GameObject newCustomer = Instantiate(customerPrefab, spawnPos, Quaternion.identity);

            customersInLine.Add(newCustomer);
            UpdateQueue();
        }
    }

    private void CheckForOrderTrigger()
    {
        // Trigger only if someone is at the window and no ticket exists yet
        if (customersInLine.Count > 0 && !isFirstCustomerAtWindow)
        {
            NavMeshAgent agent = customersInLine[0].GetComponent<NavMeshAgent>();

            // Check if NavMeshAgent has physically reached the window
            if (agent != null && !agent.pathPending && agent.remainingDistance <= arrivalThreshold)
            {
                isFirstCustomerAtWindow = true;

                // This makes the UI ticket appear!
                OrderManager.Instance.SpawnOrder();
                
                Debug.Log("[Sync] NPC reached window. Digital order is being created.");
            }
        }
    }

    // Called when Hassan's assembly plate serves the taco or an order fails
    public void OnCustomerServed()
    {
        if (customersInLine.Count > 0)
        {
            GameObject finishedCustomer = customersInLine[0];
            customersInLine.RemoveAt(0);

            isFirstCustomerAtWindow = false; // Reset for the next person in line

            // WALK-AWAY LOGIC: Send NPC to the exit
            NavMeshAgent agent = finishedCustomer.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.ResetPath();
                agent.SetDestination(exitPoint.position);
                agent.speed = 4f;
                agent.stoppingDistance = 0.1f; // Ensure they reach the exact exit point
            }

            // Cleanup: Destroy object once it's far enough from the truck
            StartCoroutine(DestroyAfterReachedExit(finishedCustomer, agent));

            // Move the rest of the queue forward
            UpdateQueue();
        }
    }

    private System.Collections.IEnumerator DestroyAfterReachedExit(GameObject customer, NavMeshAgent agent)
    {
        // Wait a brief moment for the NavMesh to register the NEW destination
        yield return new WaitForSeconds(0.5f);

        while (customer != null && agent != null)
        {
            // Only destroy if the agent has reached the EXIT, not the window
            if (!agent.pathPending && agent.remainingDistance <= destroyDistance)
            {
                break;
            }
            yield return null;
        }

        if (customer != null) Destroy(customer);
    }

    void UpdateQueue()
    {
        for (int i = 0; i < customersInLine.Count; i++)
        {
            NavMeshAgent agent = customersInLine[i].GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.SetDestination(queuePositions[i].position);
            }
        }
    }
}