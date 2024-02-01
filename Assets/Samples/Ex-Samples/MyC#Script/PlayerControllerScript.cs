using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerScript : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform head;
    public Camera camera;

    [Header("Configurations")]
    public float walkSpeed;
    public float runSpeed;
    public float jumpSpeed;
    public float impactThreshold;
    public float itemPickupDistance;

    [Header("Camera Effects")]
    public float baseCameraFov = 60f;
    public float baseCameraHeight = 0.85f;

    public float walkBobbingRate = 0.75f;
    public float runBobbingRate = 1f;
    public float maxWalkBobbingOffset = 0.2f;
    public float maxRunBobbingOffset = 0.3f;

    public float cameraShakeThreshold = 10f;
    [Range(0f, 0.03f)]
    public float cameraShakeRate = 0.015f;
    public float maxVerticalFallShakeAngle = 40f;
    public float maxHorizontalFallShakeAngle = 40f;

    [Header("Audio")]
    public AudioSource audioWalkDefault;
    public AudioSource audioWalkGrass;
    public AudioSource audioWind;
    public AudioSource audioWalkConcrete;
    public float windPitchMultiplier;

    [Header("Runtime")]
    Vector3 newVelocity;
    bool isGrounded = false;
    bool isJumping = false;
    float vyCache;
    string activeAudioName = "default";
    Transform attachedObject = null;
    float attachedDistance = 2f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
   void Update()
    {
        //Horizontal Rotation
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * 2f);

        newVelocity = Vector3.up * rb.velocity.y;
        // new Vector2(0f, rb.velocity.y, 0f)
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        newVelocity.x = Input.GetAxis("Horizontal") * speed;
        newVelocity.z = Input.GetAxis("Vertical") * speed;

        if(isGrounded)
        {
            if(Input.GetKeyDown(KeyCode.Space) && !isJumping) 
            {
                newVelocity.y = jumpSpeed;
                isJumping = true;
            }
        }

        bool isMovingOnGround = (Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f) && isGrounded;


        if (isMovingOnGround)
        {
            float bobbingRate = Input.GetKey(KeyCode.LeftShift) ? runBobbingRate : walkBobbingRate;
            float bobbingOffset = Input.GetKey(KeyCode.LeftShift) ? maxRunBobbingOffset : maxWalkBobbingOffset;
            Vector3 targetHeadPosition = Vector3.up * baseCameraHeight + Vector3.up * (Mathf.PingPong(Time.time * bobbingRate, bobbingOffset) - bobbingOffset * 0.5f);
            head.localPosition = Vector3.Lerp(head.localPosition, targetHeadPosition, 0.1f);
        }

        rb.velocity = transform.TransformDirection(newVelocity);


        //Audio
        audioWalkDefault.enabled = isMovingOnGround && activeAudioName == "default";
        audioWalkDefault.pitch = Input.GetKey(KeyCode.LeftShift) ? 1.75f : 1f;

        audioWalkGrass.enabled = isMovingOnGround && activeAudioName == "grass";
        audioWalkGrass.pitch = Input.GetKey(KeyCode.LeftShift) ? 1.75f : 1f;

        audioWalkConcrete.enabled = isMovingOnGround && activeAudioName == "concrete";
        audioWalkConcrete.pitch = Input.GetKey(KeyCode.LeftShift) ? 1.25f : 0.8f;

        audioWind.enabled = true;
        audioWind.pitch = Mathf.Clamp(Mathf.Abs(rb.velocity.y * windPitchMultiplier), 0f, 2f) + Random.Range(-0.1f,0.1f);

        // Picking Objects
        RaycastHit hit;
        bool cast = Physics.Raycast(head.position, head.forward, out hit, itemPickupDistance);
        

        if(Input.GetKeyDown(KeyCode.F))
        {
            if(attachedObject != null)
            {
                attachedObject.SetParent(null);

                if(attachedObject.GetComponent<Rigidbody>() != null)
                {
                    attachedObject.GetComponent<Rigidbody>().isKinematic = false;
                }

                if(attachedObject.GetComponent<Collider>() != null)
                {
                    attachedObject.GetComponent<Collider>().enabled = true;
                }

                attachedObject = null;
            }
            else
            {
                if (cast)
                {
                    if(hit.transform.CompareTag("pickable"))
                    {
                        attachedObject = hit.transform;
                        attachedObject.SetParent(transform);

                        if (attachedObject.GetComponent<Rigidbody>() != null)
                        {
                            attachedObject.GetComponent<Rigidbody>().isKinematic = true;
                        }

                        if (attachedObject.GetComponent<Collider>() != null)
                        {
                            attachedObject.GetComponent<Collider>().enabled = false;
                        }

                    }
                }
            }
        }
    }
    void FixedUpdate()
    {
        if ( (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f)))
        {
            isGrounded = true;

            if (hit.transform.tag == "grass")
            {
                activeAudioName = "grass";
                Debug.Log("Grass!");
            }
            else if (hit.transform.tag == "concrete" )
            {
                activeAudioName = "concrete";
                Debug.Log("Concrete!");
            }
            else
            {
                activeAudioName = "default";
                Debug.Log("Default!");
            }

        }
        else isGrounded = false;

        vyCache = rb.velocity.y;
    }

    void LateUpdate()
    {
        //Vertical Rotation
        Vector3 e = head.eulerAngles;
        e.x -= Input.GetAxis("Mouse Y") * 2f;
        e.x = RestrictAngle(e.x, -85f, 85f);
        head.eulerAngles = e;
        
        //FOV
        float fovOffset = (rb.velocity.y < 0f) ? Mathf.Sqrt(Mathf.Abs(rb.velocity.y)) : 0f;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, baseCameraFov + fovOffset, 0.25f);

        //Fall Effect
        if (!isGrounded && Mathf.Abs(rb.velocity.y) >= cameraShakeThreshold)
        {
            Vector3 newAngle = head.localEulerAngles;
            newAngle += Vector3.right * Random.Range(-maxVerticalFallShakeAngle, maxVerticalFallShakeAngle);
            newAngle += Vector3.up * Random.Range(-maxHorizontalFallShakeAngle, maxHorizontalFallShakeAngle);
            head.localEulerAngles = Vector3.Lerp(head.localEulerAngles, newAngle, cameraShakeRate);
        }
        else
        {
            e = head.localEulerAngles;
            e.y = 0f;
            head.localEulerAngles = e;
        }

        //Pick Object
        if (attachedObject != null)
        {
            attachedObject.position = head.position + head.forward * attachedDistance;
            attachedObject.Rotate(transform.right * Input.mouseScrollDelta.y * 30f, Space.World);
        }
    }

    // Clamp the vertical head rotation (prevent bending backwards)
    public static float RestrictAngle (float angle, float angleMin, float angleMax)
    {
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        if(angle >angleMax)
            angle = angleMax;
        if(angle < angleMin)
            angle = angleMin;

        return angle;
    }

    void OnCollisionStay(Collision col)
    {
        isGrounded = true;
        isJumping = false;
    }
    void OnCollisionExit(Collision col)
    {
        isGrounded = false;
    }

    void OnCollisionEnter(Collision col)
    {
        if(Vector3.Dot(col.GetContact(0).normal, Vector3.up) < 0.5f)
        {
            if(rb.velocity.y < 0.5f)
            {
                rb.velocity = Vector3.up * rb.velocity.y;
            }
            return;
        }

        float acceleration = (rb.velocity.y - vyCache) / Time.fixedDeltaTime;
        float impactForce = rb.mass * Mathf.Abs (acceleration);

        if(impactForce >= impactThreshold)
        {
            Debug.Log("Fall Damage!");
        }
    }
}
