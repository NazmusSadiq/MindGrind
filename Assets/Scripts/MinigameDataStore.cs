using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameDataStore : MonoBehaviour
{
    private static MinigameDataStore instance;

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

    [SerializeField] private GameData[] minigames;

    public GameData currentGame;

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

        InitializeData();
    }

    private void InitializeData()
    {
        minigames = new GameData[2];

        minigames[0] = new GameData
        {
            id = 0,
            gameTitle = "Cipher Rush",
            description = "Falling symbols drop from above, and your task is to quickly click the ones that match the target symbol. Each correct click gives you +10 points, while clicking the wrong symbol or missing the correct one costs you -5 points.",
            cognitiveSkills = "Attention + Perception",
            sceneName = "Minigame1"
        };

        minigames[1] = new GameData
        {
            id = 1,
            gameTitle = "Hue Hunt",
            description = "Two color-based categories will appear dynamically during gameplay. Each mole belongs to one of these categories, determined by its color. Your task is to quickly identify the correct category and whack only the matching moles while ignoring the rest. As the categories keep shifting, you’ll need sharp attention and fast reflexes to stay accurate under pressure.",
            cognitiveSkills = "Reflex + Attention",
            sceneName = "MemoryScene"
        };
    }

    public void UpdateDetails(int id)
    {
        for (int i = 0; i < minigames.Length; i++)
        {
            if (minigames[i].id == id)
            {
                currentGame = minigames[i];
            }
        }

        titleText.text = currentGame.gameTitle;
        descriptionText.text = currentGame.description;
        skillsText.text = currentGame.cognitiveSkills;

        if (thumbnailImage != null)
            thumbnailImage.sprite = currentGame.thumbnail;

        return;
    }

    public GameData GetCurrentGame()
    {
        return currentGame;
    }
}