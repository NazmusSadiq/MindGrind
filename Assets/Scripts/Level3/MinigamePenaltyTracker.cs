public static class MinigamePenaltyTracker
{
    public static float PenaltySeconds { get; private set; }

    public static void AddPenalty(float seconds)
    {
        if (seconds <= 0f) return;
        PenaltySeconds += seconds;
    }

    public static void ResetPenalty()
    {
        PenaltySeconds = 0f;
    }
}