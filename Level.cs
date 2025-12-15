using Godot;
using System;
using System.Collections.Generic;

public partial class Level : Node
{
	private string letters;
	private int width;
	private int height;
	private string solution;
	private string name;

	/// <summary>
	/// Constructor for Level
	/// </summary>
	/// <param name="letters">String containing a list of letters for the puzzle</param>
	/// <param name="width">Width of the level grid</param>
	/// <param name="height">Height of the level grid</param>
	/// <param name="solution">Optional: known solution string of length width * height</param>
	/// <param name="name">Optional: name of the level</param>
	public Level(string letters, int width, int height, string solution = "", string name = "")
	{
		this.letters = letters;
		this.width = width;
		this.height = height;
		this.solution = solution;
		this.name = name;

		ValidateLevel();
	}

	/// <summary>
	/// Validate that the level configuration is valid
	/// </summary>
	private void ValidateLevel()
	{
		int expectedGridSize = width * height;

		if (!string.IsNullOrEmpty(solution) && solution.Length != expectedGridSize)
		{
			GD.PrintErr($"Level validation failed: solution length ({solution.Length}) does not match grid size ({expectedGridSize})");
		}
	}

	/// <summary>
	/// Get the letters string
	/// </summary>
	public string GetLetters()
	{
		return letters;
	}

	/// <summary>
	/// Get the width of the level
	/// </summary>
	public int GetWidth()
	{
		return width;
	}

	/// <summary>
	/// Get the height of the level
	/// </summary>
	public int GetHeight()
	{
		return height;
	}

	/// <summary>
	/// Get the total grid size (width * height)
	/// </summary>
	public int GetGridSize()
	{
		return width * height;
	}

	/// <summary>
	/// Get the solution string
	/// </summary>
	public string GetSolution()
	{
		return solution;
	}

	/// <summary>
	/// Check if this level has a known solution
	/// </summary>
	public bool HasSolution()
	{
		return !string.IsNullOrEmpty(solution);
	}

	/// <summary>
	/// Get the name of the level
	/// </summary>
	public string GetLevelName()
	{
		return name;
	}

	/// <summary>
	/// Set the name of the level
	/// </summary>
	public void SetLevelName(string newName)
	{
		name = newName;
	}

	/// <summary>
	/// Set a solution for this level
	/// </summary>
	public void SetSolution(string newSolution)
	{
		if (newSolution.Length != GetGridSize())
		{
			GD.PrintErr($"Invalid solution length: expected {GetGridSize()}, got {newSolution.Length}");
			return;
		}
		solution = newSolution;
	}

	/// <summary>
	/// Get a letter at a specific grid position
	/// </summary>
	public char GetLetterAt(int x, int y)
	{
		if (x < 0 || x >= width || y < 0 || y >= height)
		{
			GD.PrintErr($"Position ({x}, {y}) is out of bounds for grid {width}x{height}");
			return '\0';
		}

		int index = y * width + x;
		return letters[index];
	}

	/// <summary>
	/// Get a solution character at a specific grid position
	/// </summary>
	public char GetSolutionLetterAt(int x, int y)
	{
		if (!HasSolution())
		{
			GD.PrintErr("Level does not have a solution");
			return '\0';
		}

		if (x < 0 || x >= width || y < 0 || y >= height)
		{
			GD.PrintErr($"Position ({x}, {y}) is out of bounds for grid {width}x{height}");
			return '\0';
		}

		int index = y * width + x;
		return solution[index];
	}

	/// <summary>
	/// Get all letters in a row
	/// </summary>
	public string GetRow(int y)
	{
		if (y < 0 || y >= height)
		{
			GD.PrintErr($"Row {y} is out of bounds for grid height {height}");
			return "";
		}

		int startIndex = y * width;
		return letters.Substring(startIndex, width);
	}

	/// <summary>
	/// Get all letters in a column
	/// </summary>
	public string GetColumn(int x)
	{
		if (x < 0 || x >= width)
		{
			GD.PrintErr($"Column {x} is out of bounds for grid width {width}");
			return "";
		}

		string column = "";
		for (int y = 0; y < height; y++)
		{
			column += GetLetterAt(x, y);
		}
		return column;
	}

	/// <summary>
	/// Serialize the level to a string format: "letters(alphabetical)WIDTHxHEIGHT"
	/// Example: "abcdefg5x5"
	/// </summary>
	public string Serialize()
	{
		// Sort letters alphabetically
		char[] letterArray = letters.ToCharArray();
		System.Array.Sort(letterArray);
		string sortedLetters = new string(letterArray);

		return $"{sortedLetters}{width}x{height}";
	}
}
