using Godot;
using System;

public partial class MainMenu : Node2D
{
// PRIVATE VARIABLES
	private Button _1PlayerButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_1PlayerButton = GetNode<Button>("ButtonManager/1Player");
		_1PlayerButton.GrabFocus();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _1PlayerButtonDown()
	{
		OnStartGame(1);
	}

	private void _2PlayerButtonDown()
	{
		OnStartGame(2);
	}

	private void _3PlayerButtonDown()
	{
		OnStartGame(3);
	}

	private void _4PlayerButtonDown()
	{
		OnStartGame(4);
	}

	public void OnStartGame(int playerCount) 
	{
		GameManager.SetNumPlayers(playerCount);
		GameManager.Instance.LoadDefaultLevel();
	}

	private void _quitButtonDown()
	{
		OnQuit();
	}

	public void OnQuit()
	{
		GetTree().Quit();
	}
}
