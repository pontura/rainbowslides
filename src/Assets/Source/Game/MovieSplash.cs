using UnityEngine;
using System.Collections;

public class MovieSplash : MonoBehaviour
{
	// the name of the scene that will be loaded
	public string levelToLoad;

	// the path of the movie that will be played at start. Used on Android and iPhone.
	public string moviePath;

	// parameters used to play the video on Android and iPhone.
	public Color bgColor = Color.white;
	public FullScreenMovieControlMode control;
	public FullScreenMovieScalingMode scaling;

#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR

	void Start()
	{
		StartCoroutine(StartRoutine());
	}
	
	IEnumerator StartRoutine()
	{
		Handheld.PlayFullScreenMovie(moviePath, bgColor, control, scaling);
		yield return new WaitForEndOfFrame();
		if (!string.IsNullOrEmpty(levelToLoad))
			Application.LoadLevel(levelToLoad);
	}
	
#else
	// the movie texture played at start. Used on stand alone.
	public MovieTexture movieTexture;

	// the scale mode used to render the movie texture.
	public ScaleMode scaleMode;

	// time to end after the movie texture ends playing.
	public float pauseAtEnd = 0;
	
	// the audio source used to play the movie sound.
	public AudioSource audioSource;
	
	void Start()
	{
		if (!movieTexture)
			Application.LoadLevel(levelToLoad);
		else
			StartCoroutine(StartRoutine());
	}
	
	IEnumerator StartRoutine()
	{
		yield return new WaitForEndOfFrame();
		movieTexture.Play();

		// wait until it starts playing
		while (!movieTexture.isPlaying)
			yield return new WaitForEndOfFrame();
		
		// start playing the movie sound
		if (audioSource)
		{
			audioSource.clip = movieTexture.audioClip;
			audioSource.Play();
		}
		
		// wait until the movie ends playing or the user touches the screen
		float endTime = Time.realtimeSinceStartup + movieTexture.duration + pauseAtEnd;
		while (Time.realtimeSinceStartup < endTime)
		{
			if (Input.GetMouseButton(0))
				break;
			yield return new WaitForEndOfFrame();
		}
		//loading = true;
		yield return new WaitForEndOfFrame();
		if (!string.IsNullOrEmpty(levelToLoad))
			Application.LoadLevel(levelToLoad);
		//loading = false;
		yield return new WaitForEndOfFrame();
	}
	
	void OnGUI()
	{
		GUILayout.Label("Video...");
		Rect r = new Rect(0, 0, Screen.width, Screen.height);
		if (movieTexture)
			GUI.DrawTexture(r, movieTexture, scaleMode);
	}
#endif
	
}
