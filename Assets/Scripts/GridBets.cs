using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class GridBets : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] public GameManager manager;       // has totalMoney, chipValue, etc.
    [SerializeField] public ChipSpawner chipSpawner;   // spawns / updates chip visuals
    [SerializeField] public BetHistory betHistory;     // logs placed/removed/spins
    [SerializeField] public TextMeshProUGUI betText;   // simple UI line like "10$ to 17 | 10$ to Street."

    [Header("State")]
    public List<int> betList = new List<int>();        // simple flat list for your old UI
    public List<Bet> activeBets = new List<Bet>();     // the real bet objects

    [Header("Chip Visuals")]
    public float chipYOffset = 0.02f;                  // small lift if needed

    public int winnerBetTotal;

    // --- SFX ---
    [SerializeField] AudioSource sfxSource;     // drag any AudioSource here (can be on the same GO)
    [SerializeField] AudioClip chipPlaceClip;   // sound when placing a chip
    [SerializeField] float sfxVolume = 1f;      // volume for PlayOneShot

    float _lastSfxTime;                         // tiny cooldown so multiple plays don't stack in same frame

    // ===== Helpers for robust bet matching =====
    static List<int> ParseNumbersFromName(string name)
    {
        var list = new List<int>();
        if (string.IsNullOrEmpty(name)) return list;
        int val = 0; bool inNum = false;
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsDigit(c))
            {
                inNum = true;
                val = val * 10 + (c - '0');
            }
            else
            {
                if (inNum) { list.Add(val); val = 0; inNum = false; }
            }
        }
        if (inNum) list.Add(val);
        list.Sort();
        return list;
    }

    static bool SameBet(string aName, List<int> aNums, string bName, List<int> bNums)
    {
        // quick accept when names are identical
        if (aName == bName) return true;

        bool aHasNums = aNums != null && aNums.Count > 0;
        bool bHasNums = bNums != null && bNums.Count > 0;
        if (aHasNums && bHasNums)
        {
            if (aNums.Count != bNums.Count) return false;
            for (int i = 0; i < aNums.Count; i++)
                if (aNums[i] != bNums[i]) return false;
            return true;
        }
        return false;
    }

    // ===== API used by GridInput =====

    void PlayChipSfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        // small cooldown to avoid double-plays on merge
        if (Time.time - _lastSfxTime < 0.03f) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
        _lastSfxTime = Time.time;
    }

    // place one bet (called by GridInput)
    public void PlaceBet(Bet bet, Vector3 chipWorld)
    {
        if (bet == null || bet.numbers == null || bet.numbers.Count == 0) return;
        if (manager == null || manager.totalMoney < manager.chipValue) return;

        bool merged = false;
        int newTotalStake = bet.stake;

        // numbers list in Bet is assumed to already be filled; take a sorted copy for comparison
        var betNumsSorted = new List<int>(bet.numbers);
        betNumsSorted.Sort();

        for (int i = 0; i < activeBets.Count; i++)
        {
            var existing = activeBets[i];
            var existingNumsSorted = new List<int>(existing.numbers);
            existingNumsSorted.Sort();

            if (SameBet(existing.name, existingNumsSorted, bet.name, betNumsSorted))
            {
                existing.stake += bet.stake;
                newTotalStake = existing.stake;     // <-- total on this bet
                merged = true;
                if (chipSpawner) chipSpawner.ShowOrMove(bet.name, chipWorld, existing.stake);
                break;
            }
        }
        if (!merged)
        {
            activeBets.Add(bet);
            newTotalStake = bet.stake;                   // first chip on this bet
            if (chipSpawner) chipSpawner.ShowOrMove(bet.name, chipWorld, bet.stake);
        }

        PlayChipSfx(chipPlaceClip);

        // money
        manager.totalMoney -= manager.chipValue;
        manager.UpdateTotalMoney();
        betHistory.totalBet += manager.chipValue;

        // HISTORY: send the label + TOTAL stake
        if (betHistory != null)
        {
            // straight = show number, else show name
            string label = (bet.numbers != null && bet.numbers.Count == 1)
                ? bet.numbers[0].ToString()
                : bet.name;

            betHistory.AddOrUpdatePlacedBet(label, newTotalStake);
        }

        // old quick UI list (optional, you can drop it)
        betList.AddRange(bet.numbers);
        RebuildBetText();
        SaveManager.AddBet(betHistory.bettingHistory, betHistory);
    }

    public bool TryRemoveBet(string betName, Vector3 mouseScreenPos)
    {
        // normalize target by numbers so name formatting differences don't matter
        var targetNums = ParseNumbersFromName(betName);

        for (int i = 0; i < activeBets.Count; i++)
        {
            var a = activeBets[i];
            var aNumsSorted = new List<int>(a.numbers);
            aNumsSorted.Sort();

            if (SameBet(a.name, aNumsSorted, betName, targetNums))
            {
                Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    ChipVisual chip = hit.collider.GetComponent<ChipVisual>();
                    if (chip != null)
                    {
                        // refund one chip or whatever is left on the chip
                        int refund = Mathf.Min(manager.chipValue, chip.stake);
                        a.stake -= refund;

                        // --- UPDATE / REMOVE CHIP VISUAL ---
                        if (a.stake <= 0)
                        {
                            if (chipSpawner) chipSpawner.RemoveChip(a.name);

                            // --- HISTORY: remove "Bet : ." line totally ---
                            if (betHistory != null)
                            {
                                string label = HistoryLabelFromBetName(a.name);
                                betHistory.RemovePlacedBetLine(label);
                            }

                            activeBets.RemoveAt(i);
                        }
                        else
                        {
                            if (chipSpawner) chipSpawner.ShowOrMove(a.name, chip.transform.position, a.stake);

                            // --- HISTORY: update "Bet : ." line with NEW TOTAL ---
                            if (betHistory != null)
                            {
                                string label = HistoryLabelFromBetName(a.name);
                                betHistory.AddOrUpdatePlacedBet(label, a.stake);
                            }
                        }

                        // refund to player
                        manager.totalMoney += refund;
                        manager.UpdateTotalMoney();

                        // (optional) keep a separate “Removed: … - $refund” line
                        //if (betHistory != null) betHistory.AddRemovedBet(betName, refund);

                        // refresh small HUD
                        RebuildBetText();
                        return true;
                    }
                }
            }
        }
        SaveManager.AddBet(betHistory.bettingHistory, betHistory);
        return false;
    }

    // For history: straights use just the number, others use the bet name
    string HistoryLabelFromBetName(string betName)
    {
        const string prefix = "Straight ";
        if (betName.StartsWith(prefix)) return betName.Substring(prefix.Length); // "Straight 18" -> "18"
        return betName; // e.g. "Split 17-20", "Corner 1-2-4-5", "Red", .
    }


    // called by GameManager when a spin ends
    public int ResolveSpin(int resultNumber)
    {
        int totalReturn = 0;

        for (int i = 0; i < activeBets.Count; i++)
        {
            Bet b = activeBets[i];
            bool hit = false;

            for (int k = 0; k < b.numbers.Count; k++)
            {
                if (b.numbers[k] == resultNumber) { hit = true; break; }
            }

            if (hit)
            {
                int win = b.stake * b.payout;   // winnings (stake * odds)
                totalReturn += win + b.stake;   // return includes stake back
                winnerBetTotal += b.stake;      // track how much stake hit
            }
        }

        if (totalReturn > 0 && manager != null)
        {
            manager.totalMoney += totalReturn;
            manager.UpdateTotalMoney();
        }

        // clear table
        activeBets.Clear();
        betList.Clear();
        RebuildBetText();

        if (chipSpawner) chipSpawner.ClearAll();

        return totalReturn;
    }

    public void RebuildBetText()
    {
        if (betText == null) return;

        var parts = new List<string>();
        for (int i = 0; i < activeBets.Count; i++)
        {
            var b = activeBets[i];

            // show number for straights, name for others
            string label = (b.numbers != null && b.numbers.Count == 1)
                ? b.numbers[0].ToString()
                : b.name;

            // IMPORTANT: use total stake on this bet (not chipValue)
            parts.Add($"{b.stake}$ to {label}");
        }

        betText.text = string.Join(" | ", parts);
    }
}
