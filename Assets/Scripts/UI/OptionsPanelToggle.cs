using UnityEngine;

public class OptionsPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot; // assign OptionsPanel
    public void Show() { if (panelRoot) panelRoot.SetActive(true); }
    public void Hide() { if (panelRoot) panelRoot.SetActive(false); }
    public void Toggle() { if (panelRoot) panelRoot.SetActive(!panelRoot.activeSelf); }
}
