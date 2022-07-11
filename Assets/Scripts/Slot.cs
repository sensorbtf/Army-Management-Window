using UnityEngine;

public class Slot : MonoBehaviour
{
    public GameObject unit;
    public bool tailOf2xUnit;
    public GameObject selectionMarker;

    public void SetSelected(bool shouldBeSelected)
    {
        selectionMarker.SetActive(shouldBeSelected);
    }
    public void unSelect()
    {
        selectionMarker.SetActive(false);
    }
    public void ButtonHandler()
    {
        ArmiesManager.Instance.SelectSlot(this);
    }
}
