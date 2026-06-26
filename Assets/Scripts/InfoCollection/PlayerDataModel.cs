using System;
using System.Collections.Generic;

[Serializable]
public class GameStat
{
    public int gameId;
    public int bestScore;
    public int averageScore;
    public int playCount;
}

[Serializable]
public class PlayerProfile
{
    public string userId;
    public int age;
    public string gender;
    public string country;
    public List<GameStat> gameStats = new List<GameStat>();
}