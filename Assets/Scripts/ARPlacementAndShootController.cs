using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ARPlacementAndShootController : MonoBehaviour
{
    public GameObject panierPrefab;
    public GameObject ballonPrefab;
    [SerializeField] private Button validerButton;

    private GameObject spawnedPanier;
    private GameObject currentBallon;
    private bool canShoot = true;
    private Vector2 startTouchPosition, endTouchPosition;
    private float startTime, endTime;

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isPlacementMode = true;

    private void Awake()
    {
        _arRaycastManager = GetComponent<ARRaycastManager>();
        _arPlaneManager = GetComponent<ARPlaneManager>();
        validerButton.gameObject.SetActive(true);
        validerButton.onClick.AddListener(OnValidateButtonClicked);
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Ignore les interactions sur l'UI
            if (UIBlocker.IsPointerOverUI(touch.position))
                return;

            if (isPlacementMode)
            {
                Vector2 touchPosition = touch.position;

                if (_arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;

                    if (spawnedPanier == null)
                    {
                        spawnedPanier = Instantiate(
                            panierPrefab,
                            hitPose.position,
                            hitPose.rotation * Quaternion.Euler(-90, 0, 0)
                        );
                    }
                    else
                    {
                        spawnedPanier.transform.position = hitPose.position;
                        spawnedPanier.transform.rotation = hitPose.rotation * Quaternion.Euler(-90, 0, 0);
                    }
                }
            }
            else
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        startTouchPosition = touch.position;
                        startTime = Time.time;
                        break;

                    case TouchPhase.Ended:
                        endTouchPosition = touch.position;
                        endTime = Time.time;

                        Vector2 swipe = endTouchPosition - startTouchPosition;
                        float swipeDuration = endTime - startTime;

                        if (canShoot && swipe.magnitude > 100f)
                        {
                            LaunchBall(swipe, swipeDuration);
                        }
                        break;
                }
            }
        }

        if (!isPlacementMode && (currentBallon == null))
        {
            SpawnBallonEnBas();
        }
    }

    void OnValidateButtonClicked()
    {
        isPlacementMode = false;
        validerButton.gameObject.SetActive(false);

        _arPlaneManager.enabled = false;
        foreach (var plane in _arPlaneManager.trackables)
            plane.gameObject.SetActive(false);
    }

    void SpawnBallonEnBas()
    {
        Vector3 screenPosition = new Vector3(Screen.width / 2f, Screen.height * 0.1f, 0.5f);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

        currentBallon = Instantiate(ballonPrefab, worldPosition, Quaternion.identity);

        Rigidbody rb = currentBallon.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void LaunchBall(Vector2 swipe, float duration)
    {
        if (duration <= 0f) duration = 0.1f; // éviter division par zéro

        Rigidbody rb = currentBallon.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        // Calculer une direction réaliste du tir
        Vector3 swipeDirection = new Vector3(swipe.x, swipe.y * 1.5f, 1f);
        swipeDirection = Camera.main.transform.TransformDirection(swipeDirection.normalized);

        // Calculer la force en fonction de la vitesse du swipe
        float swipeSpeed = swipe.magnitude / duration;
        float forceMultiplier = 0.0015f;

        Vector3 force = swipeDirection * swipeSpeed * forceMultiplier;

        rb.AddForce(force, ForceMode.Impulse);

        canShoot = false;
        Invoke(nameof(ResetShootCooldown), 3f);
    }

    void ResetShootCooldown()
    {
        canShoot = true;
        currentBallon = null;
    }
}
