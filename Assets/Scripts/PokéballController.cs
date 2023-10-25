using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PokéballController : MonoBehaviour
{
    public Animator pokéballAnimator;
    public ParticleSystem pokéflashPF;

    private GameObject pokémon;
    private GameObject terrain;
    private int animationStage;
    private bool didOnce;
    private Transform trainerCameraPoint;
    private bool escaped;
    private bool checkForEscape = true;

    private LevelManager levelManager;

    [SerializeField] private AudioClip collisionSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip wiggleSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip escapeSound;

    private AudioSource pokéballAudioSource;

    private bool disableCollisionSounds;
    private static readonly int State = Animator.StringToHash("State");

    private bool collectable;

    private bool displayedNoPokéballsLeft;

    // Start is called before the first frame update
    void Start()
    {
        pokéballAnimator.speed = 0;
        trainerCameraPoint = GameObject.FindWithTag("Trainer").transform.Find("CameraFocus");
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
        pokéballAudioSource = GetComponent<AudioSource>();
        disableCollisionSounds = false;
        collectable = levelManager.currentTime <= 1f;
    }

    // Uses physics calculations
    private void FixedUpdate()
    {
        Rigidbody pokéballRigidBody = gameObject.GetComponent<Rigidbody>();

        if (pokémon != null && !collectable)
        {
            switch (animationStage)
            {
                case 0: // Apply upwards force from the pokémon hit
                    pokéballRigidBody.AddForce(Vector3.up * 2, ForceMode.Impulse);
                    animationStage = 1;
                    break;
                case 1: // Check for when pokéball is coming down again
                    if (pokéballRigidBody.velocity.y < 0)
                    {
                        animationStage = 2;
                    }

                    break;
                case 2: // Hang in thin air, rotate towards pokémon, open the pokéball,
                    // spawn particle on the pokémon, and remove the pokémon
                    pokéballRigidBody.isKinematic = true; // Hang in thin air
                    Quaternion rotationTowardsPokémon = Quaternion.LookRotation(pokémon.transform.position
                        - transform.position);
                    transform.rotation = Quaternion.Lerp(transform.rotation, rotationTowardsPokémon,
                        Time.fixedDeltaTime * 3); // Rotate towards
                    pokéballAnimator.speed = 4; // Speed up when opening (which is handled by the animator)
                    if (!didOnce)
                    {
                        Instantiate(pokéflashPF, pokémon.transform.position, quaternion.identity);
                        didOnce = true;
                    }

                    pokémon.SetActive(false);

                    if (pokéballAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
                        && pokéballAnimator.GetCurrentAnimatorStateInfo(0).IsName("AN_Pokéball_Open"))
                    {
                        animationStage = 3;
                    }

                    break;
                case 3: // Close pokéball
                    pokéballAnimator.SetInteger(State, 1);
                    if (pokéballAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
                        && pokéballAnimator.GetCurrentAnimatorStateInfo(0).IsName("AN_Pokéball_Open"))
                    {
                        animationStage = 4;
                        terrain = null;
                    }

                    break;
                case 4: // Rotate towards player and drop to the ground
                    transform.LookAt(trainerCameraPoint, Vector3.up);
                    pokéballRigidBody.isKinematic = false;
                    if (terrain != null)
                    {
                        animationStage = 5;
                    }

                    break;
                case 5: // Stop physics & wiggle    
                    pokéballRigidBody.isKinematic = true;
                    pokéballAnimator.SetInteger(State, 2);
                    pokéballAnimator.speed = 1.5f;

                    if (checkForEscape)
                    {
                        int r = Random.Range(1, 10);

                        if (r == 1)
                        {
                            escaped = true;
                            pokéballAnimator.speed = 0;
                            didOnce = false;
                            animationStage = 6;
                        }

                        StartCoroutine(WaitForCheck(1));

                        checkForEscape = false;
                    }

                    if (pokéballAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 3.0f
                        && pokéballAnimator.GetCurrentAnimatorStateInfo(0).IsName("AN_Pokéball_Wiggle"))
                    {
                        pokéballAnimator.speed = 0;
                        didOnce = false;
                        animationStage = 6;
                    }

                    break;
                case 6: // Escape or not?
                    if (escaped)
                    {
                        if (!didOnce)
                        {
                            Instantiate(pokéflashPF, pokémon.transform.position, quaternion.identity);

                            pokéballAudioSource.clip = escapeSound;
                            pokéballAudioSource.Play();

                            didOnce = true;
                        }

                        if (!pokéballAudioSource.isPlaying)
                        {
                            Destroy(gameObject);
                        }
                        
                        levelManager.RemovePokémon(pokémon, false);
                        // Will not destroy pokémon because it wasn't captured

                        pokémon.SetActive(true);
                    }
                    else
                    {
                        if (!didOnce)
                        {
                            pokéballAudioSource.clip = successSound;
                            pokéballAudioSource.Play();
                            didOnce = true;
                        }

                        levelManager.RemovePokémon(pokémon, true);
                    }

                    break;
            }
        }
        else if (collectable)
        {
            GetComponent<SphereCollider>().enabled = true;
            GetComponent<Rigidbody>().useGravity = true;
            if (Vector3.Distance(GameObject.FindWithTag("Trainer").transform.position,transform.position) <= 2f 
                || disableCollisionSounds)
            {
                if (!disableCollisionSounds)
                {
                    pokéballAudioSource.clip = collisionSound;
                    pokéballAudioSource.Play();
                    disableCollisionSounds = true;
                }

                if (!pokéballAudioSource.isPlaying)
                {
                    levelManager.numPokéballs++;
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Pokémon") && pokémon == null/*So only one pokemon is captured*/
            && !collectable)
        {
            pokémon = collision.gameObject;

            pokéballAudioSource.clip = hitSound;
            pokéballAudioSource.Play();
            disableCollisionSounds = true;
        }

        if (collision.gameObject.tag.Equals("Terrain") && !collectable)
        {
            terrain = collision.gameObject;

            if (!disableCollisionSounds)
            {
                pokéballAudioSource.clip = collisionSound;
                pokéballAudioSource.Play();
            }
        }
    }

    IEnumerator WaitForCheck(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        checkForEscape = true;
    }

    public void WiggleSound()
    {
        pokéballAudioSource.clip = wiggleSound;
        pokéballAudioSource.Play();
    }
}
