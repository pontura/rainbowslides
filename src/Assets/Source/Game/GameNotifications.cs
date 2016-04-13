using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameNotifications : MonoBehaviour
{
	public TextAsset notificationsFile;
	public int intervalInSeconds = 10;
	public bool randomize = true;

	public bool playSound = false;
	public string soundName = "default";

	/*
	Because custom alert sounds are played by the iOS system-sound facility, they must be in one of the following audio data formats:
		
	Linear PCM
	MA4 (IMA/ADPCM)
	µLaw
	aLaw
	
	*/

	public void Start()
	{
#if UNITY_IPHONE
		NotificationServices.ClearLocalNotifications();
		NotificationServices.CancelAllLocalNotifications();

		if (notificationsFile == null)
			return;
		string text = notificationsFile.text;
		string[] lines = text.Split('\n');
		List<string> notificationTexts = new List<string>();
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			if (!string.IsNullOrEmpty(line) && line.Length > 10)
				notificationTexts.Add(line.Trim());
		}
		List<string> usedTexts = new List<string>();
		if (randomize)
		{
			while (notificationTexts.Count != 0)
			{
				int i = Random.Range(0, notificationTexts.Count);
				usedTexts.Add(notificationTexts[i]);
				notificationTexts.RemoveAt(i);
			}
		}
		else
		{
			usedTexts.AddRange(notificationTexts);
		}
		
		int seconds = 0;
		foreach (string nt in usedTexts)
		{
			seconds += intervalInSeconds;
			LocalNotification ln = new LocalNotification();
			ln.fireDate = System.DateTime.Now.AddSeconds(seconds);
			ln.alertBody = nt;
			//ln.repeatInterval = CalendarUnit.Year;
			if (playSound)
				ln.soundName = string.IsNullOrEmpty(soundName) ? "default" : soundName;
			NotificationServices.ScheduleLocalNotification(ln);
		}
#endif
	}

}
