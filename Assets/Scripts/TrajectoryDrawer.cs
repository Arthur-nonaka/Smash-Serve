using UnityEngine;
using Mirror;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryDrawer : NetworkBehaviour
{
    public Rigidbody ballRigidbody;

    public int resolution = 100000;
    public float timeStep = 1000f;
    public float displayDuration = 4f;
    public Vector3 trajectoryOffset = Vector3.zero;

    private LineRenderer lineRenderer;
    private bool showTrajectory = false;
    private Vector3[] trajectoryPositions;

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        if (ballRigidbody == null)
        {
            ballRigidbody = GetComponentInParent<Rigidbody>();

            if (ballRigidbody == null)
            {
                Transform currentTransform = transform;
                while (currentTransform.parent != null && ballRigidbody == null)
                {
                    currentTransform = currentTransform.parent;
                    ballRigidbody = currentTransform.GetComponent<Rigidbody>();
                }
            }

            if (ballRigidbody == null)
            {
                Debug.LogError("TrajectoryDrawer: Could not find a Rigidbody component in any parent object!");
            }
        }

        trajectoryPositions = new Vector3[resolution];
    }

    void Update()
    {
        if (showTrajectory)
        {
            // Recalculate trajectory from current position
            if (isServer)
            {
                CalculateTrajectory();
                RpcUpdateTrajectory(trajectoryPositions);
            }
            else if (isClient && isLocalPlayer)
            {
                CmdUpdateTrajectory();
            }

            DisplayTrajectory();
        }
    }

    public void TriggerTrajectoryDisplay()
    {
        if (isServer)
        {
            // Calculate trajectory on server
            CalculateTrajectory();
            // Send to all clients
            RpcShowTrajectory(trajectoryPositions);
        }
        else
        {
            CmdTriggerTrajectoryDisplay();
        }
    }

    [Command]
    public void CmdTriggerTrajectoryDisplay()
    {
        // Calculate trajectory on server
        CalculateTrajectory();
        // Send to all clients
        RpcShowTrajectory(trajectoryPositions);
    }

    [Command]
    public void CmdUpdateTrajectory()
    {
        CalculateTrajectory();
        RpcUpdateTrajectory(trajectoryPositions);
    }

    // Calculate the trajectory but don't display it yet
    void CalculateTrajectory()
    {
        if (ballRigidbody == null) return;

        Vector3 startPosition = ballRigidbody.position + trajectoryOffset;
        Vector3 initialVelocity = ballRigidbody.linearVelocity; // Changed from linearVelocity to velocity

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            trajectoryPositions[i] = startPosition + initialVelocity * t + 0.5f * Physics.gravity * t * t;
        }
    }

    // Send the pre-calculated positions to clients
    [ClientRpc]
    void RpcShowTrajectory(Vector3[] positions)
    {
        if (positions.Length != resolution)
        {
            Debug.LogError("TrajectoryDrawer: Received positions array has incorrect length!");
            return;
        }

        trajectoryPositions = positions;
        ShowTrajectory();
    }

    // Update trajectory positions on clients
    [ClientRpc]
    void RpcUpdateTrajectory(Vector3[] positions)
    {
        if (positions.Length != resolution)
        {
            Debug.LogError("TrajectoryDrawer: Received positions array has incorrect length!");
            return;
        }

        trajectoryPositions = positions;
    }

    // Just display the trajectory using the positions we received
    void DisplayTrajectory()
    {
        lineRenderer.positionCount = resolution;
        lineRenderer.SetPositions(trajectoryPositions);
    }

    private void ShowTrajectory()
    {
        showTrajectory = true;
        lineRenderer.enabled = true;

        StopAllCoroutines();
        StartCoroutine(HideTrajectoryAfterDelay(displayDuration));
    }

    public void TriggerTrajectoryDisable()
    {
        if (isServer)
        {
            RpcTriggerTrajectoryDisable();
        }
        else
        {
            CmdTriggerTrajectoryDisable();
        }
    }

    [Command]
    public void CmdTriggerTrajectoryDisable()
    {
        RpcTriggerTrajectoryDisable();
    }

    [ClientRpc]
    void RpcTriggerTrajectoryDisable()
    {
        showTrajectory = false;
        lineRenderer.enabled = false;
    }

    System.Collections.IEnumerator HideTrajectoryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        showTrajectory = false;
        lineRenderer.enabled = false;
    }
}