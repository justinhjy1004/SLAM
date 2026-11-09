using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
[RequireComponent(typeof(ARAnchorManager))]
public class AncorModelInteractive : MonoBehaviour
{
    [Header("3D Furniture Prefabs")]
    [SerializeField] private GameObject sofaPrefab;
    [SerializeField] private GameObject lamparaPrefab;
    [SerializeField] private GameObject gabinetePrefab;
    [SerializeField] private GameObject chairPrefab;

    private ARTrackedImageManager _trackedImageManager;
    private ARAnchorManager _anchorManager;

    private Dictionary<string, GameObject> _spawnedObjects = new Dictionary<string, GameObject>();
    private HashSet<string> _anchoredImages = new HashSet<string>();

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
        _anchorManager = GetComponent<ARAnchorManager>();
        _trackedImageManager.requestedMaxNumberOfMovingImages = 4;
    }

    private void OnEnable()
    {
        _trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            UpdateTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        foreach (var pair in eventArgs.removed)
        {
            ARTrackedImage trackedImage = pair.Value;
            if (trackedImage != null && trackedImage.referenceImage != null)
            {
                string imageName = trackedImage.referenceImage.name;
                if (!_anchoredImages.Contains(imageName) && _spawnedObjects.TryGetValue(imageName, out GameObject obj))
                {
                    Destroy(obj);
                    _spawnedObjects.Remove(imageName);
                }
            }
        }
    }

    private void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Skip marker updates if the model has already been locked into the real world
        if (_anchoredImages.Contains(imageName))
            return;

        // 1. Create the instance unparented in the scene root
        if (!_spawnedObjects.ContainsKey(imageName))
        {
            GameObject prefabToSpawn = GetPrefabForImage(imageName);
            if (prefabToSpawn != null)
            {
                GameObject newModel = Instantiate(prefabToSpawn, trackedImage.transform.position, trackedImage.transform.rotation);
                newModel.name = $"Model_{imageName}";
                _spawnedObjects[imageName] = newModel;
            }
        }

        // 2. Actively sync pose and visibility with the moving QR marker
        if (_spawnedObjects.TryGetValue(imageName, out GameObject model))
        {
            bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
            model.SetActive(isTracking);

            if (isTracking)
            {
                // Force absolute world pose to match the QR's latest pose
                model.transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);
            }
        }
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
            return;

        Ray ray = Camera.main.ScreenPointToRay(touch.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            foreach (var pair in _spawnedObjects)
            {
                string imageName = pair.Key;
                GameObject model = pair.Value;

                if (_anchoredImages.Contains(imageName))
                    continue;

                // Check if tapped collider is part of this model hierarchy
                if (hit.transform == model.transform || hit.transform.IsChildOf(model.transform))
                {
                    LockModelInPlace(imageName, model);
                    break;
                }
            }
        }
    }

    private void LockModelInPlace(string imageName, GameObject model)
    {
        Vector3 worldPos = model.transform.position;
        Quaternion worldRot = model.transform.rotation;

        // Create an independent spatial anchor at current pose
        GameObject anchorGo = new GameObject($"Anchor_{imageName}");
        anchorGo.transform.SetPositionAndRotation(worldPos, worldRot);

        ARAnchor newAnchor = anchorGo.AddComponent<ARAnchor>();

        if (newAnchor != null)
        {
            model.transform.SetParent(anchorGo.transform, true);
            _anchoredImages.Add(imageName);
            Debug.Log($"[AR] Object permanently anchored: {imageName}");

            Renderer rend = model.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.green;
            }
        }
        else
        {
            Debug.LogError($"[AR] Failed to add ARAnchor component to {imageName}");
        }
    }

    private GameObject GetPrefabForImage(string imageName)
    {
        switch (imageName)
        {
            case "sofaMarker": return sofaPrefab;
            case "lamparaMarker": return lamparaPrefab;
            case "gabineteMarker": return gabinetePrefab;
            case "chairMarker": return chairPrefab;
            default: return null;
        }
    }

    private TrackableId GetTrackableIdForName(string imageName)
    {
        foreach (ARTrackedImage trackable in _trackedImageManager.trackables)
        {
            if (trackable.referenceImage.name == imageName)
                return trackable.trackableId;
        }
        return TrackableId.invalidId;
    }
}