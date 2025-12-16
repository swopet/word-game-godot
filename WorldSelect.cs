using Godot;
using System;

public partial class WorldSelect : Control
{
	private VBoxContainer mainContainer;
	private VBoxContainer worldListContainer;
	private VBoxContainer worldDetailContainer;
	private Button backToMenuButton;
	private Label titleLabel;
	private int selectedWorldIndex = -1;

	public override void _Ready()
	{
		SetupUI();
	}

	/// <summary>
	/// Set up the world selection UI
	/// </summary>
	private void SetupUI()
	{
		// Create main container
		mainContainer = new VBoxContainer();
		mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(mainContainer);

		// Add title
		titleLabel = new Label();
		titleLabel.Text = "World Select";
		var titleFont = ThemeDB.FallbackFont;
		titleLabel.AddThemeFontOverride("font", titleFont);
		titleLabel.AddThemeFontSizeOverride("font_size", 32);
		titleLabel.CustomMinimumSize = new Vector2(0, 80);
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainContainer.AddChild(titleLabel);

		// Create scroll container for world buttons
		var scrollContainer = new ScrollContainer();
		scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		mainContainer.AddChild(scrollContainer);

		// Create VBox for world buttons
		worldListContainer = new VBoxContainer();
		worldListContainer.CustomMinimumSize = new Vector2(0, 0);
		scrollContainer.AddChild(worldListContainer);

		// Create world detail container (initially hidden)
		worldDetailContainer = new VBoxContainer();
		worldDetailContainer.Visible = false;
		worldDetailContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		mainContainer.AddChild(worldDetailContainer);

		// Populate worlds
		PopulateWorlds();

		// Add back button at the bottom
		backToMenuButton = new Button();
		backToMenuButton.Text = "Back to Menu";
		backToMenuButton.CustomMinimumSize = new Vector2(0, 60);
		backToMenuButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		backToMenuButton.Pressed += OnBackPressed;
		mainContainer.AddChild(backToMenuButton);
	}

	/// <summary>
	/// Populate the world list with buttons
	/// </summary>
	private void PopulateWorlds()
	{
		var worldsAutoload = (Worlds)GetTree().Root.GetNode("Worlds");
		var persistentData = (PersistentData)GetTree().Root.GetNode("PersistentData");
		int worldCount = worldsAutoload.GetWorldCount();

		for (int i = 0; i < worldCount; i++)
		{
			var world = worldsAutoload.GetWorld(i);
			if (world == null)
				continue;

			// Count solved levels for this world
			int solvedCount = 0;
			int totalLevels = world.GetLevelCount();
			
			for (int j = 0; j < totalLevels; j++)
			{
				var level = world.GetLevel(j);
				string levelSerialized = level.Serialize();
				if (persistentData.IsLevelSolved(levelSerialized))
				{
					solvedCount++;
				}
			}

			var worldButton = new Button();
			worldButton.Text = $"{world.GetTheme()} ({solvedCount}/{totalLevels})";
			worldButton.CustomMinimumSize = new Vector2(0, 60);
			
			int worldIndex = i; // Capture for closure
			worldButton.Pressed += () => OnWorldSelected(worldIndex);
			
			worldListContainer.AddChild(worldButton);
		}
	}

	/// <summary>
	/// Handle world selection
	/// </summary>
	private void OnWorldSelected(int worldIndex)
	{
		selectedWorldIndex = worldIndex;
		ShowWorldDetails(worldIndex);
	}

