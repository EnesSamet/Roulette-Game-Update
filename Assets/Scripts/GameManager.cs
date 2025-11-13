using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GridBets betController;          
    [SerializeField] GameObject winLosePanel;
    [SerializeField] TextMeshProUGUI winLoseText;
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] TextMeshProUGUI totalMoneyText;  
    [SerializeField] RouletteSpinner spinner;

    [Header("Wheel Range (inclusive)")]
    public int minNumber = 1;   // for European 1..36 (add 0 later if you want)
    public int maxNumber = 36;

    [Header("Money")]
    public int totalMoney;

    [SerializeField] BetHistory betHistory;
    public int totalProfit;
    [SerializeField] TextMeshProUGUI totalProfitText;
    public int chipValue;

    [Header("Win Bias (Optional)")]
    [Range(0f, 1f)] public float winBiasChance = 0.35f;  // 0.0 = kapalı, 0.35 = %35 oyuncu lehine seçim
    public bool weightByStake = true;                    // stake (konulan para) kadar ağırlık artsın mı
    public bool neighborBoost = true;                    // komşu sayılara minik bonus

    [SerializeField] GameObject spinPanel;
    public bool canPlayBet;

    [SerializeField] AudioSource sfxSource;     
    [SerializeField] AudioClip tellerVoice;   
    [SerializeField] AudioClip spinSound;

    private void Start()
    {
        betHistory.bettingHistory = new List<string>(new string[20]);
        betHistory.winningNumbers = new List<int>(new int[7]);
        LoadTheGame();
        UpdateTotalMoney();

        // hide result label at start
        if (winLosePanel != null)
            winLosePanel.gameObject.SetActive(false);
    }

    public void StartSpinRandom()
    {
        StartCoroutine(SpinOnceCo(null)); // null = random (bias'lı seçim burada çalışır)
    }

    public void StartSpinWithNumber(int number)
    {
        StartCoroutine(SpinOnceCo(number));
    }

    // keep a no-arg helper if you want to still support Enter key
    private void SpinOnce() { StartCoroutine(SpinOnceCo(null)); }

    // make the coroutine accept a nullable forced number
    private IEnumerator SpinOnceCo(int? forcedNumber)
    {
        sfxSource.PlayOneShot(spinSound, 1f);
        // pick a winning number: forced or biased/random
        int winningNumber;
        if (forcedNumber.HasValue)
        {
            winningNumber = Mathf.Clamp(forcedNumber.Value, minNumber, maxNumber);
        }
        else
        {
            winningNumber = GetBiasedWinningNumber(); // <— BIASLI SEÇİM
        }

        // play the wheel/ball animation if available
        if (spinner != null)
            yield return spinner.SpinToRoutine(winningNumber);

        // now settle all placed bets
        int payoutReturned = betController.ResolveSpin(winningNumber);

        // show result text (your existing UI code)
        if (winLosePanel != null)
        {
            winLosePanel.gameObject.SetActive(true);

            if (payoutReturned > 0)
            {
                winLoseText.color = Color.green;
                winLoseText.text = "WIN!\nWinning Number: " + winningNumber + "\nPayout: " + payoutReturned;
            }
            else
            {
                winLoseText.color = Color.red;
                winLoseText.text = "LOSE!\nWinning Number: " + winningNumber;
            }
        }

        if (betHistory != null) betHistory.AddSpinResult(winningNumber, payoutReturned);

        // your money/profit refresh logic (unchanged)
        totalProfit += payoutReturned - (betHistory != null ? betHistory.totalBet : 0);
        UpdateTotalMoney();

        if (totalProfitText != null)
        {
            if (totalProfit > 0)
            {
                totalProfitText.color = Color.green;
                totalProfitText.text = "Profit : " + totalProfit;
            }
            else if (totalProfit < 0)
            {
                totalProfitText.color = Color.red;
                totalProfitText.text = "Profit : " + totalProfit;
            }
            else
            {
                totalProfitText.color = Color.white;
                totalProfitText.text = "Profit : " + totalProfit;
            }
        }
        //Sonuç belli olduktan sonra belli bi süre sonra masayı resetleme
        StartCoroutine(WaitToReset(3));
    }

    public void UpdateTotalMoney()
    {
        if (totalMoneyText != null)
            totalMoneyText.text ="Money : " + totalMoney.ToString();
    }

    public void OpenSpinPanel()
    {
        if(betHistory.totalBet > 0)
        {
            spinPanel.SetActive(true);
            canPlayBet = false;
        }
    }

    public void ResetTheGame()
    {
        // hide result label
        if (winLosePanel != null)
            winLosePanel.gameObject.SetActive(false);

        // clear current round bets
        if (betController != null)
        {
            betController.betList.Clear();
            betController.activeBets.Clear();
            betController.betText.text = "";
            // clear any chip visuals if you use ChipSpawner
            var spawner = GetComponent<ChipSpawner>();
            if (spawner != null) spawner.ClearAll();
        }
        if (betHistory != null) betHistory.totalBet = 0;
        canPlayBet = true;
        sfxSource.PlayOneShot(tellerVoice, .7f);
        EndRound(betHistory.winningNumbers, totalProfit, totalMoney);
    }
    //Waits a bit to reset the game
    IEnumerator WaitToReset(int seconds)
    {
        for (int i = seconds; i >= 0; i--)
        {
            countdownText.text = "Next Round in " + i;
            yield return new WaitForSeconds(1);
        }
        ResetTheGame();
    }

    // =========================
    //      BIAS LOGIC
    // =========================

    // aktif bahislerden ağırlıklı bir sayı döndürür (bias kapalıysa normal random)
    int GetBiasedWinningNumber()
    {
        // şartlar uygun değilse normal random
        if (winBiasChance <= 0f ||
            betController == null ||
            betController.activeBets == null ||
            betController.activeBets.Count == 0)
        {
            return Random.Range(minNumber, maxNumber + 1);
        }

        // winBiasChance ihtimalle oyuncu lehine seçim
        if (Random.value > winBiasChance)
        {
            return Random.Range(minNumber, maxNumber + 1);
        }

        // ağırlık havuzu
        Dictionary<int, int> weights = new Dictionary<int, int>();

        // oyuncunun oynadığı tüm sayıları topla ve ağırlık ver
        for (int i = 0; i < betController.activeBets.Count; i++)
        {
            var bet = betController.activeBets[i];
            if (bet == null || bet.numbers == null) continue;

            for (int k = 0; k < bet.numbers.Count; k++)
            {
                int n = bet.numbers[k];
                if (n < minNumber || n > maxNumber) continue;

                int baseW = 1; // temel ağırlık
                int stakeBoost = 0;
                if (weightByStake)
                {
                    // chipValue'ya göre kaba bir boost (en az 1)
                    stakeBoost = Mathf.Max(1, bet.stake / Mathf.Max(1, chipValue));
                }

                int add = baseW + stakeBoost;

                if (!weights.ContainsKey(n)) weights[n] = 0;
                weights[n] += add;

                // komşulara ufak bonus
                if (neighborBoost)
                {
                    AddNeighborBonus(weights, n, 1);
                }
            }
        }

        // hiç ağırlık yoksa normal random
        if (weights.Count == 0)
            return Random.Range(minNumber, maxNumber + 1);

        // ağırlıklı rastgele seçim
        int total = 0;
        foreach (var kv in weights) total += kv.Value;

        int r = Random.Range(0, total); // [0, total)
        foreach (var kv in weights)
        {
            if (r < kv.Value) return kv.Key;
            r -= kv.Value;
        }

        // güvenlik
        return Random.Range(minNumber, maxNumber + 1);
    }

    // basit komşu bonusu: n-1 ve n+1'e küçük ağırlık
    void AddNeighborBonus(Dictionary<int, int> weights, int n, int bonus)
    {
        int left = n - 1;
        int right = n + 1;

        if (left >= minNumber && left <= maxNumber)
        {
            if (!weights.ContainsKey(left)) weights[left] = 0;
            weights[left] += bonus;
        }
        if (right >= minNumber && right <= maxNumber)
        {
            if (!weights.ContainsKey(right)) weights[right] = 0;
            weights[right] += bonus;
        }
    }
    //Save helpers
    public void EndRound(List<int> winningNumber, int profitChange, int moneyDelta) 
    { 
        // Append to lists + update profit (no overwrite)
        SaveManager.AddWiningNumber(winningNumber , betHistory); 
        // Update money if you need to
        SaveManager.AddMoneyAndProfit(moneyDelta, profitChange);
        SaveManager.AddBet(betHistory.bettingHistory, betHistory);
        Debug.Log("Round ended. Auto-saved."); 
    }

    public void LoadTheGame()
    {

        var data = SaveManager.Load();

        totalMoney = data.money;
        totalProfit = data.profit;

        
        if(data.bettingHistory.Count <= 20)
        {
            betHistory.bettingHistory = data.bettingHistory;
        }
        else
        {
            Debug.Log(betHistory.bettingHistory.Count);
            int x = 0;
            for (int i = data.bettingHistory.Count - 20; i < data.bettingHistory.Count; i++)
            {
                betHistory.bettingHistory[x] = data.bettingHistory[i];
                x++;
            }
        }
        betHistory.winningNumbers = data.winningNumbers;
    }
}
