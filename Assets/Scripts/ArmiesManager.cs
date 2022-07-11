using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ArmiesManager : MonoBehaviour
{
    public GameObject unit1x;
    public GameObject unit2x;

    public List<Army> armies;

    public static ArmiesManager Instance;

    private Slot firstSelection;
    private Slot secondSelection;

    private IEnumerable<Slot> SelectedSlots => new[] { firstSelection, secondSelection }.Where(s => s != null);

    [SerializeField]
    public Button swapButton;

    private void Awake()
    {
        Instance = this;

        if (SelectedSlots.Count() == 0)
            swapButton.interactable = false;

    }
    public void SelectSlot(Slot slot)
    {
        /*breaking selection of right slot of 2-slot unit altought
        in my visualisation it would be better to leave both slots of 2-slot unit active
        as I don't see a reason of putting 2-slots units in one slot*/
        if (slot.tailOf2xUnit) return;

        // selection of slots logic
        if (slot == secondSelection)
        {
            secondSelection = null;
        }
        else if (slot == firstSelection)
        {
            firstSelection = secondSelection;
            secondSelection = null;
        }
        else
        {
            secondSelection = firstSelection;
            firstSelection = slot;
        }
        // Going through list and making selections
        foreach (var army in armies)
        {
            foreach (var s in army.slots)
            {
                var shouldBeSelected = s == firstSelection || s == secondSelection;
                s.SetSelected(shouldBeSelected);
                // Swap button state (not in update/fixed update to save resources)
                InteractableButtonSwap();
            }
        }
    }
    private void InteractableButtonSwap()
    {
        // if there is less than two selected
        if (SelectedSlots.Count() < 2)
            swapButton.interactable = false;
        // if there are 2 selected but they are empty
        else if (SelectedSlots.FirstOrDefault(s => s.unit) == null)
            swapButton.interactable = false;
        // checking if there are 2  selected units
        else if (SelectedSlots.FirstOrDefault(s => s.unit) != null &&
                SelectedSlots.Where(s => s.unit).Skip(1).FirstOrDefault() != null)
        {
            // checking if that are the same units
            if (firstSelection.unit.name == secondSelection.unit.name)
                swapButton.interactable = false;
            else
                swapButton.interactable = true;
        }
        else
            swapButton.interactable = true;

    }
    public void Add1ButtonHandler()
    {
        foreach (var s in SelectedSlots)
        {
            if (s.unit != null) continue;

            s.unit = Instantiate(unit1x, s.transform);
        }
        InteractableButtonSwap();
    }

    public void Add2ButtonHandler()
    {
        foreach (var s in SelectedSlots)
        {
            if (s.unit != null) continue;

            var neighbor = GetRightNeighbor(s);
            if (neighbor == null || neighbor.unit != null) continue;

            s.unit = Instantiate(unit2x, s.transform);
            var doubleUnit = s.unit.GetComponent<DoubleUnit>();
            neighbor.unit = s.unit;
            neighbor.tailOf2xUnit = true;
            doubleUnit.tailSlot = neighbor;
        }
        InteractableButtonSwap();
    }

    private Slot GetRightNeighbor(Slot s)
    {
        // Finding army that this slot belongs to:
        var slotArmy = armies.FirstOrDefault(a => a.slots.Contains(s));
        if (slotArmy == null) return null;

        var index = slotArmy.slots.IndexOf(s);
        // not contained or most on the right
        if (index < 0 || index == slotArmy.slots.Count - 1) return null;

        // find neighbor slot in the army: 
        return slotArmy.slots[index + 1];
    }
    public void DeleteButtonHandler()
    {
        foreach (var s in SelectedSlots)
            if (s.unit != null)
            {
                // checking if unit is 2-slot and while it is, that sets false to right slot
                var doubleUnit = s.unit.GetComponent<DoubleUnit>();
                if (doubleUnit != null && doubleUnit.tailSlot != null)
                    doubleUnit.tailSlot.tailOf2xUnit = false;
                // deleting unit from slot 
                Destroy(s.unit);
            }
    }
    public void SwapButtonHandler()
    {
        // Swapping if one of the slots is empty
        if (firstSelection.unit == null || secondSelection.unit == null)
        {
            if (firstSelection.unit != null)
            {
                if (firstSelection.unit.name.Contains("1x"))
                    SwapX1WithEmpty();
                else if (firstSelection.unit.name.Contains("2x") && GetRightNeighbor(secondSelection) == firstSelection)
                    SwapOneLeftUnitx2();
                else
                    SwapX2WithEmpty();
            }
            else if (secondSelection.unit != null)
            {
                if (secondSelection.unit.name.Contains("1x"))
                    SwapX1WithEmpty();
                else if (secondSelection.unit.name.Contains("2x") && GetRightNeighbor(firstSelection) == secondSelection)
                    SwapOneLeftUnitx2();
                else
                    SwapX2WithEmpty();
            }
        }
        else if (UnitX1Exists() && GetRightNeighbor(UnitX1Exists()) == UnitX2Exists())
            SwapLeftX1WithRightX2();
        else
            SimpleSwap();
    }
    private void SimpleSwap()
    {
        Debug.LogWarning("SimpleSwap()");
        // Simple swap by name that demands having something like "2xSlot/1xSlot" in name of Units
        var doubleUnitSlot = UnitX2Exists();
        var singleUnitSlot = UnitX1Exists();
        // safety 
        if (doubleUnitSlot == null || singleUnitSlot == null) return;

        var neighbor = GetRightNeighbor(singleUnitSlot);
        // clean swap condition: 
        if (neighbor != null && neighbor.unit == null)
        {
            var doubleUnit = doubleUnitSlot.unit.GetComponent<DoubleUnit>();
            // clear tail unit slot:
            doubleUnit.tailSlot.unit = null;
            // move double unit to single unit slot:
            doubleUnitSlot.unit = singleUnitSlot.unit;
            // reset position and parent of double unit:
            doubleUnitSlot.unit.transform.SetParent(doubleUnitSlot.transform);
            doubleUnitSlot.unit.transform.localPosition = Vector3.zero;

            singleUnitSlot.unit = doubleUnit.gameObject;
            neighbor.unit = doubleUnit.gameObject;
            doubleUnit.tailSlot.tailOf2xUnit = false;
            doubleUnit.tailSlot = neighbor;
            singleUnitSlot.unit.transform.SetParent(singleUnitSlot.transform);
            singleUnitSlot.unit.transform.localPosition = Vector3.zero;
            neighbor.tailOf2xUnit = true;
        }
    }
    private void SwapX1WithEmpty()
    {
        Debug.LogWarning("SwapX1WithEmpty");
        var singleUnitSlot = UnitDontExists();
        var freeSlot = UnitExists();
        // safety 
        if (freeSlot == null || singleUnitSlot == null) return;

        freeSlot.unit = singleUnitSlot.unit;
        freeSlot.unit.transform.SetParent(freeSlot.transform);
        freeSlot.unit.transform.localPosition = Vector3.zero;

        singleUnitSlot.unit = null;
    }
    private void SwapX2WithEmpty()
    {
        Debug.LogWarning("SwapX2WithEmpty");
        var doubleUnitSlot = UnitDontExists();
        var freeSlot = UnitExists();

        if (freeSlot == null || doubleUnitSlot == null) return;

        var neighbor = GetRightNeighbor(freeSlot);
        if (neighbor != null && neighbor.unit == null)
        {
            var doubleUnit = doubleUnitSlot.unit.GetComponent<DoubleUnit>();
            doubleUnit.tailSlot.unit = null;

            freeSlot.unit = doubleUnitSlot.unit;
            freeSlot.unit.transform.SetParent(freeSlot.transform);
            freeSlot.unit.transform.localPosition = Vector3.zero;

            freeSlot.unit = doubleUnit.gameObject;
            neighbor.unit = doubleUnit.gameObject;
            doubleUnit.tailSlot.tailOf2xUnit = false;
            doubleUnit.tailSlot = neighbor;
            neighbor.tailOf2xUnit = true;

            doubleUnitSlot.unit = null;
        }
    }
    private void SwapOneLeftUnitx2()
    {
        var doubleUnitSlot = UnitDontExists();
        var freeSlot = UnitExists();

        if (freeSlot == null || doubleUnitSlot == null) return;

        var neighbor = GetRightNeighbor(freeSlot);

        if (neighbor != null && neighbor.unit != null)
        {
            Debug.LogWarning("SwapOneLeftUnitx2");

            var doubleUnit = doubleUnitSlot.unit.GetComponent<DoubleUnit>();

            doubleUnit.tailSlot.unit = null;
            doubleUnit.tailSlot.tailOf2xUnit = false;
            doubleUnit.tailSlot = neighbor;

            freeSlot.unit = doubleUnitSlot.unit;
            freeSlot.unit.transform.SetParent(freeSlot.transform);
            freeSlot.unit.transform.localPosition = Vector3.zero;

            freeSlot.unit = doubleUnit.gameObject;
            neighbor.unit = doubleUnit.gameObject;

            neighbor.tailOf2xUnit = true;
            doubleUnitSlot.unit = null;
        }
    }
    private void SwapLeftX1WithRightX2()
    {
        /*
         * TODO:
            REPAIR SINGLE SLOT UNIT TO MOVE INTO TAIL OF 2XUNIT
         */
        var singleUnitSlot = UnitX1Exists();
        var doubleUnitSlot = UnitX2Exists();

        if (doubleUnitSlot == null || singleUnitSlot == null) return;

        var neighbor = GetRightNeighbor(singleUnitSlot);

        if (neighbor != null && neighbor.unit != null)
        {
            Debug.LogWarning("SwapLeftX1WithRightX2");
            return;
        }
    }

    // getting methods to improve code readablity
    private Slot UnitExists()
    {
        return SelectedSlots.FirstOrDefault(s => s.unit == null);
    }
    private Slot UnitDontExists()
    {
        return SelectedSlots.FirstOrDefault(s => s.unit != null);
    }
    private Slot UnitX1Exists()
    {
        return SelectedSlots.FirstOrDefault(s => s.unit.name.Contains("1x"));
    }
    private Slot UnitX2Exists()
    {
        return SelectedSlots.FirstOrDefault(s => s.unit.name.Contains("2x"));
    }
}
