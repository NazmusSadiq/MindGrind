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