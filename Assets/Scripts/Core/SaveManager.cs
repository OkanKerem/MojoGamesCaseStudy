using UnityEngine;

public static class SaveManager
{
    private const string LevelIndexKey = "CurrentLevelIndex";

    public static int LoadLevelIndex()
    {
        return PlayerPrefs.GetInt(LevelIndexKey, 0);
    }

    public static void SaveLevelIndex(int index)
    {
        PlayerPrefs.SetInt(LevelIndexKey, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }
}
