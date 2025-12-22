using Godot;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance;
	public Node CurrentScene;

	private static int NumPlayers = 1;

	public static void SetNumPlayers(int newNumPlayers)
	{
		NumPlayers = newNumPlayers;
	}

	public static int GetNumPlayers()
	{
		return NumPlayers;
	}

    public override void _Ready()
    {
        Instance = this;
    }

	public void LoadDefaultLevel()
	{
		GetTree().ChangeSceneToFile("res://Levels/city_base_scene.tscn");
	}
}
