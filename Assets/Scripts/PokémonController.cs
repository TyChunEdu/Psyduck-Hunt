using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
public class PokémonController : MonoBehaviour
{
    private enum State {
        Chill, Saunter, Flee, Dig
    }

    [SerializeField]
    private State currentState;

    private bool transitionActive;
    
    [SerializeField]
    private Vector3 currentDestination;

    [SerializeField] private float runSpeed;

    private float walkingSpeed;

    private readonly float viewAngle = 0.25f;
    private readonly float viewDistance = 5f;

    private GameObject trainer;

    private Animator pokémonAnimator;

    private LevelManager levelManager;

    [SerializeField] private AudioClip[] psySounds;
    [SerializeField] private AudioClip[] grassSounds;
    [SerializeField] private AudioClip[] sandSounds;
    [SerializeField] private AudioClip[] panicSounds;

    private AudioSource pokémonAudioSource1;
    private AudioSource pokémonAudioSource2;

    private bool shutUp;
    private static readonly int Saunter = Animator.StringToHash("Saunter");
    private static readonly int Flee = Animator.StringToHash("Flee");
    private static readonly int Dig = Animator.StringToHash("Dig");

    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
        
        trainer = GameObject.FindWithTag("Trainer");
        /*transform.rotation = Quaternion.LookRotation(transform.position - trainer.transform.position);
        GetComponent<NavMeshAgent>().destination = transform.position + transform.forward * 15;
        GetComponent<NavMeshAgent>().speed = 10f;*/

        walkingSpeed = GetComponent<NavMeshAgent>().speed;

        pokémonAnimator = GetComponent<Animator>();
        
        SwitchToState(State.Chill);

        pokémonAudioSource1 = GetComponents<AudioSource>()[0];
        pokémonAudioSource1.volume = 1f;
        pokémonAudioSource1.spatialBlend = 1f;
        pokémonAudioSource1.maxDistance = 5f;
        
        pokémonAudioSource2 = GetComponents<AudioSource>()[1];
        pokémonAudioSource2.volume = 0.25f;
        pokémonAudioSource2.spatialBlend = 1f;
        pokémonAudioSource2.maxDistance = 5f;

        shutUp = true;
        Invoke(nameof(ResetShutUp), Random.Range(5f, 20f));
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case State.Chill:
                PlaySound(State.Chill);
                
                if (transitionActive)
                {
                    currentDestination = transform.position;
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    Invoke(nameof(SwitchToSaunter), Random.Range(5f, 6f));
                    UpdateAnimator(false, false, false);
                    GetComponent<NavMeshAgent>().speed = 0f;
                    transitionActive = false;
                }
                
                if (InView(trainer, viewAngle, viewDistance))
                {
                    SwitchToState(State.Flee);
                }

                break;
            case State.Saunter:
                PlaySound(State.Saunter);
                
                if (transitionActive)
                {
                    currentDestination = ValidDestination(false);
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    UpdateAnimator(true, false, false);
                    GetComponent<NavMeshAgent>().speed = walkingSpeed;
                    transitionActive = false;
                }

                if ((transform.position - currentDestination).magnitude < 2.5f)
                {
                    SwitchToState(State.Chill);
                }
                
                if (InView(trainer, viewAngle, viewDistance))
                {
                    SwitchToState(State.Flee);
                }

                break;
            case State.Flee:
                PlaySound(State.Flee);
                
                if (transitionActive)
                {
                    CancelInvoke(nameof(SwitchToSaunter));
                    Invoke(nameof(CheckForDig), 10f);
                    currentDestination = ValidDestination(true);
                    GetComponent<NavMeshAgent>().destination = currentDestination;
                    UpdateAnimator(false, true, false);
                    GetComponent<NavMeshAgent>().speed = runSpeed;
                    transitionActive = false;
                }

                if ((transform.position - currentDestination).magnitude < 2.5f)
                {
                    CancelInvoke(nameof(CheckForDig));
                    CheckForDig();
                }
                
