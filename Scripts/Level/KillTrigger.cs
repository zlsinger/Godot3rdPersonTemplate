using Godot;
using System;

public partial class KillTrigger : Area3D
{
	private void _on_body_entered(Node3D body)
	{
		PlayerBaseController enteringPlayer = (PlayerBaseController)body;
		if (enteringPlayer != null)
		{
			GameManager.CurrentLevelManager.ResetPlayer(enteringPlayer);
		}
	}
}
