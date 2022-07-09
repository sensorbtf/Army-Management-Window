using UnityEngine;

public class Slot : MonoBehaviour
{
    public GameObject unit;
    public bool tailOf2xUnit;
    public GameObject selectionMarker;
    
    public void ButtonHandler()
    {
        ArmiesManager.Instance.SelectSlot(this);
    }

    public void SetSelected(bool shouldBeSelected)
    {
        selectionMarker.SetActive(shouldBeSelected);
    }
}
