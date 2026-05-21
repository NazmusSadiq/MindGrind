using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class square_gen : MonoBehaviour
{
    public float x_pos = 0, y_pos = 10;
    public float x_spacing = 50;
    public GameObject Square;
    public string[] words = new string[5] { "Circle", "Square", "Triangle", "Rectangle", "Cube" };

    private const int RevealedLetterCount = 3;

    private string target;
    private char[] enteredLetters;
    private TMP_Text[] squareTexts;
    private Image[] squareImages;
    private TMP_Text messageText;
    private int activeIndex = -1;
#if ENABLE_INPUT_SYSTEM
    private Keyboard subscribedKeyboard;
#endif

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        SubscribeKeyboard();
    }

    void OnDisable()
    {
        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= HandleTextInput;
            subscribedKeyboard = null;
        }
    }
#endif

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Square == null)
        {
            Debug.LogWarning("square_gen needs a Square prefab assigned.", this);
            return;
        }

        target = words[Random.Range(0, words.Length)];

        int n = target.Length;
        enteredLetters = new char[n];
        squareTexts = new TMP_Text[n];
        squareImages = new Image[n];

        for (int i = 0; i < n; i++)
        {
            GameObject square = Instantiate(Square, transform);
            square.transform.SetAsLastSibling();
            square.name = "Square " + (i + 1);

            RectTransform squareRect = square.GetComponent<RectTransform>();
            if (squareRect != null)
            {
                float squareWidth = squareRect.sizeDelta.x;
                float xOffset = i * (squareWidth + x_spacing);

                squareRect.anchoredPosition = new Vector2(x_pos + xOffset, y_pos);
                squareRect.localRotation = Quaternion.identity;
                squareRect.localScale = Vector3.one;
            }

            squareImages[i] = MakeVisibleOnCanvas(square);
            squareTexts[i] = CreateLetterText(square.transform);

            if (i < RevealedLetterCount)
            {
                enteredLetters[i] = target[i];
                squareTexts[i].text = target[i].ToString();
            }

            int squareIndex = i;
            Button button = square.GetComponent<Button>();
            if (button == null)
            {
                button = square.AddComponent<Button>();
            }

            button.targetGraphic = squareImages[i];
            button.onClick.AddListener(() => TryActivateSquare(squareIndex));
        }

        CreateMessageText();
        SetActiveSquare(Mathf.Min(RevealedLetterCount, n - 1));
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        SubscribeKeyboard();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            ValidateAnswer();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ValidateAnswer();
            return;
        }

        if (activeIndex < 0)
        {
            return;
        }

        foreach (char typedCharacter in Input.inputString)
        {
            if (typedCharacter == '\b' || typedCharacter == '\n' || typedCharacter == '\r')
            {
                continue;
            }

            if (!char.IsLetter(typedCharacter))
            {
                continue;
            }

            EnterLetter(typedCharacter);
            break;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void SubscribeKeyboard()
    {
        if (Keyboard.current == null || Keyboard.current == subscribedKeyboard)
        {
            return;
        }

        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= HandleTextInput;
        }

        subscribedKeyboard = Keyboard.current;
        subscribedKeyboard.onTextInput += HandleTextInput;
    }

    private void HandleTextInput(char typedCharacter)
    {
        if (activeIndex < 0 || typedCharacter == '\n' || typedCharacter == '\r' || !char.IsLetter(typedCharacter))
        {
            return;
        }

        EnterLetter(typedCharacter);
    }
#endif

    private Image MakeVisibleOnCanvas(GameObject square)
    {
        SpriteRenderer spriteRenderer = square.GetComponent<SpriteRenderer>();
        Image image = square.GetComponent<Image>();

        if (image == null)
        {
            image = square.AddComponent<Image>();
        }

        if (spriteRenderer != null)
        {
            image.sprite = spriteRenderer.sprite;
            image.color = spriteRenderer.color;
            spriteRenderer.enabled = false;
        }

        image.raycastTarget = true;
        return image;
    }

    private TMP_Text CreateLetterText(Transform square)
    {
        GameObject textObject = new GameObject("Letter");
        textObject.layer = square.gameObject.layer;
        textObject.transform.SetParent(square, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.fontSize = 48;
        text.text = "";
        text.raycastTarget = false;

        return text;
    }

    private void CreateMessageText()
    {
        GameObject textObject = new GameObject("Result Message");
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(x_pos + 200, y_pos - 120);
        textRect.sizeDelta = new Vector2(500, 80);

        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.black;
        messageText.fontSize = 36;
        messageText.text = "";
        messageText.raycastTarget = false;
    }

    private void TryActivateSquare(int index)
    {
        if (index < RevealedLetterCount || index >= enteredLetters.Length)
        {
            return;
        }

        if (index > RevealedLetterCount && enteredLetters[index - 1] == '\0')
        {
            return;
        }

        SetActiveSquare(index);
    }

    private void SetActiveSquare(int index)
    {
        activeIndex = index >= RevealedLetterCount && index < enteredLetters.Length ? index : -1;

        for (int i = 0; i < squareImages.Length; i++)
        {
            squareImages[i].color = i == activeIndex ? new Color(1f, 0.9f, 0.35f) : Color.white;
        }
    }

    private void EnterLetter(char letter)
    {
        enteredLetters[activeIndex] = letter;
        squareTexts[activeIndex].text = letter.ToString();

        int nextIndex = activeIndex + 1;
        if (nextIndex < enteredLetters.Length)
        {
            SetActiveSquare(nextIndex);
        }
        else
        {
            SetActiveSquare(-1);
        }
    }

    private void ValidateAnswer()
    {
        string enteredWord = new string(enteredLetters);

        if (enteredWord.Equals(target, System.StringComparison.OrdinalIgnoreCase))
        {
            messageText.text = "Correct!";
        }
        else
        {
            messageText.text = "Try again";
        }
    }
}
