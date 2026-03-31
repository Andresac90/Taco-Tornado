using UnityEngine;
using System.Collections.Generic;
using TacoTornado;

public class LogicTester : MonoBehaviour
{
    public void SimulatePerfectTaco()
    {
        if (OrderManager.Instance.activeOrders.Count > 0)
        {
            // Get the exact requirements of the current first order
            TacoOrder currentOrder = OrderManager.Instance.activeOrders[0];
            List<IngredientType> perfectList = currentOrder.GetAllRequired();

            // Submit it to your manager to test the score
            OrderManager.Instance.TrySubmitTaco(perfectList);

            // Tell the queue to move forward
            FindObjectOfType<TacoQueueManager>().OnCustomerServed();
        }
    }

    public void SimulateFailedOrder()
    {
        if (OrderManager.Instance.activeOrders.Count > 0)
        {
            // Get the current order at the window
            TacoOrder currentOrder = OrderManager.Instance.activeOrders[0];

            // Force the GameManager to fail it
            GameManager.Instance.FailOrder(currentOrder);

            // Remove it from the OrderManager list
            OrderManager.Instance.activeOrders.Remove(currentOrder);

            // Tell the queue to move the NPC away
            FindObjectOfType<TacoQueueManager>().OnCustomerServed();

            Debug.Log("[Test] Manual Order Failure Triggered.");
        }
    }
}