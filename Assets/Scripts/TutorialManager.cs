using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using ExitGames.Client.Photon.StructWrapping;

public class TutorialManager : NetworkBehaviour
{
    public TextMeshProUGUI taskText;
    public TextMeshProUGUI tutorialText;
    public Slider progressBar;

    private TrainerBallSpawner ballSpawner;
    private BoxSpawner boxSpawner;

    private int currentCount = 0;
    private int taskIndex = 0;
    private string[] tutorialDescriptions = {
        "To bump the ball, hold right mouse, and hold left mouse to control the power. You can control how far the ball will go aiming down",
        "To bump the ball, hold right mouse, and hold left mouse to control the power. You can control how far the ball will go aiming down",
        "To spike the ball, jump with " + KeybindManager.Instance.GetKey("Jump") + " and hold until full to jump higher. Hold left mouse in the air to charge power and release when the ball is close.",
        "Now lets practicee Setting, hold " + KeybindManager.Instance.GetKey("Front_Set") + " to charge the power and release when the ball is close. You can also use " + KeybindManager.Instance.GetKey("Back_Set") + " to set the ball behind you. *tip: the ball will be set in the direction you are looking at*", 
      };
    private string[] taskDescriptions = { "Bump the ball in the green area", "Now, recieve the ball in the setter`s area", "Attack the ball in the green area", "Try setting in the green Area" };
    private int[] taskTargets = { 5, 2, 5, 5 };

    private Vector3[][] greenBoxSpawnPositions = {
        new Vector3[] { new Vector3(-16, 0.5f, -35), new Vector3(-4, 0.5f, -46) },
        new Vector3[] { new Vector3(2, 0.5f, -39), new Vector3(2, 0.5f, -39) },
        new Vector3[] { new Vector3(-16, 0.5f, -35), new Vector3(-4, 0.5f, -46) },
        new Vector3[] { new Vector3(2, 0.5f, -47), new Vector3(2, 0.5f, -47) },
    };

    private Vector3[][] ballSpawn = {
        new Vector3[] { new Vector3(-14, 6, -40), new Vector3(8.4f, 12, 0) },
        new Vector3[] { new Vector3(-14, 6, -40), new Vector3(8.4f, 12, 0) },
        new Vector3[] { new Vector3(2, 4f, -39), new Vector3(0, 13f, -3) },
        new Vector3[] { new Vector3(15, 3f, -39), new Vector3(-5.5f, 13f, 0) },
    };

    void Start()
    {
        ballSpawner = FindFirstObjectByType<TrainerBallSpawner>();
        boxSpawner = FindFirstObjectByType<BoxSpawner>();
        if (ballSpawner == null)
        {
            Debug.LogError("BallSpawner not found in the scene!");
        }
        if (boxSpawner == null)
        {
            Debug.LogError("BoxSpawner not found in the scene!");
        }

        InvokeRepeating(nameof(SpawnBallOnServer), 5f, 5f);
        SpawnGreenBox();
        UpdateTaskUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            OnTaskComplete();
            SpawnGreenBox();
            UpdateTaskUI();
        }
    }

    [Command]
    private void CmdRequestSpawnBall()
    {
        Debug.Log("CmdRequestSpawnBall called on the server.");
        if (ballSpawner != null)
        {
            Debug.Log("Calling CmdSpawnBallTutorialBump on ballSpawner from CmdRequestSpawnBall.");
            ballSpawner.CmdSpawnBallTutorialBump(greenBoxSpawnPositions[taskIndex][0], greenBoxSpawnPositions[taskIndex][1]);
        }
        else
        {
            Debug.LogError("BallSpawner is not available or not running on the server!");
        }
    }

    private void SpawnGreenBox()
    {
        if (boxSpawner != null)
        {
            boxSpawner.SpawnBox(greenBoxSpawnPositions[taskIndex][0], greenBoxSpawnPositions[taskIndex][1]);
        }
    }

    [Server]
    private void SpawnBallOnServer()
    {
        if (isServer)
        {
            Debug.Log("SpawnBall called on the server.");
            if (ballSpawner != null)
            {
                Debug.Log("Calling CmdSpawnBallTutorialBump on ballSpawner.");
                ballSpawner.CmdSpawnBallTutorialBump(ballSpawn[taskIndex][0], ballSpawn[taskIndex][1]);
            }
            else
            {
                Debug.LogError("BallSpawner is not available or not running on the server!");
            }
        }
        else
        {
            Debug.LogWarning("SpawnBall can only be called on the server! Requesting the server to spawn the ball.");
            CmdRequestSpawnBall();
        }
    }

    public void TargetHit()
    {
        if (currentCount < taskTargets[taskIndex])
        {
            currentCount++;
            Debug.Log($"Target hit! Current count: {currentCount}");
            if (currentCount >= taskTargets[taskIndex])
            {
                OnTaskComplete();
            }
            UpdateTaskUI();
        }
        SpawnGreenBox();
    }

    private void UpdateTaskUI()
    {
        tutorialText.text = tutorialDescriptions[taskIndex];
        taskText.text = $"{taskDescriptions[taskIndex]} {currentCount}/{taskTargets[taskIndex]}";
        if (progressBar != null)
        {
            progressBar.value = (float)currentCount / taskTargets[taskIndex];
        }
    }

    private void OnTaskComplete()
    {
        taskIndex++;
        currentCount = 0;
        if(taskIndex >= taskDescriptions.Length)
        {
            taskIndex = 0;
        }
        Debug.Log("Task completed!");
    }
}