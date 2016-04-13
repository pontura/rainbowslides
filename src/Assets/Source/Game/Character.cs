using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class handles the Character behaviour.
public class Character : BoardEntity
{
	public enum State
	{
		idle,
		idleAndReady,
		waitAnimationEnd,
		moving,
		pushing,
		waitingToBePushed,
		beingPushed,
		onLadder,
		onChute,
		onSpringJump,
		playingGasp,
		fallingFromBubble,
		onTrapDoor,
		waitingForCameraFocus,
		playingHello,
		playingVictory,
		playingLose,
		waitAsideUltilOwnerIsReady
	}
	
	public enum MoveMode
	{
		walk,
		jump,
		chute,
		ladder,
		spring,
	}

	// The transform root where the model is located. Should be a first child.
	public Transform modelRoot;
	
	// A transform used to determine the center of the model. This is used by the camera to properly focus the character.
	public Transform center;

	// The behaviour that handles the animation events, and also contains animation related properties.
	private CharacterEvents charEvents;
	
	// The component that contains the animation names.
	private CharacterAnimNames animNames;
	
	// The cached floor shadow reference.
	private FloorShadow floorShadow;
	
	// The amount of cell moves left to move.
	private int queuedMoves;
	
	// The desired move mode that is going to be used once it starts moving thowards a target.
	private MoveMode movingMode;
	private MoveMode queuedMoveMode;
	
	// The position of the character when SetMoveTarget was invoked the last time.
	private Vector3 moveStartPosition;
	// The time when SetMoveTarget was invoked the last time.
	private float moveStartTime;
	
	// The current state of the character.
	public State _state = State.idle;
	
	// The time when the state was last changed.
	private float stateChangeTime;

	// The player that owns this character.
	public Player player;
	private Animation anim;
	
	// Skateboard bonus variables.
	public bool usedSkateboard;
	
	// Booble bonus
	public GameObject usingBubble;
	public float bubbleTravelHeight = 2.5f;
	
	// Rain special event
	public GameObject cloudRain;
	public GameObject skate;
	
	// The amount of time this character has suffered the trap door bad event.
	public int trapDoorCount;
	
	// temp variables
	private float tempTimer;
	private int tempStep;
	private bool clickedRequest;
	
	// Does this character requires the camera to zoom on it?
	private bool requiresCameraFocus;

	// Its a victory animation queued to be played when it becomes idle?
	private bool queuedVictoryPlay;

	// Its a raining animation queued to be played when it becomes idle?
	private bool queuedRainPlay;

