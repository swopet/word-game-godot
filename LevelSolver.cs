using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LevelSolver : Control
{
	// Static properties for passing level selection
	public static int PendingWorldIndex { get; set; } = -1;
	public static int PendingLevelIndex { get; set; } = -1;

	private Level currentLevel;
	private World currentWorld;
	private int currentWorldIndex = -1;
	private int currentLevelIndex = -1;

	// UI Elements
	private Label titleLabel;
	private Button leftArrowButton;
	private Button rightArrowButton;
	private GridContainer gridContainer;
	private FlowContainer letterButtonsContainer;
	private Button shuffleButton;
	private Button resetButton;
	private Button sortButton;
	private Button backButton;

	// Game State
	private GridTile[] gridTiles;
	private Letter[] letters;
	private int activeTileIndex = -1;
	private List<string> originalLetters = new List<string>();
	private Dictionary<int, int> placedLetters = new Dictionary<int, int>(); // letterIndex -> gridIndex

	private LetterTile movingLetter = null;
	private double movingStartTime = 0;
	private Vector2 movingOriginalPosition = Vector2.Zero;
	private Node movingOriginalParent = null;
	private int movingOriginalIndex = -1;
	private GridTile highlightedGridTile = null;

	public override void _Ready()
	{
		SetupUI();
		
		// Get level selection from static properties
		int worldIndex = PendingWorldIndex;
		int levelIndex = PendingLevelIndex;
		
		LoadLevel(worldIndex, levelIndex);
	}

    public override void _Process(double delta)
    {
        base._Process(delta);
		if (movingLetter != null)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			movingLetter.SetGlobalPosition(mousePos - movingLetter.GetSize() / 2);
			if (highlightedGridTile != null)
			{
				highlightedGridTile.Modulate = Colors.White;
				highlightedGridTile = null;
			}
			var overlappedTile = null as GridTile;
			foreach (var gt in gridTiles)
			{
				var rect = gt.GetGlobalRect();
				if (rect.HasPoint(mousePos))
				{
					overlappedTile = gt;
					break;
				}
			}
			// If released over empty grid tile, place there
			if (overlappedTile != null && overlappedTile.Letter == "")
			{
				overlappedTile.Modulate = Colors.Green;
				highlightedGridTile = overlappedTile;
			}
		}
    }


	/// <summary>
	/// Load a level from a world and level index
	/// </summary>
	public void LoadLevelFromSelection(int worldIndex, int levelIndex)
	{
		currentWorldIndex = worldIndex;
		currentLevelIndex = levelIndex;
		LoadLevel(worldIndex, levelIndex);
	}

	private void LoadLevel(int worldIndex, int levelIndex)
	{
		// Track world and level indices
		currentWorldIndex = worldIndex;
		currentLevelIndex = levelIndex;

		var worldsAutoload = (Worlds)GetTree().Root.GetNode("Worlds");
		currentWorld = worldsAutoload.GetWorld(worldIndex);
		currentLevel = currentWorld.GetLevel(levelIndex);

		if (currentLevel == null)
		{
			GD.PrintErr($"Failed to load level {levelIndex} from world {worldIndex}");
			return;
		}

		// Update title
		titleLabel.Text = $"{currentWorld.GetTheme()}: {currentLevel.GetLevelName()}";

		// Update arrow buttons
		leftArrowButton.Disabled = (currentLevelIndex == 0);
		rightArrowButton.Disabled = (currentLevelIndex == currentWorld.GetLevelCount() - 1);

		// Update button colors
		var blue = new Color(0.5f, 0.6f, 0.7f);
		var grey = new Color(0.8f, 0.8f, 0.8f);
		var leftStyle = new StyleBoxFlat();
		var rightStyle = new StyleBoxFlat();
		if (leftArrowButton.Disabled)
			leftStyle.BgColor = grey;
		else
			leftStyle.BgColor = blue;
		if (rightArrowButton.Disabled)
			rightStyle.BgColor = grey;
		else
			rightStyle.BgColor = blue;
		leftArrowButton.AddThemeStyleboxOverride("normal", leftStyle);
		rightArrowButton.AddThemeStyleboxOverride("normal", rightStyle);

		// Initialize game state
		originalLetters.Clear();
		placedLetters.Clear();
		activeTileIndex = -1;

		// Get and shuffle letters
		var letters = currentLevel.GetLetters().ToList();
		originalLetters = letters.Select(c => c.ToString()).ToList();
		// Fisher-Yates shuffle
		for (int i = letters.Count - 1; i > 0; i--)
		{
			int randomIndex = (int)(GD.Randf() * (i + 1));
			char temp = letters[i];
			letters[i] = letters[randomIndex];
			letters[randomIndex] = temp;
		}

		// Set up grid tiles
		SetupGridTiles();

		// Set up letter tiles
		SetupLetterTiles(letters);
	}

	private void SetupUI()
	{
		// Main container
		var mainContainer = new VBoxContainer();
		mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(mainContainer);

		// Title and navigation row (combined)
		var titleNavContainer = new HBoxContainer();
		titleNavContainer.CustomMinimumSize = new Vector2(0, 60);
		mainContainer.AddChild(titleNavContainer);

		// Left arrow
		leftArrowButton = new Button();
		leftArrowButton.Text = "←";
		leftArrowButton.CustomMinimumSize = new Vector2(50, 50);
		leftArrowButton.Pressed += OnLeftArrowPressed;
		titleNavContainer.AddChild(leftArrowButton);

		// Title (centered)
		titleLabel = new Label();
		titleLabel.AddThemeFontSizeOverride("font_size", 24);
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		titleNavContainer.AddChild(titleLabel);

		// Right arrow
		rightArrowButton = new Button();
		rightArrowButton.Text = "→";
		rightArrowButton.CustomMinimumSize = new Vector2(50, 50);
		rightArrowButton.Pressed += OnRightArrowPressed;
		titleNavContainer.AddChild(rightArrowButton);

		// Grid container (middle, no scroll)
		var gridPanel = new Panel();
		gridPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		mainContainer.AddChild(gridPanel);

		var centerContainer = new CenterContainer();
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		gridPanel.AddChild(centerContainer);

		gridContainer = new GridContainer();
		centerContainer.AddChild(gridContainer);

		// Letter buttons container (wrapping, no scroll)
		var letterPanel = new Panel();
		letterPanel.CustomMinimumSize = new Vector2(0, 120);
		mainContainer.AddChild(letterPanel);

		letterButtonsContainer = new FlowContainer();
		// Prevent overflow: stretch horizontally, expand, and clip contents
		letterButtonsContainer.AnchorLeft = 0;
		letterButtonsContainer.AnchorRight = 1;
		letterButtonsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		letterButtonsContainer.ClipContents = true;
		letterButtonsContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		letterPanel.AddChild(letterButtonsContainer);

		// Bottom buttons
		var bottomContainer = new HBoxContainer();
		bottomContainer.CustomMinimumSize = new Vector2(0, 60);
		mainContainer.AddChild(bottomContainer);

		shuffleButton = new Button();
		shuffleButton.Text = "Shuffle";
		shuffleButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		shuffleButton.Pressed += OnShufflePressed;
		bottomContainer.AddChild(shuffleButton);

		resetButton = new Button();
		resetButton.Text = "Reset";
		resetButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		resetButton.Pressed += OnResetPressed;
		bottomContainer.AddChild(resetButton);

		sortButton = new Button();
		sortButton.Text = "ABCDE";
		sortButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		sortButton.Pressed += OnSortPressed;
		bottomContainer.AddChild(sortButton);

		backButton = new Button();
		backButton.Text = "Back";
		backButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		backButton.Pressed += OnBackPressed;
		bottomContainer.AddChild(backButton);
	}

	private void SetupGridTiles()
	{
		int width = currentLevel.GetWidth();
		int height = currentLevel.GetHeight();

		gridContainer.Columns = width;
		gridContainer.CustomMinimumSize = new Vector2(0, 0);

		// Calculate tile size based on viewport constraints
		// Get available space for grid (subtract top/bottom UI elements)
		Vector2 viewportSize = GetViewportRect().Size;
		float availableWidth = viewportSize.X;
		float availableHeight = viewportSize.Y - 60 - 120 - 60; // title, letters, buttons heights
		
		// Account for margins: left/right padding (8px each) and gaps between tiles (4px gap)
		float horizontalPadding = 8 + 8; // left + right padding
		float horizontalMargin = (width - 1) * (width-1); // Gaps between tiles
		float verticalMargin = (height - 1) * (height-1); // Gaps between tiles
		
		// Calculate max tile size that fits both width and height constraints
		float tileWidthSize = (availableWidth - horizontalPadding - horizontalMargin) / width;
		float tileHeightSize = (availableHeight - verticalMargin) / height;
		float tileSize = Mathf.Min(tileWidthSize, tileHeightSize);

		// Clear existing tiles
		foreach (var child in gridContainer.GetChildren())
		{
			child.QueueFree();
		}

		gridTiles = new GridTile[width * height];

		for (int i = 0; i < width * height; i++)
		{
			var tile = new GridTile();
			tile.Index = i;
			tile.CustomMinimumSize = new Vector2(tileSize, tileSize);
			int capturedIndex = i; // Proper closure
			tile.GuiInput += (InputEvent ev) => OnGridTileInput(capturedIndex, ev);
			gridContainer.AddChild(tile);
			gridTiles[i] = tile;
		}

		// Update letter tile sizes to match grid tile global size
		Vector2 gridTileSize = gridTiles.Length > 0 ? gridTiles[0].GetGlobalRect().Size : new Vector2(tileSize, tileSize);
		if (letters != null)
		{
			foreach (var letter in letters)
			{
				letter.Tile.CustomMinimumSize = gridTileSize;
				letter.Placeholder.CustomMinimumSize = gridTileSize;
			}
		}
	}

	private void SetupLetterTiles(List<char> letters)
	{
		foreach (var child in letterButtonsContainer.GetChildren())
		{
			child.QueueFree();
		}
		this.letters = new Letter[letters.Count];
		Vector2 gridTileSize = gridTiles != null && gridTiles.Length > 0 ? gridTiles[0].GetGlobalRect().Size : new Vector2(50, 50);
		for (int i = 0; i < letters.Count; i++)
		{
			var letter = new Letter(letters[i].ToString(), i);
			var tile = letter.Tile;
			tile.ButtonDown += () => OnLetterTilePressed(tile);
			letter.Tile.CustomMinimumSize = gridTileSize;
			letter.Placeholder.CustomMinimumSize = gridTileSize;
			this.letters[i] = letter;
			letterButtonsContainer.AddChild(letter.Placeholder);
			letterButtonsContainer.AddChild(letter.Tile);
		}
	}

	private void OnGridTileInput(int index, InputEvent inputEvent)
	{
		// Only process empty grid tiles
		if (gridTiles[index].Letter != "")
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			GD.Print($"Grid tile clicked: {index}");

			// Clear arrow from previous active tile
			if (activeTileIndex >= 0 && activeTileIndex != index)
			{
				gridTiles[activeTileIndex].Arrow = GridTile.ArrowType.None;
			}

			// Cycle through arrows: None -> Right -> Down -> None
			gridTiles[index].Arrow = (GridTile.ArrowType)(((int)gridTiles[index].Arrow + 1) % 3);
			activeTileIndex = index;
		}
	}

	private void OnLetterTilePressed(LetterTile tile)
	{
		if (movingLetter != null) return; // Only one at a time
		movingLetter = tile;
		movingStartTime = Time.GetTicksMsec() / 1000.0;
		movingOriginalPosition = GetViewport().GetMousePosition();
		movingOriginalParent = tile.GetParent();
		movingOriginalIndex = movingOriginalParent.GetIndex();
		letters[tile.Index].Placeholder.Visible = true;
		
		tile.GetParent()?.CallDeferred("remove_child", tile);
		GetTree().Root.CallDeferred("add_child", tile);
		// Reset anchors and position for root scene
		tile.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		tile.Position = Vector2.Zero;
		var mousePos = GetViewport().GetMousePosition();
		movingLetter.SetGlobalPosition(mousePos - movingLetter.GetSize() / 2);
		GD.Print($"Letter {tile.Index} is now moving");
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (movingLetter != null && @event is InputEventMouseButton mouseEvent && !mouseEvent.Pressed)
		{
			OnLetterTileReleased(movingLetter);
		}
	}

	private void OnLetterTileReleased(LetterTile tile)
	{
		if (movingLetter != tile)
		{
			// ...existing code for tap/unplace...
			return;
		}
		var viewport = GetViewport();
		var mousePos = viewport.GetMousePosition();
		GridTile overlappedTile = null;
		foreach (var gt in gridTiles)
		{
			var rect = gt.GetGlobalRect();
			if (rect.HasPoint(mousePos))
			{
				overlappedTile = gt;
				GD.Print("Overlapped grid tile: " + gt.Index);
				break;
			}
		}
		double now = Time.GetTicksMsec() / 1000.0;
		bool snappedBack = false;
		// If released over empty grid tile, place there
		if (overlappedTile != null && overlappedTile.Letter == "")
		{
			int letterIndex = tile.Index;
			int gridIndex = overlappedTile.Index;
			PlaceLetterOnTile(letterIndex, gridIndex);
			overlappedTile.Modulate = Colors.White;
			highlightedGridTile = null;
		}
		// If released over original position and <1s, treat as tap
		else if ((mousePos - movingOriginalPosition).Length() < 10.0 && (now - movingStartTime) < 1.0)
		{
			GD.Print("Tapped letter tile " + tile.Index);
			int index = tile.Index;
			if (activeTileIndex >= 0 && !letters[index].IsPlaced)
			{
				PlaceLetterOnTile(index, activeTileIndex);
			}
			else
			{
				snappedBack = true;
			}
		}
		else
		{
			snappedBack = true;
		}
		// Restore parent and position if snapped back
		if (snappedBack)
		{
			if (tile.GetParent() != null)
				tile.GetParent().CallDeferred("remove_child", tile);
			// Insert tile before its placeholder for correct layout
			int placeholderIndex = letterButtonsContainer.GetChildren().IndexOf(letters[tile.Index].Placeholder);
			if (placeholderIndex > 0)
				letterButtonsContainer.CallDeferred("add_child", tile);
			else
				letterButtonsContainer.CallDeferred("add_child", tile);
			// Move tile to the correct index (before placeholder)
			letterButtonsContainer.CallDeferred("move_child", tile, Math.Max(0, placeholderIndex));
			// Reset anchors and position for flow container
			tile.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			tile.Position = Vector2.Zero;
			letters[tile.Index].Placeholder.Visible = false;
			letters[tile.Index].IsPlaced = false;
			letters[tile.Index].OriginalGridIndex = null;
			if (placedLetters.ContainsKey(tile.Index)){
				gridTiles[placedLetters[tile.Index]].Letter = "";
				placedLetters.Remove(tile.Index); 
			}
			
			if (activeTileIndex >= 0 && activeTileIndex < gridTiles.Length)
			{
				gridTiles[activeTileIndex].Arrow = GridTile.ArrowType.None;
			}
			activeTileIndex = -1;
		}
		movingLetter = null;
		movingStartTime = 0;
		movingOriginalPosition = Vector2.Zero;
		movingOriginalParent = null;
		movingOriginalIndex = -1;
		if (highlightedGridTile != null)
		{
			highlightedGridTile.Modulate = Colors.White;
			highlightedGridTile = null;
		}
		GD.Print($"Letter {tile.Index} released");
	}

	private void PlaceLetterOnTile(int letterIndex, int gridIndex)
	{
		GD.Print($"Attempting to place tile {letterIndex} over grid tile {gridIndex}");
		// Place the letter on the grid and update state
		if (letterIndex < 0 || letterIndex >= letters.Length)
		{
			GD.PrintErr($"Invalid letterIndex: {letterIndex}");
			return;
		}
		if (gridIndex < 0 || gridIndex >= gridTiles.Length)
		{
			GD.PrintErr($"Invalid gridIndex: {gridIndex}");
			return;
		}
		if (placedLetters.ContainsValue(gridIndex))
		{
			GD.Print("Grid tile already occupied");
			return;
		}
		int width = currentLevel.GetWidth();
		int height = currentLevel.GetHeight();
		placedLetters[letterIndex] = gridIndex;
		letters[letterIndex].IsPlaced = true;
		if (letters[letterIndex].OriginalGridIndex != null)
			gridTiles[letters[letterIndex].OriginalGridIndex.Value].Letter = "";
		letters[letterIndex].OriginalGridIndex = gridIndex;
		gridTiles[gridIndex].Letter = letters[letterIndex].Text;
		GD.Print($"Placed letter tile {letterIndex} over grid tile {gridIndex}");
		letters[letterIndex].Placeholder.Visible = true;
		if (letters[letterIndex].Tile.GetParent() == letterButtonsContainer)
			letterButtonsContainer.RemoveChild(letters[letterIndex].Tile);
		else if (letters[letterIndex].Tile.GetParent() != null)
			letters[letterIndex].Tile.GetParent().RemoveChild(letters[letterIndex].Tile);
		gridTiles[gridIndex].AddChild(letters[letterIndex].Tile);
		// Reset position and anchors so tile is centered in grid
		letters[letterIndex].Tile.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		letters[letterIndex].Tile.Position = Vector2.Zero;
		letters[letterIndex].Tile.Visible = true;
		GD.Print($"Added letter tile {letterIndex} to grid tile {gridIndex}");
		// Arrow navigation logic
		GridTile.ArrowType currentArrow = gridTiles[gridIndex].Arrow;
		int nextTileIndex = -1;
		if (currentArrow == GridTile.ArrowType.Right)
		{
			int nextIndex = gridIndex + 1;
			if (nextIndex % width != 0)
				nextTileIndex = nextIndex;
		}
		else if (currentArrow == GridTile.ArrowType.Down)
		{
			int nextIndex = gridIndex + width;
			if (nextIndex < width * height)
				nextTileIndex = nextIndex;
		}
		gridTiles[gridIndex].Arrow = GridTile.ArrowType.None;
		int searchIndex = nextTileIndex;
		bool found = false;
		while (searchIndex >= 0 && searchIndex < width * height)
		{
			bool isUnplaced = true;
			foreach (Node child in gridTiles[searchIndex].GetChildren())
			{
				if (child is Button)
				{
					isUnplaced = false;
					break;
				}
			}
			if (isUnplaced)
			{
				GD.Print($"Moving activeTileIndex to {searchIndex}");
				activeTileIndex = searchIndex;
				gridTiles[searchIndex].Arrow = currentArrow;
				found = true;
				break;
			}
			if (currentArrow == GridTile.ArrowType.Right)
			{
				int next = searchIndex + 1;
				if (next % width == 0) break;
				searchIndex = next;
			}
			else if (currentArrow == GridTile.ArrowType.Down)
			{
				int next = searchIndex + width;
				if (next >= width * height) break;
				searchIndex = next;
			}
			else
			{
				break;
			}
		}
		if (!found)
		{
			GD.Print($"No next tile available, clearing activeTileIndex");
			activeTileIndex = -1;
		}
		// Check for puzzle completion
		if (letters.All(l => l.IsPlaced))
		{
			CheckPuzzleSolved();
		}
	}
	private void CheckPuzzleSolved()
	{
		// Collect all words of length 2 or more from the grid
		var words = new List<string>();
		int width = currentLevel.GetWidth();
		int height = currentLevel.GetHeight();
		// Horizontal words
		for (int y = 0; y < height; y++)
		{
			string word = "";
			for (int x = 0; x < width; x++)
			{
				int idx = y * width + x;
				string letter = gridTiles[idx].Letter;
				if (!string.IsNullOrEmpty(letter))
					word += letter;
				else
				{
					if (word.Length >= 2) words.Add(word);
					word = "";
				}
			}
			if (word.Length >= 2) words.Add(word);
		}
		// Vertical words
		for (int x = 0; x < width; x++)
		{
			string word = "";
			for (int y = 0; y < height; y++)
			{
				int idx = y * width + x;
				string letter = gridTiles[idx].Letter;
				if (!string.IsNullOrEmpty(letter))
					word += letter;
				else
				{
					if (word.Length >= 2) words.Add(word);
					word = "";
				}
			}
			if (word.Length >= 2) words.Add(word);
		}

		// Check all words against WordList
		var wordList = GetNodeOrNull<WordList>("/root/WordList");
		if (wordList == null)
		{
			GD.PrintErr("WordList autoload not found!");
			return;
		}
		bool allValid = words.All(w => wordList.IsValidWord(w));
		if (allValid)
		{
			ShowSolvedDialog();
			// Mark level as solved in PersistentData
			var persistent = GetNodeOrNull<PersistentData>("/root/PersistentData");
			if (persistent != null)
			{
				var sortedLetters = new List<string>(originalLetters);
				sortedLetters.Sort();
				string levelId = string.Join("", sortedLetters) + $"{currentLevel.GetWidth()}x{currentLevel.GetHeight()}";
				persistent.MarkLevelSolved(levelId);
				GD.Print($"Level marked as solved: {levelId}");
			}
		}
	}
	private void ShowSolvedDialog()
		
	{
		var dialogScene = GD.Load<PackedScene>("res://SolvedDialog.tscn");
		var dialog = dialogScene.Instantiate<Panel>();
		// Make dialog and buttons consume all mouse/button events
		dialog.MouseFilter = Control.MouseFilterEnum.Stop;
		var buttonBack = dialog.GetNode<Button>("VBox/ButtonBack");
		var buttonNext = dialog.GetNode<Button>("VBox/ButtonNext");
		buttonBack.MouseFilter = Control.MouseFilterEnum.Stop;
		buttonNext.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(dialog);
		// Make dialog a small centered box
		dialog.AnchorLeft = 0.25f;
		dialog.AnchorTop = 0.35f;
		dialog.AnchorRight = 0.75f;
		dialog.AnchorBottom = 0.65f;
		dialog.OffsetLeft = 0;
		dialog.OffsetTop = 0;
		dialog.OffsetRight = 0;
		dialog.OffsetBottom = 0;
		dialog.ZIndex = 100;
		// Style: minimal box, grey background
		var stylebox = new StyleBoxFlat();
		stylebox.BgColor = new Color(0.85f, 0.85f, 0.85f); // light grey
		stylebox.BorderColor = new Color(0.6f, 0.6f, 0.6f);
		stylebox.BorderWidthTop = 2;
		stylebox.BorderWidthBottom = 2;
		stylebox.BorderWidthLeft = 2;
		stylebox.BorderWidthRight = 2;
		dialog.AddThemeStyleboxOverride("panel", stylebox);
		dialog.Modulate = new Color(1,1,1,0.9f); // semi-transparent
		buttonBack.Pressed += () => {
			dialog.QueueFree();
			OnBackPressed();
		};
		buttonNext.Pressed += () => {
			dialog.QueueFree();
			GoToNextPuzzle();
		};
	}
	private void GoToNextPuzzle()
	{
		if (currentLevelIndex < currentWorld.GetLevelCount() - 1)
		{
			currentLevelIndex++;
			LoadLevel(currentWorldIndex, currentLevelIndex);
		}
	}

	private void OnLeftArrowPressed()
	{
		if (currentLevelIndex > 0)
		{
			currentLevelIndex--;
			LoadLevel(currentWorldIndex, currentLevelIndex);
		}
	}

	private void OnRightArrowPressed()
	{
		if (currentLevelIndex < currentWorld.GetLevelCount() - 1)
		{
			currentLevelIndex++;
			LoadLevel(currentWorldIndex, currentLevelIndex);
		}
	}

	private void OnShufflePressed()
	{
		// Shuffle only unplaced letter tiles
		var unplacedIndices = new List<int>();
		var placedIndices = new List<int>();
		for (int i = 0; i < letters.Length; i++)
		{
			if (!letters[i].IsPlaced)
				unplacedIndices.Add(i);
			else
				placedIndices.Add(i);
		}
		for (int i = unplacedIndices.Count - 1; i > 0; i--)
		{
			int randomIndex = (int)(GD.Randf() * (i + 1));
			int temp = unplacedIndices[i];
			unplacedIndices[i] = unplacedIndices[randomIndex];
			unplacedIndices[randomIndex] = temp;
		}
		for (int i = placedIndices.Count - 1; i > 0; i--)
		{
			int randomIndex = (int)(GD.Randf() * (i + 1));
			int temp = placedIndices[i];
			placedIndices[i] = placedIndices[randomIndex];
			placedIndices[randomIndex] = temp;
		}
		for (int i = letterButtonsContainer.GetChildCount() - 1; i >= 0; i--)
		{
			var node = letterButtonsContainer.GetChild(i);
			if (node is LetterTile lt && !letters[lt.Index].IsPlaced)
				letterButtonsContainer.RemoveChild(lt);
			if (node is LetterPlaceholder lp && letters[lp.Index].IsPlaced)
				letterButtonsContainer.RemoveChild(lp);
		}
		// Build new letters array in shuffled order
		var newLetters = new List<Letter>();
		foreach (int idx in unplacedIndices)
			newLetters.Add(letters[idx]);
		// Add placed letters at the end (preserve their order)
		for (int i = 0; i < placedIndices.Count; i++)
			if (letters[placedIndices[i]].IsPlaced)
				newLetters.Add(letters[placedIndices[i]]);

		// Update indices and UI indices
		for (int i = 0; i < newLetters.Count; i++)
		{
			newLetters[i].Index = i;
			newLetters[i].Tile.Index = i;
			newLetters[i].Placeholder.Index = i;
		}
		letters = newLetters.ToArray();

		// Rebuild flow container
		for (int i = letterButtonsContainer.GetChildCount() - 1; i >= 0; i--)
		{
			var node = letterButtonsContainer.GetChild(i);
			if (node is LetterTile || node is LetterPlaceholder)
				letterButtonsContainer.RemoveChild(node);
		}
		for (int i = 0; i < letters.Length; i++)
		{
			if (!letters[i].IsPlaced)
			{
				letterButtonsContainer.AddChild(letters[i].Placeholder);
				letterButtonsContainer.AddChild(letters[i].Tile);
				letters[i].Placeholder.Visible = false;
			}
			else
			{
				letterButtonsContainer.AddChild(letters[i].Placeholder);
				letters[i].Placeholder.Visible = true;
			}
		}
	}

	private void OnResetPressed()
	{
		// Remove all placed letters and return them to placeholders
		placedLetters.Clear();
		// Clear all arrows and active tile
		if (activeTileIndex >= 0 && activeTileIndex < gridTiles.Length)
		{
			gridTiles[activeTileIndex].Arrow = GridTile.ArrowType.None;
		}
		activeTileIndex = -1;
		for (int i = 0; i < gridTiles.Length; i++)
		{
			gridTiles[i].Letter = "";
			gridTiles[i].Arrow = GridTile.ArrowType.None;
		}
		// Move all placeholders and letter tiles back to the flow container in correct order
		// Remove all letter tiles and placeholders from flow container
		for (int i = letterButtonsContainer.GetChildCount() - 1; i >= 0; i--)
		{
			var node = letterButtonsContainer.GetChild(i);
			if (node is LetterTile || node is LetterPlaceholder)
				letterButtonsContainer.RemoveChild(node);
		}
		// Placeholders must be in the correct locations for layout stability
		for (int i = 0; i < letters.Length; i++)
		{
			var letterTile = letters[i].Tile;
			var placeholder = letters[i].Placeholder;
			letters[i].IsPlaced = false;
			letters[i].OriginalGridIndex = null;
			letterTile.Visible = true;
			placeholder.Visible = false;
			if (letterTile.GetParent() != null)
				letterTile.GetParent().RemoveChild(letterTile);
			if (placeholder.GetParent() != null)
				placeholder.GetParent().RemoveChild(placeholder);
			// Add placeholder first, then tile, to preserve layout
			letterButtonsContainer.AddChild(placeholder);
			letterButtonsContainer.AddChild(letterTile);
		}
	}

	private void OnSortPressed()
	{
		// Sort only unplaced letter tiles alphabetically, but also process placed indices for layout
		var unplacedIndices = new List<int>();
		var placedIndices = new List<int>();
		for (int i = 0; i < letters.Length; i++)
		{
			if (!letters[i].IsPlaced)
				unplacedIndices.Add(i);
			else
				placedIndices.Add(i);
		}
		unplacedIndices.Sort((a, b) => letters[a].Text.CompareTo(letters[b].Text));

		for (int i = letterButtonsContainer.GetChildCount() - 1; i >= 0; i--)
		{
			var node = letterButtonsContainer.GetChild(i);
			if (node is LetterTile lt && !letters[lt.Index].IsPlaced)
				letterButtonsContainer.RemoveChild(lt);
			if (node is LetterPlaceholder lp && letters[lp.Index].IsPlaced)
				letterButtonsContainer.RemoveChild(lp);
		}
		// Build new letters array in sorted order
		var newLetters = new List<Letter>();
		foreach (int idx in unplacedIndices)
			newLetters.Add(letters[idx]);
		// Add placed letters at the end (preserve their order)
		for (int i = 0; i < placedIndices.Count; i++)
			if (letters[placedIndices[i]].IsPlaced)
				newLetters.Add(letters[placedIndices[i]]);

		// Update indices and UI indices
		for (int i = 0; i < newLetters.Count; i++)
		{
			newLetters[i].Index = i;
			newLetters[i].Tile.Index = i;
			newLetters[i].Placeholder.Index = i;
		}
		letters = newLetters.ToArray();

		// Rebuild flow container
		for (int i = letterButtonsContainer.GetChildCount() - 1; i >= 0; i--)
		{
			var node = letterButtonsContainer.GetChild(i);
			if (node is LetterTile || node is LetterPlaceholder)
				letterButtonsContainer.RemoveChild(node);
		}
		for (int i = 0; i < letters.Length; i++)
		{
			if (!letters[i].IsPlaced)
			{
				letterButtonsContainer.AddChild(letters[i].Placeholder);
				letterButtonsContainer.AddChild(letters[i].Tile);
				letters[i].Placeholder.Visible = false;
			}
			else
			{
				letterButtonsContainer.AddChild(letters[i].Placeholder);
				letters[i].Placeholder.Visible = true;
			}
		}
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://WorldSelect.tscn");
	}
}

