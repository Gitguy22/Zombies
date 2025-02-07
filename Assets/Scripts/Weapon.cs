using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] bool addBulletSpread = true;
    public int weaponDamage;
    [SerializeField] Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] ParticleSystem shootingSystem;
    [SerializeField] Transform barrelExit;
    [SerializeField] ParticleSystem impactParticleSystem;
    [SerializeField] TrailRenderer bulletTracer;
    [SerializeField] LayerMask mask;
    [SerializeField] float bulletVelocity = 100;
    [SerializeField] float maxRange = 100f;
    [SerializeField] float minTrailDuration = 0.1f;

    [Header("Ammo & Reload Settings")]
    [SerializeField] int ammoInMag;
    [SerializeField] int magCapacity;
    [SerializeField] int ammoInReserves;
    [SerializeField] int maxReserves;
    private int ammoToLoad;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI ammoInMagText;
    [SerializeField] TextMeshProUGUI ammoInReservesText;

    [Header("Sounds")]
    [SerializeField] AudioClip shootSound;
    [SerializeField] AudioClip clickSound;
    [SerializeField] AudioClip reloadSound;

    private PlayerInputs playerInput;
    private Animator animator;
    private AudioSource audioSource;
    private float lastShootTime;

    private InputAction shootAction;
    private InputAction reloadAction;

    private bool isReloading;

    private bool gotHit;
    private bool gotKill;
    private string bodyPart;

    private float shootDelay;

    private GameObject owner;
    [Header("PointSystem Reference")]
    public GameObject pointManager;
    private PointManager points;

    void OnEnable()
    {
        owner = transform.root.gameObject;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        playerInput = new PlayerInputs();
        playerInput.Enable();

        shootAction = playerInput.OnFoot.Shoot;
        reloadAction = playerInput.OnFoot.Reload;

        isReloading = false;

        points = pointManager.GetComponent<PointManager>();
    }

    private void Update()
    {
        if (shootAction.triggered && Time.time >= lastShootTime + shootDelay)
        {
            OnShoot();
        }

        if (reloadAction.triggered && ammoInReserves > 0 && ammoInMag < magCapacity && !isReloading)
        {
            StartCoroutine(OnReload());
        }

        UpdateAmmoUI();
    }

    private void OnShoot()
    {
        // Reset tracking variables
        gotHit = false;
        gotKill = false;
        bodyPart = "";

        if (isReloading)
        {
            var whatIsPlaying = animator.GetCurrentAnimatorClipInfo(0);
            Debug.Log("Now Playing  " + whatIsPlaying[0].clip.name);
            animator.StopPlayback();
        }
        else if (ammoInMag > 0)
        {
            animator.SetTrigger("Fire");
            audioSource.PlayOneShot(shootSound);

            shootingSystem.Play();
            Vector3 direction = GetDirection();
            Debug.DrawLine(barrelExit.position, direction, Color.red, 100f);

            if (Physics.Raycast(barrelExit.position, direction, out RaycastHit hit, maxRange, mask))
            {
                TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, hit.point, hit));
                Debug.Log("Hit: " + hit.transform.name);

                Vector3 hitDirection = (hit.point - barrelExit.position).normalized * weaponDamage * 10f;

                if (hit.transform.gameObject.GetComponent<Limb>())
                {
                    ZombieAI zombie = hit.transform.root.GetComponent<ZombieAI>();
                    Limb limb = hit.transform.gameObject.GetComponent<Limb>();
                    bodyPart = limb.name;

                    // Store zombie's current state before applying damage
                    bool wasAlive = !zombie.dead;
                    float previousHP = zombie.zombieHP;

                    // Apply damage
                    limb.GetHit(weaponDamage, hit.point, hitDirection);

                    // Only award points if the zombie was alive when hit
                    if (wasAlive)
                    {
                        gotHit = true;
                        // Award kill points if this hit killed the zombie
                        gotKill = (previousHP >= 0 && zombie.dead);
                    }
                }

                if (hit.transform.gameObject.GetComponent<NonAmputatableLimb>())
                {
                    ZombieAI zombie = hit.transform.root.GetComponent<ZombieAI>();
                    NonAmputatableLimb nLimb = hit.transform.gameObject.GetComponent<NonAmputatableLimb>();

                    // Store zombie's current state before applying damage
                    bool wasAlive = !zombie.dead;
                    float previousHP = zombie.zombieHP;

                    // Apply damage
                    nLimb.GetHit(weaponDamage, hit.point, hitDirection);

                    // Only award points if the zombie was alive when hit
                    if (wasAlive)
                    {
                        gotHit = true;
                        // Award kill points if this hit killed the zombie
                        gotKill = (previousHP >= 0 && zombie.dead);
                    }

                }

                    if (hit.transform.gameObject.GetComponent<ZombieAI>())
                {
                    ZombieAI zombie = hit.transform.root.GetComponent<ZombieAI>();

                    // Store zombie's current state before applying damage
                    bool wasAlive = !zombie.dead;
                    float previousHP = zombie.zombieHP;

                    // Apply damage
                    zombie.GetHit(weaponDamage, hit.point, hitDirection);

                    // Only award points if the zombie was alive when hit
                    if (wasAlive)
                    {
                        gotHit = true;
                        // Award kill points if this hit killed the zombie
                        gotKill = (previousHP >= 0 && zombie.dead);
                    }
                }

                lastShootTime = Time.time;


                points.AddPoints(owner, gotHit, gotKill, bodyPart);
                Debug.Log($"Adding points - Hit: {gotHit}, Kill: {gotKill}, BodyPart: {bodyPart}");
            }
            else
            {
                TrailRenderer trail = Instantiate(bulletTracer, barrelExit.position, Quaternion.identity);
                StartCoroutine(SpawnTrail(trail, barrelExit.position + direction * maxRange, null));
            }

            ammoInMag--;
        }
        else
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private IEnumerator OnReload()
    {
        animator.SetTrigger("Reload");
        animator.SetBool("CanReload", true);
        isReloading = true;
        if (ammoInReserves > 0)
        {
            audioSource.PlayOneShot(reloadSound);

            ammoToLoad = Mathf.Min(magCapacity - ammoInMag, ammoInReserves);

            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

            ammoInMag += ammoToLoad;
            ammoInReserves -= ammoToLoad;
            isReloading = false;
            animator.SetBool("CanReload", false);
        }
    }

    private void UpdateAmmoUI()
    {
        ammoInMagText.text = ammoInMag.ToString();
        ammoInReservesText.text = ammoInReserves.ToString();
    }

    private Vector3 GetDirection()
    {
        Vector3 direction = transform.forward;

        if (addBulletSpread)
        {
            direction += new Vector3
            (
                Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x),
                Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y),
                Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z)
            );

            direction.Normalize();
        }

        return direction;
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, RaycastHit? hit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;
        float startingDistance = Vector3.Distance(trail.transform.position, hitPoint);
        float distance = startingDistance;

        while (distance > 0)
        {
            time += Time.deltaTime * bulletVelocity / startingDistance;
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            distance -= Time.deltaTime * bulletVelocity;

            yield return null;
        }

        yield return new WaitForSeconds(minTrailDuration);

        trail.transform.position = hitPoint;

        if (hit.HasValue)
        {
            Instantiate(impactParticleSystem, hit.Value.point, Quaternion.LookRotation(hit.Value.normal));
        }

        Destroy(trail.gameObject, trail.time);
    }
}