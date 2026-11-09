using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class Marcador2Model2 : MonoBehaviour
{
    [Header("Prefabs de Mobles (Objectes 3D)")]
    [Tooltip("El model 3D que apareixerà sobre el marcador 'sofaMarker'")]
    [SerializeField] private GameObject sofaPrefab;

    [Tooltip("El model 3D que apareixerà sobre el marcador 'lamparaMarker'")]
    [SerializeField] private GameObject lamparaPrefab;

    [Tooltip("El model 3D que apareixerà sobre el marcador 'gabineteMarker'")]
    [SerializeField] private GameObject gabinetePrefab;

    [Tooltip("El model 3D que apareixerà sobre el marcador 'chairMarker'")]
    [SerializeField] private GameObject chairPrefab;

    private ARTrackedImageManager _trackedImageManager;

    // Diccionari per fer el seguiment de cada objecte 3D instanciat de manera independent
    private Dictionary<string, GameObject> _spawnedModels = new Dictionary<string, GameObject>();

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();

        // Ens assegurem que ARCore pugui rastrejar com a mínim 2 marcadors alhora en pantalla
        _trackedImageManager.requestedMaxNumberOfMovingImages = 2;
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
            ProcessTrackedImage(trackedImage);
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            ProcessTrackedImage(trackedImage);
        }
    }

    private void ProcessTrackedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            // Si la imatge es detecta i encara no hem creat el seu objecte 3D...
            if (!_spawnedModels.ContainsKey(imageName))
            {
                GameObject prefabToSpawn = null;

                // Triem quin prefab utilitzar segons el nom exacte de la imatge a la llibreria
                if (imageName == "sofaMarker")
                {
                    prefabToSpawn = sofaPrefab;
                }
                else if (imageName == "lamparaMarker")
                {
                    prefabToSpawn = lamparaPrefab;
                }
                else if (imageName == "gabineteMarker")
                {
                    prefabToSpawn = gabinetePrefab;
                } 
                else if (imageName == "chairMarker")
                {
                    prefabToSpawn = chairPrefab;
                }

                // Si hem trobat una coincidència de nom i tenim el prefab assignat, l'instanciem
                if (prefabToSpawn != null)
                {
                    GameObject newModel = Instantiate(prefabToSpawn, trackedImage.transform);
                    newModel.transform.localPosition = Vector3.zero;
                    newModel.transform.localRotation = Quaternion.identity;

                    _spawnedModels.Add(imageName, newModel);
                    UnityEngine.Debug.Log($"🛋️ S'ha instanciat el model corresponent per al marcador: {imageName}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"⚠️ Es detecta el marcador '{imageName}', però no té cap prefab associat al codi o està buit a l'Inspector.");
                }
            }

            // Si l'objecte ja estava creat, ens assegurem que es mostra de manera visible
            if (_spawnedModels.TryGetValue(imageName, out GameObject model))
            {
                model.SetActive(true);
            }
        }
        else
        {
            // Si es perd el rastreig d'aquest marcador en concret, ocultem només el seu objecte
            if (_spawnedModels.TryGetValue(imageName, out GameObject model))
            {
                model.SetActive(false);
            }
        }
    }
}
