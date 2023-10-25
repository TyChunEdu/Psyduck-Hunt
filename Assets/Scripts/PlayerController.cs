using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Random = UnityEngine.Random;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class PlayerController : MonoBehaviour
{
    [SerializeField] // Shows field in Unity without the controller being public
    [Tooltip("Insert Character Controller")]
    private CharacterController controller;
    
    [SerializeField] 
    [Tooltip("Insert Main Camera")]
    private Camera mainCamera;
    
    [SerializeField] 
    [Tooltip("Insert Animator Controller")]
    private Animator playerAnimation;

    [SerializeField]
    [Tooltip("Insert Pokéball Prefab")]
    private GameObject pokéballPF;
    
    [SerializeField]
    [Tooltip("Insert Pokéball Bone Transform")]
    private Transform pokéballBone;

    private Vector3 velocity;
    private bool grounded;
    private readonly float gravity = -9.8f;
    private readonly float groundCastDist = 0.05f;
    
    public float jumpHeight = 20f;
    public float speed = 2f;
    public float runSpeed = 6f;

    private bool throwing;
    public float throwStrength = 8f;
    private GameObject instantiatedPokéBall;
    
    [SerializeField] private AudioClip[] grassSounds;
    [SerializeField] private AudioClip[] sandSounds;
    
    [SerializeField]
    private AudioClip jumpSound;
    
    private AudioSource playerAudioSource;
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsLaunching = Animator.StringToHash("IsLaunching");
    private static readonly int IsLanding = Animator.StringToHash("IsLanding");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsThrowing = Animator.StringToHash("IsThrowing");
    
    private LevelManager levelManager;

    // Start is called before the first frame update
    void Start()
    {
        playerAudioSource = GetComponent<AudioSource>();
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Grounded
        Transform playerTransform = transform;
        Transform cameraTransform = mainCamera.transform;
        var position = playerTransform.position;
        grounded = Physics.Raycast(position, Vector3.down, groundCastDist);

        // Debug - visualize raycast
        Debug.DrawRay(position, Vector3.down, grounded ? Color.blue : Color.red);

        // Ground movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movement = playerTransform.right * x 
                           + playerTransform.forward * z;
        // Vector3 movement = playerTransform.forward * z;

        // Throw
        // Stop moving when throwing
        if (Input.GetButtonDown("Fire1") && grounded && levelManager.numPokéballs > 0)
        {
            throwing = true;
            SpawnPokéballToBone();
            playerAnimation.SetBool(IsThrowing, true);
        }

        // Apply movement
        if (!throwing)
        {
            if (Input.GetKey(KeyCode.LeftShift) && movement.magnitude > 0)
            {
                controller.Move(movement * (runSpeed * Time.deltaTime));
                playerAnimation.SetBool(IsRunning, true);
                playerAudioSource.volume = 0.25f;
            }
            else
            {
                controller.Move(movement * (speed * Time.deltaTime));
                playerAnimation.SetBool(IsRunning, false);
                playerAudioSource.volume = 0.10f;
            }
            
            // Gravity and Jumping
            velocity.y += gravity * Time.deltaTime;
            if (Input.GetButtonDown("Jump") && grounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight);
                playerAudioSource.PlayOneShot(jumpSound);
            }
            controller.Move(velocity * Time.deltaTime);
            playerAnimation.SetBool(IsLaunching, !grounded && velocity.y > 0);
            playerAnimation.SetBool(IsLanding, !grounded && velocity.y < 0);
        }

        playerAnimation.SetBool(IsWalking, movement.magnitude > 0);

        // Rotate alongside camera
        playerTransform.rotation = Quaternion.AngleAxis(cameraTransform.rotation.eulerAngles.y,
            Vector3.up);
        // playerTransform.rotation = Quaternion.AngleAxis(playerTransform.rotation.eulerAngles.y + (x * 3),
        //     Vector3.up);
    }

    public void ThrowEnded()
    {
        throwing = false;
        playerAnimation.SetBool(IsThrowing, false);
        levelManager.numPokéballs--;
    }

    private void SpawnPokéballToBone()
    {
        if (instantiatedPokéBall == null)
        {
            instantiatedPokéBall = Instantiate(pokéballPF, pokéballBone, false);
        }
    }

    public void ReleasePokéball()
    {
        if (instantiatedPokéBall != null) // If there is a Pokéball
        {
            instantiatedPokéBall.transform.parent = null; // Detach Pokéball from hand
            instantiatedPokéBall.GetComponent<SphereCollider>().enabled = true;
            instantiatedPokéBall.GetComponent<Rigidbody>().useGravity = true;
                
            // To make the ball launch
            Transform cameraTransform = mainCamera.transform;
            // Vector3 throwAdjustment = new Vector3(0f, 0.5f, 0f);
            Vector3 throwVector = cameraTransform.forward * throwStrength;
            instantiatedPokéBall.GetComponent<Rigidbody>().AddForce(throwVector, ForceMode.Impulse);
            
            // Let the Pokéball do its own thing
            instantiatedPokéBall = null;
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        Cursor.lockState = hasFocus 
            ? CursorLockMode.Locked : CursorLockMode.None; 
        // Mouse becomes invisible and stays in the middle while in-game 
    }
    
    private float[] GetTextureMix(Vector3 playerPosition, Terrain terrain)
    {
        Vector3 terrainPosition = terrain.transform.position;
        TerrainData terrainData = terrain.terrainData;

        // Position of player in relation to terrain alphamap
        int mapPositionX = Mathf.RoundToInt((playerPosition.x 
                                             - terrainPosition.x)
                                            / terrainData.size.x 
                                            * terrainData.alphamapWidth);
        int mapPositionZ = Mathf.RoundToInt((playerPosition.z 
                                             - terrainPosition.z) 
                                            / terrainData.size.z 
                                            * terrainData.alphamapHeight);

        // 3D: 1st x, 2nd z, 3rd percent of the terrain layers (grass/sand) used
        float[,,] splatMapData = terrainData.GetAlphamaps(mapPositionX, mapPositionZ, 1, 1);
        
        // Extract all of the values into that cell mix converting 3D array to a 1D array
        float[] cellMix = new float[splatMapData.GetUpperBound(2) + 1];
        for (int i = 0; i < cellMix.Length; i++)
        {
            cellMix[i] = splatMapData[0, 0, i];
            
        }

        return cellMix;
    }

    private string FootStepLayerName(Vector3 playerPosition, Terrain terrain)
    {
        float[] cellMix = GetTextureMix(playerPosition, terrain);
        float strongestTexture = 0;

        int maxIndex = 0;

        for (int i = 0; i < cellMix.Length; i++)
        {
            if (cellMix[i] > strongestTexture)
            {
                strongestTexture = cellMix[i];
                maxIndex = i;
            }
        }

        return terrain.terrainData.terrainLayers[maxIndex].name;
    }

    public void FootStep()
    {
        playerAudioSource.clip = grassSounds[Random.Range(0, grassSounds.Length)];
        if (FootStepLayerName(transform.position, Terrain.activeTerrain) == "TL_Sand")
        {
            playerAudioSource.clip = sandSounds[Random.Range(0, sandSounds.Length)];
        }
        playerAudioSource.Play();
    }
}
