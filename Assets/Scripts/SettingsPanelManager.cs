using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SettingsPanelManager : MonoBehaviour
{
    [Header("Which panel should be open when Settings opens")]
    public Animator initiallyOpen;

    private int m_OpenParameterId;
    private Animator m_CurrentOpenPanel;
    private GameObject m_PreviouslySelected;

    private const string OPEN_PARAM = "Open";
    private const string CLOSED_STATE = "Closed";

    private void Awake()
    {
        m_OpenParameterId = Animator.StringToHash(OPEN_PARAM);
    }

    private void OnEnable()
    {
        if (initiallyOpen != null)
            OpenPanel(initiallyOpen);
    }

    public void OpenPanel(Animator panel)
    {
        if (m_CurrentOpenPanel == panel)
            return;

        panel.gameObject.SetActive(true);

        var newPrev = EventSystem.current.currentSelectedGameObject;
        panel.transform.SetAsLastSibling();

        CloseCurrentPanel();

        m_PreviouslySelected = newPrev;

        m_CurrentOpenPanel = panel;
        m_CurrentOpenPanel.SetBool(m_OpenParameterId, true);

        GameObject first = FindFirstSelectable(panel.gameObject);
        SetSelected(first);
    }

    private GameObject FindFirstSelectable(GameObject root)
    {
        foreach (var sel in root.GetComponentsInChildren<Selectable>(true))
        {
            if (sel.IsActive() && sel.IsInteractable())
                return sel.gameObject;
        }
        return null;
    }

    public void CloseCurrentPanel()
    {
        if (m_CurrentOpenPanel == null)
            return;

        m_CurrentOpenPanel.SetBool(m_OpenParameterId, false);

        SetSelected(m_PreviouslySelected);

        StartCoroutine(DisableAfterClose(m_CurrentOpenPanel));
        m_CurrentOpenPanel = null;
    }

    private IEnumerator DisableAfterClose(Animator panel)
    {
        bool reachedClosed = false;
        bool stillClosing = true;

        while (!reachedClosed && stillClosing)
        {
            if (!panel.IsInTransition(0))
                reachedClosed = panel.GetCurrentAnimatorStateInfo(0).IsName(CLOSED_STATE);

            stillClosing = !panel.GetBool(m_OpenParameterId);

            yield return null;
        }

        if (stillClosing)
            panel.gameObject.SetActive(false);
    }

    private void SetSelected(GameObject obj)
    {
        EventSystem.current.SetSelectedGameObject(obj);
    }
}
