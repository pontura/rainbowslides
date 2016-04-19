using UnityEngine;
using System.Collections;

// This class contains several values involved in different character evets/actions.
public class CharacterEvents : MonoBehaviour
{
	// The animation time when the character takes off the ground on the single jump animation.
	public float jumpStartTime;

	// The animation time when the character lands on the single jump animation.
	public float jumpEndTime;
	// The time on the push animation when the character actually pushes the other.
	public float pushTime;
	
	// A delay to start the cell's spring jump animation. This should match the moment when the character lifts from the ground.
	public float springCellAnimDelay;
	// The time at which the character lifts from the ground on the spring animation.
	public float springStartTime;
	// The time at which the character lands on the spring animation.
	public float springEndTime;
	// The flying height used on the spring jump.
	public AnimationCurve springHeight;
	
	// A cell distance used to locate the character when climbing a ladder.
	public float climbLadderCellPosStart = 0.4f;
	// A cell distance used to locate the character when climbing a ladder.
	public float climbLadderCellPosEnd = 0.6f;
	
	// The fade time (short) to combine animations.
	public float fadeTimeShort = 0.01f;
	// The fade time (long) to combine animations.
	public float fadeTimeLong = 0.1f;
	
	// The distance used to lift the character when it has the skate.
	public float skateHeight = 0.4f;
	
	// The walk speed scale value when its walking after it has fallen in a trap door.
	public float trapDoorWalkSpeed = 0.5f;

	// Different angles to rotate the character. Use these to make it properly look at the desired position.
	public float angleLookLeft = 10f;
	public float angleLookRight = -10f;
	public float angleLookCenter = 0f;
	public float angleLookHello = 0f;
	public float angleLookHelloAvatar = 0f;
	public float angleLookVictory = 0f;
	
	// This value is changed by animation events to make the character stop when raising the foot when climbing a ladder.
	public bool ladderMove;
	
	// This speed is used while climbing the ladder.
	public float ladderSpeed = 1f;
	
	// If enabled, the ladder notes will be played according to the trabveled distance.
	// Otherwise you must add PlayLadderSound events on the animation.
	public bool autoPlayLadderNotes = true;
	
	// Should the note loop when it reach the highest?
	public bool loopNotes = true;
	
	// Used to play the ladder sounds manually.
	[HideInInspector]
	public int nextLadderNote = 0;
	
	public void ResetLadderVariables()
	{
		ladderMove = true;
		nextLadderNote = 0;
	}
	
	public void PlayLadderNote()
	{
		AudioClip[] notes = Sound.instance.fxLadderNotes;
		if (notes.Length == 0)
			return;
		if (loopNotes || nextLadderNote < notes.Length)
			Sound.Play(notes[nextLadderNote % notes.Length]);
		nextLadderNote++;
	}
	
}
