using UnityEngine;
using System.Collections;

public class AngleRangeAnimation : MonoBehaviour
{

    public float interval;
    public float animationSpeed = 1f;
    public Transform parent;
    public Transform dot;
    public Transform weapon;
    public PlayerAiming aim;
    public Transform player;

    public Sprite particleTexture1;
    public Sprite particleTexture2;
    public Sprite particleTexture3;
    public Sprite particleTexture4;
    public Sprite particleTexture5;

	private bool started = false;
	private bool startedUp = false;

	private Transform turret;

    private int number;
    private double rotation;
    private Weapon gun;
    private float maxRange;
    private float minRange;
	private float maxAngle;
    private bool active;
    private float time;
    private float outerRimEnterTime = 0;
    private float leftSideEnterTime;
    private float innerRimEnterTime;
    private float rightSideEnterTime;
    private float maxTime;
    private ArrayList particles;
    private float innerAnimationSpeed;
    private float outerAnimationSpeed;

    private Transform outerRimContainer;
    private Transform innerRimContainer;
    private Transform leftSideContainer;
    private Transform rightSideContainer;

    public void StartUp()
    {
		started = true;
		startedUp = false;

        gun = weapon.GetComponent<Weapon>();
		turret = weapon.parent;

        active = !aim.canTurn360;

		transform.rotation = player.rotation;

		if (active)
        {
            // Create containers

            particles = new ArrayList();

            maxRange = gun.range;

            minRange = gun.minRange;

			maxAngle = turret.GetComponent<Turret>().maxTurn;

			// Calc length of boundaris

            // Calc outer rim

            float outerRim = (float) (maxRange * 2f * Mathf.PI * (maxAngle / 180f));

            // Calc innner rim

            float innerRim = (float) (minRange * 2f * Mathf.PI * (maxAngle / 180f));

			// Calc sides
			
			float side = maxRange - minRange;

			// Calculate extent

			float extent = outerRim + innerRim + (2 * side);

            // Calculate how many dots

            number = (int)Mathf.Round(extent / interval);
            if (number < 1)
                number = 1;

			CreateContainers();
            
            // Clone dot

            for (int i = 0; i < number; i++)
            {
                // Create clone

                Transform nextDot = Instantiate(dot);

                // Apply it to Container

                float position = ((float)i / number) * extent;
                
                if (position < outerRim)
                {
					// Create in outer rim

					nextDot.parent = outerRimContainer;

					float posInContainer = position;

					float angleInContainer = (posInContainer / outerRim) * (float)(maxAngle * 2) - maxAngle - 90;

					nextDot.localPosition = new Vector3(GetX(angleInContainer, maxRange), GetY(angleInContainer, maxRange), 0);

                    nextDot.localRotation = Quaternion.Euler(new Vector3(0, 0, angleInContainer - 90));

                    nextDot.GetComponent<NumberContainer>().number = 0;
                } else if (position < (outerRim + side))
                {
					// Create on left side

					nextDot.parent = leftSideContainer;

					float posInContainer = position - outerRim;

                    nextDot.localPosition = new Vector3(0, -(posInContainer / side) * (maxRange - minRange) + maxRange, 0);
					
					nextDot.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));

					nextDot.GetComponent<SpriteRenderer>().sprite = particleTexture5;

                    nextDot.GetComponent<NumberContainer>().number = 1;
                } else if (position < (outerRim + side + innerRim))
                {
					// Create in inner rim

					nextDot.parent = innerRimContainer;

					float posInContainer = position - (outerRim + side);

					float angleInContainer = -(posInContainer / innerRim) * (float)(maxAngle * 2) + maxAngle - 90;

					nextDot.localPosition = new Vector3(GetX(angleInContainer, minRange), GetY(angleInContainer, minRange), 0);

                    nextDot.localRotation = Quaternion.Euler(new Vector3(0, 0, angleInContainer - 90));

                    nextDot.GetComponent<NumberContainer>().number = 2;
                } else
                {
					// Create on right side

					nextDot.parent = rightSideContainer;

					float posInContainer = position - (outerRim + side + innerRim);

                    nextDot.localPosition = new Vector3(0, (posInContainer / side) * (maxRange - minRange) + minRange, 0);

					nextDot.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));

					nextDot.GetComponent<SpriteRenderer>().sprite = particleTexture5;

