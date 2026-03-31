using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playyer : MonoBehaviour
{
        private Rigidbody2D rb2d;
        private SpriteRenderer sr;
        private BoxCollider2D coll;
        private Animator anim;

        [SerializeField] private LayerMask jumpableGround;
    
        private float dirX = 0f;

        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float  JumpSpeed= 7f;
    
        private enum MovementState {idle,running,jumping,falling}
    
        [SerializeField] private AudioSource jumpSound;
        // Start is called before the first frame update
        void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            coll = GetComponent<BoxCollider2D>();
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            dirX = Input.GetAxisRaw("Horizontal");
            rb2d.velocity = new Vector2(dirX * moveSpeed, rb2d.velocity.y);
            if (Input.GetButtonDown("Jump") && IsGrounded())
            {
                jumpSound.Play();
                rb2d.velocity = new Vector2(rb2d.velocity.x, JumpSpeed);
            }
            UpdateAnimationState();
        
        }

        private void UpdateAnimationState()
        {
            MovementState State;
            if (dirX > 0)
            {
                State=MovementState.running;
                sr.flipX = false; 
            }
            else if (dirX < 0)
            {
                State=MovementState.running;
                sr.flipX = true;
            }
            else
            {
                State=MovementState.idle;
            }

            if (rb2d.velocity.y > .1f)
            {
                State=MovementState.jumping;
            }
            else if (rb2d.velocity.y < -.1f)
            {
                State=MovementState.falling;
            }
            anim.SetInteger("State",(int)State);
        
        }

        private bool IsGrounded()
        {
            return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 
                0f,Vector2.down ,.1f,jumpableGround);
        }
    
}
