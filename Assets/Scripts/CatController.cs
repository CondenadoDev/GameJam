using UnityEngine;
using UnityEngine.InputSystem;

public class CatController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Cat Abilities")]
    [SerializeField] private float purringRange = 10f;
    [SerializeField] private float concentrationRange = 15f;
    [SerializeField] private bool isPurring = false;
    [SerializeField] private bool isConcentrating = false;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction purringAction;
    private InputAction concentrationAction;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        
        moveAction = playerInput.actions["Move"];
        purringAction = playerInput.actions["Purring"];
        concentrationAction = playerInput.actions["Concentration"];
    }
    
    void OnEnable()
    {
        purringAction.performed += OnPurringPerformed;
        purringAction.canceled += OnPurringCanceled;
        
        concentrationAction.performed += OnConcentrationPerformed;
        concentrationAction.canceled += OnConcentrationCanceled;
    }
    
    void OnDisable()
    {
        purringAction.performed -= OnPurringPerformed;
        purringAction.canceled -= OnPurringCanceled;
        
        concentrationAction.performed -= OnConcentrationPerformed;
        concentrationAction.canceled -= OnConcentrationCanceled;
    }
    
    void Update()
    {
        HandleMovementInput();
    }
    
    void FixedUpdate()
    {
        HandleMovement();
    }
    
    void HandleMovementInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }
    
    void HandleMovement()
    {
        Vector2 velocity = moveInput * moveSpeed;
        rb.linearVelocity = velocity;
    }
    
    void OnPurringPerformed(InputAction.CallbackContext context)
    {
        isPurring = true;
        Debug.Log("Gato maullando - distrayendo guardias");
        
        DistractNearbyGuards();
    }
    
    void OnPurringCanceled(InputAction.CallbackContext context)
    {
        isPurring = false;
        Debug.Log("Gato dejó de maullar");
    }
    
    void OnConcentrationPerformed(InputAction.CallbackContext context)
    {
        isConcentrating = true;
        Debug.Log("Gato concentrándose - detectando enemigos");
    }
    
    void OnConcentrationCanceled(InputAction.CallbackContext context)
    {
        isConcentrating = false;
        Debug.Log("Gato dejó de concentrarse");
    }
    
    void DistractNearbyGuards()
    {
        GameObject[] guards = GameObject.FindGameObjectsWithTag("Guard");
        
        foreach (GameObject guard in guards)
        {
            float distance = Vector2.Distance(transform.position, guard.transform.position);
            
            if (distance <= purringRange)
            {
                GuardController guardController = guard.GetComponent<GuardController>();
                if (guardController != null)
                {
                    guardController.GetDistracted(transform.position);
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (isPurring)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, purringRange);
        }
        
        if (isConcentrating)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, concentrationRange);
        }
    }
}