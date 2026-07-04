using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameDataStore : MonoBehaviour
{
    private static MinigameDataStore instance;
    private static GameData[] minigames;
    private static GameData currentGame;

    public static MinigameDataStore Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<MinigameDataStore>();

            return instance;
        }
    }

    [System.Serializable]
    public struct GameData
    {
        public int id;
        public string gameTitle;
        public string description;
        public string cognitiveSkills;
        public string sceneName;
        public Sprite thumbnail;
    }

    [Header("UI Text References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text skillsText;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text bestScoreText;    // Reference for Best Score Display
    [SerializeField] private TMP_Text averageScoreText; // Reference for Average Score Display

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureDataInitialized();
    }

    private static void EnsureDataInitialized()
    {
        if (minigames != null && minigames.Length > 0) return;

        minigames = new GameData[35];

        // Core Game Levels (id >= 30 are time-based)
        minigames[30] = new GameData
        {
            id = 30,
            gameTitle = "Level 1",
            description = "Your movement keys adapt to the orientation arrow, which always represents UP. (e.g., If the arrow points RIGHT, D moves you Up, and S moves you Right). Avoid enemy traps and rush to the Portal Gate as fast as possible. You cannot escape through the portal if an enemy is hot on your trail! Defeating enemies is optional, but doing so reduces your time taken by 15 seconds.",
            cognitiveSkills = "Attention + Reflex",
            sceneName = "Level1"
        };

        minigames[31] = new GameData
        {
            id = 31,
            gameTitle = "Level 2",
            description = "Ten boxes lie ahead: 5 hold the power sources required to escape, and 5 contain hidden explosives. Pay attention at the start of the game when the correct boxes are shown with blue glitter. Opening a box containing explosive will cost you health. Avoid enemy traps and rush to the Portal Gate as fast as possible. You cannot escape through the portal if an enemy is hot on your trail! Defeating enemies is optional, but doing so reduces your time taken by 15 seconds.",
            cognitiveSkills = "Memory + Attention",
            sceneName = "Level2"
        };

        minigames[32] = new GameData
        {
            id = 32,
            gameTitle = "Level 3",
            description = "Cryptic rhymes play on a continuous loop around you. To escape, you must decode the password hidden within its verses by extracting the fourth letter of every single word. You will need to give the password to go through each of the music doors to pass to its next region. You cannot escape through the portal if an enemy is hot on your trail! Defeating enemies is optional, but doing so reduces your time taken by 15 seconds.",
            cognitiveSkills = "Memory + Attention",
            sceneName = "Level3"
        };

        minigames[33] = new GameData
        {
            id = 33,
            gameTitle = "Level 4",
            description = "Multiple cannons lie ahead, tracking your moves and firing from all sides. Raise your shield in time to block incoming fireballs, or use the environment strategically to let objects take the hit for you. Mistiming your guard or getting caught in the open will cost you precious health.",
            cognitiveSkills = "Reflex + Attention",
            sceneName = "Level4"
        };

        minigames[34] = new GameData
        {
            id = 34,
            gameTitle = "Level 5",
            description = "Five broken grid networks block your path to freedom. To escape, you must rotate and align the junctions of each power circuit to send power from source to destination. Solve the puzzles, restore the power flows, and rush to the Portal Gate as fast as possible. You cannot escape through the portal if an enemy is hot on your trail! Defeating enemies is optional, but doing so reduces your time taken by 15 seconds.",
            cognitiveSkills = "Reasoning + Attention",
            sceneName = "Level5"
        };

        // Minigames (id < 30 are point-based)
        minigames[0] = new GameData
        {
            id = 0,
            gameTitle = "Cipher Rush",
            description = "Falling symbols drop from above, and your task is to quickly react to the target symbol. If the target is white, click matching symbols for +10 points, while wrong clicks or missed matches cost -5. If the target turns red, do not click matching symbols; clicking them at this time costs -5, while letting them go gives +10. Clicking a matching symbol or letting it fall always updates the target symbol.",
            cognitiveSkills = "Attention + Perception",
            sceneName = "Minigame1"
        };

        minigames[1] = new GameData
        {
            id = 1,
            gameTitle = "Match-a-Mole",
            description = "A single mole appears at one of nine possible locations. Before each rise, the target can be one of the two mole types or a bomb symbol. Click the mole for +10 points when its type matches the target image, lose -10 for clicking the wrong type, and if the target is a bomb, clicking any mole gives -10.",
            cognitiveSkills = "Reflex + Attention",
            sceneName = "Minigame2"
        };

        minigames[2] = new GameData
        {
            id = 2,
            gameTitle = "Good Guy, Bad Guy",
            description = "Before each round, you see two rows showing good guys and bad guys for a short time. Then these 8 guys appear on a board. Your task is to click only on the bad guys before the timer expires; each correct click adds 10 points, and each wrong click deducts 10 points. The game ends after 5 rounds",
            cognitiveSkills = "Memory + Attention",
            sceneName = "Minigame3"
        };

        minigames[3] = new GameData
        {
            id = 3,
            gameTitle = "Feed The Fishes",
            description = "Some identical fishes swim around the tank and keep changing direction as they bounce off the boundaries. Click a fish to feed it: feeding an unfed fish gives +10 points, but feeding a fish that has already been fed costs -10 points. After each feeding, you must wait for some time before feeding another fish.",
            cognitiveSkills = "Memory + Attention",
            sceneName = "Minigame4"
        };

        minigames[4] = new GameData
        {
            id = 4,
            gameTitle = "Train of Thoughts",
            description = "Colored cars roll along the tracks and change direction as you toggle the junctions. Click a junction to switch its path: routing a car into a matching colored house gives +10 points, wrong house costs -5 points. Keep your eyes on the timer and guide as many cars home as you can before time runs out!",
            cognitiveSkills = "Reflex + Attention",
            sceneName = "Minigame5"
        };

        minigames[6] = new GameData
        {
            id = 6,
            gameTitle = "Wordify",
            description = "Each round shows a set of columns with three letters in each column. Type one letter from each column to form a valid dictionary word. A correct word gives +10 points and advances to the next round with one more column, while an invalid word costs -5 points.",
            cognitiveSkills = "Learning",
            sceneName = "Minigame7"
        };

        minigames[8] = new GameData
        {
            id = 8,
            gameTitle = "Word Reveal",
            description = "Tap on the target object before they disappear. Every correct tap reveals the next letter of a word in sequence, remember them. Once all letters have been revealed, reconstruct the word. Each correct tap gives +10, incorrect tap gives -10. Forming correct word gives +20, incorrect word gives -10",
            cognitiveSkills = "Attention + Memory",
            sceneName = "Minigame9"
        };

        minigames[10] = new GameData
        {
            id = 10,
            gameTitle = "Sequence Grid",
            description = "A 4x4 grid will light up in a sequence. Recreate the sequence by clicking the cells in the correct order. Each success increases the sequence length by 1 and adds 10 points.",
            cognitiveSkills = "Memory + Attention",
            sceneName = "Minigame11"
        };

        minigames[11] = new GameData
        {
            id = 11,
            gameTitle = "Audio Visual Match",
            description = "A word is shown on screen, while the sound of another word is played. Click 'Match' if the spoken word was previously shown on screen, or 'No Match' if it was not. A correct answer adds 10 points; an incorrect answer deducts 10 points.",
            cognitiveSkills = "Attention + Memory",
            sceneName = "Minigame12"
        };

        minigames[12] = new GameData
        {
            id = 12,
            gameTitle = "Odd One Out",
            description = "Six boxes appear on screen: five share a common trait and one is the odd one out. Click the box that does not belong. A correct pick scores +10 and raises the difficulty — the differences become subtler as you progress. A wrong pick costs -5 and resets the round. Three wrong answers end the game.",
            cognitiveSkills = "Attention + Perception",
            sceneName = "Minigame13"
        };

        minigames[13] = new GameData
        {
            id = 13,
            gameTitle = "Shape Sync",
            description = "Squares, triangles, and circles keep changing between green, blue, and pink. Watch the target shape at the top and press Enter only when every object of that shape has the same color. A correct answer adds 10 points and increases the number of objects, while a wrong answer deducts 10 points.",
            cognitiveSkills = "Attention + Reflex",
            sceneName = "Minigame14"
        };

        minigames[14] = new GameData
        {
            id = 14,
            gameTitle = "Word Bubble",
            description = "You are shown the first 3 letters of a word and must complete it using a valid dictionary word. Each correct word gives points equal to its full length (min 5 letters, max 10 letters), and no word can be repeated. A new random prefix appears immediately after every correct answer. You can also skip the current prefix to move to a new one, but doing so deducts 3 points.",
            cognitiveSkills = "Learning",
            sceneName = "Minigame15"
        };

        minigames[15] = new GameData
        {
            id = 15,
            gameTitle = "Sequence Match",
            description = "A sequence of characters is shown briefly, then a shuffled target list appears. Enter the target characters in the order they appeared in the original sequence. For example: if sequence was 'friend' and target is 'def' then enter 'fed' as the letters d,e,f occured in that order. Correct answers add 10 points and increase the target count, while wrong answers deduct 10 points.",
            cognitiveSkills = "Memory",
            sceneName = "Minigame16"
        };

        minigames[16] = new GameData
        {
            id = 16,
            gameTitle = "Verbal Memory",
            description = "You are shown a word. Click 'New' if it is the first time the word is shown in this round, or 'Seen' if it has already appeared. You have 3 lives, and each correct answer gives 10 points.",
            cognitiveSkills = "Memory",
            sceneName = "Minigame17"
        };

        minigames[17] = new GameData
        {
            id = 17,
            gameTitle = "Number Memory",
            description = "Memorize the sequence of digits shown. You have 5 seconds before the number disappears, then re-enter the number in the input field. Success increases the number length by 1, and you have 3 lives.",
            cognitiveSkills = "Memory",
            sceneName = "Minigame18"
        };
    }

    public static bool TryGetGameData(int id, out GameData gameData)
    {
        EnsureDataInitialized();
        for (int i = 0; i < minigames.Length; i++)
        {
            if (minigames[i].id == id && !string.IsNullOrWhiteSpace(minigames[i].sceneName))
            {
                gameData = minigames[i];
                return true;
            }
        }
        gameData = default;
        return false;
    }

    public static bool TryGetGameDataBySceneName(string sceneName, out GameData gameData)
    {
        EnsureDataInitialized();
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            gameData = default;
            return false;
        }
        for (int i = 0; i < minigames.Length; i++)
        {
            if (minigames[i].sceneName == sceneName)
            {
                gameData = minigames[i];
                return true;
            }
        }
        gameData = default;
        return false;
    }

    public static void SetCurrentGame(GameData gameData)
    {
        currentGame = gameData;
    }

    public void UpdateDetails(int id)
    {
        if (!TryGetGameData(id, out currentGame))
        {
            Debug.LogWarning($"Could not find minigame data for id '{id}'.", this);
            return;
        }

        titleText.text = currentGame.gameTitle;
        descriptionText.text = currentGame.description;
        skillsText.text = currentGame.cognitiveSkills;

        if (thumbnailImage != null)
            thumbnailImage.sprite = currentGame.thumbnail;

        string idStr = currentGame.id.ToString();
        int playCount = PlayerPrefs.GetInt($"PlayCount_{idStr}", 0);
        bool isTimeBased = currentGame.id >= 30;

        if (bestScoreText != null)
        {
            if (playCount == 0)
            {
                bestScoreText.text = isTimeBased ? "Best Time: None" : "Best Score: None";
            }
            else
            {
                int bestScore = MinigameBestScoreStore.GetBestScore(idStr);
                bestScoreText.text = isTimeBased ? $"Best Time: {bestScore}s" : $"Best Score: {bestScore}";
            }
        }

        if (averageScoreText != null)
        {
            if (playCount == 0)
            {
                averageScoreText.text = isTimeBased ? "Average Time: None" : "Average Score: None";
            }
            else
            {
                int averageScore = MinigameBestScoreStore.GetAverageScore(idStr);
                averageScoreText.text = isTimeBased ? $"Average Time: {averageScore}s" : $"Average Score: {averageScore}";
            }
        }
    }

    public static GameData GetCurrentGame()
    {
        EnsureDataInitialized();
        //Debug.Log($"[MinigameDataStore] Current Game Info -> ID: {currentGame.id} | Title: {currentGame.gameTitle} | Scene: {currentGame.sceneName}");
        return currentGame;
    }
}