/// <summary>
/// Represents a grid tile in the puzzle
/// </summary>
public partial class GridTile : Panel
{
	public enum ArrowType { None, Right, Down }

	public int Index { get; set; }
	public ArrowType Arrow { get; set; } = ArrowType.None;
	public string Letter { get; set; } = "";

	private Panel backgroundPanel;
	private Label arrowLabel;

	public override void _Ready()
	{
		SetAnchorsPreset(Control.LayoutPreset.FullRect);

		// Background panel (grey box)
		backgroundPanel = new Panel();
		backgroundPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		backgroundPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		var stylebox = new StyleBoxFlat();
		stylebox.BgColor = new Color(0.85f, 0.85f, 0.85f); // light grey
		stylebox.BorderColor = new Color(0.6f, 0.6f, 0.6f);
		stylebox.BorderWidthTop = 2;
		stylebox.BorderWidthBottom = 2;
		stylebox.BorderWidthLeft = 2;
		stylebox.BorderWidthRight = 2;
		backgroundPanel.AddThemeStyleboxOverride("panel", stylebox);
		AddChild(backgroundPanel);

		// Arrow label
		arrowLabel = new Label();
		arrowLabel.AddThemeFontSizeOverride("font_size", 24);
		arrowLabel.HorizontalAlignment = HorizontalAlignment.Center;
		arrowLabel.VerticalAlignment = VerticalAlignment.Center;
		arrowLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		arrowLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(arrowLabel);

		UpdateDisplay();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		// Show/hide background and arrow depending on whether a LetterTile is present
		bool hasLetterTile = false;
		foreach (Node child in GetChildren())
		{
			if (child is LetterTile)
			{
				hasLetterTile = true;
				break;
			}
		}
		backgroundPanel.Visible = !hasLetterTile;
		arrowLabel.Visible = !hasLetterTile;

		// Update arrow
		arrowLabel.Text = Arrow switch
		{
			ArrowType.Right => "→",
			ArrowType.Down => "↓",
			_ => ""
		};
	}
}

