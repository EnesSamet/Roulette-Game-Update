Roulette Game Prototype

Overview This Unity project is a **Roulette Game Prototype** built for the **Joker Games 2025 Case Study**.  
It simulates a full roulette table experience — with interactive betting, animated wheel and ball spins, realistic chip visuals, sound effects, and UI feedback.

---

Gameplay & Controls

Placing Bets | Action | Description |  
| **Left Click (LMB)** | Place a chip on a number, edge, or outside bet. |   
| **Right Click (RMB)** | Remove a single chip from that bet. |   
| **Shift \+ LMB** | Place a *Street (lane)* bet (3-number horizontal line). |   
| **Ctrl \+ LMB** | Place a *Six Line* bet (two adjacent rows). |   
| **Buttons** | Change the chip value. | 

Starting a Spin

- Press the End Betting button to open the number selection menu. After opening the menu either write a number and press the Select Number button or press the Random Number button to spin the wheel.  
- You can **select a deterministic number** or **spin randomly** using the number selection panel.  
- The wheel and ball spin realistically — the ball makes several laps before landing.  
- A result panel appears showing:  
  - Winning number (with color highlight)  
  - Win/Lose message  
  - Payout or loss amount

Bet History

- Press **History** to toggle the **Bet History Panel**.  
- Shows:  
  - All placed bets (“Bet: 18 – $20”)  
  - Round results with color-coded winning numbers (red/black/green)  
  - Total spins, stakes, and payouts.  
- Keeps the **last 10 spins** (This can be adjusted) for performance and readability.

Chip Values

- The **bottom buttons** change the chip denomination.  
- A sound plays when placing or removing a chip.  
- All chips automatically merge on the same bet and update their amount visually.

Menus & Quit

- **MainMenu.cs** controls the title screen:  
  - *PlayTheGame()* → Loads the roulette scene.  
  - *QuitTheGame()* → Closes the application.  
- **Quit button** exits the game (calls `Application.Quit()`).

---

Design Patterns Used

| Pattern | Used In | Purpose |
| :---- | :---- | :---- |
| **Singleton** | `GameManager` | Manages global state, player balance, and transitions. |
| **Factory** | `BetBuilder` | Creates different bet types (Straight, Split, Street, etc.) without duplicating logic. |
| **Observer** | `BetHistory`, `ChipSpawner` | Update UI and visuals automatically when bets change. |
| **MVC (Model–View–Controller)** | Global architecture | *Model:* `Bet`, *View:* Chips/UI Panels, *Controller:* `GridInput`, `GridBets`, and `GameManager`. |
| **Strategy (minor use)** | `RouletteSpinner` easing curves | Allows configurable wheel/ball motion. |

---

Known Issues 

- You can't place a bet on zero  
- There is no numbers on the roulette just colors   
- There is no background on the main menu and the game scene   
- The table is just a png there is no table model

Future Improvements

- Being able place a bet on zero   
- Adding better texture to the roulette to add both numbers and color to it   
- Adding a background for the main menu and the game scene  
- Adding a table model with the table texture   
- Making the UI look prettier (I am terrible at design but I think I can make it look better)   
- Adding a save system (I added save systems for the 2 games I worked on, it is easy to me but didn't have time to implement into this game)   
- Being able to switch between american and european roulette table \-Adding max bet limit

## Demo Video Link

https://youtu.be/krnRXPRxEY8