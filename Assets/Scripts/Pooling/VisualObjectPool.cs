using System.Collections.Generic;
using UnityEngine;

public class VisualObjectPool
{
    private readonly Queue<GameObject> _available = new Queue<GameObject>();
    private readonly List<GameObject> _active = new List<GameObject>();
    private readonly GameObject _prefab;
    private readonly Transform _root;
    private readonly string _objectName;

    public VisualObjectPool(GameObject prefab, Transform root, string objectName)
    {
        _prefab = prefab;
        _root = root;
        _objectName = objectName;
    }

    public GameObject Get(Vector3 position, Vector3 scale)
    {
        GameObject instance;
        if (_available.Count > 0)
        {
            instance = _available.Dequeue();
        }
        else if (_prefab != null)
        {
            instance = Object.Instantiate(_prefab, _root);
            instance.name = _objectName;
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = _objectName;
            instance.transform.SetParent(_root, false);
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        instance.transform.SetParent(_root, false);
        instance.transform.position = position;
        instance.transform.localScale = scale;
        instance.SetActive(true);
        _active.Add(instance);
        return instance;
    }

    public void ReturnAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Return(_active[i]);
        }
    }

    private void Return(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        _active.Remove(instance);
        instance.SetActive(false);
        instance.transform.SetParent(_root, false);
        _available.Enqueue(instance);
    }
}
