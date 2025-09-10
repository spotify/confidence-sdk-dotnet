using System;
using System.Collections.Generic;
using UnityEngine;
using UnityOpenFeature.Client;
using UnityOpenFeature.Core;
using UnityOpenFeature.Providers;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private float groundCheckRadius = 0.2f;

    // OpenFeature provider
    private ConfidenceProvider provider;
    private IFeatureClient client;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Initialize OpenFeature provider
        provider = new ConfidenceProvider("CLIENT_SECRET");
        try
        {
            Debug.Log("Attempting to initialize OpenFeature API...");
            var apiInstance = OpenFeatureAPI.Instance;
            if (apiInstance == null)
            {
                Debug.LogError("OpenFeatureAPI.Instance is null");
                client = null;
                return;
            }
            Debug.Log("OpenFeatureAPI.Instance is available");
            apiInstance.SetEvaluationContext(new EvaluationContext().SetAttribute("user_id", "fdema"));
            client = apiInstance.GetClient();

            apiInstance.SetProvider(provider, (success, error) =>
            {
                if (!success)
                {
                    Debug.LogError($"Error initializing provider and fetching flags: {error}");
                    return;
                }
                Debug.Log($"Provider initialized and flags fetched successfully!: {success}");
                var value = client.GetBooleanDetails("vahid-test.enabled", false);
                Debug.Log("Flag value: " + value.Value);
                Debug.Log("Flag variant: " + value.Variant);
                Debug.Log("Error code: " + value.ErrorCode);
                Debug.Log("Error message: " + value.ErrorMessage);
            });

            Debug.Log("OpenFeature provider initialization started");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize OpenFeature: {ex.Message}");
            Debug.LogError($"Exception type: {ex.GetType().Name}");
            client = null;
        }
    }
    
    void Update()
    {
        Move();
        Jump();
    }

    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");

        // Ask for boolean value from OpenFeature provider for double speed
        float currentSpeed = moveSpeed;
        if (client != null)
        {
            currentSpeed = true ? moveSpeed * 8f : moveSpeed;
        }

        rb.velocity = new Vector2(horizontal * currentSpeed, rb.velocity.y);
    }
    
    void Jump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Ask for boolean value from OpenFeature client
        bool canJump = true;

        if (Input.GetButtonDown("Jump") && isGrounded && canJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            // Check debug mode flag for logging
            if (client != null)
            {
            
            }
        }
    }
}