/// <summary>
/// Represents a letter tile in the letter selection area
/// </summary>
public partial class LetterTile : Button
{
	   public int Index { get; set; }
	   public string Letter { get; set; } = "";
	   public bool IsPlaced { get; set; } = false;

	   public override void _Ready()
	   {
		   // Capital letter
		   Text = Letter.ToUpper();
		   AddThemeFontSizeOverride("font_size", 16);

		   // Colors
		   var darkBrown = new Color(0.36f, 0.25f, 0.13f);
		   var lightTan = new Color(0.98f, 0.92f, 0.82f);

		   // Border and background
		   var stylebox = new StyleBoxFlat();
		   stylebox.BgColor = lightTan;
		   stylebox.BorderColor = darkBrown;
		   stylebox.BorderWidthTop = 3;
		   stylebox.BorderWidthBottom = 3;
		   stylebox.BorderWidthLeft = 3;
		   stylebox.BorderWidthRight = 3;
		   // Apply the same stylebox to all button states
		   AddThemeStyleboxOverride("normal", stylebox);
		   AddThemeStyleboxOverride("hover", stylebox);
		   AddThemeStyleboxOverride("pressed", stylebox);
		   AddThemeStyleboxOverride("disabled", stylebox);
		   AddThemeStyleboxOverride("focus", stylebox);

		   // Apply black font color to all states (Godot 4+ uses these keys)
		   AddThemeColorOverride("font_color", Colors.Black);
		   AddThemeColorOverride("font_hover_color", Colors.Black);
		   AddThemeColorOverride("font_pressed_color", Colors.Black);
	   }
}

