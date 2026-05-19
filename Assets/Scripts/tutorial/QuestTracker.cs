using UnityEngine;
using TMPro;

public class QuestTracker : MonoBehaviour
{
    [Header("UI Configuration")]
    public TextMeshProUGUI objectiveText;

    [Header("Tutorial UI Grouping")]
    [Tooltip("Drag your Gameplay_HUD_Group object here!")]
    public GameObject gameplayHudGroup;

    private int currentObjectiveIndex = 0;
    private Vector3 playerStartPos;
    private bool hasMoved = false;

    void Start()
    {
        UpdateObjectiveUI("W, A, S, D - Move"); // Phase 1 default 


        if (gameplayHudGroup != null)
        {
            gameplayHudGroup.SetActive(false);
        }

        GameObject player = GameObject.Find("Player_Kaelen");
        if (player != null)
        {
            playerStartPos = player.transform.position;
        }
    }

    void Update()
    {

        if (currentObjectiveIndex == 0 && !hasMoved)
        {
            GameObject player = GameObject.Find("Player_Kaelen");
            if (player != null)
            {
                if (Vector3.Distance(player.transform.position, playerStartPos) > 2f)
                {
                    hasMoved = true;

                    // --- THE MOMENT KAELEN SPAWNS & MOVES ---
                    // Snap the health and corruption UI into view instantly!
                    if (gameplayHudGroup != null)
                    {
                        gameplayHudGroup.SetActive(true);
                    }

                    AdvanceObjective(1, "Investigate the facility entrance"); // Phase 2 start 
                }
            }
        }
    }

    public void AdvanceObjective(int expectedStep, string newObjectiveText)
    {
        if (currentObjectiveIndex == expectedStep - 1)
        {
            currentObjectiveIndex = expectedStep;
            UpdateObjectiveUI(newObjectiveText);
        }
    }

    public int GetCurrentObjective()
    {
        return currentObjectiveIndex;
    }

    private void UpdateObjectiveUI(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }
}