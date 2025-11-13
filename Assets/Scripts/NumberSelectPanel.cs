using UnityEngine;
using TMPro;

public class NumberSelectPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameManager gameManager;   // drag your GameManager here
    [SerializeField] private TMP_InputField numberInput; // the input field where player types number
    [SerializeField] private TextMeshProUGUI errorText;  // optional: show validation errors

    // Called by the "Spin With Number" button
    public void OnClick_SpinWithNumber()
    {
        if (gameManager == null) return;

        // read input
        string txt = numberInput != null ? numberInput.text : "";
        int num;

        if (!int.TryParse(txt, out num))
        {
            ShowError("Please enter a number.");
            return;
        }

        // Validate 0..36 (European wheel, if you only use 1..36 change min to 1)
        if (num < 0 || num > 36)
        {
            ShowError("Number must be between 0 and 36.");
            return;
        }

        // OK → hide panel and spin to this number
        gameObject.SetActive(false);
        gameManager.StartSpinWithNumber(num);
    }

    // Called by the "Random" button
    public void OnClick_SpinRandom()
    {
        if (gameManager == null) return;

        gameObject.SetActive(false);
        gameManager.StartSpinRandom();
    }

    void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }

    // Optional: clear errors when opened
    void OnEnable()
    {
        if (errorText != null) errorText.text = "";
    }
}
