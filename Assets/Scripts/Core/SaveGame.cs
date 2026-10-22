// Сохранение: каждое утро (сцена new_day) состояние пишется в файл. При запуске можно продолжить с того утра.
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveGame
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Checkpoint()
    {
        try
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(G.S));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Не удалось сохранить игру: " + e.Message);
        }
    }

    public static bool Exists => File.Exists(FilePath);

    // Состояние из файла (не применяет его) или null
    public static GameState Peek()
    {
        try
        {
            return Exists ? JsonConvert.DeserializeObject<GameState>(File.ReadAllText(FilePath)) : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Сохранение повреждено: " + e.Message);
            return null;
        }
    }

    // Продолжить: утро сохранённого дня, без повторного входа в сцену (карманные уже выданы)
    public static void Continue(GameState saved)
    {
        G.S = saved;
        Game.Render("new_day", skipRoute: true);
    }

    public static void Delete()
    {
        try { if (Exists) File.Delete(FilePath); }
        catch (Exception) { }
    }
}
