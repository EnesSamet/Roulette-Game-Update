using System.Collections.Generic;

public static class BetBuilder
{
    // red numbers helper
    public static readonly HashSet<int> Reds = new HashSet<int> {
        1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36
    };

    public static Bet Straight(int number, int stake)
    {
        return new Bet
        {
            name = "Straight " + number,
            numbers = new List<int> { number },
            payout = 35,
            stake = stake
        };
    }

    public static Bet Split(int a, int b, int stake)
    {
        var nums = new List<int> { a, b };
        nums.Sort();
        return new Bet
        {
            name = $"Split {nums[0]}-{nums[1]}",
            numbers = nums,
            payout = 17,
            stake = stake
        };
    }

    public static Bet Corner(List<int> nums, int stake)
    {
        nums.Sort();
        return new Bet
        {
            name = $"Corner {string.Join("-", nums)}",
            numbers = nums,
            payout = 8,
            stake = stake
        };
    }

    public static Bet Street(List<int> nums, int stake)
    {
        nums.Sort();
        return new Bet
        {
            name = $"Street {string.Join("-", nums)}",
            numbers = nums,
            payout = 11,
            stake = stake
        };
    }

    public static Bet SixLine(List<int> nums, int stake)
    {
        nums.Sort();
        return new Bet
        {
            name = $"Six Line {string.Join("-", nums)}",
            numbers = nums,
            payout = 5,
            stake = stake
        };
    }

    // Outside bets: columns, dozens, high/low, odd/even, red/black
    public static Bet Outside(int code, int stake)
    {
        Bet b = new Bet { stake = stake };

        switch (code)
        {
            case -1: b.name = "Column 1"; b.payout = 2; for (int i = 1; i <= 34; i += 3) b.numbers.Add(i); return b;
            case -2: b.name = "Column 2"; b.payout = 2; for (int i = 2; i <= 35; i += 3) b.numbers.Add(i); return b;
            case -3: b.name = "Column 3"; b.payout = 2; for (int i = 3; i <= 36; i += 3) b.numbers.Add(i); return b;

            case -4: b.name = "Dozen 25–36"; b.payout = 2; for (int i = 25; i <= 36; i++) b.numbers.Add(i); return b;
            case -5: b.name = "Dozen 13–24"; b.payout = 2; for (int i = 13; i <= 24; i++) b.numbers.Add(i); return b;
            case -6: b.name = "Dozen 1–12"; b.payout = 2; for (int i = 1; i <= 12; i++) b.numbers.Add(i); return b;

            case -7: b.name = "High 19–36"; b.payout = 1; for (int i = 19; i <= 36; i++) b.numbers.Add(i); return b;
            case -8: b.name = "Odd"; b.payout = 1; for (int i = 1; i <= 36; i++) if ((i % 2) != 0) b.numbers.Add(i); return b;
            case -9: b.name = "Even"; b.payout = 1; for (int i = 2; i <= 36; i += 2) b.numbers.Add(i); return b;

            case -10: b.name = "Low 1–18"; b.payout = 1; for (int i = 1; i <= 18; i++) b.numbers.Add(i); return b;
            case -11: b.name = "Red"; b.payout = 1; for (int i = 1; i <= 36; i++) if (Reds.Contains(i)) b.numbers.Add(i); return b;
            case -12: b.name = "Black"; b.payout = 1; for (int i = 1; i <= 36; i++) if (!Reds.Contains(i)) b.numbers.Add(i); return b;
        }

        return null;
    }
    // ========= NEW HELPERS (fix GridInput errors) =========

    // "Straight 17"
    public static string StraightName(int n)
    {
        return "Straight " + n;
    }

    // "Split 8-11" (always sorted)
    public static string SplitName(int a, int b)
    {
        if (a > b) { int t = a; a = b; b = t; }
        return $"Split {a}-{b}";
    }

    // Outside display name from code (match the names used above)
    public static string OutsideName(int code)
    {
        switch (code)
        {
            case -1: return "Column 1";
            case -2: return "Column 2";
            case -3: return "Column 3";
            case -4: return "Dozen 25–36";
            case -5: return "Dozen 13–24";
            case -6: return "Dozen 1–12";
            case -7: return "High 19–36";
            case -8: return "Odd";
            case -9: return "Even";
            case -10: return "Low 1–18";
            case -11: return "Red";
            case -12: return "Black";
        }
        return null; // unknown outside code
    }
}
