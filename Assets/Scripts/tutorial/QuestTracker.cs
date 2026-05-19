using UnityEngine;
using TMPro;

public class QuestTracker : MonoBehaviour
{
    [Header("UI Configuration")]
    public TextMeshProUGUI objectiveText;

    private int currentObjectiveIndex = 0;
    private Vector3 playerStartPos;
    private bool hasMoved = false;

    void Start()
    {
        UpdateObjectiveUI("W, A, S, D - Move");

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
                    AdvanceObjective(1, "Investigate the facility entrance");
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