                break;
            case State.Dig:
                if (transitionActive)
                {
                    currentDestination = transform.position;
                    GetComponent<NavMeshAgent>().speed = 0f;
                    UpdateAnimator(false, false, true);
                    transitionActive = false;
                }
                break;
        }
    }

    void SwitchToState(State newState)
    {
        transitionActive = true;
        currentState = newState;
    }
    
    void SwitchToSaunter()
    {
        SwitchToState(State.Saunter);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(SwitchToSaunter));
        CancelInvoke(nameof(CheckForDig));
        SwitchToState(State.Flee);
    }

    void CheckForDig()
    {
        SwitchToState((transform.position - trainer.transform.position).magnitude > 25f ? State.Chill : State.Dig);
    }

    public void DigCompleted()
    {
        levelManager.RemovePokémon(gameObject, false);
    }

    void UpdateAnimator(bool saunter, bool flee, bool dig)
    {
        pokémonAnimator.SetBool(Saunter, saunter);
        pokémonAnimator.SetBool(Flee, flee);
        pokémonAnimator.SetBool(Dig, dig);
    }
    
    void PlaySound(State state)
    {
        if (state == State.Chill || state == State.Saunter)
        {
            pokémonAudioSource1.loop = false;
            if (!shutUp)
            {
                if (Random.Range(1, 10) == 1)
                {
                    pokémonAudioSource1.clip = psySounds[Random.Range(0, psySounds.Length)];
                    pokémonAudioSource1.Play();
                    shutUp = true;
                    Invoke(nameof(ResetShutUp), Random.Range(5f, 20f));
                }
            }
        }

        if (state == State.Flee)
        {
            if (transitionActive) // Do not repeat this every frame
            {
                pokémonAudioSource1.clip = panicSounds[Random.Range(0, panicSounds.Length)];
                pokémonAudioSource1.loop = true;
                pokémonAudioSource1.Play();
            }
        }
    }

    void ResetShutUp()
    {
        shutUp = false;
    }
    
    Vector3 ValidDestination(bool avoidTrainer)
    {
        float[,] boundaries = { { 56f, 206f }, { 62f, 213f } };

        float x = Random.Range(boundaries[0, 0], boundaries[0, 1]);
        float z = Random.Range(boundaries[1, 0], boundaries[1, 1]);

        if (avoidTrainer)
        {
            var position = trainer.transform.position;
            x = position.x - boundaries[0, 0] >= boundaries[0, 1] 
                - position.x ? boundaries[0, 0] : boundaries[0, 1];

            z = position.z - boundaries[1, 0] >= boundaries[1, 1] 
                - position.z ? boundaries[0, 0] : boundaries[0, 1];
        }
        
        Vector3 destination = new Vector3(x, 
            Terrain.activeTerrain.SampleHeight(new Vector3(x, 0f, z)), 
            z);

        return destination;
    }

    bool InView(GameObject target, float viewingAngle, float viewingDistance)
    {
        float dotProduct = Vector3.Dot(transform.forward,
            Vector3.Normalize(target.transform.position - transform.position));

        float view = 1f - viewingAngle;

        float distance = (transform.position - target.transform.position).magnitude;

        if (dotProduct >= view && distance < viewingDistance)
        {
            return true;
        }

        return false;
    }

    private float[] GetTextureMix(Vector3 pokémonPosition, Terrain terrain)
    {
        Vector3 terrainPosition = terrain.transform.position;
        TerrainData terrainData = terrain.terrainData;

        // Position of player in relation to terrain alphamap
        int mapPositionX = Mathf.RoundToInt((pokémonPosition.x 
                                            - terrainPosition.x)
                                            / terrainData.size.x 
                                            * terrainData.alphamapWidth);
        int mapPositionZ = Mathf.RoundToInt((pokémonPosition.z 
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

    private string FootStepLayerName(Vector3 pokémonPosition, Terrain terrain)
    {
        float[] cellMix = GetTextureMix(pokémonPosition, terrain);
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
        pokémonAudioSource2.clip = grassSounds[Random.Range(0, grassSounds.Length)];
        if (FootStepLayerName(transform.position, Terrain.activeTerrain) == "TL_Sand")
        {
            pokémonAudioSource2.clip = sandSounds[Random.Range(0, sandSounds.Length)];
        }
        pokémonAudioSource2.Play();
    }
}
