using System;
using System.Collections.Generic;
using Godot;

public class Global : Node
{
    private const string SAVE_DIRECTORY = "user://";
    private const string SAVE_FILE_PREFIX = "save-";
    private const string SAVE_FILE_SUFFIX = ".json";

    private const string SETTINGS_SAVE_FILE = "settings";
    private const string DEFAULT_SAVE_FILE = "default";

    private const string BEST_LEVEL_KEY = "bestLevel";

    private const string MASTER_VOLUME_KEY = "masterVolume";
    private const string SFX_VOLUME_KEY = "sfxVolume";
    private const string MUSIC_VOLUME_KEY = "musicVolume";
    private const string QUALITY_KEY = "quality";

    private const int FIRST_LEVEL = 1;
    private static int currentLevel = FIRST_LEVEL;
    public static int CurrentLevel { get => currentLevel; }

    private static string currentLevelPath = GetLevelScenePath(currentLevel);
    public static string CurrentLevelPath { get => currentLevelPath; }

    private static int bestLevel = FIRST_LEVEL;
    public static int BestLevel { get => bestLevel; }

    private static bool hasBeatenAllLevels = false;
    public static bool HasBeatenAllLevels { get => hasBeatenAllLevels; }

    private static GameSettings settings;
    public static GameSettings Settings { get => settings; }

    public static bool ReturningToMenu = false;

    public override void _Ready()
    {
        // if (save_file_exists(SETTINGS_SAVE_FILE))
        // {
        //     settings.initialize_from_dictionary(load_data(SETTINGS_SAVE_FILE));
        // }
    }

    public static void SetLevelToBest()
    {
        currentLevel = bestLevel;
        currentLevelPath = GetLevelScenePath(currentLevel);
    }

    public static void SetLevelToFirst()
    {
        currentLevel = FIRST_LEVEL;
        currentLevelPath = GetLevelScenePath(currentLevel);
    }

    public static bool WinLevel(int level)
    {
        currentLevel = level;
        int lastLevel = currentLevel;
        string nextLevelPath = GetLevelScenePath(currentLevel + 1);
        if (new File().FileExists(nextLevelPath))
        {
            ++currentLevel;
            currentLevelPath = nextLevelPath;
            bestLevel = Mathf.Max(bestLevel, currentLevel);
            SaveBestLevel(bestLevel);
            return true;
        }
        hasBeatenAllLevels = true;
        SetLevelToFirst();
        return false;
    }

    private static string GetLevelScenePath(int level)
    {
        return "res://scenes/Level" + level + ".tscn";
    }

    public static void SaveBestLevel(int bestLevel)
    {
        var data = new Dictionary<string, object>();
        data.Add(BEST_LEVEL_KEY, bestLevel);
        SaveData(BEST_LEVEL_KEY, data);
    }

    public static bool LoadBestLevel()
    {
        var data = LoadData(BEST_LEVEL_KEY);
        if (data == null)
        {
            return false;
        }
        object result = data[BEST_LEVEL_KEY];
        bestLevel = result == null ? FIRST_LEVEL : Convert.ToInt32(result);
        return true;
    }

    public static void SaveSettings()
    {
        var data = new Dictionary<string, object>();
        data.Add(MASTER_VOLUME_KEY, settings.MasterVolume);
        data.Add(SFX_VOLUME_KEY, settings.SfxVolume);
        data.Add(MUSIC_VOLUME_KEY, settings.MusicVolume);
        data.Add(QUALITY_KEY, (int)settings.Quality);
        SaveData(SETTINGS_SAVE_FILE, data);
    }

    public static void LoadSettings()
    {
        if (settings != null)
        {
            return;
        }
        var data = LoadData(SETTINGS_SAVE_FILE);
        settings = new GameSettings();
        if (data == null)
        {
            GD.Print("No data for settings could be loaded");
            return;
        }

        // Generally ignoring exceptions like this is a bad idea, but keys not being present is to be expected;

        try { settings.MasterVolume = Convert.ToInt32(data[MASTER_VOLUME_KEY]); }
        catch (KeyNotFoundException) { }

        try { settings.SfxVolume = Convert.ToInt32(data[SFX_VOLUME_KEY]); }
        catch (KeyNotFoundException) { }

        try { settings.MusicVolume = Convert.ToInt32(data[MUSIC_VOLUME_KEY]); }
        catch (KeyNotFoundException) { }

        try { settings.Quality = (GameQuality)(Convert.ToInt32(data[QUALITY_KEY])); }
        catch (InvalidCastException)
        {
            GD.PushError(String.Format("Failed to cast the raw quality value '{0}' to a GameQuality enum", data["quality"]));
        }
        catch (KeyNotFoundException) { }
    }

