using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Animator that controls the pause panel open/close animation.")]
    public Animator pauseAnimator;

    [Header("UI References")]
    [Tooltip("Continue button reference (optional - will auto-setup if assigned).")]
    public Button continueButton;

    [Header("Pause Settings")]
    [Tooltip("When true the ESC key will toggle the pause menu.")]
    public bool toggleWithEscape = true;

    private int m_OpenParameterId;
    private Animator m_Open;
    private GameObject m_PreviouslySelected;

    // store animator update mode 
    private AnimatorUpdateMode m_PreviousAnimatorUpdateMode = AnimatorUpdateMode.Normal;

    const string k_OpenTransitionName = "Open";
    const string k_ClosedStateName = "Closed";

    void Start()
    {
        m_OpenParameterId = Animator.StringToHash(k_OpenTransitionName);

        if (pauseAnimator == null)
        {
            Debug.LogWarning("PauseMenuManager: No pause animator assigned.");
            return;
        }

        // Ensure pause panel starts closed
        if (pauseAnimator.gameObject.activeSelf)
        {
            pauseAnimator.gameObject.SetActive(false);
        }

        // Auto-setup continue button if assigned
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Continue);
        }
    }

    void Update()
    {
        if (!toggleWithEscape)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Toggle: open if closed, otherwise close
            if (m_Open == null)
                OpenPauseMenu();
            else
                ClosePauseMenu();
        }
    }

    // Public API: Open pause menu (does NOT pause time)
    public void OpenPauseMenu()
    {
        if (pauseAnimator == null)
        {
            Debug.LogWarning("PauseMenuManager: No pause animator assigned.");
            return;
        }

        // Prevent re-opening while already open
        if (m_Open == pauseAnimator && pauseAnimator.GetBool(m_OpenParameterId))
            return;

        Debug.Log("Opening Pause Menu");

        // Show and animate pause panel
        OpenPanel(pauseAnimator);

        // Make cursor visible and unlock
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Public API: Close pause menu
    public void ClosePauseMenu()
    {
        if (m_Open == null)
            return;

        Debug.Log("Closing Pause Menu");

        CloseCurrent();
    }

    // Public API: Continue/resume game - alias for ClosePauseMenu
    public void Continue()
    {
        ClosePauseMenu();
    }

    // Public API: Quit game (wire this to your Quit button)
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenPanel(Animator anim)
    {
        if (m_Open == anim)
            return;

        // Save currently selected control
        var newPreviouslySelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        // Activate object and bring to front
        anim.gameObject.SetActive(true);
        anim.transform.SetAsLastSibling();

        // Close any currently open panel
        CloseCurrent();

        m_PreviouslySelected = newPreviouslySelected;
        m_Open = anim;

        // Save current animator update mode
        m_PreviousAnimatorUpdateMode = m_Open.updateMode;

        // Rebind and force an update so parameter changes are applied immediately
        m_Open.Rebind();
        m_Open.Update(0f);

        // Set open parameter and force apply so the open animation starts on this same frame
        m_Open.SetBool(m_OpenParameterId, true);
        m_Open.Update(0f);

        GameObject go = FindFirstEnabledSelectable(anim.gameObject);
        SetSelected(go);
    }

    static GameObject FindFirstEnabledSelectable(GameObject gameObject)
    {
        GameObject go = null;
        var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable.IsActive() && selectable.IsInteractable())
            {
                go = selectable.gameObject;
                break;
            }
        }

        // If no selectable found, return the gameObject itself
        if (go == null)
            go = gameObject;

        return go;
    }

    public void CloseCurrent()
    {
        if (m_Open == null)
            return;

        m_Open.SetBool(m_OpenParameterId, false);
        SetSelected(m_PreviouslySelected);

        // Start coroutine that waits for the closed state
        StartCoroutine(DisablePanelDelayed(m_Open));
    }

    IEnumerator DisablePanelDelayed(Animator anim)
    {
        bool closedStateReached = false;
        bool wantToClose = true;
        int safetyCounter = 0;
        int maxFrames = 300; // 5 seconds at 60fps as safety

        while (!closedStateReached && wantToClose && safetyCounter < maxFrames)
        {
            if (!anim.IsInTransition(0))
                closedStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(k_ClosedStateName);

            wantToClose = !anim.GetBool(m_OpenParameterId);
            safetyCounter++;

            yield return null;
        }

        // Deactivate panel only if still wanting to close
        if (wantToClose)
        {
            anim.gameObject.SetActive(false);
        }

        // Restore animator update mode
        try
        {
            anim.updateMode = m_PreviousAnimatorUpdateMode;
        }
        catch { /* ignore if animator destroyed */ }

        // Clear m_Open reference
        if (m_Open == anim)
            m_Open = null;

        // Only lock the cursor if the whole pause menu is closed
        if (m_Open == null)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

    }

    private void SetSelected(GameObject go)
    {
        if (EventSystem.current == null)
            return;
        EventSystem.current.SetSelectedGameObject(go);
    }

    // Public getter to check if pause menu is open
    public bool IsPauseMenuOpen()
    {
        return m_Open != null;
    }
}