                    nextDot.GetComponent<NumberContainer>().number = 3;
                }

                // Add to list

                particles.Add(nextDot);
            }

            // When maxTime is reached the animation is resetted

            maxTime = extent / animationSpeed;

            // Calc enter timers (when particles switch state)

            leftSideEnterTime = outerRim / animationSpeed;
            innerRimEnterTime = leftSideEnterTime + (side / animationSpeed);
            rightSideEnterTime = innerRimEnterTime + (innerRim + animationSpeed);

            time = 0;

			// Calculate rotation speeds

			innerAnimationSpeed = animationSpeed / gun.minRange / Mathf.PI;
            outerAnimationSpeed = animationSpeed / gun.range / Mathf.PI;

            // Remove particle

            Destroy(dot.gameObject);
        }
        else
        {
			// Remove particle

            Destroy(dot.gameObject);
			
            // Remove script

            Destroy(gameObject.GetComponent<AngleRangeAnimation>());
		}

		startedUp = true;
	}

	private void CreateContainers()
	{
		transform.rotation = turret.rotation;

		outerRimContainer = new GameObject().transform;
		innerRimContainer = new GameObject().transform;
		leftSideContainer = new GameObject().transform;
		rightSideContainer = new GameObject().transform;

		outerRimContainer.parent = transform;
		innerRimContainer.parent = transform;
		leftSideContainer.parent = transform;
		rightSideContainer.parent = transform;

		outerRimContainer.localPosition = new Vector3(0, 0, 0);
		innerRimContainer.localPosition = new Vector3(0, 0, 0);
		leftSideContainer.localPosition = new Vector3(0, 0, 0);
		rightSideContainer.localPosition = new Vector3(0, 0, 0);

		outerRimContainer.name = "outerRim";
		innerRimContainer.name = "innerRim";
		leftSideContainer.name = "leftSide";
		rightSideContainer.name = "rightSide";

		// Set rotations

		leftSideContainer.localRotation = Quaternion.Euler(0, 0, aim.maxTurn);
		rightSideContainer.localRotation = Quaternion.Euler(0, 0, -aim.maxTurn);
	}

	public void SetWeapon(Transform newWeapon)
	{
		weapon = newWeapon;
	}

	void FixedUpdate() {
		if (GameMaster.IsPaused()) return;

		if (!started)
			return;
		if (!startedUp)
			return;

		// Animate containers

		outerRimContainer.localRotation = Quaternion.Euler(0, 0, outerRimContainer.localEulerAngles.z + outerAnimationSpeed);

        innerRimContainer.localRotation = Quaternion.Euler(0, 0, innerRimContainer.localEulerAngles.z - innerAnimationSpeed);

        leftSideContainer.position -= leftSideContainer.up * (animationSpeed / 180);

		rightSideContainer.position += rightSideContainer.up * (animationSpeed / 180);

		transform.rotation = player.rotation;

		// Set each particles position

		foreach (Object obj in particles)
        {
            Transform particle = obj as Transform;

            NumberContainer numb = particle.GetComponent<NumberContainer>();

            if (numb.number == 0)
            {
				// Particle is in outer rim

				// Calc rotation

				Vector2 partPos = particle.position;

				Vector2 tankCenter = turret.position; // It's not the same as Call Center

				Vector2 difference = partPos - tankCenter;

				float ang = Vector2.Angle(difference, player.up);
				Vector3 cross = Vector3.Cross(difference, player.up);

				if (cross.z > 0)
					ang = 360 - ang;

				if (ang > 180)
					ang -= 360;

				ang *= -1;

				// Test if moving into rim

				if (ang > (maxAngle - 5) && particle.GetComponent<SpriteRenderer>().sprite != particleTexture1)
				{
					if (ang < (maxAngle - 6 / maxRange))
					{
						particle.GetComponent<SpriteRenderer>().sprite = particleTexture1;
						particle.localScale = new Vector3(1, 1, 1);
					}
				}

				// Test if moved to left side

				if (ang < -maxAngle)
				{
					numb.number = 1;

					particle.parent = leftSideContainer;

					particle.localRotation = Quaternion.Euler(0, 0, 0);

					Vector3 diff = leftSideContainer.position - turret.position;
					particle.localPosition = new Vector3(0, maxRange + diff.magnitude, 0);

					particle.GetComponent<SpriteRenderer>().sprite = particleTexture4;
				}
				else if (ang < -maxAngle + 3 / maxRange)
				{
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture3;
				}
				else if (ang < -maxAngle + 6 / maxRange)
				{
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture2;
				}
			}
			else if (numb.number == 1)
			{
				// Particle is on left side

				float height = (particle.position - turret.position).magnitude;

				// Test if moving into side

				if (height > maxRange-1)
				{
					if (height < maxRange-0.1)
					{
						particle.GetComponent<SpriteRenderer>().sprite = particleTexture5;
					}
				}

				// Test if moved to inner rim

				if (height < minRange)
				{
					numb.number = 2;

					particle.parent = innerRimContainer;

					float angleInContainer = -innerRimContainer.localRotation.eulerAngles.z + maxAngle + 90;

					particle.localPosition = new Vector3(GetX(angleInContainer, minRange), GetY(angleInContainer, minRange), 0);

					particle.localRotation = Quaternion.Euler(new Vector3(0, 0, angleInContainer - 90));

					particle.GetComponent<SpriteRenderer>().sprite = particleTexture2;
				}
				else if (height < (minRange + 0.075))
				{
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture3;
				}
				else if (height < (minRange + 0.15))
				{
					particle.localScale = new Vector3(1, -1, 1);
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture4;
				}
			}
			else if (numb.number == 2)
			{
				// Particle is in inner rim

				// Calc rotation

				Vector2 partPos = particle.position;

				Vector2 tankCenter = turret.position; // It's not the same as Call Center

				Vector2 difference = partPos - tankCenter;

				float ang = Vector2.Angle(difference, player.up);
				Vector3 cross = Vector3.Cross(difference, player.up);

				if (cross.z > 0)
					ang = 360 - ang;

				if (ang > 180)
					ang -= 360;

				ang *= -1;

				// Test if moving into rim

				if (ang < (-maxAngle + 5) && particle.GetComponent<SpriteRenderer>().sprite != particleTexture1)
				{
					if (ang > (-maxAngle + (6 / minRange)))
					{
						particle.GetComponent<SpriteRenderer>().sprite = particleTexture1;

						particle.localScale = new Vector3(1, 1, 1);
					}
				}

				// Test if moved to right side
				
				if (ang > maxAngle)
				{
					numb.number = 3;

					particle.parent = rightSideContainer;

					particle.localRotation = Quaternion.Euler(0, 0, 180);

					Vector3 diff = rightSideContainer.position - turret.position;
					particle.localPosition = new Vector3(0, minRange - diff.magnitude, 0);

					particle.GetComponent<SpriteRenderer>().sprite = particleTexture4;
				}
				else if (ang > maxAngle - 4 / minRange)
				{
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture3;
				}
				else if (ang > maxAngle - 8 / minRange && particle.GetComponent<SpriteRenderer>().sprite != particleTexture2)
				{
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture2;

					particle.localRotation = Quaternion.Euler(new Vector3(0, 0, particle.localRotation.eulerAngles.z + 180));
				}
			}
			else
			{
				// Particle is on right side

				float height = (particle.position - turret.position).magnitude;

				// Test if moving into side

				if (height < minRange + 1)
				{
					if (height > minRange + 0.1 && particle.GetComponent<SpriteRenderer>().sprite != particleTexture5)
					{
						particle.GetComponent<SpriteRenderer>().sprite = particleTexture5;

						particle.localScale = new Vector3(1, 1, 1);
						particle.localRotation = new Quaternion();
					}
				}

				// Test if moved to outer rim

				if (height > maxRange)
				{
					numb.number = 0;

					particle.parent = outerRimContainer;

					float angleInContainer = -outerRimContainer.localRotation.eulerAngles.z - maxAngle + 90;

					particle.localPosition = new Vector3(GetX(angleInContainer, maxRange), GetY(angleInContainer, maxRange), 0);

					particle.localRotation = Quaternion.Euler(new Vector3(0, 0, angleInContainer - 90));

					particle.localScale = new Vector3(-1, 1, 1);

					particle.GetComponent<SpriteRenderer>().sprite = particleTexture2;
				}
				else if (height > (maxRange - 0.075))
				{
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture3;
				}
				else if (height > (maxRange - 0.15))
				{
					particle.localScale = new Vector3(-1, 1, 1);
					particle.GetComponent<SpriteRenderer>().sprite = particleTexture4;
				}
			}
		}
    }

    // Get a point (the X-coordinate) at at circle
    private float GetX(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Cos(angle));
    }

    // Get a point (the Y-coordinate) at at circle
    private float GetY(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Sin(angle));
    }
}
