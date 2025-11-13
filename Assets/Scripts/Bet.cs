using System.Collections.Generic;

[System.Serializable]
public class Bet
{
    public string name;
    public List<int> numbers = new List<int>(); // covered numbers
    public int payout;   // e.g. 35 means 35:1
    public int stake;    // how much money on this bet
}
