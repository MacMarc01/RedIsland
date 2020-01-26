using UnityEngine;
using UnityEngine.UI;

public class GearSelection : MonoBehaviour
{
	public IngameMenu ingameMenu;
	public Transform strategyPanel;
	public Text coinView;

	public int startMoney = 10;

	[HideInInspector] public int money;

	public GameObject[] weaponButtons;
	public GameObject[] armorButtons;
	public GameObject[] engineButtons;
	public int[] weaponPrices;
	public int[] armorPrices;
	public int[] enginePrices;
	public float[] turretOffsets;
	[HideInInspector] public int selectedWeapon;
	[HideInInspector] public int selectedArmor;
	[HideInInspector] public int selectedEngine;

	// Sprites to highlight selected buttons
	public Sprite normal, normalSelected, highlighted, highlightedSelected, pressed, pressedSelected, disabled;

	private SpriteState normalState, selectedState;

	// Start is called before the first frame update
	void Start()
    {
		money = startMoney;

		selectedWeapon = 0;
		selectedArmor = 0;
		selectedEngine = 0;

		// Init states

		normalState = new SpriteState();
		normalState.pressedSprite = pressed;
		normalState.highlightedSprite = highlighted;
		normalState.selectedSprite = normal;
		normalState.disabledSprite = disabled;

		selectedState = new SpriteState();
		selectedState.pressedSprite = pressedSelected;
		selectedState.highlightedSprite = highlightedSelected;
		selectedState.selectedSprite = normalSelected;
	}

	public void SelectWeapon(int index) {
		selectedWeapon = index;

		ResetButtons();
		UpdateMoney();
		DisableExpensiveButtons();
		HighlightSelectedButtons();
	}

	public void SelectArmor(int index) {
		selectedArmor = index;

		ResetButtons();
		UpdateMoney();
		DisableExpensiveButtons();
		HighlightSelectedButtons();
	}

	public void SelectEngine(int index) {
		selectedEngine = index;

		ResetButtons();
		UpdateMoney();
		DisableExpensiveButtons();
		HighlightSelectedButtons();
	}

	public void Go() {
		// Select gear

		weaponButtons[selectedWeapon].GetComponent<GearButton>().ChooseAction.Invoke();
		armorButtons[selectedArmor].GetComponent<GearButton>().ChooseAction.Invoke();
		engineButtons[selectedEngine].GetComponent<GearButton>().ChooseAction.Invoke();

		// Resume game

		strategyPanel.gameObject.SetActive(true);
		ingameMenu.ResumeGame();
	}

	private void ResetButtons() {
		// Reset weapon buttons

		foreach (GameObject weaponButton in weaponButtons) {
			weaponButton.GetComponent<Button>().interactable = true;
			weaponButton.GetComponent<CanvasGroup>().alpha = 1;

			weaponButton.GetComponent<Image>().sprite = normal;

			Button button = weaponButton.GetComponent<Button>();
			button.spriteState = normalState;
		}

		// Reset armor buttons

		foreach (GameObject armorButton in armorButtons) {
			armorButton.GetComponent<Button>().interactable = true;
			armorButton.GetComponent<CanvasGroup>().alpha = 1;

			armorButton.GetComponent<Image>().sprite = normal;

			Button button = armorButton.GetComponent<Button>();
			button.spriteState = normalState;
		}

		// Reset engine buttons

		foreach (GameObject engineButton in engineButtons) {
			engineButton.GetComponent<Button>().interactable = true;
			engineButton.GetComponent<CanvasGroup>().alpha = 1;

			engineButton.GetComponent<Image>().sprite = normal;

			Button button = engineButton.GetComponent<Button>();
			button.spriteState = normalState;
		}
	}

	private void DisableExpensiveButtons() {
		// Weapon buttons

		int avaibleWeaponMoney = money + weaponPrices[selectedWeapon];

		for (int i = 0; i < weaponButtons.Length; i++) {
			int price = weaponPrices[i];

			if (price > avaibleWeaponMoney) {
				// disable button

				weaponButtons[i].GetComponent<Button>().interactable = false;
				weaponButtons[i].GetComponent<CanvasGroup>().alpha = 0.5f;
			}
		}

		// armor buttons

		int avaibleArmorMoney = money + armorPrices[selectedArmor];

		for (int i = 0; i < armorButtons.Length; i++) {
			int price = armorPrices[i];

			if (price > avaibleArmorMoney) {
				// disable button

				armorButtons[i].GetComponent<Button>().interactable = false;
				armorButtons[i].GetComponent<CanvasGroup>().alpha = 0.5f;
			}
		}

		// engine buttons

		int avaibleEngineMoney = money + enginePrices[selectedEngine];

		for (int i = 0; i < engineButtons.Length; i++) {
			int price = enginePrices[i];

			if (price > avaibleEngineMoney) {
				// disable button

				engineButtons[i].GetComponent<Button>().interactable = false;
				engineButtons[i].GetComponent<CanvasGroup>().alpha = 0.5f;
			}
		}
	}

	private void HighlightSelectedButtons() {
		// Highlight selected weapon

		GameObject weaponButton = weaponButtons[selectedWeapon];
		weaponButton.GetComponent<Image>().sprite = normalSelected;
		weaponButton.GetComponent<Button>().spriteState = selectedState;

		// Highlight selected armor

		GameObject armorButton = armorButtons[selectedArmor];
		armorButton.GetComponent<Image>().sprite = normalSelected;
		armorButton.GetComponent<Button>().spriteState = selectedState;

		// Highlight selected engine

		GameObject engineButton = engineButtons[selectedEngine];
		engineButton.GetComponent<Image>().sprite = normalSelected;
		engineButton.GetComponent<Button>().spriteState = selectedState;
	}

	private void UpdateMoney() {
		money = startMoney - weaponPrices[selectedWeapon] - armorPrices[selectedArmor] - enginePrices[selectedEngine];

		coinView.text = "" + money;
	}
}
