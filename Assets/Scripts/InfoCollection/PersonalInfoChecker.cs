using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PersonalInfoChecker : MonoBehaviour
{
    [Header("UI Registration Panel")]
    [SerializeField] private GameObject registrationCanvas;
    [SerializeField] private TMP_InputField ageInputField;
    [SerializeField] private TMP_Dropdown genderDropdown;
    [SerializeField] private TMP_InputField countryInputField;
    [SerializeField] private Button submitButton;

    [Header("Country Autocomplete Setup")]
    [SerializeField] private TextAsset countriesTextFile;         // <-- Assign your countries.txt asset here!
    [SerializeField] private GameObject suggestionsContainerPanel;
    [SerializeField] private GameObject suggestionButtonPrefab;
    [SerializeField] private int maxSuggestionsToShow = 5;

    [Header("Validation Feedback (Optional)")]
    [SerializeField] private TMP_Text errorText;

    private PlayerProfile activeProfile;
    private string saveFilePath;

    // Country Logic Variables
    private List<string> allCountries = new List<string>();
    private List<GameObject> activeSuggestionButtons = new List<GameObject>();
    private bool isCountrySelectedFromList = false;

    private void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "player_analytics.json");
        Debug.Log($"EXACT JSON PATH: {saveFilePath}");

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitPersonalInfo);
        }

        if (errorText != null) errorText.text = "";

        // Load country dataset from the Inspector assigned text file asset
        LoadCountryDatabase();

        if (countryInputField != null)
        {
            countryInputField.onValueChanged.RemoveAllListeners();
            countryInputField.onValueChanged.AddListener(OnCountryInputFieldChanged);
        }

        if (suggestionsContainerPanel != null)
        {
            suggestionsContainerPanel.SetActive(false);
        }

        LoadOrCreateProfile();
    }

    private void LoadCountryDatabase()
    {
        if (countriesTextFile != null)
        {
            // Split by line breaks seamlessly across Windows, Unix, and Mac file variations
            string[] lines = countriesTextFile.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                allCountries.Add(line.Trim());
            }
            Debug.Log($"Successfully loaded {allCountries.Count} countries from the assigned Inspector text asset.");
        }
        else
        {
            Debug.LogError("PersonalInfoChecker: 'Countries Text File' slot is empty in the inspector! Autocomplete will not work.");
        }
    }

    private void OnCountryInputFieldChanged(string currentText)
    {
        if (isCountrySelectedFromList) return;

        ClearActiveSuggestions();

        if (string.IsNullOrWhiteSpace(currentText))
        {
            if (suggestionsContainerPanel != null) suggestionsContainerPanel.SetActive(false);
            return;
        }

        string searchWord = currentText.Trim().ToLower();
        List<string> matches = allCountries.FindAll(c => c.ToLower().Contains(searchWord));

        if (matches.Count > 0)
        {
            if (suggestionsContainerPanel != null) suggestionsContainerPanel.SetActive(true);

            int displayCount = Mathf.Min(matches.Count, maxSuggestionsToShow);
            for (int i = 0; i < displayCount; i++)
            {
                string countryName = matches[i];
                CreateSuggestionButton(countryName);
            }
        }
        else
        {
            if (suggestionsContainerPanel != null) suggestionsContainerPanel.SetActive(false);
        }
    }

    private void CreateSuggestionButton(string countryName)
    {
        GameObject btnObj = Instantiate(suggestionButtonPrefab, suggestionsContainerPanel.transform);
        activeSuggestionButtons.Add(btnObj);

        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null) btnText.text = countryName;

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => SelectCountry(countryName));
        }
    }

    private void SelectCountry(string selectedCountryName)
    {
        isCountrySelectedFromList = true;

        countryInputField.text = selectedCountryName;
        ClearActiveSuggestions();

        if (suggestionsContainerPanel != null) suggestionsContainerPanel.SetActive(false);

        isCountrySelectedFromList = false;
    }

    private void ClearActiveSuggestions()
    {
        foreach (GameObject go in activeSuggestionButtons)
        {
            Destroy(go);
        }
        activeSuggestionButtons.Clear();
    }

    private void LoadOrCreateProfile()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                activeProfile = JsonUtility.FromJson<PlayerProfile>(json);
                if (registrationCanvas != null) registrationCanvas.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading profile: {e.Message}. Re-creating profile.");
                CreateDefaultProfile();
            }
        }
        else
        {
            CreateDefaultProfile();
        }
    }

    private void CreateDefaultProfile()
    {
        activeProfile = new PlayerProfile
        {
            userId = Guid.NewGuid().ToString(),
            age = 0,
            gender = "Unknown",
            country = "Unknown"
        };

        if (registrationCanvas != null) registrationCanvas.SetActive(true);
    }

    public void SubmitPersonalInfo()
    {
        if (activeProfile == null) return;

        // 1. Validate Age Field
        if (string.IsNullOrWhiteSpace(ageInputField.text) || !int.TryParse(ageInputField.text, out int resultAge) || resultAge <= 0 || resultAge > 120)
        {
            ShowValidationError("Please enter a valid age.");
            return;
        }

        // 2. Validate Gender Dropdown
        if (genderDropdown.value == 0 && genderDropdown.options[0].text.Contains("Select"))
        {
            ShowValidationError("Please select your gender.");
            return;
        }

        // 3. Validate Country Field explicitly against loaded lines
        string enteredCountry = countryInputField.text.Trim();
        string matchedExactCountry = allCountries.Find(c => c.Equals(enteredCountry, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(matchedExactCountry))
        {
            ShowValidationError("Please select a valid country from the suggestions list.");
            return;
        }

        // --- SUBMISSION LAYER ---
        if (errorText != null) errorText.text = "";

        activeProfile.age = resultAge;
        activeProfile.gender = genderDropdown.options[genderDropdown.value].text;
        activeProfile.country = matchedExactCountry;

        SyncAllExistingScores();
        SaveProfileToDisk();

        if (registrationCanvas != null) registrationCanvas.SetActive(false);
        Debug.Log("User profile validated and saved. Canvas closed.");
    }

    private void ShowValidationError(string message)
    {
        Debug.LogWarning($"Profile Validation Failed: {message}");
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
    }

    public void SyncAllExistingScores()
    {
        if (activeProfile == null) return;
        activeProfile.gameStats.Clear();

        for (int id = 0; id < 35; id++)
        {
            string idStr = id.ToString();
            string playCountKey = $"PlayCount_{idStr}";

            if (PlayerPrefs.HasKey(playCountKey))
            {
                GameStat stat = new GameStat
                {
                    gameId = id,
                    bestScore = PlayerPrefs.GetInt($"BestScore_{idStr}", 0),
                    playCount = PlayerPrefs.GetInt(playCountKey, 0),
                    averageScore = MinigameBestScoreStore.GetAverageScore(idStr)
                };
                activeProfile.gameStats.Add(stat);
            }
        }
    }

    public void SaveProfileToDisk()
    {
        if (activeProfile == null) return;
        try
        {
            string json = JsonUtility.ToJson(activeProfile, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write data file to disk: {e.Message}");
        }
    }
}