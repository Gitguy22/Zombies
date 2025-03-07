using UnityEngine;

public interface IInteractable
{
    void BuyItem(GameObject player);
    string GetItemName();
    int GetCost();
    bool IsPaidFor();
}