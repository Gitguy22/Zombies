using System.Collections.Generic;
using UnityEngine;

public class ElectricityObjectPool
{
    private Stack<GameObject> pool = new Stack<GameObject>();
    private System.Func<GameObject> createFunc;
    private System.Action<GameObject> onGet;
    private System.Action<GameObject> onRelease;
    private System.Action<GameObject> onDestroy;

    public ElectricityObjectPool(System.Func<GameObject> createFunc,
                                System.Action<GameObject> onGet = null,
                                System.Action<GameObject> onRelease = null,
                                System.Action<GameObject> onDestroy = null,
                                bool prewarm = false,
                                int prewarmCount = 0,
                                int maxSize = 100)
    {
        this.createFunc = createFunc;
        this.onGet = onGet;
        this.onRelease = onRelease;
        this.onDestroy = onDestroy;

        if (prewarm)
        {
            for (int i = 0; i < prewarmCount; i++)
            {
                Release(createFunc());
            }
        }
    }

    public GameObject Get()
    {
        GameObject item = pool.Count > 0 ? pool.Pop() : createFunc();
        onGet?.Invoke(item);
        return item;
    }

    public void Release(GameObject item)
    {
        onRelease?.Invoke(item);
        pool.Push(item);
    }

    public int Count => pool.Count;

    public void Clear()
    {
        while (pool.Count > 0)
        {
            GameObject item = pool.Pop();
            onDestroy?.Invoke(item);
        }
    }
}