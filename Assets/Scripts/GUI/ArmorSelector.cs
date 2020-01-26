using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorSelector : MonoBehaviour
{
	public GameObject textures;

	public GameObject player;

	public GameObject oldLeftTrack;
	public GameObject oldRightTrack;

	public GameObject leftTrackPrefab;
	public GameObject rightTrackPrefab;

	public void SelectSpecialArmor() {
		oldLeftTrack.SetActive(false);
		oldRightTrack.SetActive(false);

		GameObject leftTrack = Instantiate(leftTrackPrefab);
		GameObject rightTrack = Instantiate(rightTrackPrefab);

		leftTrack.transform.parent = textures.transform;
		rightTrack.transform.parent = textures.transform;

		leftTrack.transform.localPosition = leftTrackPrefab.transform.position;
		rightTrack.transform.localPosition = rightTrackPrefab.transform.position;

		try {
			player.GetComponent<Vehicle>().leftTracks = new Transform[] { leftTrack.transform.GetChild(0), leftTrack.transform.GetChild(1) };
			player.GetComponent<Vehicle>().rightTracks = new Transform[] { rightTrack.transform.GetChild(0), rightTrack.transform.GetChild(1) };

			player.GetComponent<Vehicle>().SetAnims();
		} catch (Exception e) {
			Debug.LogError(e.Message);
		}
	}
}
