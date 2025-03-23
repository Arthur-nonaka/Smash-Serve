using UnityEngine;
using Mirror;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryDrawer : NetworkBehaviour
{
    public Rigidbody ballRigidbody;

    public int resolution = 15;

    public float timeStep = 0.14f;
    public float displayDuration = 3f;

    public Vector3 trajectoryOffset = Vector3.zero;

    private LineRenderer lineRenderer;

    private bool showTrajectory = false;

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (showTrajectory)
        {
            DrawTrajectory();
        }
    }

    [Command]
    public void CmdTriggerTrajectoryDisplay(Vector3 initialPosition, Vector3 initialVelocity)
    {
        RpcTriggerTrajectoryDisplay(initialPosition, initialVelocity);
    }

    [ClientRpc]
    void RpcTriggerTrajectoryDisplay(Vector3 initialPosition, Vector3 initialVelocity)
    {
        showTrajectory = true;
        lineRenderer.enabled = true;

        StopAllCoroutines();
        StartCoroutine(HideTrajectoryAfterDelay(displayDuration));

        ballRigidbody.position = initialPosition;
        ballRigidbody.angularVelocity = initialVelocity;
    }

    public void TriggerTrajectoryDisplay()
    {
        if (isServer)
        {
            Vector3 initialPosition = ballRigidbody.position + trajectoryOffset;
            Vector3 initialVelocity = ballRigidbody.angularVelocity;
            RpcTriggerTrajectoryDisplay(initialPosition, initialVelocity);
        }
        else
        {
            Vector3 initialPosition = ballRigidbody.position + trajectoryOffset;
            Vector3 initialVelocity = ballRigidbody.angularVelocity;
            CmdTriggerTrajectoryDisplay(initialPosition, initialVelocity);
        }
    }

    public void TriggerTrajectoryDisable()
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

    void DrawTrajectory()
    {
        if (ballRigidbody == null)
            return;

        Vector3[] positions = new Vector3[resolution];

        Vector3 startPosition = ballRigidbody.position + trajectoryOffset;

        Vector3 initialVelocity = ballRigidbody.linearVelocity;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            Vector3 pos = startPosition + initialVelocity * t + 0.5f * Physics.gravity * t * t;
            positions[i] = pos;
        }

        lineRenderer.positionCount = resolution;
        lineRenderer.SetPositions(positions);
    }
}
