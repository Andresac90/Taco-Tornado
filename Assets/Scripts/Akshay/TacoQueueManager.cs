using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class TacoQueueManager : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform[] queuePositions; // Drag Point_1, Point_2, Point_3 here
    private List<GameObject> customersInLine = new List<GameObject>();

    void Update()
    {
        // Press Space to simulate a new customer arriving
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnCustomer();
        }

        // Press Enter to "Serve" the first customer
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ServeCustomer();
        }
    }

    void SpawnCustomer()
    {
        if (customersInLine.Count < queuePositions.Length)
        {
            // Spawn at a distance (e.g., 5 meters back from the last point)
            Vector3 spawnPos = queuePositions[queuePositions.Length - 1].position + Vector3.back * 5;
            GameObject newCustomer = Instantiate(customerPrefab, spawnPos, Quaternion.identity);

            customersInLine.Add(newCustomer);
            UpdateQueue();
        }
    }

    public void ServeCustomer()
    {
        if (customersInLine.Count > 0)
        {
            GameObject finishedCustomer = customersInLine[0];
            customersInLine.RemoveAt(0);

            // For now, just destroy them. Later, we can make them walk away.
            Destroy(finishedCustomer);
            UpdateQueue();
        }
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