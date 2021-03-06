﻿using Mirror;

using UnityEngine;

namespace Scripts.PlayerScripts.Control
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;

        [SerializeField] private Animator        animator;
        [SerializeField] private NetworkAnimator networkAnimator;

        private static readonly int _moving     = Animator.StringToHash("Moving");
        private static readonly int _horizontal = Animator.StringToHash("Horizontal");
        private static readonly int _vertical   = Animator.StringToHash("Vertical");
        private static readonly int _isRunning  = Animator.StringToHash("IsRunning");

        [Header("Cameras")]
        public Transform firstPersonCamera;

        [SerializeField] private GameObject thirdPersonCameraPrefab;
        [SerializeField] private Vector3    thirdPersonCameraOffset;

        private GameObject cameraTargetInstance;
        private GameObject thirdPersonCameraInstance;

        [Header("Movement")]
        [SerializeField] private MouseLook mouseLook = new MouseLook();

        [SerializeField] private float jogSpeed          = 2.5f;
        [SerializeField] private float runSpeed          = 5f;
        [SerializeField] private float jumpForce         = 10f;
        [SerializeField] private float gravityMultiplier = 2.5f;

        [SerializeField] [Range(0f, 1f)] private float smoothFactor = .25f;

        [HideInInspector] public float speedMultiplier     = 1f;
        [HideInInspector] public float jumpForceMultiplier = 1f;

        [HideInInspector] public bool freezed;
        private                  bool stopped;

        private bool jumpEnabled;

        private float speed;

        private Vector3 input;

        private Vector3 velocity;
        private Vector3 localVelocity;

        private void Start()
        {
            mouseLook.Setup(transform, firstPersonCamera);
            SwitchToFPP();

            animator.SetFloat(_horizontal, 0f);
            animator.SetFloat(_vertical, 0f);

            freezed = true;

            speed = jogSpeed;

            input = new Vector3();

            velocity      = new Vector3();
            localVelocity = new Vector3();
        }

        private void Update()
        {
            if (stopped)
                return;

            mouseLook.InputRotation();

            if (freezed)
                return;

            if (controller.isGrounded)
            {
                if (Input.GetButtonDown("Run"))
                {
                    speed = runSpeed;

                    animator.SetBool(_isRunning, true);
                }
                else if (Input.GetButtonUp("Run"))
                {
                    speed = jogSpeed;

                    animator.SetBool(_isRunning, false);
                }

                input.Set(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

                if (Input.GetButtonDown("Jump") && jumpEnabled)
                    velocity.y = jumpForce * jumpForceMultiplier;
            }
            else
                velocity += Physics.gravity * (gravityMultiplier * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            var desiredVelocity = transform.right * input.x +
                                  transform.forward * input.z;

            velocity = Vector3.Lerp(velocity,
                                    desiredVelocity * (speed * speedMultiplier),
                                    smoothFactor);

            localVelocity = Vector3.Lerp(localVelocity, input, smoothFactor);

            controller.Move(velocity * Time.fixedDeltaTime);

            animator.SetBool(_moving, desiredVelocity != Vector3.zero);

            animator.SetFloat(_horizontal, localVelocity.x);
            animator.SetFloat(_vertical, localVelocity.z);
        }

        private void LateUpdate()
        {
            if (stopped)
                return;

            mouseLook.Rotate();
        }

        public void SwitchToFPP()
        {
            firstPersonCamera.gameObject.SetActive(true);
            thirdPersonCameraPrefab.gameObject.SetActive(false);

            networkAnimator.enabled = true;
            animator.enabled        = true;

            jumpEnabled = false;

            Destroy(thirdPersonCameraInstance);

            mouseLook.SwitchToFPP();
        }

        public void SwitchToTPP(Transform model)
        {
            firstPersonCamera.gameObject.SetActive(false);
            thirdPersonCameraPrefab.gameObject.SetActive(true);

            networkAnimator.enabled = false;
            animator.enabled        = false;

            jumpEnabled = true;

            cameraTargetInstance = new GameObject("CameraTarget");
            cameraTargetInstance.transform.SetParent(model);
            cameraTargetInstance.transform.SetPositionAndRotation(model.position, model.rotation);

            thirdPersonCameraInstance =
                Instantiate(thirdPersonCameraPrefab, cameraTargetInstance.transform);

            thirdPersonCameraInstance.transform.localPosition = thirdPersonCameraOffset;
            thirdPersonCameraInstance.name                    = thirdPersonCameraPrefab.name;

            mouseLook.SwitchToTPP(thirdPersonCameraInstance.transform, cameraTargetInstance.transform);
        }

        public void SetPropFreeze(bool state)
        {
            freezed                       = state;
            mouseLook.freezeModelRotation = state;

            if (state)
                input = Vector3.zero;
        }

        public void SetStop(bool state)
        {
            stopped = state;

            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;

            if (state)
                input = Vector3.zero;
        }
    }
}