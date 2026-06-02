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

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text skillsText;
    [SerializeField] private Image thumbnailImage;

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
        if (minigames != null && minigames.Length > 0)
        {
            return;
        }

        minigames = new GameData[15];

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

        minigames[14] = new GameData
        {
            id = 14,
            gameTitle = "Word Game",
            description = "You are shown the first 3 letters of a word and must complete it using a valid dictionary word. Each correct word gives points equal to its full length (min 5 letters, max 10 letters), and no word can be repeated. A new random prefix appears immediately after every correct answer. You can also skip the current prefix to move to a new one, but doing so deducts 3 points.",
            cognitiveSkills = "Learning",
            sceneName = "Minigame15"
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

        return;
    }

    public static GameData GetCurrentGame()
    {
        EnsureDataInitialized();
        return currentGame;
    }
}