using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(WeaponManager), typeof(PlayerMovement))]
public class AnimationRigController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rig armRig;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform leftElbowHint;
    [SerializeField] private Transform rightElbowHint;
    [SerializeField] private Transform backAttachPoint;

    [Header("Animation Settings")]
    [SerializeField] private float defaultRigWeight = 1f;
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Hand Offsets (Local Space)")]
    [Tooltip("Offset relative to attach point in local space.")]
    [SerializeField] private Vector3 defaultOffset = Vector3.zero;
    [SerializeField] private Vector3 reloadOffset = new Vector3(0f, -0.3f, 0f);
    [SerializeField] private Vector3 runOffset = new Vector3(0f, 0.15f, 0f);

    private Weapon currentWeapon;
    private WeaponManager weaponManager;
    private PlayerMovement playerMovement;

    private enum ArmState { Default, Reloading, Running, Swapping }
    private ArmState currentState = ArmState.Default;
    private float targetRigWeight;
    private Vector3 targetOffset;
    private Transform leftAttachPoint;
    private Transform rightAttachPoint;
    private Coroutine swapCoroutine;
    private bool isReloading = false;

    private void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
        playerMovement = GetComponent<PlayerMovement>();
        targetRigWeight = defaultRigWeight;
        targetOffset = defaultOffset;
    }

    private void Start()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += OnWeaponChanged;
            weaponManager.onWeaponSwitched.AddListener(OnWeaponSwitched);
        }
        UpdateWeaponReference();
        RegisterWeaponCallbacks(true);
    }

    private void OnDestroy()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= OnWeaponChanged;
            weaponManager.onWeaponSwitched.RemoveListener(OnWeaponSwitched);
        }
        RegisterWeaponCallbacks(false);
    }

    private void RegisterWeaponCallbacks(bool register)
    {
        if (currentWeapon == null) return;
        if (register) currentWeapon.OnWeaponAction += OnWeaponAction;
        else currentWeapon.OnWeaponAction -= OnWeaponAction;
    }

    private void LateUpdate()
    {
        // Smoothly blend rig weight
        if (armRig != null)
            armRig.weight = Mathf.Lerp(armRig.weight, targetRigWeight, Time.deltaTime * transitionSpeed);

        // Determine and set current arm state
        bool running = playerMovement.GetCurrentSpeed() > playerMovement.walkSpeed;
        if (!isReloading && swapCoroutine == null)
            SetArmState(running ? ArmState.Running : ArmState.Default);

        // Smoothly move and rotate hand targets
        SmoothHandTarget(rightAttachPoint, rightHandTarget, targetOffset);
        SmoothHandTarget(leftAttachPoint, leftHandTarget, targetOffset);
    }

    private void SmoothHandTarget(Transform attachPoint, Transform handTarget, Vector3 offset)
    {
        if (attachPoint == null || handTarget == null) return;

        Vector3 desiredPos = attachPoint.TransformPoint(offset);
        Quaternion desiredRot = attachPoint.rotation;

        handTarget.position = Vector3.Lerp(handTarget.position, desiredPos, Time.deltaTime * transitionSpeed);
        handTarget.rotation = Quaternion.Slerp(handTarget.rotation, desiredRot, Time.deltaTime * transitionSpeed);
    }

    private void OnWeaponChanged(string weaponName)
    {
        RegisterWeaponCallbacks(false);
        UpdateWeaponReference();
        RegisterWeaponCallbacks(true);
    }

    private void OnWeaponSwitched(GameObject weaponObject)
    {
        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);
        swapCoroutine = StartCoroutine(SwapWeaponAnimation(weaponObject));
    }

    private IEnumerator SwapWeaponAnimation(GameObject newWeapon)
    {
        isReloading = false;
        SetArmState(ArmState.Swapping);
        targetRigWeight = 0f;
        yield return new WaitForSeconds(0.5f);
        UpdateWeaponReference();
        targetRigWeight = defaultRigWeight;
        SetArmState(ArmState.Default);
        swapCoroutine = null;
    }

    private void OnWeaponAction(string action)
    {
        switch (action)
        {
            case "reload_start": StartReload(); break;
            case "reload_complete":
            case "reload_canceled": EndReload(); break;
        }
    }

    private void StartReload()
    {
        isReloading = true;
        SetArmState(ArmState.Reloading);
    }

    private void EndReload()
    {
        isReloading = false;
        SetArmState(ArmState.Default);
    }

    private void UpdateWeaponReference()
    {
        if (weaponManager == null) return;
        GameObject weaponObj = weaponManager.GetCurrentWeapon();

        if (weaponObj != null)
        {
            currentWeapon = weaponObj.GetComponent<Weapon>();
            leftAttachPoint = weaponObj.transform.Find("LeftHandAttach");
            rightAttachPoint = weaponObj.transform.Find("RightHandAttach");
        }
        else
        {
            currentWeapon = null;
            leftAttachPoint = backAttachPoint;
            rightAttachPoint = backAttachPoint;
        }
    }

    private void SetArmState(ArmState state)
    {
        if (currentState == state) return;
        currentState = state;
        switch (state)
        {
            case ArmState.Default:
                targetOffset = defaultOffset;
                targetRigWeight = defaultRigWeight;
                break;
            case ArmState.Reloading:
                targetOffset = reloadOffset;
                targetRigWeight = defaultRigWeight;
                break;
            case ArmState.Running:
                targetOffset = runOffset;
                targetRigWeight = defaultRigWeight;
                break;
            case ArmState.Swapping:
                break;
        }
    }
}
