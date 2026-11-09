using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
// Exigim també l'ARAnchorManager, obligatori a Unity 6 per poder generar àncores space-tracking
[RequireComponent(typeof(ARAnchorManager))]
public class AnchorModel2Marc2 : MonoBehaviour
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
    private ARAnchorManager _anchorManager;

    // Conjunt per recordar quins marcadors ja han estat ancorats a l'espai permanentment
    private HashSet<string> _anchoredImages = new HashSet<string>();

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
        _anchorManager = GetComponent<ARAnchorManager>();

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
            //Destroy(hit.collider.gameObject);
            Renderer renderer = hit.collider.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = new Color(
                Random.value,
                Random.value,
                Random.value
                );
            }
        }
    }

    private void ProcessTrackedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Comprovem si ARCore té un rastreig segur i és la PRIMERA VEGADA que ancorem aquesta imatge
        if (trackedImage.trackingState == TrackingState.Tracking && !_anchoredImages.Contains(imageName))
        {
            GameObject prefabToSpawn = null;

            // Triem el prefab que toca segons el nom del marcador detectat
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


            if (prefabToSpawn != null)
            {
                // 1. Bloquegem el marcador per a que no es torni a repetir el procés d'ancoratge
                _anchoredImages.Add(imageName);
                UnityEngine.Debug.Log($"📌 Marcador '{imageName}' detectat estable. Fixant àncora permanent a l'espai...");

                // 2. Creem un node buit com a base de l'àncora a les coordenades físiques del marcador actual
                GameObject anchorGo = new GameObject($"Anchor_{imageName}");
                anchorGo.transform.position = trackedImage.transform.position;
                anchorGo.transform.rotation = trackedImage.transform.rotation;

                // 3. Convertim el node en una àncora espacial oficial de Unity 6 / ARCore
                ARAnchor newAnchor = anchorGo.AddComponent<ARAnchor>();

                if (newAnchor != null)
                {
                    // 4. Instanciem el model 3D final com a fill d'aquest punt geogràfic fixat
                    GameObject newModel = Instantiate(prefabToSpawn, anchorGo.transform);
                    newModel.transform.localPosition = Vector3.zero;
                    newModel.transform.localRotation = Quaternion.identity;
                    newModel.SetActive(true);

                    UnityEngine.Debug.Log($"🛋️ S'ha clavat el model a l'entorn real per a: {imageName}. El paper ja no és necessari.");
                }
                else
                {
                    UnityEngine.Debug.LogError($"❌ Error greu: No s'ha pogut afegir el component ARAnchor a {imageName}.");
                    _anchoredImages.Remove(imageName); // Deixem la porta oberta per reintentar si falla el sistema
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"⚠️ Marcador '{imageName}' detectat, però no té cap prefab assignat a l'Inspector.");
            }
        }
    }
}
