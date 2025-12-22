using Godot;
using System;

public partial class LevelManager : Node
{
	[Export] private PackedScene[] PlayerScenes;
	[Export] private Node3D[] SpawnPoints;
	private int SceneIndex = 0;
	private Node3D PlayerScene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SceneIndex = GameManager.GetNumPlayers() - 1;
		PlayerScene = PlayerScenes[SceneIndex].Instantiate<Node3D>();
		PlayerScene.Name = "PlayerScene";
		GetTree().Root.AddChild(PlayerScene);

		SetupPlayers();
	}

	void SetupPlayers()
	{
		PlayerScreenManager screenManager = (PlayerScreenManager)PlayerScene;
		for (int i = 0; i < screenManager.Players.Length; i++)
		{
			screenManager.Players[i].GlobalPosition = SpawnPoints[i].GlobalPosition;
		}
	}
}
