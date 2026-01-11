using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Camera mainCamera;

    [Header("Movement Settings")]
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private float lookRotationSpeed = 8f;
    [SerializeField] private float maxTapMovement = 20f;

    [Header("Tutorial Reference")]
    [SerializeField] private Tutorial tutorial;

    private Vector2 touchStartPos;
    public static bool IsInputLocked = false;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (IsInputLocked)
        {
            agent.isStopped = true; // stop moving
            animator.Play("Idle");
            return;
        }
        else
        {
            if (agent != null && agent.isStopped)
                agent.isStopped = false;
        }
        HandleInput();
        FaceTarget();
        UpdateAnimationState();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string hitLayer = LayerMask.LayerToName(hit.collider.gameObject.layer);

                // Ignore input during tutorial text
                if (tutorial.tutorialTextActive) return;

                // Ignore interactables
                if (hitLayer == "Interactable") return;

                // Move on ground
                if (hitLayer == "Ground")
                {
                    MoveAgent(hit.point);
                    return;
                }
            }
        }
    }

    private void MoveAgent(Vector3 destination)
    {
        if (agent == null) return;

        agent.SetDestination(destination);

        if (clickEffect != null)
            Instantiate(clickEffect, destination + Vector3.up * 0.1f, Quaternion.identity);
    }

    private void FaceTarget()
    {
        if (agent == null) return;

        Vector3 direction = (agent.destination - transform.position);
        direction.y = 0;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * lookRotationSpeed
        );
    }

    private void UpdateAnimationState()
    {
        if (animator == null || agent == null) return;

        if (agent.velocity.sqrMagnitude < 0.01f)
            animator.Play("Idle");
        else
            animator.Play("Walk");
    }
}