	public State state
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state != value)
			{
				_state = value;
				stateChangeTime = Time.time;
			}
		}
	}
	
	// Time elapsed since the State was changed.
	public float StateTime
	{
		get
		{
			return Time.time - stateChangeTime;
		}
	}
	
	// Is the character ready? (a character is ready when its waiting and has nothing else to do).
	// Ingame should wait until all characters are ready to change state.
	public bool Ready
	{
		get
		{
			return state == State.idleAndReady && queuedMoves == 0 && TargetPlace == null;
		}
	}
	
	// Is the character idle? idle means its not moving.
	// This does not requires to be ready to switch ingame states.
	// A character can be idle, waiting another finish moving so it can start a push animation,
	// but it doesn't mean its ready to switch Ingame states (eg: change player turn).
	public bool IsIdle
	{
		get
		{
			return TargetPlace == null && (state == State.idleAndReady || state == State.idle);
		}
	}
	
	// Does this character require the camera to focus on it?
	// For exmaple, if its pushing another character, it will require a camera focus (its doing an important action),
	// but if its returning to the center of the cell after being pushed, it will not require a focus.
	public bool RequiresCameraFocus
	{
		get
		{
			return requiresCameraFocus;
		}
	}
	
	// Is this the character of the current moving player?
	public bool IsMyTurn
	{
		get
		{
			Player currentPlayerTurn = Ingame.instance.PlayerTurn;
			return currentPlayerTurn != null && currentPlayerTurn.character == this;
		}
	}
	
	// The amount of cells that the character must still advance (or go back if its negative).
	public int QueuedMoves
	{
		get
		{
			return queuedMoves;
		}
	}
	
	// The position where the camera should center when focusing this character.
	public Vector3 FocusPosition
	{
		get
		{
			Vector3 p = _t.position;
			// the camera should always look as it where on the ground (avoids moving up/down when popping the bubble)
			p.y = 1f;
			return p;
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		DestroyBubble();
		DestroyRain();
		DestroySkate();
	}

	public void Restore()
	{
		DestroyBubble();
		DestroyRain();
		DestroySkate();
		trapDoorCount = 0;
		queuedMoves = 0;
		queuedMoveMode = MoveMode.walk;
		movingMode = MoveMode.walk;
		tempTimer = 0;
		tempStep = 0;
		clickedRequest = false;
		state = State.idle;
		stateChangeTime = Time.time;
		CancelQueuedPlays();
	}

	void CancelQueuedPlays()
	{
		queuedVictoryPlay = false;
		queuedRainPlay = false;
	}

	void DestroyBubble()
	{
		if (usingBubble)
		{
			GameObject.Destroy(usingBubble);
			usingBubble = null;
		}
	}
	
	void DestroyRain()
	{
		if (cloudRain)
		{
			GameObject.Destroy(cloudRain);
			cloudRain = null;
		}
	}
	
	public void DestroySkate()
	{
		if (skate)
		{
			GameObject.Destroy(skate);
			skate = null;
		}
		usedSkateboard = true;
	}
	
	public bool QueueMove(bool singleJump)
	{
		if (singleJump)
		{
			queuedMoveMode = Character.MoveMode.jump;
			queuedMoves++;
			return true;
		}
		else if (queuedMoves == 0)
		{
			queuedMoveMode = Character.MoveMode.walk;
			queuedMoves++;
			return true;
		}
		return false;
	}
	
	public void SetMoveTarget(Transform targetPlace, MoveMode mode)
	{
		if (!gameObject.activeInHierarchy)
			return;
		TargetPlace = targetPlace;
		moveStartPosition = _t.position;
		moveStartTime = Time.time;
		
		// make sure the special jump animations stops playing
		anim.Stop(animNames.SINGLE_JUMP_NAME);
		anim.Stop(animNames.SPRINGBOARD_NAME);
		
		// safety check...
		if (!anim || skate || usingBubble || !targetPlace)
		{
			movingMode = MoveMode.walk;
		}
		// jump mode
		else if (mode == MoveMode.jump)
		{
			Play(animNames.SINGLE_JUMP_NAME);
			
			// custom single jump speed
			float singleJumpSpeed = animNames.singleJumpTimeScale;
			if (queuedMoves > 1)
				singleJumpSpeed *= animNames.singleJumpTimeScale2;
			anim[animNames.SINGLE_JUMP_NAME].speed = singleJumpSpeed;
			
			movingMode = MoveMode.jump;
		}
		// spring mode
		else if (mode == MoveMode.spring)
		{
			Play(animNames.SPRINGBOARD_START_NAME);
			movingMode = MoveMode.spring;
		}		
		// other modes
		else
		{
			movingMode = mode;
		}
		
		state = State.moving;
	}
	
	protected override bool MoveTo(Vector3 position, bool look)
	{
		if (skate)
			position += Vector3.up * charEvents.skateHeight;
		
		// single jump mode: lerp between the start jump position and the target position according with the animation time.
		if (IsPlaying(animNames.SINGLE_JUMP_NAME))
			return JumpMove(position, look, anim[animNames.SINGLE_JUMP_NAME], charEvents.jumpStartTime, charEvents.jumpEndTime);
		if (IsPlaying(animNames.SPRINGBOARD_NAME))
			return JumpMove(position, look, anim[animNames.SPRINGBOARD_NAME], charEvents.springStartTime, charEvents.springEndTime);
		
		// normal mode: use the speed to translate thowards the target
		bool result = base.MoveTo(position, look);
		if (!result && skate)
			Sound.RequestPlaySkateboard();
		return result;
	}
	
	private bool JumpMove(Vector3 position, bool look, AnimationState state, float startTime, float endTime)
	{
		if (look)
			LookLerp(position - moveStartPosition);
		float timeK = (state.time - startTime) / (endTime - startTime);
		_t.position = Vector3.Lerp(moveStartPosition, position, Mathf.Clamp01(timeK));
		return state.time >= state.clip.length;
	}
		
	protected bool WalkTo(Vector3 position)
	{
		return WalkTo(position, true);
	}
	
	protected bool WalkTo(Vector3 position, bool look)
	{
		if (!MoveTo(position, look))
		{
			state = State.moving;
			return false;
		}
		return true;
	}
	
	public void Update()
	{
		// do not update the animation if the board camera is disabled (you are at game setup)
		if (!Ingame.instance.boardCamera.Camera.enabled)
		{
			anim.enabled = false;
			return;
		}
		anim.enabled = true;
		
		// by default, the camera should focus it... unless its absolutely idle or doing something not important.
		requiresCameraFocus = state != State.idleAndReady;
		
		// idle or general moving state...
		if (state == State.idle ||
			state == State.idleAndReady ||
			state == State.waitingToBePushed ||
			state == State.moving ||
			state == State.waitingForCameraFocus)
		{
			// set the correct animation
			if (usingBubble)
			{
				CrossFade(animNames.ON_BUBBLE_NAME);
				//if (TargetPlace == null)
				LookToCamera(true);
			}
			else if (state == State.moving)
			{
				// jump move mode
				if (movingMode == MoveMode.jump)
				{
					// safety check: if its still moving and the jump animation ended, play the walk anim
					if (!IsPlaying(animNames.SINGLE_JUMP_NAME))
						CrossFade(animNames.WALK_NAME);
				}
				// spring move mode
				else if (movingMode == MoveMode.spring)
				{
					// safety check: if its still moving and the spring animation ended, play the walk anim
					if (!IsPlaying(animNames.SPRINGBOARD_NAME))
						CrossFade(animNames.WALK_NAME);
				}
				// default move mode: walk
				else
				{
					CrossFade(skate ? animNames.SKATEBOARD_NAME : animNames.WALK_NAME);
				}
			}
			else if (cloudRain)
			{
				LookToCamera(true);
				CrossFade(animNames.ON_RAIN_LOOP_NAME);
			}
			else
			{
				// set the character facing direction
				if (TargetPlace == null && queuedMoves == 0)
				{
					// look to the next cell while waiting input, or on skate.
					if (skate || (Ingame.instance.state == Ingame.State.waitingPlayerMoveInput && Ingame.instance.PlayerTurn && Ingame.instance.PlayerTurn.character == this))
					{
						LookToNext(true);
					}
					// otherwise, look to the camera (after a moment to avoid the glitch when switching from pushed to pushing)
					else if ((state == State.idle || state == State.idleAndReady) && StateTime > 0.1f)
					{
						LookToCameraAndNext();
					}
				}
				
				// if i have a skate, play the cool skate pose
				if (skate)
				{
					CrossFade(animNames.SKATEBOARD_NAME, true);
				}
				// i'm idle... play idle...
				else
				{
					CrossFade(animNames.IDLE_NAME, true);
				}
			}
			
			// if it has a target, start moving to that place
			if (TargetPlace != null)
			{
				Vector3 targetPosition = TargetPlace.position;
				// add some height if its moving on a bubble
				if (usingBubble)
					targetPosition += Vector3.up * bubbleTravelHeight;
				if (WalkTo(targetPosition, !usingBubble))
				{
					// i have arrived. Now, the target place is my current place.
					CurrentPlace = TargetPlace;
					TargetPlace = null;
					state = State.idle;
				}
			}
			// there is no target place to move to...
			else
			{
				// its idle, so un-queue the next move
				if (queuedMoves != 0)
				{
					Cell cell = CurrentCell;
					if (queuedMoves > 0)
					{
						if (cell == null)
							SetMoveTarget(Cell.firstCell.transform, queuedMoveMode);
						else if (cell.next)
						{
							// notify the other entities that i'm passing by...
							foreach (BoardEntity e in cell.entities)
							{
								if (e != this)
								{
									Character otherCharacter = e as Character;
									if (otherCharacter)
										otherCharacter.OnOtherCharacterPasses();
								}
							}
							// move to cell 100 must be done by jump, and if it has a skate, it must be destroyed
							// is it at cell 99?
							if (cell.next && cell.next.next == null)
							{
								if (skate)
								{
									StaticAssets.Spawn(StaticAssets.instance.dustSkateDisappearPrefab, skate.transform.position, 1f);
									DestroySkate();
								}
								SetMoveTarget(cell.next.transform, MoveMode.jump);
							}
							else
							{
								SetMoveTarget(cell.next.transform, queuedMoveMode);
							}
						}
						queuedMoves--;
					}
					else if (queuedMoves < 0)
					{
						if (cell && cell.previous) // moving back is done by walking (barking dog)
							SetMoveTarget(cell.previous.transform, MoveMode.walk);
						else
							SetMoveTarget(player.startPlace.transform, MoveMode.jump);
						queuedMoves++;
						if (!CurrentCell)
							queuedMoves = 0;
					}
				}
				// there are no queued moves...
				// if its inside a bubble, break it
				else if (usingBubble)
				{
					// here the bubble may instantiate some particles
					usingBubble.GetComponent<Bubble>().RequestDestroy();
					usingBubble = null;
					state = State.fallingFromBubble;
				}
				// there are no queued moves...
				else
				{
					// if i'm stading on a cell...
					Cell cell = CurrentCell;
					if (cell)
					{
						// if the player is still moving the character...
						if (Ingame.instance.state == Ingame.State.waitingPlayerMoveInput)
						{
							// if there is someone else at my cell, move away to let him pass
							if (cell.entities.Count > 1 && !IsMyTurn)
							{
								if (WalkTo(cell.SidePosition))
								{
									// i have arrived. Stay idle
									state = State.idle;
								}
								
								// its not important to focus on me.
								requiresCameraFocus = false;
							}
							// there is noone else on my cell... make sure im at the center of it.
							else
							{
								if (WalkTo(cell._t.position))
								{
									// i have arrived. Stay idle
									state = State.idleAndReady;
								}

								// its not important to focus on me.
								requiresCameraFocus = false;
							}
						}
						// its not waiting for user input...
						// connections must be resolved...
						// and conflicts between characters must be resolved by pushes!
						else
						{
							// if its sanding on a connected cell, resolve the connection
							if (cell.connection)
							{
								// make sure i'm at the center of the cell
								if (WalkTo(cell._t.position))
								{
									// wait until im focused by the camera... (ingame manages the camera focus)
									// or its fast forwarding the time (skipping resolves all connections simultaneously)
									if (Ingame.instance.AllowConnectionResolveMove(this))
									{
										// in case its comming from another ladder, or spring jump, bubble, or any bonus...
										// cancel any previous queued bonus animations
										CancelQueuedPlays();

										// then, resolve the connection
										Transform dest = TargetPlace = cell.connection.transform;
										Cell.ConnectionType ct = cell.GetConnectionType();
										if (ct == Cell.ConnectionType.ladder)
										{
											if (charEvents.autoPlayLadderNotes)
												Sound.Play(Sound.instance.fxLadderFirst);
											SetMoveTarget(dest, MoveMode.ladder);
											state = State.onLadder; // override with ladder special state
											queuedVictoryPlay = true;
											tempStep = 0; // used to split the movement in 2 phases
											charEvents.ResetLadderVariables();
										}
										else if (ct == Cell.ConnectionType.chute)
										{
											Sound.Play(Sound.instance.fxChuteFall);
											SetMoveTarget(dest, MoveMode.chute);
											queuedRainPlay = true;
											state = State.onChute; // override with chute special state
										}
										else
										{
											SetMoveTarget(dest, MoveMode.walk);
											state = State.moving;
										}
									}
									// stay idle waiting for the camera to focus this character
									else
									{
										state = State.waitingForCameraFocus;
									}
								}
							}
							// there is someone else at my cell
							else if (cell.entities.Count > 1)
							{
								CancelQueuedPlays();

								// if i am the last who entered in this cell, i own this place.
								// i will push the others!...
								if (cell.entities.IndexOf(this) == cell.entities.Count - 1)
								{
									// first i'll move to the push position
									if (WalkTo(cell.ToPushPosition))
									{
										// then i'll wait until everybody has arrived...
										bool ready = true;
										foreach (BoardEntity e in cell.entities)
										{
											Character c = e as Character;
											if (c != this && c.state != State.waitingToBePushed)
												ready = false;
										}
									
										// and when they are ready, i'll push them
										if (ready)
										{
											PlayPush();
										}
										else
										{
											CrossFade(animNames.IDLE_NAME);
											LookTo(_t.position - cell._t.position, true);
											state = State.idle;
										}
									}
								}
								// i'm not the owner of this cell, i will be pushed!
								else
								{
									// if the owner character is coming from a ladder (ending on a left direction cell), or chute fall, wait on a side to avoid clipping
									Character ownerCharacter = cell.entities[cell.entities.Count - 1] as Character;
									if (ownerCharacter && (ownerCharacter.state == State.onChute || (ownerCharacter.state == State.onLadder && Vector3.Dot(cell.NextDirection, Vector3.right) < 0.8f)))
									{
										state = State.waitAsideUltilOwnerIsReady;
									}
									// the owner character is not on a "clipping-possible" state... move to the pushed position
									else
									{
										// first i'll move to the correct position
										if (WalkTo(cell.ToBePushedPosition))
										{
											// waiting to be pushed
											state = State.waitingToBePushed;
											LookToCameraAndNext();
										}
									}
								}
							}
							// i'm alone at the cell
							else
							{
								// make sure i'm at the center of the cell
								if (WalkTo(cell._t.position))
								{
									// if the game has ended...
									if (Ingame.instance.state == Ingame.State.endScene)
									{
										// if i'am at the last cell, play the victory animation
										if (cell.next == null)
										{
											state = State.playingVictory;
										}
										// otherwise, play the rain animation
										else
										{
											state = State.playingLose;
										}
									}
									// the game has not ended yet...
									else
									{
										if (queuedVictoryPlay)
										{
											CrossFade(animNames.VICTORY_NAME);
											state = State.waitAnimationEnd;
											CancelQueuedPlays();
										}
										else if (queuedRainPlay)
										{
											CrossFade(animNames.ON_RAIN_START_NAME);
											state = State.waitAnimationEnd;
											CancelQueuedPlays();
										}
										else
										{
											// stay idle
											state = State.idleAndReady;
										}
									}
								}
								
								// its not important to focus on me.
								requiresCameraFocus = false;
							}
						}
					}
					// there is no cell at my place... where should i be?
					else if (player && player.startPlace)
					{
						if (WalkTo(player.startPlace.transform.position))
						{
							// when i arrive, stay idle
							state = State.idleAndReady;
							// consume queued animations on the start position
							CancelQueuedPlays();
						}
					}
				}
			}
		}
		else if (state == State.waitAsideUltilOwnerIsReady)
		{
			Cell cell = CurrentCell;
			if (cell && cell.entities.Count > 1)
			{
				Character ownerCharacter = cell.entities[cell.entities.Count - 1] as Character;
				if (ownerCharacter == null || ownerCharacter == this)
				{
					// this should never happen
					state = State.idle;
				}
				else
				{
					// ok, the owner has arrived at the end position and its waiting for me to return.
					if (ownerCharacter.IsIdle && ownerCharacter.StateTime > 0.1f) // && ownerCharacter.IsPlaying(animNames.IDLE_NAME))
					{
						state = State.idle;
					}
					// the owner is still traveling... wait at a side.
					else
					{
						// wait on a side until it finished the chute fall or ladder climb
						if (WalkTo(cell.SidePosition))
						{
							CrossFade(animNames.IDLE_NAME);
							LookToCameraAndNext();
						}
						else
						{
							CrossFade(animNames.WALK_NAME);
						}
					}
				}
			}
			else
			{
				state = State.idle;
			}
		}
		else if (state == State.pushing)
		{
			Cell cell = CurrentCell;
			
			// wait until the animation ends
			if (IsPlaying(animNames.PUSH_NAME) && cell)
				LookTo(_t.position - cell._t.position, true);
			else
				state = State.idle;
			
			// at the precise time, do the actual pushing logic
			if (tempTimer < charEvents.pushTime)
			{
				tempTimer += Time.deltaTime;
				if (tempTimer >= charEvents.pushTime)
				{
					if (cell)
					{
						List<BoardEntity> list = new List<BoardEntity>(cell.entities);
						foreach (BoardEntity e in list)
						{
							Character c = e as Character;
							if (c != this)
							{
								c.PlayPushed();
								// Steal the skateboard.
								if (c.skate)
								{
									// if i already have a skate, destroy the other
									if (skate)
									{
										c.DestroySkate();
									}
									else
									{
										AttachSkate(c.skate);
										c.skate = null;
									}
									usedSkateboard = false;
								}
								// Steal the cloud rain.
								if (cloudRain == null && c.cloudRain)
								{
									cloudRain = c.cloudRain;
									c.cloudRain = null;
									Cloud cloud = cloudRain.GetComponent<Cloud>();
									cloud.target = _t;
									cloud.consumed = false;
									CrossFade(animNames.ON_RAIN_START_NAME);
									state = State.waitAnimationEnd;
								}
							}
						}	
					}
				}
			}
		}
		else if (state == State.beingPushed)
		{
			// keep moving until the animation has ended
			Cell cell = CurrentCell;
			Vector3 targetPosition = cell ? cell.ToPushedPosition : player.startPlace.transform.position;
			MoveTo(targetPosition, false);
			if (IsPlaying(animNames.PUSHED_NAME))
			{
				LookToNext(true);
			}
			else
			{
				CrossFade(animNames.GETUP_NAME);
				state = State.waitAnimationEnd;
			}
		}
		else if (state == State.onChute)
		{
			if (TargetPlace != null)
			{
				Vector3 targetPosition = TargetPlace.position;
				float t = Time.time - moveStartTime;
				float tt = StaticAssets.instance.chuteCharacterMoveTimeK * Vector3.Distance(moveStartPosition, targetPosition) / Cell.SIZE;
				float tk = StaticAssets.instance.chuteCharacterTimeCurve.Evaluate(Mathf.Clamp01(t / tt));
				Vector3 relPos = targetPosition - moveStartPosition;
				relPos.x *= tk;
				relPos.z *= StaticAssets.instance.chuteCharacterMoveCurve.Evaluate(tk);
				_t.position = moveStartPosition + relPos;
				if (tk >= 1f)
					state = State.idle;
			}
			else
			{
				state = State.idle;
			}
			CrossFade(animNames.ON_CHUTE_NAME);
			Look(Vector3.right);
		}
		else if (state == State.onLadder)
		{
			if (tempStep == 0)
			{
				Cell currentCell = CurrentCell;
				if (currentCell)
				{
					Vector3 targetPosition = currentCell._t.position + Vector3.left * Cell.SIZE * charEvents.climbLadderCellPosStart;
					if (MoveToInternal(targetPosition, true, 1f))
					{
						tempStep++;
					}
					CrossFade(animNames.WALK_NAME);
				}
				else
				{
					tempStep++;
				}
			}
			else if (tempStep == 1)
			{
				if (TargetPlace != null)
				{
					Vector3 targetPosition = TargetPlace.position;
					Cell targetCell = TargetCell;
					if (targetCell)
						targetPosition += Vector3.right * Cell.SIZE * charEvents.climbLadderCellPosEnd;
					float startDistance = Vector3.Distance(moveStartPosition, _t.position);
				
					// always look left while climbing ladders
					Look(Vector3.left);
				
					// only move when the animation allows it (the move flag must be changed by animation events)
					float animSpeed = charEvents.ladderSpeed;
					if (IsPlaying(animNames.ON_LADDER_NAME) && !charEvents.ladderMove)
						animSpeed = 0f;
				
					if (MoveToInternal(targetPosition, false, animSpeed))
					{
						// If im arriving to a cell whose next direction is left,
						// then move to the center of the cell before ending the "ladder" state.
						// This is to prevent the possible other character clipping with me.
						// It should stay on the "side" position while i'm on "ladder" state.
						// When i end the ladder state, the other character will move to the center of the cell (pushed position)
						// and i will move to the push position.
						if (targetCell && Vector3.Dot(targetCell.NextDirection, Vector3.left) > 0.5f)
						{
							tempStep++;
						}
						// Let the idle state handle the last part of the travel.
						// It will move to the center of the cell if there is noone at that cell, or...
						// It will move to the push position if there is someone already there.
						else
						{
							CurrentPlace = TargetPlace;
							TargetPlace = null;
							state = State.idle;
						}
					}
					// its still climbing...
					else
					{
						// play a note sound 
						if (charEvents.autoPlayLadderNotes)
						{
							float endDistance = Vector3.Distance(moveStartPosition, _t.position);
							float stepDistance = 2.6f;
							int startStep = (int)(startDistance / stepDistance);
							int endStep = (int)(endDistance / stepDistance);
							if (startStep != endStep)
							{
								AudioClip[] notes = Sound.instance.fxLadderNotes;
								if (endStep < notes.Length)
									Sound.Play(notes[endStep % notes.Length]);
							}
						}
					}
				}
				else
				{
					state = State.idle;
				}
				CrossFade(animNames.ON_LADDER_NAME);
			}
			else
			{
				if (TargetPlace == null || MoveToInternal(TargetPlace.position, true, 1f))
				{
					CurrentPlace = TargetPlace;
					TargetPlace = null;
					state = State.idle;
				}
				CrossFade(animNames.WALK_NAME);
			}
		}
		else if (state == State.onSpringJump)
		{
			if (IsPlaying(animNames.SPRINGBOARD_START_NAME))
			{
				// wait until it finishes
				if (TargetPlace)
					LookTo(TargetPlace.position - _t.position, true);
			}
			else
			{
				if (TargetPlace)
				{
					CrossFade(animNames.SPRINGBOARD_LOOP_NAME);
					Vector3 targetPosition = TargetPlace.position;
					Vector3 moveDelta = targetPosition - moveStartPosition;
					float totalMoveDistance = moveDelta.magnitude;
					float distanceK = (_t.position - moveStartPosition).magnitude / totalMoveDistance;
					float height = charEvents.springHeight.Evaluate(distanceK);
					Vector3 pos = _t.position;
					pos.y = targetPosition.y;
					_t.position = pos;
					if (MoveTo(targetPosition, true))
					{
						CrossFade(animNames.SPRINGBOARD_END_NAME);
						CurrentPlace = TargetPlace;
						TargetPlace = null;
					}
					else
					{
						pos = _t.position;
						pos.y += height;
						_t.position = pos;
					}
				}
				else if (!IsPlaying(animNames.SPRINGBOARD_END_NAME))
				{
					state = State.idle;
				}
			}
		}
		else if (state == State.fallingFromBubble)
		{
			CrossFade(animNames.FALLING_NAME);
			LookToCamera(true);
			
			Cell cell = CurrentCell;
			Vector3 targetPosition = cell ? cell._t.position : player.startPlace.transform.position;
			if (MoveTo(targetPosition, false))
				state = State.idle;
		}
		else if (state == State.onTrapDoor)
		{
			float lastTimer = tempTimer;
			tempTimer += Time.deltaTime;

			LookToCamera(true);
			
			Transform ct = CurrentPlace;
			
			// spawn trap door
			if (lastTimer <= 0f && tempTimer > 0f)
			{
				if (ct)
				{
					if (ct != player.startPlace.transform)
					{
						GameObject.Instantiate(StaticAssets.instance.trapDoorPrefab, ct.position + Vector3.up * 0.1f, ct.rotation);
						state = State.onTrapDoor;
						Cell cell = ct.GetComponent<Cell>();
						if (cell)
							cell.HideMeshes();
						if (anim[animNames.TRAPDOOR_FALL_NAME])
							Play(animNames.TRAPDOOR_FALL_NAME);
					}
				}
			}
			
			// play falling sound
			if (lastTimer <= 1.2f && tempTimer > 1.2f)
			{
				Sound.Play(Sound.instance.fxCharacterFalling);
			}
			
			// fall, and appear on start position
			if (tempTimer > 1f)
			{
				if (ct)
				{
					Transform start = player.startPlace.transform;
					if (ct != start)
					{
						CrossFade(animNames.FALLING_NAME);
						Vector3 targetPosition = ct.position + Vector3.down * 8f;
						if (tempTimer > 0.5f && MoveTo(targetPosition, false))
						{
							CurrentPlace = start;
							// instant teleport
							_t.position = start.position + Vector3.left * 8f;
						}
					}
					else
					{
						if (MoveToInternal(start.position, false, charEvents.trapDoorWalkSpeed))
						{
							if (anim[animNames.REMOVE_DUST_NAME])
							{
								Play(animNames.REMOVE_DUST_NAME);
								state = State.waitAnimationEnd;
							}
							else
							{
								state = State.idle;
							}
						}
						else
						{
							Look(start.position - _t.position);
							CrossFade(animNames.TRAPDOOR_WALK_NAME);
						}
					}
				}
				else
				{
					state = State.idle;
				}
			}
		}
		else if (state == State.waitAnimationEnd || state == State.playingGasp)
		{
			if (state == State.playingGasp)
			{
				requiresCameraFocus = false;
				LookToCamera(true);
			}
			if (!IsPlayingLastAnim())
			{
				if (lastPlayedAnim == animNames.ON_RAIN_START_NAME && !cloudRain)
				{
					CrossFade(animNames.ON_RAIN_LOOP_NAME);
				}
				else if (lastPlayedAnim == animNames.ON_RAIN_LOOP_NAME && !cloudRain)
				{
					CrossFade(animNames.ON_RAIN_END_NAME);
				}
				else
				{
					CrossFade(animNames.IDLE_NAME);
					state = State.idle;
					LookToCamera(true);
				}
			}
			else
			{
				if (lastPlayedAnim == animNames.VICTORY_NAME || lastPlayedAnim == animNames.ON_RAIN_START_NAME || lastPlayedAnim == animNames.ON_RAIN_LOOP_NAME)
				{
					LookToCamera(true);
				}
				if (lastPlayedAnim == animNames.ON_RAIN_LOOP_NAME && Time.time - stateChangeTime > 1.8f)
				{
					CrossFade(animNames.ON_RAIN_END_NAME);
				}
			}
		}
		else if (state == State.playingHello)
		{
			LookToCameraAngled(charEvents.angleLookHello, true);
			if (!IsPlaying(animNames.HELLO_NAME))
				state = State.idle;
		}
		else if (state == State.playingVictory)
		{
			//LookToCameraAngled(charEvents.angleLookVictory, true);
			LookTo(Ingame.instance.forward, true);
			CrossFade(animNames.VICTORY_NAME);
		}
		else if (state == State.playingLose)
		{
			LookToCameraAngled(charEvents.angleLookVictory, true);
			CrossFade(animNames.ON_RAIN_LOOP_NAME);
		}

		// Attach the bubble to my position
		if (usingBubble)
		{
			usingBubble.transform.position = _t.position;
		}
		
		// Make sure the skate is at the correct position
		if (skate)
		{
			AttachSkate(skate);
			skate.GetComponent<Skate>().UpdateModel(_t);
		}
		
		// Update the shadow size
		if (floorShadow)
		{
			bool showShadow = !skate && !usingBubble && !IsPlaying(animNames.SPRINGBOARD_LOOP_NAME) && !IsPlaying(animNames.FALLING_NAME) && !IsPlaying(animNames.ON_CHUTE_NAME) && !IsPlaying(animNames.ON_LADDER_NAME);

			// hide the shadow on the last cell
			if (showShadow && CurrentCell && CurrentCell.next == null)
				showShadow = false;

			floorShadow.targetScale = showShadow ? 1f : 0f;
		}
		
		// handle the clicked request (let pass a little time to avoid triggering a hello when reaching the top of the a ladder, or other sequence of movements)
		if (clickedRequest)
		{
			if ((state == State.idleAndReady || state == State.idle) && StateTime > 0.1f)
				PlayHello();
			clickedRequest = false;
		}
	}
		
	void LookToCamera(bool lerp)
	{
		LookToCameraAngled(charEvents.angleLookCenter, lerp);
	}
	
	void LookToCameraAngled(float angle, bool lerp)
	{
		LookTo(Quaternion.Euler(0, angle, 0) * Ingame.instance.forward, lerp);
	}
	
	void LookToCameraAndNext()
	{
		Cell currentCell = CurrentCell;
		Vector3 nextDir = currentCell ? currentCell.NextDirection : Cell.firstCell ? Cell.firstCell._t.position - _t.position : Vector3.forward;
		Vector3 forwardDir = Ingame.instance.forward;
		float angle;
		if (Vector3.Dot(Vector3.right, nextDir) > 0.5f)
			angle = charEvents.angleLookRight;
		else if (Vector3.Dot(Vector3.left, nextDir) > 0.5f)
			angle = charEvents.angleLookLeft;
		else
			angle = charEvents.angleLookCenter;
		LookTo(Quaternion.Euler(0, angle, 0) * forwardDir, true);
	}
	
	void OnOtherCharacterPasses()
	{
		if (!cloudRain)
		{
			CrossFade(animNames.GASP_NAME);
			anim[animNames.GASP_NAME].speed = animNames.gaspTimeScale;
			state = State.playingGasp;
		}
		Sound.Play(Sound.instance.fxCharacterPass);
	}
	
	void PlayPush()
	{
		Play(animNames.PUSH_NAME);
		state = State.pushing;
		tempTimer = 0f;
	}
	
	void PlayPushed()
	{
		Cell cell = CurrentCell;
		if (cell && cell.previous)
		{
			CurrentPlace = cell.previous.transform;
		}
		// there are no more places to go... move it to the start place
		else
		{
			StartPlace sp = player.startPlace;
			if (sp)
				CurrentPlace = sp.transform;
		}
		state = State.beingPushed;
		Play(animNames.PUSHED_NAME);
		LookToPrevious(true);
	}
	
	public void PlayHello()
	{
		// ignore hello while skating
		if (skate)
			return;
		// ignore hello if its already playing it
		if (state == State.playingHello)
			return;
		// make sure its not fading out the hello anim
		if (anim.IsPlaying(animNames.HELLO_NAME))
			return;
		Play(animNames.HELLO_NAME);
		state = State.playingHello;
		Sound.Play(Sound.instance.fxCharacterHello);
		Ingame.instance.ResetHelloTime(); // in case hello was triggered by click
	}
	
	public void SetModel(GameObject model)
	{
		if (!modelRoot)
		{
			Debug.LogError("Cannot set model: modelroot is not set.", this);
			return;
		}
		Transform modelT = model.transform;
		modelT.position = _t.position;
		modelT.rotation = _t.rotation;
		modelT.parent = modelRoot;
		anim = modelRoot.GetComponentInChildren<Animation>();
		charEvents = modelRoot.GetComponentInChildren<CharacterEvents>();
		animNames = modelRoot.GetComponentInChildren<CharacterAnimNames>();
		floorShadow = modelRoot.GetComponentInChildren<FloorShadow>();
	}
	
	public void InstantMove()
	{
		if (TargetPlace)
		{
			CurrentPlace = TargetPlace;
			_t.position = TargetPlace.position;
		}
	}
	
	public bool ConsumeCloundRain()
	{
		if (!cloudRain)
			return false;
		Cloud cloud = cloudRain.GetComponent<Cloud>();
		bool consumed = !cloud.consumed;
		cloud.consumed = true;
		return consumed;
	}

	public void OnTurnStart()
	{
		if (cloudRain)
		{
			Cloud cloud = cloudRain.GetComponent<Cloud>();
			cloud.RequestDestroy();
			cloudRain = null;
			CrossFade(animNames.ON_RAIN_END_NAME);
			state = State.waitAnimationEnd;
		}
	}
	
	public void OnSpinnerResult(Spinner.Result r)
	{
		if (r == Spinner.Result.goodSpring)
		{
			Cell cell = CurrentCell;
			if (cell)
			{
				Cell iso = cell.FindNextIsometricCell();
				if (iso)
				{
					SetMoveTarget(iso.transform, MoveMode.spring);
					state = State.onSpringJump;
					cell.StartSpringEffect(charEvents.springCellAnimDelay);
					queuedVictoryPlay = true;
				}
			}
		}
		else if (r == Spinner.Result.goodBubbles)
		{
			queuedMoves = 10;
			usingBubble = (GameObject)GameObject.Instantiate(StaticAssets.instance.bubblePrefab, _t.position, Quaternion.identity);
			queuedVictoryPlay = true;
		}
		else if (r == Spinner.Result.goodSkateboard)
		{
			AttachSkate((GameObject)GameObject.Instantiate(StaticAssets.instance.skatePrefab, _t.position, Quaternion.identity));
			StaticAssets.Spawn(StaticAssets.instance.dustSkateAppearPrefab, _t.position, 1f);
			usedSkateboard = false;
			queuedVictoryPlay = true;
		}
		else if (r == Spinner.Result.badRain)
		{
			cloudRain = (GameObject)GameObject.Instantiate(StaticAssets.instance.cloudRainPrefab, _t.position, Quaternion.identity);
			Cloud c = cloudRain.GetComponent<Cloud>();
			c.consumed = true;
			c.target = _t;
			c.InstantMoveToTarget();
			CrossFade(animNames.ON_RAIN_START_NAME);
			state = State.waitAnimationEnd;
		}
		else if (r == Spinner.Result.badBarkingDog)
		{
			int backMoves = 5;
			Cell cell = CurrentCell;
			
			// create the dog
			Cell dogStart = cell;
			if (dogStart)
			{
				if (dogStart.next)
					dogStart = dogStart.next;
				if (dogStart)
				{
					GameObject dog = (GameObject)GameObject.Instantiate(StaticAssets.instance.dogPrefab, dogStart.transform.position, dogStart.transform.rotation);
					BarkingDog bd = dog.GetComponent<BarkingDog>();
					bd.CurrentPlace = dogStart.transform;
					bd.TargetPlace = dogStart.transform;
					bd.movesLeft = backMoves;
					StaticAssets.Spawn(StaticAssets.instance.dustDogAppearPrefab, dog.transform.position, 1f);
				}
			}
			
			// here the affected characters may play a "scared" animation,
			// and then wait for the animation to finish (on Update) before starting to unqueue the moves
			
			int count = 0;
			while (cell && backMoves > 0 && count < 6)
			{
				if (cell.entities.Count != 0)
				{
					foreach (BoardEntity e in cell.entities)
					{
						Character c = e as Character;
						if (c)
						{
							if (c.state == State.playingHello || c.IsPlaying(animNames.HELLO_NAME))
							{
								c.anim.Play(animNames.IDLE_NAME, PlayMode.StopAll);
								c.state = State.idle;
							}
							c.queuedMoves = -backMoves;
						}
					}
				}
				else
				{
					backMoves--;
				}
				cell = cell.previous;
				count++;
			}
			queuedRainPlay = true;
		}
		else if (r == Spinner.Result.badTrapDoor)
		{
			tempTimer = 0f;
			state = State.onTrapDoor;
			trapDoorCount++;
		}
	}
	
	void AttachSkate(GameObject go)
	{
		skate = go;
	}
		
	public void OnClicked()
	{
		clickedRequest = true;
	}

	#region ANIMATION
	string lastPlayedAnim = "";
	
	bool IsPlayingLastAnim()
	{
		return IsPlaying(lastPlayedAnim);
	}
	
	bool IsPlaying(string animName)
	{
		if (lastPlayedAnim != animName)
			return false;
		if (!anim.IsPlaying(animName))
			return false;
		AnimationState animState = anim[animName];
		if (!animState)
			return false;
		if (animState.wrapMode == WrapMode.ClampForever && animState.time > animState.clip.length)
			return false;
		return true;
	}
	
	void Play(string animName)
	{
		anim.Play(animName, PlayMode.StopAll);
		lastPlayedAnim = animName;
	}
	
	void CrossFade(string animName)
	{
		CrossFade(animName, false);
	}
	
	void CrossFade(string animName, bool longLength)
	{
		anim.CrossFade(animName, longLength ? charEvents.fadeTimeLong : charEvents.fadeTimeShort);
		lastPlayedAnim = animName;
	}
	#endregion
	
}
