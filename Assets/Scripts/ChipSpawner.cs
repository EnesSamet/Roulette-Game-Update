using System.Collections.Generic;
using UnityEngine;

public class ChipSpawner : MonoBehaviour
{
    public GameObject chipPrefab;
    public Transform chipParent;
    public float yOffset = 0.02f;

    private Dictionary<string, ChipVisual> chips = new Dictionary<string, ChipVisual>();

    public void ShowOrMove(string betName, Vector3 worldPos, int totalStakeForThisBet)
    {
        if (chipPrefab == null) return;

        ChipVisual chip;
        if (!chips.TryGetValue(betName, out chip) || chip == null)
        {
            GameObject go = Instantiate(chipPrefab, worldPos, Quaternion.identity, chipParent);
            chip = go.GetComponent<ChipVisual>();
            chips[betName] = chip;
        }
        else
        {
            //chip.transform.position = worldPos;
        }

        chip.SetStake(totalStakeForThisBet);
    }

    // NEW: remove one chip object by bet name (used when stake becomes 0)
    public void RemoveChip(string betName)
    {
        ChipVisual chip;
        if (chips.TryGetValue(betName, out chip) && chip != null)
        {
            Destroy(chip.gameObject);
        }
        chips.Remove(betName);
    }

    public void ClearAll()
    {
        foreach (var kv in chips)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        chips.Clear();
    }
}
