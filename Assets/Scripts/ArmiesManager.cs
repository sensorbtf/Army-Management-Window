using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArmiesManager : MonoBehaviour
{
    public GameObject unit1x;
    public GameObject unit2x;
    
    public List<Army> armies;
    
    public static ArmiesManager Instance;

    private Slot lastSelection;
    private Slot previousSelection;

    private IEnumerable<Slot> SelectedSlots => new[] {lastSelection, previousSelection}.Where(s => s != null);

    private void Awake()
    {
        Instance = this;
    }

    public void SelectSlot(Slot slot)
    {
        if (slot.tailOf2xUnit) return;
        
        // handle selection:
        if (slot == previousSelection)
        {
            previousSelection = null;
        }
        else if (slot == lastSelection)
        {
            lastSelection = previousSelection;
            previousSelection = null;
        }
        else
        {
            previousSelection = lastSelection;
            lastSelection = slot;
        }
        
        foreach (var army in armies)
        {
            foreach (var s in army.slots)
            {
                var shouldBeSelected = s == lastSelection || s == previousSelection;
                s.SetSelected(shouldBeSelected);
            }
        }
    }

    public void Add1ButtonHandler()
    {
        foreach (var s in SelectedSlots)
        {
            if (s.unit != null) continue;

            s.unit = Instantiate(unit1x, s.transform);
        }
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
    }

    private Slot GetRightNeighbor(Slot s)
    {
        // find army that this slot belongs to:
        var slotArmy = armies.FirstOrDefault(a => a.slots.Contains(s));
        if (slotArmy == null) return null;

        var index = slotArmy.slots.IndexOf(s);
        // not contained or right most, continue (cannot place 2x unit)
        if (index < 0 || index == slotArmy.slots.Count - 1) return null;
            
        // find neighbor slot in the army: 
        return slotArmy.slots[index + 1];
    }

    public void DeleteButtonHandler()
    {
        foreach (var s in SelectedSlots)
            if (s.unit != null)
            {
                var doubleUnit = s.unit.GetComponent<DoubleUnit>();
                if (doubleUnit != null && doubleUnit.tailSlot != null)
                    doubleUnit.tailSlot.tailOf2xUnit = false;

                Destroy(s.unit);
            }
    }

    public void SwapButtonHandler()
    {
        // nothing to swap:
        if (SelectedSlots.Count() < 2) return;

        // TODO: swapping if one of the slots is empty? 
        // for now, only swapping full slots: 
        if (lastSelection.unit == null || previousSelection.unit == null) return;
        
        // not swapping the same units:
        // there's a case where you can end up with leftover highlight on a 2x unit tail, but let's ignore that for now.
        if (lastSelection.unit.name == previousSelection.unit.name) return;

        // clean swap conditions: 
        // checking by name is hacky, but let's have it for now:
        var doubleUnitSlot = SelectedSlots.FirstOrDefault(s => s.unit.name.Contains("2x"));
        var singleUnitSlot = SelectedSlots.FirstOrDefault(s => s.unit.name.Contains("1x"));
        // safety check
        if (doubleUnitSlot == null || singleUnitSlot == null) return;

        var neighbor = GetRightNeighbor(singleUnitSlot);
        // clean swap condition: 
        if (neighbor != null && neighbor.unit == null)
        {
            // perform the swap: 
            var doubleUnit = doubleUnitSlot.unit.GetComponent<DoubleUnit>();
            // clear tail unit slot:
            doubleUnit.tailSlot.unit = null;
            // move single unit to double unit slot:
            doubleUnitSlot.unit = singleUnitSlot.unit;
            // reset position and parent of single unit:
            doubleUnitSlot.unit.transform.SetParent(doubleUnitSlot.transform);
            doubleUnitSlot.unit.transform.localPosition = Vector3.zero;

            // move the double unit to single unit slot:
            singleUnitSlot.unit = doubleUnit.gameObject;
            neighbor.unit = doubleUnit.gameObject;
            doubleUnit.tailSlot.tailOf2xUnit = false;
            doubleUnit.tailSlot = neighbor;
            singleUnitSlot.unit.transform.SetParent(singleUnitSlot.transform);
            singleUnitSlot.unit.transform.localPosition = Vector3.zero;
            neighbor.tailOf2xUnit = true;
        }
    }
}
