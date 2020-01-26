using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script to periodically show tips on the strategy panel
 */
public class Tips : MonoBehaviour
{
	public Image iconImg;
	public Image panelImg;

	public Sprite tipSpriteIcon;
	public Sprite missionSpriteIcon;
	public Sprite tutorialSpriteIcon;

	public Sprite tipSpritePanel;
	public Sprite missionSpritePanel;
	public Sprite tutorialSpritePanel;

	public GameObject panel;
	public Text text;

	public string[] missions; // all possible missions
	public string[] tutorials; // all possible tutorials
	public string[] tips; // all possible tips
	public float showTime; // how many seconds each tip is shown
	public float waitTime; // how many seconds between tips
	public float fadeTime;

	private float orgFadeTime;
	private float orgShowTime;

	private CanvasGroup panelCG;

	public int section; // 0 - mission, 1 - tutorial, 2 - tips
	private int mode; // 0 - waiting, 1 - fading in, 2 - showing, 3 - fading out
	private int lastShownTip = -1;

    void Start()
    {
		// Inits

		panelCG = panel.GetComponent<CanvasGroup>();
		panelCG.alpha = 0;

		orgFadeTime = fadeTime;
		orgShowTime = showTime;

		// Wait

		mode = 0;
		lastShownTip = -1;
		section = 0;

		StartWaitCounter();
	}

	private void StartWaitCounter() {
		StartCoroutine(WaitCounter());
	}

	private IEnumerator WaitCounter() {
		if (section == 0) {
			// check if mission available

			if (lastShownTip < missions.Length - 1) {
				// wait

				if (lastShownTip != -1)
					yield return new WaitForSeconds(fadeTime);

				lastShownTip++;

				// show mission

				text.text = missions[lastShownTip];

				// change UI

				iconImg.sprite = missionSpriteIcon;
				panelImg.sprite = missionSpritePanel;
				fadeTime = orgFadeTime / 2f;
				showTime = orgShowTime * 0.6f;

				mode = 1;
			} else {
				// All missions shown

				lastShownTip = -1;
				section = 1;
			}
		}
		
		if (section == 1) {
			// check if tutorial available

			if (lastShownTip < tutorials.Length - 1) {
				// wait

				if (lastShownTip != -1)
					yield return new WaitForSeconds(fadeTime);

				lastShownTip++;

				// show mission

				text.text = tutorials[lastShownTip];

				// change UI

				iconImg.sprite = tutorialSpriteIcon;
				panelImg.sprite = tutorialSpritePanel;

				fadeTime = orgFadeTime / 2f;
				showTime = orgShowTime / 2f;

				mode = 1;
			} else {
				// All tutorials shown

				lastShownTip = -1;
				section = 2;
			}
		}
		
		if (section == 2) {
			// change UI

			iconImg.sprite = tipSpriteIcon;
			panelImg.sprite = tipSpritePanel;

			fadeTime = orgFadeTime;
			showTime = orgShowTime;

			// show tip

			yield return new WaitForSeconds(waitTime);

			mode = 1;

			// Find new tip

			int rand = Random.Range(0, tips.Length - 1);
			if (rand >= lastShownTip)
				rand++;

			text.text = "<b>" + tips[rand] + "</b>";
			lastShownTip = rand;
		}
	}

	private void StartShowCounter() {
		StartCoroutine(ShowCounter());
	}

	private IEnumerator ShowCounter() {
		yield return new WaitForSeconds(showTime);

		mode = 3;
	}

	void Update()
    {
		if (mode != 1 && mode != 3)
			return;

		if (mode == 1) {
			float alpha = panelCG.alpha;
			alpha += Time.deltaTime / fadeTime;

			if (alpha > 1) {
				mode = 2;
				alpha = 1;
				StartShowCounter();
			}

			panelCG.alpha = alpha;
		} else {
			float alpha = panelCG.alpha;
			alpha -= Time.deltaTime / fadeTime;

			if (alpha < 0) {
				mode = 0;
				alpha = 0;
				StartWaitCounter();
			}

			panelCG.alpha = alpha;
		}
    }
}
