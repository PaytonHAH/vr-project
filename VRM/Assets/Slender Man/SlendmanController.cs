using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class SlenderManFollow : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float baseSpeed = 0.5f;
    public float detectionDistance = 1f;
    public float spawnDistance = 10f;
    public float invisibilityDelay = 2f;
    public bool overrideVisibility = false;
    [Range(0f, 180f)] public float fieldOfViewAngle = 60f;
    [Range(0f, 10f)] public float runWhenSeenSpeed = 0.9f;

    [Header("UI Components")]
    public Button connectButton;
    public Button startButton;
    public Button checkObjectCountButton;
    public TextMeshProUGUI stopWatchText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI speedText;

    private float initialYPosition;
    private NetworkVariable<float> currentSpeed = new NetworkVariable<float>(0f);
    private NetworkVariable<float> animatorSpeed = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> isVisible = new NetworkVariable<bool>(true);
    private NetworkVariable<float> stopWatchTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> speedIncreaseTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);
    private bool gameOver = false;
    private Animator animator;

    private List<GameObject> unityCreatorObjects = new List<GameObject>();

    void Start()
    {
        connectButton.onClick.AddListener(ConnectToGame);
        startButton.interactable = false;

        startButton.onClick.AddListener(() =>
        {
            Debug.Log("Start Button Clicked");
            StartGameServerRpc();
        });

        checkObjectCountButton.onClick.AddListener(CheckObjectCount);

        if (IsServer)
        {
            initialYPosition = transform.position.y;
            currentSpeed.Value = baseSpeed;
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }

        animator = GetComponent<Animator>();
        gameOverText.gameObject.SetActive(false);
        FindUnityCreatorObjects();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }
    }

    public void ConnectToGame()
    {
        if (IsServer)
        {
            Debug.Log("Server is ready.");
        }
        else
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Client is connecting...");
        }

        startButton.interactable = true;
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            if (playerObject != null)
            {
                StartCoroutine(DelayedUnityCreatorSearch(playerObject, clientId));
            }
        }
    }

    private IEnumerator DelayedUnityCreatorSearch(GameObject playerObject, ulong clientId)
    {
        yield return new WaitForSeconds(1f);
        FindUnityCreatorObjects();
        Debug.Log("Total Unity Creator objects detected after connection: " + unityCreatorObjects.Count);
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            if (playerObject != null)
            {
                FindUnityCreatorObjects();
                Debug.Log("Unity Creator object list updated after player disconnect.");
            }
        }
    }

    private void CheckObjectCount()
    {
        Debug.Log("Number of Unity Creator objects currently detected: " + unityCreatorObjects.Count);
    }

    public void FindUnityCreatorObjects()
    {
        unityCreatorObjects.Clear();
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Unity Creator")
            {
                unityCreatorObjects.Add(obj);
                Debug.Log("Unity Creator object found: " + obj.name);
            }
        }

        Debug.Log("Total Unity Creator objects detected: " + unityCreatorObjects.Count);
    }

    void Update()
    {
        if (isGameActive.Value && !gameOver)
        {
            if (IsServer)
            {
                if (unityCreatorObjects != null && unityCreatorObjects.Count > 0)
                {
                    GameObject closestObject = GetClosestUnityCreator();
                    if (closestObject != null)
                    {
                        float distanceToObject = Vector3.Distance(transform.position, closestObject.transform.position);
                        Debug.Log("Distance to target: " + distanceToObject + ", Detection Distance: " + detectionDistance);

                        if (distanceToObject <= detectionDistance)
                        {
                            GameOver();
                        }

                        UpdateTimers(distanceToObject);

                        if (currentSpeed.Value > 0)
                        {
                            MoveTowardsObject(closestObject);
                        }

                        transform.position = new Vector3(transform.position.x, initialYPosition, transform.position.z);
                        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

                        UpdateAnimations();
                    }
                }
            }

            if (IsClient)
            {
                animator.SetFloat("Speed", animatorSpeed.Value);
            }
        }

        if (IsClient)
        {
            stopWatchText.text = "Time: " + Mathf.FloorToInt(stopWatchTimer.Value) + "s";
            speedText.text = "Speed: " + currentSpeed.Value.ToString("F1");
        }
    }

    private GameObject GetClosestUnityCreator()
    {
        GameObject closestObject = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject obj in unityCreatorObjects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestObject = obj;
            }
        }

        return closestObject;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        Debug.Log("StartGameServerRpc triggered");

        isGameActive.Value = true;
        gameOver = false;

        HideGameOverClientRpc();

        stopWatchTimer.Value = 0f;
        currentSpeed.Value = baseSpeed;
        speedIncreaseTimer.Value = 0f;

        animator.SetFloat("Speed", 0.16f);
        SpawnSlenderMan();
    }

    [ClientRpc]
    private void HideGameOverClientRpc()
    {
        gameOverText.gameObject.SetActive(false);
    }

    private void SpawnSlenderMan()
    {
        if (unityCreatorObjects == null || unityCreatorObjects.Count == 0)
        {
            Debug.LogError("No Unity Creator objects found to spawn Slender Man.");
            return;
        }

        GameObject closestObject = GetClosestUnityCreator();
        if (closestObject == null)
        {
            Debug.LogError("No closest Unity Creator object found to spawn Slender Man.");
            return;
        }

        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        randomDirection.y = 0;

        Vector3 spawnPosition = closestObject.transform.position + randomDirection * spawnDistance;
        transform.position = new Vector3(spawnPosition.x, initialYPosition, spawnPosition.z);

        Debug.Log("Slender Man spawned at: " + transform.position);
    }

    private void MoveTowardsObject(GameObject targetObject)
    {
        if (currentSpeed.Value > 0)
        {
            Vector3 directionToObject = targetObject.transform.position - transform.position;
            Vector3 movement = directionToObject.normalized * currentSpeed.Value * Time.deltaTime;
            transform.position += movement;

            Quaternion targetRotation = Quaternion.LookRotation(directionToObject);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * baseSpeed);
        }
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            float speedValue;
            if (currentSpeed.Value == 0)
            {
                speedValue = 0f;
            }
            else if (currentSpeed.Value > 0 && currentSpeed.Value <= 0.16f)
            {
                speedValue = 0.16f;
            }
            else if (currentSpeed.Value > 0.16f && currentSpeed.Value <= 0.5f)
            {
                speedValue = 0.5f;
            }
            else
            {
                speedValue = 1f;
            }

            animatorSpeed.Value = speedValue;
        }
    }

    private void UpdateTimers(float distanceToObject)
    {
        if (IsServer)
        {
            stopWatchTimer.Value += Time.deltaTime;
            speedIncreaseTimer.Value += Time.deltaTime;

            if (speedIncreaseTimer.Value >= 30f)
            {
                currentSpeed.Value += 1f;
                speedIncreaseTimer.Value = 0f;
            }
        }
    }

    private void GameOver()
    {
        if (IsServer)
        {
            isGameActive.Value = false;
            gameOver = true;
            Debug.Log("Game Over! Time survived: " + Mathf.FloorToInt(stopWatchTimer.Value) + " seconds.");

            ShowGameOverClientRpc();
        }
    }

    [ClientRpc]
    private void ShowGameOverClientRpc()
    {
        gameOverText.gameObject.SetActive(true);
        Debug.Log("Game Over text shown for all players.");
    }

    void OnDrawGizmos()
    {
        if (unityCreatorObjects != null && unityCreatorObjects.Count > 0)
        {
            foreach (var obj in unityCreatorObjects)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(obj.transform.position, transform.position);
            }
        }
    }
}
