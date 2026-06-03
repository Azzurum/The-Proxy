using UnityEngine;
using TMPro;

/// <summary>
/// Manages the progression of tutorial objectives and coordinates early-game UI unlocks.
/// </summary>
public class QuestTracker : MonoBehaviour
{
    [Header("UI Configuration")]
    [Tooltip("The text element displaying the current objective prompt.")]
    public TextMeshProUGUI objectiveText;

    [Header("Tutorial UI Grouping")]
    [Tooltip("The parent GameObject containing the player's core HUD elements.")]
    public GameObject gameplayHudGroup;

    private int _currentObjectiveIndex = 0;
    private Vector3 _playerStartPos;
    private bool _hasMoved = false;
    private Transform _cachedPlayer;

    private void Start()
    {
        UpdateObjectiveUI("W, A, S, D - Move"); 

        if (gameplayHudGroup != null)
        {
            gameplayHudGroup.SetActive(false);
        }

        // Look for Kaelen even if the Director has disabled him temporarily
        PlayerController pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (pc != null)
        {
            _cachedPlayer = pc.transform;
            _playerStartPos = _cachedPlayer.position;
        }
    }

    private void Update()
    {
        if (_currentObjectiveIndex == 0 && !_hasMoved && _cachedPlayer != null)
        {
            if (Vector3.Distance(_cachedPlayer.position, _playerStartPos) > 2f)
            {
                _hasMoved = true;

                if (gameplayHudGroup != null)
                {
                    gameplayHudGroup.SetActive(true);
                }

                AdvanceObjective(1, "Investigate the facility entrance"); 
            }
        }
    }

    /// <summary>
    /// Progresses the quest sequence if the required step index is met.
    /// </summary>
    public void AdvanceObjective(int expectedStep, string newObjectiveText)
    {
        // Flexibly allow progression even if the player skipped an optional step
        if (_currentObjectiveIndex < expectedStep)
        {
            _currentObjectiveIndex = expectedStep;
            UpdateObjectiveUI(newObjectiveText);
        }
    }

    /// <summary>
    /// Retrieves the integer index of the current active objective.
    /// </summary>
    public int GetCurrentObjective()
    {
        return _currentObjectiveIndex;
    }

    private void UpdateObjectiveUI(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }
}