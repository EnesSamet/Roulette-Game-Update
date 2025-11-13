using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BetHistory : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject betHistoryPanel;     // panel you toggle open/close
    [SerializeField] TextMeshProUGUI historyText;    // the text inside the panel (assign in Inspector)
    [SerializeField] TextMeshProUGUI winningNumbersText;  // optional, to show all winning numbers
    [SerializeField] GridBets betController;

    [Header("Settings")]
    [SerializeField] int maxLines = 10;             // keep last N lines

    // stored lines like: "Bet: Street 1-2-3 - $10"
    public List<string> bettingHistory = new List<string>(new string[20]);

    // Track number of spins, winning numbers, and total payout/stakes
    public int totalSpins = 0;
    public List<int> winningNumbers = new List<int>(new int[7]);  // List to store winning numbers
    public List<int> payouts = new List<int>();         // List to store the payouts

    public int totalBet; //Amount of money played in one round
    [SerializeField] int maxSpinLines; // Max spin lines

    private void Start()
    {
        RefreshText();
        UpdateWinningNumber();
    }
    // toggle the panel
    public void OpenBetHistory()
    {
        if (betHistoryPanel == null) return;
        betHistoryPanel.SetActive(!betHistoryPanel.activeSelf);
    }

    // call this when a bet is PLACED
    public void AddPlacedBet(Bet bet, int chipValue)
    {
        if (bet == null) return;

        // if it's a straight, show the number; else show the bet name
        string label = (bet.numbers != null && bet.numbers.Count == 1)
            ? bet.numbers[0].ToString()
            : bet.name;

        string line = $"Bet: {label} - ${bet.stake}";
        PushLine(line);
    }

    // call this when a bet is REMOVED (optional)
    public void AddRemovedBet(string betName, int amount)
    {
        string line = $"Removed: {betName} - ${amount}";
        PushLine(line);
    }

    // call this AFTER a spin is resolved
    public void AddSpinResult(int winningNumber, int totalReturned)
    {
        totalSpins++;  // count every spin

        // store spin result
        winningNumbers.Add(winningNumber);

        totalReturned += betController.winnerBetTotal - totalBet;
        payouts.Add(totalReturned);

        // keep only last maxSpinLines entries
        if (winningNumbers.Count > maxSpinLines)
            winningNumbers.RemoveRange(0, winningNumbers.Count - maxSpinLines);

        if (payouts.Count > maxSpinLines)
            payouts.RemoveRange(0, payouts.Count - maxSpinLines);

        // write a line and let PushLine() enforce its own max (you already had maxLines there)
        string result = (totalReturned > 0) ? $"WIN +${totalReturned}" : "LOSE";
        string line = $"Spin {totalSpins}: {winningNumber} -> {result}";
        PushLine(line); // PushLine trims bettingHistory using its own maxLines

        // update the colored winning numbers text (only last maxSpinLines)
        UpdateWinningNumber();
    }


    public void UpdateWinningNumber()
    {
        // update the colored winning numbers text (only last maxSpinLines)
        if (winningNumbersText != null)
        {
            var colored = new List<string>();
            // show only the last N items we kept in winningNumbers
            for (int i = Mathf.Max(0, winningNumbers.Count - maxSpinLines); i < winningNumbers.Count; i++)
            {
                int number = winningNumbers[i];
                string color = (number == 0) ? "green" : (BetBuilder.Reds.Contains(number) ? "red" : "black");
                colored.Add($"<color={color}>{number}</color>");
            }
            winningNumbersText.text = string.Join(", ", colored);
        }
    }

    // clear all history (optional button)
    public void ClearHistory()
    {
        bettingHistory.Clear();
        winningNumbers.Clear();
        payouts.Clear();
        totalSpins = 0;  // Reset spin count
        RefreshText();
    }

    // --- internal helpers ---
    void PushLine(string line)
    {
        bettingHistory.Add(line);
        // keep only last maxLines
        if (bettingHistory.Count > maxLines)
            bettingHistory.RemoveRange(0, bettingHistory.Count - maxLines);
        RefreshText();
    }

    void RefreshText()
    {
        if (historyText == null) return;
        historyText.text = string.Join("\n", bettingHistory);
    }
    public void AddOrUpdatePlacedBet(string label, int totalStakeForThisBet)
    {
        // Example final text: "Bet : 18 - $20"
        string targetPrefix = $"Bet : {label} - $";

        // look for an existing line that starts with this bet
        int foundIndex = -1;
        for (int i = 0; i < bettingHistory.Count; i++)
        {
            if (bettingHistory[i].StartsWith(targetPrefix))
            {
                foundIndex = i;
                break;
            }
        }

        string newLine = $"Bet : {label} - ${totalStakeForThisBet}";

        if (foundIndex >= 0)
        {
            bettingHistory[foundIndex] = newLine; // update the total
        }
        else
        {
            bettingHistory.Add(newLine);          // first time this bet shows up
            if (bettingHistory.Count > maxLines)
                bettingHistory.RemoveRange(0, bettingHistory.Count - maxLines);
        }

        RefreshText();
    }
    public void RemovePlacedBetLine(string label)
    {
        // matches the same format you used in AddOrUpdatePlacedBet
        string prefix = $"Bet : {label} - $";
        for (int i = 0; i < bettingHistory.Count; i++)
        {
            if (bettingHistory[i].StartsWith(prefix))
            {
                bettingHistory.RemoveAt(i);
                break;
            }
        }
        RefreshText();
    }


}
