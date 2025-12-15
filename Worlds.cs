using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class Worlds : Node
{
	private List<World> worlds = new List<World>();

	public override void _Ready()
	{
		InitializeWorlds();
	}

	/// <summary>
	/// Initialize the default worlds
	/// </summary>
	private void InitializeWorlds()
	{
		// Load World 1 from JSON
		LoadWorldFromJson("res://Worlds/World1.json");
		LoadWorldFromJson("res://Worlds/World2.json");

		GD.Print($"Initialized {worlds.Count} world(s)");
	}

	/// <summary>
	/// Load a world from a JSON file
	/// </summary>
	private void LoadWorldFromJson(string filePath)
	{
		try
		{
			string jsonContent = FileAccess.GetFileAsString(filePath);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var worldData = JsonSerializer.Deserialize<WorldData>(jsonContent, options);

			if (worldData == null)
			{
				GD.PrintErr($"Failed to deserialize world from {filePath}");
				return;
			}

			var world = new World(worldData.Name);

			// Add all levels from the JSON
			if (worldData.Levels != null)
			{
				foreach (var levelData in worldData.Levels)
				{
					var level = new Level(
						levelData.Letters,
						levelData.Width,
						levelData.Height,
						"",
						levelData.Name
					);
					world.AddLevel(level);
				}
			}

			worlds.Add(world);
			GD.Print($"Loaded world '{worldData.Name}' with {worldData.Levels?.Count ?? 0} levels from {filePath}");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Error loading world from {filePath}: {ex.Message}");
		}
	}

	/// <summary>
	/// Get a world by index
	/// </summary>
	public World GetWorld(int index)
	{
		if (index < 0 || index >= worlds.Count)
		{
			GD.PrintErr($"World index {index} is out of bounds");
			return null;
		}
		return worlds[index];
	}

	/// <summary>
	/// Get a world by theme name
	/// </summary>
	public World GetWorldByTheme(string theme)
	{
		foreach (var world in worlds)
		{
			if (world.GetTheme() == theme)
			{
				return world;
			}
		}
		GD.PrintErr($"World with theme '{theme}' not found");
		return null;
	}

	/// <summary>
	/// Get the total number of worlds
	/// </summary>
	public int GetWorldCount()
	{
		return worlds.Count;
	}

	/// <summary>
	/// Add a new world
	/// </summary>
	public void AddWorld(World world)
	{
		worlds.Add(world);
	}

	/// <summary>
	/// Get all worlds
	/// </summary>
	public List<World> GetAllWorlds()
	{
		return new List<World>(worlds);
	}
}

/// <summary>
/// Represents a world containing multiple levels
/// </summary>
public class World
{
	private string theme;
	private List<Level> levels = new List<Level>();

	public World(string theme)
	{
		this.theme = theme;
	}

	/// <summary>
	/// Get the theme of this world
	/// </summary>
	public string GetTheme()
	{
		return theme;
	}

	/// <summary>
	/// Set the theme of this world
	/// </summary>
	public void SetTheme(string newTheme)
	{
		theme = newTheme;
	}

	/// <summary>
	/// Add a level to this world
	/// </summary>
	public void AddLevel(Level level)
	{
		// If the level doesn't have a name, set it to "Level X" where X is the index + 1
		if (string.IsNullOrEmpty(level.GetLevelName()))
		{
			level.SetLevelName($"Level {levels.Count + 1}");
		}
		levels.Add(level);
	}

	/// <summary>
	/// Get a level by index
	/// </summary>
	public Level GetLevel(int index)
	{
		if (index < 0 || index >= levels.Count)
		{
			GD.PrintErr($"Level index {index} is out of bounds for world '{theme}'");
			return null;
		}
		return levels[index];
	}

	/// <summary>
	/// Get the total number of levels in this world
	/// </summary>
	public int GetLevelCount()
	{
		return levels.Count;
	}

	/// <summary>
	/// Get all levels in this world
	/// </summary>
	public List<Level> GetAllLevels()
	{
		return new List<Level>(levels);
	}
}

/// <summary>
/// Data structure for deserializing world JSON
/// </summary>
public class WorldData
{
	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("levels")]
	public List<LevelData> Levels { get; set; }
}

/// <summary>
/// Data structure for deserializing level JSON
/// </summary>
public class LevelData
{
	[JsonPropertyName("letters")]
	public string Letters { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }
}
