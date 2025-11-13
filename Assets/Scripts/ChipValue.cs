using UnityEngine;

public class ChipValue : MonoBehaviour
{
    [SerializeField] int newChipValue; //New value to change the cips value
    [SerializeField] GameManager manager; //The chip value is hold in this script
    [SerializeField] ChipSpawner chipSpawner; //The chip is hold here
    [SerializeField] GameObject chipPrefab; //The chip prefab

    //Updating the chip value
    public void UpdateChipValue()
    {
        manager.chipValue = newChipValue;
        chipSpawner.chipPrefab = chipPrefab;
    }
}
