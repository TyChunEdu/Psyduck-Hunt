using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private int amountOfPokémon;

    private int amountOfPokémonCaught;

    public int numPokéballs;

    public float currentTime;
    public float endTime;

    private GUIManager gui;

    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private AudioClip tenseMusic;
    [SerializeField] private AudioClip tickSound;

    private AudioSource ambientAudioSource;
    private AudioSource tenseAudioSource;
    private AudioSource tickAudioSource;

    private bool tensePlayed;
    private bool tickPlayed;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;
        amountOfPokémon = GameObject.FindGameObjectsWithTag("Pokémon").Length;
        endTime = 300f;

        gui = GameObject.Find("GUI").GetComponent<GUIManager>();

        numPokéballs = 3;

        ambientAudioSource = GetComponents<AudioSource>()[0];
        tenseAudioSource = GetComponents<AudioSource>()[1];
        tickAudioSource = GetComponents<AudioSource>()[2];

        ambientAudioSource.clip = ambientSound;
        ambientAudioSource.Play();

        tensePlayed = false;
        tickPlayed = false;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime >= endTime)
        {
            EndGame("before time ran out");
        } 
        if (endTime - currentTime <= 10 && !tensePlayed)
        {
            ambientAudioSource.Pause();
            tenseAudioSource.clip = tenseMusic;
            tenseAudioSource.Play();
            tensePlayed = true;
        }
        if (endTime - currentTime <= 5 && !tickPlayed)
        {
            tenseAudioSource.volume = 0.5f;
            tickAudioSource.clip = tickSound;
            tickAudioSource.Play();
            tickPlayed = true;
            tickAudioSource.volume = 0.75f;
        }
    }

    public void RemovePokémon(GameObject pokémon, bool capture)
    {
        if (capture)
        {
            amountOfPokémon--;
            Destroy(pokémon);
            amountOfPokémonCaught++;
            gui.ReportToPlayer("Success!", amountOfPokémon + " remaining", false, 2f);
        }
        else
        {
            gui.ReportToPlayer("A Psyduck Escaped!", 
                amountOfPokémon + " remaining",
                false, 
                2f);
        }
        if (amountOfPokémon <= 0)
        {
            EndGame("before they all escaped");
        }
    }

    private void EndGame(string reason)
    {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        if (amountOfPokémonCaught == 1)
        {
            // Debug.Log("Well done! You have captured " + amountOfPokémonCaught + " Psyduck " + reason);
            gui.ReportToPlayer("Well done!", "You have captured " + amountOfPokémonCaught + " Psyduck",
                reason, true, 10f);
        }
        else
        {
            // Debug.Log("Well done! You have captured " + amountOfPokémonCaught + " Psyducks " + reason);
            gui.ReportToPlayer("Well done!", "You have captured " + amountOfPokémonCaught + " Psyducks",
                reason, true, 10f);
        }
    }
}
