using UnityEngine;
using System.Collections;

// This class contains the animation clip names that will be used for different actions.
public class CharacterAnimNames : MonoBehaviour
{
	// animation names
	public string IDLE_NAME = "idle";
	public string WALK_NAME = "walk";
	public string GASP_NAME = "gasp";
	public float gaspTimeScale = 1f;
	public string SINGLE_JUMP_NAME = "singleJump";
	public float singleJumpTimeScale = 1f;
	// the single jump animation scale is multiplied by this value then there are many queued moves on the character
	public float singleJumpTimeScale2 = 1.5f;
	public string PUSH_NAME = "push";
	public string PUSHED_NAME = "pushed";
	public string GETUP_NAME = "";
	
	// for a single springboard animation (contains start, loop and end, like a single jump)
	public string SPRINGBOARD_NAME = "singleJump";
	
	// springboard splitted in start, loop and end
	public string SPRINGBOARD_START_NAME = "";
	public string SPRINGBOARD_LOOP_NAME = "";
	public string SPRINGBOARD_END_NAME = "";
	
	public string SKATEBOARD_NAME = "skate";
	public string ON_LADDER_NAME = "ladder";
	public string ON_CHUTE_NAME = "chute";
	public string ON_BUBBLE_NAME = "bubble";
	
	// rain animation splitted in start, loop and end
	public string ON_RAIN_START_NAME = "";
	public string ON_RAIN_LOOP_NAME = "";
	public string ON_RAIN_END_NAME = "";
	
	public string FALLING_NAME = "falling";
	public string REMOVE_DUST_NAME = "dust";
	public string HELLO_NAME = "hello";
	public string TRAPDOOR_FALL_NAME = "trapdoor_fall";
	public string TRAPDOOR_WALK_NAME = "trapdoor_walk";
	public string VICTORY_NAME = "victory";

}