	/// <summary>
	/// Show the level selection for a specific world
	/// </summary>
	private void ShowWorldDetails(int worldIndex)
	{
		var worldsAutoload = (Worlds)GetTree().Root.GetNode("Worlds");
		var world = worldsAutoload.GetWorld(worldIndex);

		if (world == null)
			return;

		// Hide world list, show details
		worldListContainer.GetParent<ScrollContainer>().Visible = false;
		worldDetailContainer.Visible = true;
		backToMenuButton.Visible = false;
		titleLabel.Visible = false;

		// Clear previous content
		foreach (var child in worldDetailContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Add world name title
		var worldTitleLabel = new Label();
		worldTitleLabel.Text = world.GetTheme();
		worldTitleLabel.AddThemeFontSizeOverride("font_size", 32);
		worldTitleLabel.CustomMinimumSize = new Vector2(0, 80);
		worldTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		worldDetailContainer.AddChild(worldTitleLabel);

		// Create scroll container for level grid
		var levelScrollContainer = new ScrollContainer();
		levelScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		levelScrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		worldDetailContainer.AddChild(levelScrollContainer);

		// Create grid container for level buttons
		var gridContainer = new GridContainer();
		gridContainer.Columns = 5; // 5 columns to fit 5 buttons across
		var spacing = 2;
		gridContainer.AddThemeConstantOverride("h_separation", spacing);
		gridContainer.AddThemeConstantOverride("v_separation", spacing);
		
		// Calculate button size based on screen width
		float buttonSize = (GetViewportRect().Size.X - spacing * (gridContainer.Columns + 1))  / (float)gridContainer.Columns;
		gridContainer.CustomMinimumSize = new Vector2(0, 0);
		levelScrollContainer.AddChild(gridContainer);

		// Add level buttons
		int levelCount = world.GetLevelCount();
		var persistentData = (PersistentData)GetTree().Root.GetNode("PersistentData");
		
		for (int i = 0; i < levelCount; i++)
		{
			var level = world.GetLevel(i);
			string levelSerialized = level.Serialize();
			bool isSolved = persistentData.IsLevelSolved(levelSerialized);

			var levelButton = new Button();
			levelButton.Text = (i + 1).ToString();
			levelButton.CustomMinimumSize = new Vector2(buttonSize, buttonSize);
			
			// Set button modulate color based on solved status
			if (isSolved)
			{
				levelButton.Modulate = new Color(0.3f, 0.6f, 0.3f, 1.0f); // Light green
			}
			else
			{
				levelButton.Modulate = new Color(0.85f, 0.85f, 0.85f, 1.0f); // Light grey
			}
			
			int levelIndex = i;
			levelButton.Pressed += () => OnLevelSelected(worldIndex, levelIndex);
			gridContainer.AddChild(levelButton);
		}

		// Add back to worlds button
		var backToWorldsButton = new Button();
		backToWorldsButton.Text = "World Select";
		backToWorldsButton.CustomMinimumSize = new Vector2(0, 60);
		backToWorldsButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		backToWorldsButton.Pressed += OnBackToWorlds;
		worldDetailContainer.AddChild(backToWorldsButton);
	}

	/// <summary>
	/// Handle level selection
	/// </summary>
	private void OnLevelSelected(int worldIndex, int levelIndex)
	{
		var worldsAutoload = (Worlds)GetTree().Root.GetNode("Worlds");
		var world = worldsAutoload.GetWorld(worldIndex);
		var level = world.GetLevel(levelIndex);

		GD.Print($"Selected level: {level.GetLevelName()} from world {world.GetTheme()}");
		
		// Store indices temporarily in LevelSolver's static property
		LevelSolver.PendingWorldIndex = worldIndex;
		LevelSolver.PendingLevelIndex = levelIndex;
		
		// Load the level solver scene
		GetTree().ChangeSceneToFile("res://LevelSolver.tscn");
	}

	/// <summary>
	/// Go back to world list from level selection
	/// </summary>
	private void OnBackToWorlds()
	{
		worldDetailContainer.Visible = false;
		worldListContainer.GetParent<ScrollContainer>().Visible = true;
		backToMenuButton.Visible = true;
		titleLabel.Visible = true;
		selectedWorldIndex = -1;
	}

	/// <summary>
	/// Handle back button press
	/// </summary>
	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://MainMenu.tscn");
	}
}
