using UnityEngine;
using UnityEngine.UI;

public class ClockController : MonoBehaviour
{
    [Header("Clock Parts")]
    [SerializeField] private Image clockFace;
    // [SerializeField] private Image markerImage;
    [SerializeField] private RectTransform hourHand;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;     // degrees per second
    [SerializeField] private bool clockwise = true;

    private float currentAngle;
    private float targetAngle;

    private bool isRunning;
    private bool isStopped;

    #region Properties

    public float CurrentAngle => currentAngle;

    public float TargetAngle => targetAngle;

    public bool IsStopped => isStopped;

    #endregion

    private void Start()
    {
        ApplyRotation();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        float delta = rotationSpeed * Time.deltaTime;

        if (clockwise)
            currentAngle += delta;
        else
            currentAngle -= delta;

        currentAngle %= 360f;

        if (currentAngle < 0f)
            currentAngle += 360f;

        ApplyRotation();
    }

    #region Public Functions

    public void StartClock()
    {
        isRunning = true;
        isStopped = false;
    }

    public void StopClock()
    {
        isRunning = false;
        isStopped = true;
    }

    public void ResumeClock()
    {
        isRunning = true;
        isStopped = false;
    }

    public void ResetClock()
    {
        currentAngle = 0f;
        ApplyRotation();

        isStopped = false;
        isRunning = false;
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    public void SetCurrentAngle(float angle)
    {
        currentAngle = NormalizeAngle(angle);
        ApplyRotation();
    }

    public void SetTargetTime(string timeString)
    {
        targetAngle = ConvertTimeToAngle(timeString);
    }

    #endregion

    #region Private

    private void ApplyRotation()
    {
        if (hourHand == null)
            return;

        // Unity UI rotates clockwise with negative Z
        hourHand.localRotation =
            Quaternion.Euler(0f, 0f, 90f - currentAngle);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    private float ConvertTimeToAngle(string time)
    {
        if (string.IsNullOrEmpty(time))
            return 0f;

        string[] split = time.Split(':');

        if (split.Length != 2)
            return 0f;

        int hour = int.Parse(split[0]);
        int minute = int.Parse(split[1]);

        if (hour == 12)
            hour = 0;

        float totalHours =
            hour + minute / 60f;

        return totalHours * 30f;
    }

    #endregion

#if UNITY_EDITOR

    private void OnValidate()
    {
        ApplyRotation();
    }

#endif
}