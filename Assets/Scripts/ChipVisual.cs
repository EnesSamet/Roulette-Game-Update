using UnityEngine;
using TMPro;

public class ChipVisual : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label; // optional number label on the chip
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float stackScaleStep = 0.03f; // slight scale-up when stake increases
    [SerializeField] Vector3 chipSize;

    public int stake;

    public void SetStake(int newStake)
    {
        stake = newStake;

        // Update label text (e.g., show total stake)
        if (label != null)
            label.text = stake.ToString();

        // Optional: make the chip slightly bigger as stake grows
        float s = baseScale + (stake / 10f) * stackScaleStep; // 10 = your chipValue
        transform.localScale = chipSize * s;
    }
}
