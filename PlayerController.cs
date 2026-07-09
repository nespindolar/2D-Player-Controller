using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;

    public Transform groundCheck;
    public Transform wallCheck;
    public float groundCheckSize = 0.1f;    
    public float wallCheckSize = 0.1f;

    float horizontalInput = 0f;
    float verticalInput = 0f;
    public float maxFallSpeed = -12f;

    private float gravityScale;
    private float gravityScaleHeavy;
    public float gravityModifier = 1.1f;

    private Vector3 startingPosition;

    public float runSpeed = 2f;
    public float airMoveSpeed = 4f;    

    public Vector2 wallJumpForce = new Vector2(3f, 8f);
        
    private Vector2 dashDirection;
    public float dashSpeed = 8f;
    public float WallSlideSpeed = 0.5f;
    public float dashFloatTime = 0.05f;
    public float dashTime = 0.5f;
    public float dashCooldown = 1.5f;
    
    private bool isFacingRight = true;   
    private bool canDash = true;
    private bool isDashing;
    private bool isSliding= false;

    private float jumpSpeed;
    public float jumpHeight = 4;
    public float timeToJumpApex = 0.5f;
    private int jumpCount;
    public int jumpNumber = 2;
    private float jumpGraceTimer;
    private float wallJumpGraceTimer;
    public float jumpGraceTime = 0.05f;
    public float wallJumpGraceTime = 0.08f;    
    private bool isAirborne;
        
    private bool isWallJumping = false;

    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] LayerMask spikesLayer;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        jumpCount = jumpNumber;
        jumpSpeed = 2 * jumpHeight / timeToJumpApex;         

        spriteRenderer = GetComponent<SpriteRenderer>();

        gravityScale = rb.gravityScale = 0.5f * jumpHeight / Mathf.Pow(timeToJumpApex,2);
        gravityScaleHeavy = rb.gravityScale * gravityModifier;

        startingPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (isDashing) 
        {
            return;
        }
                
        //move player
        if (!isSliding && !isWallJumping && IsGrounded())
        {
            if (horizontalInput != 0)
            {
                rb.linearVelocity = new Vector2(horizontalInput * runSpeed, rb.linearVelocity.y);
            }

            if (IsGrounded() && Mathf.Abs(horizontalInput) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else if (!isSliding && !IsGrounded() && horizontalInput != 0)
        {
            rb.AddForce(new Vector2(airMoveSpeed * horizontalInput, 0));

            if (Mathf.Abs(rb.linearVelocity.x) > runSpeed)
            {
                rb.linearVelocity = new Vector2(horizontalInput * runSpeed, rb.linearVelocity.y);
            }            
        }
        //gradually stop player airborne inertia if let go of horizontal input
        if (!isSliding && !IsGrounded() && horizontalInput == 0 && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            int dir = (rb.linearVelocity.x > 0) ? 1 : -1;
            rb.AddForce(new Vector2(airMoveSpeed * -dir, 0), ForceMode2D.Force);

            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);                
            }
        }
        
        //limit fall speed
        if (rb.linearVelocity.y < maxFallSpeed && !isDashing) 
        {
            rb.linearVelocity = new Vector2 (rb.linearVelocity.x, maxFallSpeed);        
        }

        //gravity modifier
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScaleHeavy;
        }

        else
        {
            rb.gravityScale = gravityScale;
        }

        //flip
        if (!isSliding)
        {
            if (!isFacingRight && horizontalInput > 0)
            {
                Flip();
            }
            else if (isFacingRight && horizontalInput < 0)
            {
                Flip();
            }
        }                       
        
        //grace timers
        if (IsGrounded())
        {
            jumpGraceTimer = 0;
        }
        else 
        {
            jumpGraceTimer += Time.deltaTime;
        }

        if (IsGrounded() || IsWalled() || isSliding)
        {
            isAirborne = false;
        }
        else if (!IsGrounded() && !IsWalled() && !isSliding)
        {
            isAirborne = true;
        }

        if (IsWalled())
        {
            wallJumpGraceTimer = 0;            
        }
        else
        {
            wallJumpGraceTimer += Time.deltaTime;            
        }
               
             
        if ((IsGrounded() || IsWalled()) && !isDashing)
        { 
            canDash = true;
        }
        
        //allow jumpNumber of Jumps if grounded, sliding or walled
        if (!isAirborne)
        {        
            jumpCount = jumpNumber;
        }

        //allow 1 jump if current jumpNumber == 2 and NO jump if jumpNumber == 1 when player falls from platform without jumping first
        
        /*
        if (isAirborne && jumpCount == jumpNumber) 
        {
            if (jumpGraceTimer > jumpGraceTime)
            {                
                if (jumpNumber > 1)
                {
                    jumpCount = 1;
                }

                else if (jumpNumber == 1)
                {
                    jumpCount = 0;                    
                }                
            }
        }
        */        
        dashDirection = new Vector2(horizontalInput, verticalInput);             
        
        Slide();

        Debug.Log(TouchSpike());

        if (TouchSpike()) 
        {
            transform.position = startingPosition;
        }
        
    }
    

    private bool IsGrounded() 
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckSize, groundLayer);        
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, wallCheckSize, wallLayer);
    }
    
    private bool TouchSpike() 
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckSize, spikesLayer);
    }

    private void Flip() 
    {
        isFacingRight = !isFacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
    public void Move(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
        verticalInput = context.ReadValue<Vector2>().y;
    }

    public void Jump(InputAction.CallbackContext context) 
    {
        if (context.performed && jumpCount > 0)
        {
            
            if (IsGrounded() || !IsGrounded() && (jumpGraceTimer < jumpGraceTime) || !IsWalled() && wallJumpGraceTimer < wallJumpGraceTime)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);                
            }
            

            if (isSliding || !IsGrounded() && IsWalled())
            {
                int dir = (isFacingRight == true) ? 1 : -1;
                rb.AddForce(new Vector2(wallJumpForce.x * -dir, wallJumpForce.y), ForceMode2D.Impulse);                
            }

            jumpCount--;
        }

        if (context.canceled)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    public void Slide()
    {
        if (IsWalled() && !IsGrounded() && horizontalInput != 0)
        {
            if (rb.linearVelocity.y <= 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -WallSlideSpeed, float.MaxValue));
                isSliding = true;
            }
        }
        else
        {
            isSliding = false;
        }       
    }
    
    public void Dash(InputAction.CallbackContext context) 
    {
        if (context.performed && canDash)
        {
            StartCoroutine(DashCoroutine());
        }        
    }
    
    private IEnumerator DashCoroutine()
    {
        
        Vector2 dashVector = new Vector2(dashDirection.x, dashDirection.y).normalized;
        canDash = false;
        isDashing = true;        
        float gravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        yield return new WaitForSeconds(dashFloatTime);//player suspended on air before dash

        if (dashVector.x == 0 && dashVector.y == 0)
        {
            float dir = transform.localScale.x;
            rb.linearVelocity = new Vector2(dir * dashSpeed, dashVector.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(dashVector.x * dashSpeed, dashVector.y * dashSpeed);
        }

        Color spriteColor = spriteRenderer.color;
        spriteRenderer.color = Color.blue;
        
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravityScale;
        isDashing = false;
        spriteRenderer.color = spriteColor;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(groundCheck.position, groundCheckSize);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(wallCheck.position, wallCheckSize);        
    }
}
