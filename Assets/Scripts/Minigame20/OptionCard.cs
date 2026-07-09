using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionCard : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private ShapeSpawner shapeSpawner;
    [SerializeField] private Button selectButton;

    [Header("Colors")]
    [SerializeField] private Color[] backgroundColors = new Color[4];

    private int cardIndex;

    private Action<int> onSelected;

    public void Initialize(int index, Action<int> callback)
    {
        cardIndex = index;

        onSelected = callback;

        selectButton.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        onSelected?.Invoke(cardIndex);
    }

    public void SetInteractable(bool value)
    {
        selectButton.interactable = value;
    }

    public void SetData(OddOneOut.OptionData data)
    {
        //----------------------------------
        // Background
        //----------------------------------

        backgroundImage.color =
            backgroundColors[data.backgroundIndex];

        //----------------------------------
        // Spawn Shapes
        //----------------------------------

        shapeSpawner.GenerateShapes(
            data.shape,
            data.objectColorIndex,
            data.count);
    }
}