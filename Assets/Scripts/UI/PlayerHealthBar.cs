using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Image barImage;
    [SerializeField] private string playerTag = "Player";

    private PlayerController playerController;

    private void Awake()
    {
        if (barImage == null)
            barImage = GetBarImage();

        ConfigureBarImage();
        CachePlayerController();
        UpdateBar();
    }

    private void OnEnable()
    {
        UpdateBar();
    }

    private void Update()
    {
        if (playerController == null)
            CachePlayerController();

        UpdateBar();
    }

    private void ConfigureBarImage()
    {
        if (barImage == null)
            return;

        barImage.type = Image.Type.Filled;
        barImage.fillMethod = Image.FillMethod.Horizontal;
        barImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        barImage.fillAmount = 1f;
    }

    private void CachePlayerController()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
            return;

        playerController = playerObject.GetComponent<PlayerController>() ?? playerObject.GetComponentInParent<PlayerController>();
    }

    private void UpdateBar()
    {
        if (barImage == null || playerController == null)
            return;

        barImage.fillAmount = playerController.HealthNormalized;
    }

    private void Reset()
    {
        if (barImage == null)
            barImage = GetBarImage();
    }

    private Image GetBarImage()
    {
        Transform barTransform = transform.Find("Bar");
        if (barTransform != null)
            return barTransform.GetComponent<Image>();

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.transform == transform)
                continue;

            return image;
        }

        return GetComponent<Image>();
    }
}
