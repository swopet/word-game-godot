using Godot;
using System;
using System.Collections.Generic;

public partial class WordList : Node
{
	private Dictionary<string, int> words = new Dictionary<string, int>();

	public override void _Ready()
	{
		LoadWordList();
	}

	private void LoadWordList()
	{
		try
		{
			// Load the NWL20.txt resource file
			var file = FileAccess.Open("res://NWL20.txt", FileAccess.ModeFlags.Read);
			
			if (file == null)
			{
				GD.PrintErr("Failed to open NWL20.txt");
				return;
			}

			while (!file.EofReached())
			{
				string line = file.GetLine().Trim();
				
				if (string.IsNullOrEmpty(line))
					continue;

				// Parse the line: word,frequency (where frequency is 0 for uncommon, 1 for common)
				string[] parts = line.Split(",");
				
				if (parts.Length == 2)
				{
					string word = parts[0].Trim().ToUpper();
					if (int.TryParse(parts[1].Trim(), out int frequency))
					{
						words[word] = frequency;
					}
				}
			}

			GD.Print($"Loaded {words.Count} words from NWL20.txt");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Error loading word list: {ex.Message}");
		}
	}

	/// <summary>
	/// Check if a word exists in the word list
	/// </summary>
	public bool IsValidWord(string word)
	{
		return words.ContainsKey(word.ToUpper());
	}

	/// <summary>
	/// Get the frequency of a word (0 = uncommon, 1 = common)
	/// </summary>
	public int GetWordFrequency(string word)
	{
		string upperWord = word.ToUpper();
		if (words.TryGetValue(upperWord, out int frequency))
		{
			return frequency;
		}
		return -1; // Not found
	}

	/// <summary>
	/// Check if a word is considered common
	/// </summary>
	public bool IsCommonWord(string word)
	{
		return GetWordFrequency(word) == 1;
	}

	/// <summary>
	/// Check if a word is considered uncommon
	/// </summary>
	public bool IsUncommonWord(string word)
	{
		return GetWordFrequency(word) == 0;
	}

	/// <summary>
	/// Get the total number of words loaded
	/// </summary>
	public int GetWordCount()
	{
		return words.Count;
	}
}
