using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed;
    public float groundMovementSmoothing;
    public float airMovementSmoothing;

    public float jumpForce;
    public float jumpHoldDuration;
    public float jumpResetDuration;

    public float bounceForce;

    public LayerMask whatIsGround;
    public LayerMask whatIsBouncy;

    public GameObject groundColliderObject; // onground collision
    public GameObject rightColliderObject; // for detecting side wall collisions
    public GameObject leftColliderObject;

    public GameObject bounceColliderObject; // to detect bounce objects
    [Space(15)]



    [SerializeField]
    private Rigidbody2D rb2b;

    [SerializeField]
    private float moveDirX;

    [SerializeField]
    private Vector2 velocity;

    [SerializeField]
    private bool jumpInput;

    [SerializeField]
    private bool onGround;

    [SerializeField]
    private bool canJump;



    // -------------------------------------------- //

    private SpriteRenderer sprRen;
    private Animator anim;
    private const string PLAYER_WALK = "PlayerWalk";
    private const string PLAYER_IDLE = "PlayerIdle";

    private BoxCollider2D groundDetectionCollider;
    private BoxCollider2D rightDetectionCollider;
    private BoxCollider2D leftDetectionCollider;
    private CapsuleCollider2D bounceDetectionCollider;


    private bool isJumping;

    private float jumpHoldTimer;
    private float jumpResetTimer;


    void Start()
    {
        rb2b = GetComponent<Rigidbody2D>();
        sprRen = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        anim.Play(PLAYER_WALK);

        groundDetectionCollider = groundColliderObject.GetComponent<BoxCollider2D>();
        rightDetectionCollider = rightColliderObject.GetComponent<BoxCollider2D>();
        leftDetectionCollider = leftColliderObject.GetComponent<BoxCollider2D>();
        bounceDetectionCollider = bounceColliderObject.GetComponent<CapsuleCollider2D>();

        canJump = true;
    }

    void Update() 
    {
        if (!IsOwner) return;

        moveDirX = Input.GetAxisRaw("Horizontal");
        if (moveDirX == 0.0f)
            anim.Play(PLAYER_IDLE);
        else
            anim.Play(PLAYER_WALK);


        if (moveDirX > 0.0f)
        {
            FlipSpriteClientRpc(false);
            TestServerRpc(false);
            sprRen.flipX = false;
        }
        if (moveDirX < 0.0f)
        {
            FlipSpriteClientRpc(true);
            TestServerRpc(true);
            sprRen.flipX = true;
        }



        jumpInput = Input.GetKey(KeyCode.Space);

        onGround = groundDetectionCollider.IsTouchingLayers(whatIsGround);
        if (onGround)
        {
            float smooth = Mathf.SmoothStep(rb2b.velocity.x, moveDirX * moveSpeed, groundMovementSmoothing);
            velocity = new Vector2(smooth, rb2b.velocity.y);
        }
        else
        {
            canJump = false;

            float smooth = Mathf.SmoothStep(rb2b.velocity.x, moveDirX * moveSpeed, airMovementSmoothing);
            velocity = new Vector2(smooth, rb2b.velocity.y);

            if (rightDetectionCollider.IsTouchingLayers(whatIsGround) || leftDetectionCollider.IsTouchingLayers(whatIsGround))
            {
                velocity.x = rb2b.velocity.x;
            }
        }

        if (jumpInput && canJump && onGround)
        {
            canJump = false;
            jumpHoldTimer = Time.time + jumpHoldDuration;
            jumpResetTimer = Time.time + jumpResetDuration;
            isJumping = true;
        }

        if (Time.time >= jumpResetTimer && !canJump)
        {
            canJump = true;
        }

        if (bounceDetectionCollider.IsTouchingLayers(whatIsBouncy)) {
            // todo: consider changing the bounce angle based on
            //       x offset to object (this may be the wrong approach for that)
            //float playerCenter = transform.position.x;
            velocity.y = bounceForce;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (isJumping)
        {
            if (jumpInput)
            {
                if (Time.time < jumpHoldTimer)
                {
                    ApplyJumpForce();
                }
                else
                {
                    isJumping = false;
                }
            }
            else
            {
                isJumping = false;
            }
        }

        rb2b.velocity = velocity;
    }

    private void ApplyJumpForce()
    {
        velocity.y = jumpForce;
    }

    [ServerRpc]
    private void TestServerRpc(bool value)
    {
        sprRen.flipX = value;
    }

    [ClientRpc]
    private void FlipSpriteClientRpc(bool value)
    {
        sprRen.flipX = value;
    }

}
