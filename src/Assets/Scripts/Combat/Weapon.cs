using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour {

    public float shotsPerSecond;
    public float damage; // Only effective when ballistic
    public float accuracyMistakeInDegrees;
    public bool hasMagazine = true;
    public int magazineSize = 1;
    public int reloadBullets = 1;
    public bool shootWhileReload = false;
    public float reloadtimeInSec = 2;
    public bool aimDir = false;
    public bool singleBurst = false;
    public float rateWhenHot = 1.0f;
    public float hotnessRaiseOnShoot = 0;
    public int hotnessStart = 0;
    public int hotnessEnd = 0;
    public float range;
    public float minRange = 0; // Only effective, when aimDir is actived. The bullet won't normally hit below this distance.
    public int timeTillShoot = 0;
    public LayerMask toHit;
    public Transform bulletTrialPrefab;
	public float bulletRangeSubtract;
    public Transform muzzleFlashPrefab;
    public Transform shootingEffect;
    public Transform reloadSound;
    public DetectionMode BulletHitDetection = DetectionMode.BallisticThroughRaycast; // Use Raycast for very fast moving bullets
    public Sprite invisibleSprite;
    public float explosionDamage = 0; // Only effective when explosive
    public float explosionRadius = 0; // Only effective when explosive

	[HideInInspector]
    public float hotness = 0;
    [HideInInspector]
    public float speed;
	[HideInInspector]
	public float fireDelay;
    private Transform firingPoint;
	[HideInInspector]
    public double firedLastBulletAt = 0;
    [HideInInspector]
    public double bulletsLeft = 0;
	[HideInInspector]
    public double startedReloadingMagazineAt = 0;

    public enum DetectionMode
    {
        ExplosionWithCollider,
        BallisticThroughRaycast
    }

	// Use this for initialization
	void Start () {
        firingPoint = transform.Find("FirePoint");
        fireDelay = 1f / shotsPerSecond;
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
		if (GameMaster.IsPaused()) return;

        if (timeTillShoot > 0 && speed > 0)
        {
            // Slow down Machine gun
            speed--;
            if (speed < 0)
                speed = 0;
        }
        if (rateWhenHot < 1.0)
        {
            hotness--;
			hotness = hotness > 0 ? hotness : 0;
        };

        // Check for reload (only player can reload before magazine is empty

        if (((transform.parent.parent.parent == GameMaster.player && Input.GetKeyDown("r")) && hasMagazine == true && bulletsLeft > 0 && bulletsLeft < magazineSize && magazineSize > 0) || (hasMagazine && bulletsLeft < 1) || (shootWhileReload == true && bulletsLeft < magazineSize))
        {
            if (startedReloadingMagazineAt == 0)
            {
                // Start reload routine

                if (!shootWhileReload)
                     bulletsLeft = 0;

                startedReloadingMagazineAt = Time.time;
            }
            else
            {
                if ((startedReloadingMagazineAt + reloadtimeInSec) < Time.time)
                {
                    // End reload routine

                    startedReloadingMagazineAt = 0;

                    bulletsLeft += reloadBullets;

                    if (bulletsLeft > magazineSize)
                        bulletsLeft = magazineSize;

                    // Play sound

                    if (reloadSound != null)
                    {
                        Transform sound = Instantiate(reloadSound, firingPoint.position, Quaternion.Euler(0, 0, 0)) as Transform;

                        Destroy(sound.gameObject, 1f);
                    }
                }
            }
        }
	}

    // Use this method to shoot at something, the weapon will only fire, when it's possible
	// return dealt damage
    public int CheckForShot(Vector2 target)
    {
	    int dmgDealt = 0;

        if (timeTillShoot > 0) // Basically just for mini guns
        {
            // Speed up mini gun
            
            speed += 2;

            // Check if fast enough to shoot

            if (speed >= timeTillShoot)
            {
                // Limit speed

                speed = timeTillShoot;

                // Check if there are currently bullets, which can be shot

                if (((hasMagazine == true && bulletsLeft > 0) || hasMagazine == false) && (Time.time - firedLastBulletAt) > fireDelay)
                {
                    // Check if weapon can get hot

                    if (rateWhenHot < 1.0)
                    {
                        // Check if weapon is cool

                        if (hotness < hotnessStart)
                            dmgDealt = Shoot(target);
                        else
                        {
                            // Calculate the extra time the weapon needs to starting shooting again due to hotness
                            float m = (float) (hotness - hotnessStart) / (hotnessEnd - hotnessStart);

                            if (m > 1)
                                m = 1;
                            
                            float extraTimeInSeconds = m * (1 / rateWhenHot) * fireDelay;
                            
                            // Check if that time has already passed

                            if ((Time.time - firedLastBulletAt) > (fireDelay + extraTimeInSeconds))
                            {
                                dmgDealt = Shoot(target);
                            }
                        }
                    }
                    else
                        dmgDealt = Shoot(target);
                }
            }
        } else
        {
            // Check if there are currently bullets, which can be shot
            
            if (((hasMagazine == true && bulletsLeft > 0) || hasMagazine == false) && (Time.time - firedLastBulletAt) > fireDelay)
            {
                // Check if weapon can get hot
                
                if (rateWhenHot < 1.0)
                {
                    // Check if weapon is cool

                    if (hotness < hotnessStart)
                        dmgDealt = Shoot(target);
                    else
                    {
                        // Calculate the extra time the weapon needs to starting shooting again due to hotness
                        
                        float m = (float)hotness / (hotnessEnd - hotnessStart);

                        float extraTimeInSeconds = m * (1 - rateWhenHot) * fireDelay;

                        // Check if that time has already passed

                        if ((Time.time - firedLastBulletAt) > (fireDelay + extraTimeInSeconds))
                        {
                            dmgDealt = Shoot(target);
                        }
                    }
                }
                else
                    dmgDealt = Shoot(target);
            }
        }

        return dmgDealt;
    }
    
    /* Fire bullet
	 * return dealt damage
     */
    private int Shoot(Vector2 target)
    {
        hotness += hotnessRaiseOnShoot;

        // Calculate bullet

        Vector2 start = new Vector2(firingPoint.position.x, firingPoint.position.y); // From where the bullet starts

        Vector2 dir = target - start;

        float angle = Mathf.Atan2(dir.x, dir.y);

        angle /= Mathf.PI;
        angle *= 180;

        // Calculate Inaccuracy
        
        angle += accuracyMistakeInDegrees * (Random.Range(-1f, +1f));

        if (angle < -180)
            angle = 360 + angle;
        else if (angle > 180)
            angle = -360 + angle;
        
        // Calculate distance

        float dis = 0;
        
        if (aimDir == true)
        {
            // Get distance which is aimed at

            dis = dir.magnitude;

            // Calculate accuracy mistake

            dis = dis * accuracyMistakeInDegrees / 360 * Random.Range(-1f, +1f) * 8f + dis;
        } else
        {
            // Get max distance

            dis = range;

            // Calculate accuracy mistake

            dis = dis * accuracyMistakeInDegrees / 360 * Random.Range(-1f, +1f) + dis;
        }
        dir.x = GetX(angle, dis);
        dir.y = GetY(angle, dis);

        // Spawn Bullet
        
        firedLastBulletAt = Time.time;

        angle *= -1;
        if (angle < 0)
            angle += 360;
        int dmgDealt = SpawnBullet(angle, dis);

        // Play effect and destroy it after 10 seconds

        GameObject parent = GameObject.Find("effects") ?? new GameObject("effects");
        Transform effect = Instantiate(shootingEffect, firingPoint.position, Quaternion.Euler(0, 0, 0), parent.transform) as Transform;

        Destroy(effect.gameObject, 10);
        
        if (hasMagazine)
            bulletsLeft--;

        return dmgDealt;
    }

    /*
     * Spawns bullet, return:
     *   dealt damage if it was dealt immediately
     *   0 if nothing was hit
     *  -1 if damage is not set to immediate
     *  -2 in case of unexpected error
     */
    private int SpawnBullet(float angle, float dis)
    {
	    int dmgValue;

        Transform bullet = (Transform) Instantiate(bulletTrialPrefab, firingPoint.position, Quaternion.Euler(0, 0, angle));

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (BulletHitDetection == DetectionMode.ExplosionWithCollider)
        {
			// Shoot the bullet, collision detection and damage or other animations will be made by the bullet itself

			bulletScript.rangeSubtract = bulletRangeSubtract;
			bulletScript.SetRange(dis);
            bulletScript.toHit = toHit;
            bulletScript.parent = transform.parent.parent.parent;
            bulletScript.explosionDamage = explosionDamage;
            bulletScript.explosionRadius = explosionRadius;

            dmgValue = -1;
        } else
        {
            // Bullet will be only there as decoration

            Vector2 start = firingPoint.parent.parent.position;
            Vector2 end = new Vector2(start.x - GetX(angle, dis), start.y + GetY(angle, dis));
            
            Ray2D ray = new Ray2D(start, end - start);
            RaycastHit2D hit = Physics2D.Raycast(start, end - start, dis, toHit);
            Vector2 direction = ray.direction;
            direction.Normalize();

			bulletScript.rangeSubtract = bulletRangeSubtract;
			if (hit.distance == 0)
                bulletScript.SetRange(dis);
            else
                bulletScript.SetRange(hit.distance);

            // Collision

            Collider2D col = hit.collider;

            if (col != null && col.transform != transform.parent.parent.parent.transform && toHit == (toHit | (1 << col.gameObject.layer))) // Check for hit
            {
                if (col.gameObject.GetComponent<Entity>() != null)
                {
                    // Bullet has hit an friend / enemy

                    if (col.gameObject.GetComponent<Armored>() == null)
                    {
						// Entity has no armor

						// Calc damage
						dmgValue = Mathf.RoundToInt(Random.Range(0.8f * damage, 1.2f * damage));

						// Apply
						col.gameObject.GetComponent<Entity>().damage(dmgValue);
						return dmgValue;
                    }
                    else
                    {
                        // Entity has armor
                        
                        // Calculate which side of the BoxCollider2D was hit

                        Vector2 hitPoint = start + (ray.direction * hit.distance);

                        Vector2 colCenter = col.bounds.center; // It's not the same as Call Center

                        Vector2 difference = hitPoint - colCenter;

                        float ang = Vector2.Angle(difference, col.transform.up);
                        Vector3 cross = Vector3.Cross(difference, col.transform.up);

                        if (cross.z > 0)
                            ang = 360 - ang;
                        
                        if (ang > 180)
                            ang -= 360;

                        ang *= -1;

                        Armored armor = col.gameObject.GetComponent<Armored>();

                        int side = -1; // 0: Front, 1: Right, 2: Back, 3: Left

                        float armorAmount;

                        if (ang >= armor.angleFront && ang < armor.angleRight)
                        {
                            // Front

                            side = 0;

                            armorAmount = armor.FrontArmorMultiplier;
                        } else if (ang >= armor.angleRight && ang < armor.angleBack)
                        {
                            // Right

                            side = 1;

                            armorAmount = armor.SideArmorMultiplier;
                        } else if (ang >= armor.angleBack || ang < armor.angleLeft)
                        {
                            // Back

                            side = 2;

                            armorAmount = armor.BackArmorMultiplier;
                        } else
                        {
                            // Left

                            side = 3;

                            armorAmount = armor.SideArmorMultiplier;
						}

                        // Calculate number of sparks

                        int sparkNumber = Mathf.Clamp(Mathf.RoundToInt(.5f * Mathf.Sqrt(armorAmount * damage * Random.Range(1, 3))), 0, 30);

                        // Calculate rotation
                        
                        float rot = col.transform.rotation.eulerAngles.z - side * 90;

						// Add spark particles

						GameObject container = GameObject.Find("effects") ?? new GameObject("effects");
						Transform particleParent = (Transform) Instantiate(armor.HitPrefab, hitPoint, Quaternion.Euler(0, 0, rot), container.transform);

						// spawn delayed

                        ParticleSystem particles = particleParent.Find("Particle System").GetComponent<ParticleSystem>();
						float delay = hit.distance / bullet.GetComponent<Bullet>().moveSpeed * 0.1f;

						IEnumerator SpawnDelayed(float time)
						{
							yield return new WaitForSeconds(time);

							try
							{
								particles.Emit(sparkNumber);
							}
							catch (Exception e)
							{}
						}
						StartCoroutine(SpawnDelayed(delay));

						Destroy(particleParent.gameObject, delay + 2f);

						// Calc damage
						dmgValue = Mathf.RoundToInt(Random.Range(0.8f * damage, 1.2f * damage) / armorAmount);

						// Apply
						col.gameObject.GetComponent<Entity>().damage(dmgValue);
                    }
                }
                else
                {
	                dmgValue = -2; // unexpected error
                }
            }
            else
            {
				// nothing was hit

				dmgValue = 0;
            }

            // Disable Bullet when hitted object is very close

            if (col != null && hit.distance < bullet.localScale.y)
            {
                bullet.GetComponent<SpriteRenderer>().sprite = invisibleSprite;
            }
        }

        // Spawn Muzzle Flash

        if (muzzleFlashPrefab != null)
        {
            Transform muzzleFlash = (Transform) Instantiate(muzzleFlashPrefab, firingPoint.position, firingPoint.rotation, firingPoint);

            float size = Random.Range(0.8f, 1.1f);

            Vector3 oldSize = muzzleFlash.localScale;
            muzzleFlash.localScale = new Vector3(oldSize.x * size, oldSize.y * size, 0);

            Destroy(muzzleFlash.gameObject, 0.02f * muzzleFlash.localScale.x);
        }

        return dmgValue;
    }

    // Get a point (the X-coordinate) at at circle
    private float GetX(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Sin(angle));
    }

    // Get a point (the Y-coordinate) at at circle
    private float GetY(float angle, float distance)
    {
        angle *= Mathf.PI / 180;

        return (distance * Mathf.Cos(angle));
    }
}
