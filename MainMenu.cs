using Godot;
using System;

public partial class MainMenu : MarginContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		var playButton = GetNode<Button>("ButtonsVBoxContainer/PlayButton");
		playButton.Pressed += OnPlayPressed;
    }

	private void OnPlayPressed()
	{
		GetTree().ChangeSceneToFile("res://WorldSelect.tscn");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
