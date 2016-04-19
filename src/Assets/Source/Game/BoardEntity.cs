using UnityEngine;
using System.Collections;

// The base class of any board entity.
public class BoardEntity : MonoBehaviour
{
	// Where its currently located.
	protected Transform _currentPlace;
	
	// Where it should be heading.
	protected Transform _targetPlace;
	public float moveSpeed = 5f;
	
	// Cached reference
	protected Transform _t;
	
	protected void Awake()
	{
		_t = transform;
	}
	
	public Transform CurrentPlace
	{
		get
		{
			return _currentPlace;
		}
		set
		{
			Cell cell = CurrentCell;
			if (cell && _targetPlace != _currentPlace)
				cell.entities.Remove(this);
			_currentPlace = value;
			cell = CurrentCell;
			if (cell && !cell.entities.Contains(this))
				cell.entities.Add(this);
		}
	}
	
	public Transform TargetPlace
	{
		get
		{
			return _targetPlace;
		}
		set
		{
			if (_targetPlace != value)
			{
				Cell cell = TargetCell;
				if (cell && _targetPlace != _currentPlace)
					cell.entities.Remove(this);
				_targetPlace = value;
				cell = TargetCell;
				if (cell && !cell.entities.Contains(this))
					cell.entities.Add(this);
			}
		}
	}
	
	public Cell CurrentCell
	{
		get
		{
			return _currentPlace != null ? _currentPlace.GetComponent<Cell>() : null;
		}
	}
	
	public Cell TargetCell
	{
		get
		{
			return _targetPlace != null ? _targetPlace.GetComponent<Cell>() : null;
		}
	}
	
	public virtual void OnDisable()
	{
		CurrentPlace = null;
		TargetPlace = null;
	}

	protected void LookTo(Vector3 direction, bool lerp)
	{
		if (lerp)
			LookLerp(direction);
		else
			Look(direction);
	}
	
	protected void LookToPrevious(bool lerp)
	{
		Cell cell = CurrentCell;
		if (cell)
		{
			if (lerp)
				LookLerp(cell.PreviousDirection);
			else
				Look(cell.PreviousDirection);
		}
	}

	protected void LookToNext(bool lerp)
	{
		Vector3 direction;
		Cell cell = CurrentCell;
		if (cell)
			direction = cell.NextDirection;
		else
			direction = Cell.firstCell._t.position - _t.position;
		if (lerp)
			LookLerp(direction);
		else
			Look(direction);
	}
	
	protected void LookLerp(Vector3 direction)
	{
		if (direction.sqrMagnitude > 0.1f)
		{
			_t.rotation = Quaternion.Lerp(_t.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
			ClampRotation();
		}
	}

	protected void Look(Vector3 direction)
	{
		if (direction.sqrMagnitude > 0.1f)
		{
			_t.rotation = Quaternion.LookRotation(direction);
			ClampRotation();
		}
	}
	
	protected virtual bool MoveTo(Vector3 position, bool look)
	{
		return MoveToInternal(position, look, 1f);
	}
	
	protected void ClampRotation()
	{
		// make sure the head its pointing up
		_t.rotation = Quaternion.FromToRotation(_t.up, Vector3.up) * _t.rotation;
	}
	
	protected bool MoveToInternal(Vector3 position, bool look, float speedK)
	{
		float dt = Time.deltaTime;
		float delta = moveSpeed * speedK * dt;
		Vector3 relPos = position - _t.position;
		if (relPos.magnitude <= delta)
		{
			_t.position = position;
			return true;
		}
		else
		{
			Vector3 direction = relPos.normalized;
			_t.position += direction * delta;
			direction.y = 0;
			if (look && direction.sqrMagnitude > 0.1f)
				LookLerp(direction.normalized);
			return false;
		}
	}

}