    private static void SaveData(string save_name, Dictionary<string, object> data)
    {
        var path = GetUserJsonFilePath(save_name);
        // LOG.info("Saving data to: %s" % path);
        var file = new File();
        var openError = file.Open(path, File.ModeFlags.Write);
        if (openError != 0)
        {
            GD.Print("Open of ", path, " error: ", openError);
            return;
        }
        // LOG.check_error_code(file.open(path, File.WRITE), "Opening '%s'" % path);
        // LOG.info("Saving to: " + file.get_path_absolute());
        file.StoreString(JSON.Print(data));
        file.Close();
    }

    private string[] GetSaveFiles()
    {
        var dir = OpenSaveDirectory();
        dir.ListDirBegin(false, false);
        // LOG.check_error_code(dir.list_dir_begin(false, false), "Listing the files of " + SAVE_DIRECTORY);
        var file_name = dir.GetNext();

        List<string> result = new List<string>();
        while (file_name != "")
        {
            if (!dir.CurrentIsDir() && file_name.StartsWith(SAVE_FILE_PREFIX)
                    && file_name.EndsWith(SAVE_FILE_SUFFIX))
            {
                result.Add(file_name.Substr(SAVE_FILE_PREFIX.Length,
                    file_name.Length - SAVE_FILE_PREFIX.Length - SAVE_FILE_SUFFIX.Length));
            }
            file_name = dir.GetNext();
        }
        dir.ListDirEnd();

        result.Sort();
        return result.ToArray();
    }

    Node GetSingleNodeInGroup(string group)
    {
        Godot.Collections.Array nodes = GetTree().GetNodesInGroup(group);
        return nodes.Count > 0 ? (Node)nodes[0] : null;
    }

    private Directory OpenSaveDirectory()
    {
        var dir = new Directory();
        // LOG.check_error_code(dir.open(SAVE_DIRECTORY), "Opening " + SAVE_DIRECTORY);
        return dir;
    }

    private bool LoadGame(string save_name = DEFAULT_SAVE_FILE)
    {
        if (!SaveFileExists(save_name))
        {
            return false;
        }
        var loaded_data = LoadData(save_name);
        if (loaded_data.Count == 0)
        {
            return false;
        }
        var game_data = loaded_data;
        currentLevel = (int)(game_data["level"] ?? FIRST_LEVEL);
        return true;
    }

    private static bool SaveFileExists(string save_name)
    {
        var path = GetUserJsonFilePath(save_name);
        return new File().FileExists(path);
    }

    private static Godot.Collections.Dictionary LoadData(string fileName)
    {
        var path = GetUserJsonFilePath(fileName);
        var file = new File();
        if (!file.FileExists(path))
        {
            return null;
        }
        file.Open(path, File.ModeFlags.Read);
        // LOG.check_error_code(file.open(path, File.READ), "Opening file " + path);
        var raw_data = file.GetAsText();
        file.Close();
        var loaded_data = JSON.Parse(raw_data);
        if (loaded_data.Result != null && loaded_data.Result is Godot.Collections.Dictionary)
        {
            return (Godot.Collections.Dictionary)loaded_data.Result;
        }
        else
        {
            GD.PushWarning(String.Format("Corrupted data in file '{0}'!", path));
            return null;
        }
    }

    private static string GetUserJsonFilePath(string save_name)
    {
        return SAVE_DIRECTORY + save_name + ".json";
    }
}

public class GameSettings
{
    public int MasterVolume = 20;
    public float NormalizedMasterVolume { get => MasterVolume * .01f; }

    public int SfxVolume = 80;
    public float NormalizedSfxVolume { get => SfxVolume * .01f; }

    public int MusicVolume = 80;
    public float NormalizedMusicVolume { get => MusicVolume * .01f; }

    public GameQuality Quality = GameQuality.MEDIUM;
}

public enum GameQuality
{
    LOW, MEDIUM, HIGH
}