/// <summary>
/// Transparent placeholder for a letter tile in the flow container
/// </summary>
public partial class LetterPlaceholder : Panel
{
	public int Index { get; set; }

	public override void _Ready()
	{
		SetAnchorsPreset(Control.LayoutPreset.FullRect);
		var stylebox = new StyleBoxFlat();
		stylebox.BgColor = new Color(0, 0, 0, 0); // fully transparent
		AddThemeStyleboxOverride("panel", stylebox);
	}
}

/// <summary>
/// Encapsulates a letter's state and UI elements
/// </summary>
public class Letter
{
	public int? OriginalGridIndex { get; set; } = null; // Track where the letter was placed
	public int Index { get; set; }
	public string Text { get; set; }
	public bool IsPlaced { get; set; } = false;
	public LetterTile Tile { get; set; }
	public LetterPlaceholder Placeholder { get; set; }

	public Letter(string text, int index)
	{
		Index = index;
		Text = text;
		Tile = new LetterTile();
		Tile.Letter = text;
		Tile.Index = index;
		Tile.CustomMinimumSize = new Vector2(50, 50);
		Placeholder = new LetterPlaceholder();
		Placeholder.Index = index;
		Placeholder.CustomMinimumSize = new Vector2(50, 50);
		Placeholder.Visible = false;
	}
}
