using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using YARG.Serialization;
using YARG.UI;

namespace YARG {
	public class Game : MonoBehaviour {
		public const float SONG_START_OFFSET = 1f;
		public const float HIT_MARGIN = 0.1f;
		public const bool ANCHORING = true;

		public static readonly DirectoryInfo SONG_FOLDER = new(@"B:\Clone Hero Alpha\Songs");
		public static readonly FileInfo CACHE_FILE = new(Path.Combine(SONG_FOLDER.ToString(), "yarg_cache.json"));

		public static SongInfo song = null;

		[SerializeField]
		private GameObject soundAudioPrefab;
		[SerializeField]
		private GameObject trackPrefab;

		public static Game Instance {
			get;
			private set;
		} = null;

		public bool SongStarted {
			get;
			private set;
		} = false;

		private List<AudioSource> audioSources;

		private float realSongTime = 0f;
		public float SongTime {
			get => realSongTime + PlayerManager.globalCalibration;
		}

		public Chart chart;

		private void Awake() {
			Instance = this;

			// Song

			StartCoroutine(StartSong());
		}

		private IEnumerator StartSong() {
			// Load audio
			audioSources = new();
			foreach (var file in song.folder.GetFiles("*.ogg")) {
				if (file.Name == "preview.ogg") {
					continue;
				}

				// Load file
				using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(file.FullName, AudioType.OGGVORBIS);
				((DownloadHandlerAudioClip) uwr.downloadHandler).streamAudio = true;
				yield return uwr.SendWebRequest();
				var clip = DownloadHandlerAudioClip.GetContent(uwr);

				// Create audio source
				var songAudio = Instantiate(soundAudioPrefab, transform);
				var audioSource = songAudio.GetComponent<AudioSource>();
				audioSource.clip = clip;
				audioSources.Add(audioSource);
			}

			// Load midi
			var parser = new MidiParser(Path.Combine(song.folder.FullName, "notes.mid"), song.delay);
			chart = new Chart();
			parser.Parse(chart);

			// Spawn tracks
			for (int i = 0; i < PlayerManager.players.Count; i++) {
				var track = Instantiate(trackPrefab,
					new Vector3(i * 25f, 0f, 0f), trackPrefab.transform.rotation);
				track.GetComponent<Track>().player = PlayerManager.players[i];
			}

			yield return new WaitForSeconds(SONG_START_OFFSET);

			// Start all audio at the same time
			foreach (var audioSource in audioSources) {
				audioSource.Play();
			}
			realSongTime = audioSources[0].time;
			SongStarted = true;
		}

		private void Update() {
			if (!SongStarted) {
				return;
			}

			realSongTime += Time.deltaTime;

			if (Keyboard.current.upArrowKey.wasPressedThisFrame) {
				PlayerManager.globalCalibration += 0.01f;
			}

			if (Keyboard.current.downArrowKey.wasPressedThisFrame) {
				PlayerManager.globalCalibration -= 0.01f;
			}

			// End song
			if (realSongTime > song.songLength.Value + 0.5f) {
				Exit();
			}
		}

		public void Exit() {
			SceneManager.LoadScene(0);
		}
	}
}