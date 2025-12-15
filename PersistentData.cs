using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class PersistentData : Node
{
	private const string SAVE_FILE_PATH = "user://persistentdata.json";

	private int highScore = -1;
	private List<string> solvedLevels = new List<string>();

	public override void _Ready()
	{
		LoadData();
	}

	/// <summary>
	/// Get the current high score (-1 indicates no high score yet)
	/// </summary>
	public int GetHighScore()
	{
		return highScore;
	}

	/// <summary>
	/// Set the high score and automatically save to disk
	/// </summary>
	public void SetHighScore(int score)
	{
		highScore = score;
		SaveData();
	}

	/// <summary>
	/// Get the list of solved levels
	/// </summary>
	public List<string> GetSolvedLevels()
	{
		return new List<string>(solvedLevels);
	}

	/// <summary>
	/// Set the solved levels list and automatically save to disk
	/// </summary>
	public void SetSolvedLevels(List<string> levels)
	{
		solvedLevels = new List<string>(levels);
		SaveData();
	}

	/// <summary>
	/// Check if a specific level has been solved
	/// </summary>
	public bool IsLevelSolved(string levelId)
	{
		return solvedLevels.Contains(levelId);
	}

	/// <summary>
	/// Mark a level as solved and automatically save to disk
	/// </summary>
	public void MarkLevelSolved(string levelId)
	{
		if (!solvedLevels.Contains(levelId))
		{
			solvedLevels.Add(levelId);
			SaveData();
		}
	}

	/// <summary>
	/// Load persistent data from disk
	/// </summary>
	public void LoadData()
	{
		try
		{
			if (!FileAccess.FileExists(SAVE_FILE_PATH))
			{
				GD.Print("No persistent data file found. Starting with defaults.");
				return;
			}

			string jsonContent = FileAccess.GetFileAsString(SAVE_FILE_PATH);
			GD.Print(jsonContent);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var data = JsonSerializer.Deserialize<PersistentDataFile>(jsonContent, options);

			if (data != null)
			{
				highScore = data.HighScore;
				solvedLevels = data.SolvedLevels ?? new List<string>();
				GD.Print($"Loaded persistent data: HighScore={highScore}, SolvedLevels={solvedLevels.Count}");
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Error loading persistent data: {ex.Message}");
			// Reset to defaults on error
			highScore = -1;
			solvedLevels = new List<string>();
			SaveData();
		}
	}

	/// <summary>
	/// Save persistent data to disk
	/// </summary>
	public void SaveData()
	{
		try
		{
			var data = new PersistentDataFile
			{
				HighScore = highScore,
				SolvedLevels = solvedLevels
			};

			var options = new JsonSerializerOptions { WriteIndented = true };
			string jsonContent = JsonSerializer.Serialize(data, options);

			var file = FileAccess.Open(SAVE_FILE_PATH, FileAccess.ModeFlags.Write);
			file.StoreString(jsonContent);

			GD.Print("Persistent data saved successfully: " + jsonContent);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Error saving persistent data: {ex.Message}");
		}
	}

	/// <summary>
	/// Internal class for JSON serialization
	/// </summary>
	private class PersistentDataFile
	{
		public int HighScore { get; set; }
		public List<string> SolvedLevels { get; set; }
	}
}
