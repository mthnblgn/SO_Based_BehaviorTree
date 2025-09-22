using System.Collections.Generic;
using UnityEngine;

namespace Game.Patterns
{
    /// <summary>
    /// Generic lightweight object pool for Components.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Queue<T> pool = new Queue<T>();
        private readonly Transform parent;

        public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = UnityEngine.Object.Instantiate(prefab, parent);
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = UnityEngine.Object.Instantiate(prefab, parent);
            }
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void ReturnToPool(T obj)
        {
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
