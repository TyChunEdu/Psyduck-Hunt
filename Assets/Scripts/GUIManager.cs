using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GUIManager : MonoBehaviour
{
    private GroupBox timerGb;

    private Label time;

    private GroupBox reportGb;
    private Label titleLabel;
    private Label line1Label;
    private Label line2Label;
    private Button returnButton;

    private GroupBox numPokéballsGb;
    private Label numPokéballsLabel;

    private LevelManager levelManager;

    [SerializeField] private AudioClip buttonSound;

    private AudioSource buttonAudioSource;

    // private bool goToMainMenu;

    // Start is called before the first frame update
    void Start()
    {
        levelManager = GameObject.Find("Level Manager").GetComponent<LevelManager>();
        
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        timerGb = root.Q<GroupBox>("Timer");
        time = root.Q<Label>("Time");
        
        reportGb = root.Q<GroupBox>("Report");
        titleLabel = root.Q<Label>("Title");
        line1Label = root.Q<Label>("Line1");
        line2Label = root.Q<Label>("Line2");
        returnButton = root.Q<Button>("Return");
        
        // ReSharper disable once StringLiteralTypo
        numPokéballsGb = root.Q<GroupBox>("NumPokeballs");
        numPokéballsLabel = root.Q<Label>("Amount");

        returnButton.clicked += ReturnButtonPressed;
        
        SetDisplay(reportGb, false);
        SetDisplay(timerGb, true);
        SetDisplay(numPokéballsGb, true);

        returnButton.style.unityTextOutlineWidth = 0.5f;
        returnButton.style.unityTextOutlineColor = new StyleColor(new Color(172f, 67f, 250f));

        buttonAudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetDisplay(timerGb))
        {
            if ((levelManager.endTime - levelManager.currentTime).ToString("0").Equals("1"))
            {
                time.text = (levelManager.endTime - levelManager.currentTime).ToString("0") + " second remaining";
            }
            else
            {
                time.text = (levelManager.endTime - levelManager.currentTime).ToString("0") + " seconds remaining";
            }

            if (levelManager.endTime - levelManager.currentTime < 5f)
            {
                time.style.color = new StyleColor(new Color(255f, 0f, 0f));
                time.style.fontSize = 68;
            }
        }

        if (GetDisplay(numPokéballsGb))
        {
            // ReSharper disable once StringLiteralTypo
            if (levelManager.numPokéballs == 1)
            {
                numPokéballsLabel.text = levelManager.numPokéballs.ToString("0") + " Pokeball left";
            }
            else if (levelManager.numPokéballs <= 0)
            {
                // ReSharper disable once StringLiteralTypo
                numPokéballsLabel.text = "No more Pokeballs left! Search around the map to find more!";
            }
            else
            {
                // ReSharper disable once StringLiteralTypo
                numPokéballsLabel.text = levelManager.numPokéballs.ToString("0") + " Pokeballs left";
            }
        }

        returnButton.RegisterCallback<MouseOverEvent>(MouseOver);
        returnButton.RegisterCallback<MouseOutEvent>(MouseOut);
    }

    private void MouseOver(MouseOverEvent moe)
    {
        returnButton.style.unityTextOutlineColor = new StyleColor(new Color(255f, 0f, 0f));
    }
    
    private void MouseOut(MouseOutEvent moe)
    {
        returnButton.style.unityTextOutlineColor = new StyleColor(new Color(172f, 67f, 250f));
    }

    public void ReportToPlayer(string title, string line1, string line2, bool showButton, float reportTime)
    {
        if (GetDisplay(reportGb))
        {
            CancelInvoke(nameof(HideReport));
        }
        
        SetDisplay(timerGb, false);
        SetDisplay(reportGb, true);
        SetDisplay(numPokéballsGb, false);
        titleLabel.text = title;
        line1Label.text = line1;
        line2Label.text = line2;
        titleLabel.visible = true;
        line1Label.visible = true;
        line2Label.visible = true;
        if (showButton)
        {
            returnButton.visible = true;
            returnButton.text = "Return to Start";
        }
        else
        {
            returnButton.visible = false;
        }

        Invoke(nameof(HideReport), reportTime);
    }
    
    public void ReportToPlayer(string title, string line1, string line2, bool showButton)
    {
        if (GetDisplay(reportGb))
        {
            CancelInvoke(nameof(HideReport));
        }
        
        SetDisplay(timerGb, false);
        SetDisplay(reportGb, true);
        SetDisplay(numPokéballsGb, false);
        titleLabel.text = title;
        line1Label.text = line1;
        line2Label.text = line2;
        titleLabel.visible = true;
        line1Label.visible = true;
        line2Label.visible = true;
        if (showButton)
        {
            returnButton.visible = true;
            returnButton.text = "Return to Start";
        }
        else
        {
            returnButton.visible = false;
        }
    }
    
    public void ReportToPlayer(string title, string line1, bool showButton, float reportTime)
    {
        if (GetDisplay(reportGb))
        {
            CancelInvoke(nameof(HideReport));
        }
        
        SetDisplay(timerGb, false);
        SetDisplay(reportGb, true);
        SetDisplay(numPokéballsGb, false);
        titleLabel.text = title;
        line1Label.text = line1;
        titleLabel.visible = true;
        line1Label.visible = true;
        line2Label.visible = false; 
        if (showButton)
        {
            returnButton.visible = true;
            returnButton.text = "Return to Start";
        }
        else
        {
            returnButton.visible = false;
        }
        Invoke(nameof(HideReport), reportTime);
    }

    private void HideReport()
    {
        SetDisplay(timerGb, true);
        SetDisplay(reportGb, false);
        SetDisplay(numPokéballsGb, true);
    }

    private void ReturnButtonPressed()
    {
        buttonAudioSource.clip = buttonSound;
        buttonAudioSource.Play();
        StartCoroutine(WaitForAudio(1f));
        SceneManager.LoadScene("Scenes/Start");
    }

    private void SetDisplay(GroupBox gb, bool display)
    {
        gb.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private bool GetDisplay(GroupBox gb)
    {
        if (gb.style.display == DisplayStyle.Flex)
        {
            return true;
        }

        return false;
    }
    
    IEnumerator WaitForAudio(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        // goToMainMenu = true;
    }
}
